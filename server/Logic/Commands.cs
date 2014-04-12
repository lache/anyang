using Server.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Server.Logic
{
    [Initializer(Phase = InitializePhase.Game)]
    class Commands
    {
        public const string CommandPrefix = ".";
        public static readonly Commands Instance = new Commands();

        private readonly MultiDictionary<string, Handler> _commandMap = new MultiDictionary<string, Handler>();

        private Commands()
        {
        }

        public IEnumerable<CommandHandlerInfo> HandlerInfos
        {
            get { return from handlers in _commandMap.Values from each in handlers select each.Info; }
        }

        public bool IsAdminCommand(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            return input.StartsWith(CommandPrefix);
        }

        public bool Dispatch(Actor actor, string input, out bool success)
        {
            success = false;
            if (!IsAdminCommand(input))
                return false;

            var args = new CommandArguments(input.Substring(1));    // remove leading . character.
            var command = args.Command.ToLower();
            if (!_commandMap.ContainsKey(command))
                return false;

            success = _commandMap[command].Aggregate(false, (result, handler) => InvokeHandler(actor, args, handler.IsStatic ? null : actor, handler.Info) || result);
            return true;
        }

        public void Add(string command, bool isStatic, MethodInfo method)
        {
            _commandMap.Add(command, new Handler {IsStatic = isStatic, Info = new CommandHandlerInfo(method)});
        }

        private static bool InvokeHandler(Actor admin, CommandArguments args, object handler, CommandHandlerInfo handlerInfo)
        {
            if (handlerInfo.Parameters.Length == 0)
                return (bool) handlerInfo.Method.Invoke(handler, new object[0]);

            var objects = new List<object>();

            // 첫 번재 인자로 admin을 요청할 경우에는 admin을 넣어준다.
            if (handlerInfo.Parameters[0].ParameterType == typeof (Actor))
                objects.Add(admin);

            for (var index = 1; index < handlerInfo.Parameters.Length; ++index)
            {
                var argsIndex = index - 1;
                var parameter = handlerInfo.Parameters[index];

                var isAfterAll = false;
                object value = null;

                // CommandArgument의 속성에 따라 value를 미리 구해본다.
                if (parameter.IsOptional)
                    value = parameter.Default;

                if (parameter.IsAfterAll)
                {
                    // AfterAll은 꼭 마지막에 와야한다.
                    Debug.Assert(index == handlerInfo.Parameters.Length - 1);
                    value = args.GetAll(argsIndex);
                    isAfterAll = true;
                }

                // Attribute로부터 값을 구하지 못했고, 입력된 args도 없다면 인자 부족으로 종료한다.
                if (args.Count <= argsIndex)
                {
                    if (value == null)
                        return false;
                }
                else
                {
                    // AfterAll이 아니면 값을 가져올 수 있다.
                    if (!isAfterAll)
                    {
                        // Actor 형일 경우 int를 받아서 Actor를 가져온다.
                        if (parameter.ParameterType == typeof (Actor))
                        {
                            if (!args.Is<int>(argsIndex))
                                return false;

                            var targetActorId = args.Get<int>(argsIndex);
                            var targetActor = ActorManager.Instance[targetActorId];
                            if (targetActor == null)
                                return false;

                            value = targetActor;
                        }
                        else
                        {
                            // 잘못된 인자가 넘어왔다면 인자 오류로 종료한다.
                            if (!args.IsType(argsIndex, parameter.ParameterType))
                                return false;

                            value = args.GetValue(argsIndex, parameter.ParameterType);
                        }
                    }
                }

                objects.Add(value);
            }

            return (bool) handlerInfo.Method.Invoke(handler, objects.ToArray());
        }

        #region Register

        public static void Initialize()
        {
            Instance.Register(Assembly.GetExecutingAssembly());
        }

        public void Register(Assembly targetAssembly)
        {
            foreach (var eachType in targetAssembly.GetTypes())
            {
                // bind handler static methods
                RegisterMethods(eachType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic), true);

                // bind handler instance methods
                if (typeof(Actor).IsAssignableFrom(eachType))
                {
                    var instanceMethods = eachType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    RegisterMethods(instanceMethods, false);
                }
            }
        }

        private void RegisterMethods(IEnumerable<MethodInfo> methods, bool isStatic)
        {
            foreach (var method in methods)
            {
                var commandHandler = method.GetCustomAttribute<CommandHandlerAttribute>();
                if (commandHandler != null)
                    Add(commandHandler.Command, isStatic, method);
            }
        }

        #endregion

        private class Handler
        {
            public bool IsStatic { get; set; }
            public CommandHandlerInfo Info { get; set; }
        }
    }

    public enum CommandArgumentOption
    {
        None,
        Optional,
        AfterAll,
    }

    [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public class CommandArgumentAttribute : Attribute
    {
        public CommandArgumentAttribute(string description)
            : this(description, CommandArgumentOption.None)
        {
        }

        public CommandArgumentAttribute(string description, object defaultValue)
            : this(description, CommandArgumentOption.Optional, defaultValue)
        {
        }

        public CommandArgumentAttribute(string description, CommandArgumentOption options)
            : this(description, options, null)
        {
        }


        public CommandArgumentAttribute(string description, CommandArgumentOption options, object defaultValue)
        {
            Description = description;
            Options = options;
            Default = defaultValue;
        }

        public string Description { get; set; }
        public CommandArgumentOption Options { get; set; }
        public object Default { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class CommandHandlerAttribute : Attribute
    {
        public CommandHandlerAttribute(string command, string description)
        {
            Command = command;
            Description = description;
        }

        public string Command { get; set; }
        public string Description { get; set; }
    }

    public class CommandHandlerInfo
    {
        internal CommandHandlerInfo(MethodInfo method)
        {
            Method = method;
            Attribute = method.GetCustomAttribute<CommandHandlerAttribute>();
            Debug.Assert(Attribute != null);

            Parameters = (from e in method.GetParameters() select new CommandParameterInfo(e)).ToArray();
        }

        public string Command
        {
            get { return Attribute.Command; }
        }

        public string Description
        {
            get { return Attribute.Description; }
        }

        internal MethodInfo Method { get; private set; }
        internal CommandHandlerAttribute Attribute { get; private set; }

        public CommandParameterInfo[] Parameters { get; private set; }
    }

    public class CommandParameterInfo
    {
        private readonly object _default;
        private readonly string _description;
        private readonly CommandArgumentOption _option = CommandArgumentOption.None;

        internal CommandParameterInfo(ParameterInfo parameter)
        {
            ParameterType = parameter.ParameterType;

            var attribute = parameter.GetCustomAttribute<CommandArgumentAttribute>();
            if (attribute != null)
            {
                _description = attribute.Description;
                _default = attribute.Default;
                _option = attribute.Options;
            }

            Parameter = parameter;
            Attribute = attribute;
        }

        public Type ParameterType { get; private set; }

        public string Description
        {
            get { return _description ?? ""; }
        }

        public bool IsOptional
        {
            get { return _option == CommandArgumentOption.Optional; }
        }

        public bool IsAfterAll
        {
            get { return _option == CommandArgumentOption.AfterAll; }
        }

        public object Default
        {
            get
            {
                var result = _default ?? (ParameterType.IsValueType ? Activator.CreateInstance(ParameterType) : null);
                return result != null ? Convert.ChangeType(result, ParameterType) : null;
            }
        }

        public bool IsActorType
        {
            get { return ParameterType == typeof(Actor); }
        }

        public Type ActualType
        {
            get { return IsActorType ? typeof(int) : ParameterType; }
        }

        internal ParameterInfo Parameter { get; private set; }
        internal CommandArgumentAttribute Attribute { get; private set; }
    }

    public class CommandArguments
    {
        private static readonly Regex WhitespaceRegex = new Regex(@"\s+", RegexOptions.Compiled);
        private readonly string[] _arguments;

        public CommandArguments(string input)
        {
            if (input.Contains(" "))
            {
                Command = input.Substring(0, input.IndexOf(' '));
                Argument = input.Substring(input.IndexOf(' ')).TrimStart(' ');
                _arguments = WhitespaceRegex.Split(Argument.Trim());
            }
            else
            {
                Command = input;
                Argument = "";
                _arguments = new string[0];
            }
        }

        public int Count
        {
            get { return _arguments.Length; }
        }

        public string Command { get; private set; }

        public string Argument { get; private set; }

        public T Get<T>(int index, T defaultValue = default(T))
        {
            return (T)GetValue(index, typeof(T), defaultValue);
        }

        public bool Is<T>(int index)
        {
            return IsType(index, typeof(T));
        }

        public object GetValue(int index, Type valueType, object defaultValue = null)
        {
            if (!IsType(index, valueType))
                return valueType.IsValueType ? Activator.CreateInstance(valueType) : defaultValue;

            return Convert.ChangeType(_arguments[index], valueType);
        }

        public bool IsType(int index, Type valueType)
        {
            if (index < 0 || index >= Count)
                return false;

            try
            {
                // ReSharper disable ReturnValueOfPureMethodIsNotUsed
                Convert.ChangeType(_arguments[index], valueType);
                // ReSharper restore ReturnValueOfPureMethodIsNotUsed
            }
            // ReSharper disable EmptyGeneralCatchClause
            catch (Exception)
            // ReSharper restore EmptyGeneralCatchClause
            {
                return false;
            }
            return true;
        }

        public string GetAll(int index)
        {
            var startPos = 0;
            while (index-- > 0)
            {
                startPos = Argument.IndexOf(" ", startPos, StringComparison.Ordinal);
                if (startPos < 0)
                    return "";

                ++startPos;
            }
            return Argument.Substring(startPos);
        }
    }
}
