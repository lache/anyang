using System;
using System.Reflection;

namespace Server.Core
{
    public enum InitializePhase
    {
        Program,
        Game
    }

    public enum InitializePlatform
    {
        Any,
        Mono,
        Microsoft
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class InitializerAttribute : Attribute
    {
        public InitializePhase Phase { get; set; }
        public InitializePlatform Platform { get; set; }

        public InitializerAttribute()
        {
            Phase = InitializePhase.Program;
            Platform = InitializePlatform.Any;
        }
    }

    public static class Initializer
    {
        public static void Do(InitializePhase phase)
        {
            var runningOnMono = Type.GetType("Mono.Runtime") != null;
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                var attribute = type.GetCustomAttribute<InitializerAttribute>();
                if (attribute == null)
                    continue;

                if (attribute.Phase != phase)
                    continue;

                if ((attribute.Platform == InitializePlatform.Microsoft && runningOnMono) ||
                    (attribute.Platform == InitializePlatform.Mono && !runningOnMono))
                    continue;

                var method = type.GetMethod("Initialize", BindingFlags.Static | BindingFlags.Public);
                method.Invoke(null, new object[0]);
                Logger.Write("Initialize [{0}] at ({1})", type.Name, phase.ToString());
            }
        }
    }

}
