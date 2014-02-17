﻿using Server.Core;
using Server.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Server.Logic
{
    // Actor는 행위를 수행하는 기본 단위이다.
    // Coroutine을 통해 추가 작업을 수행하거나, World를 통해 정보를 질의할 수 있다.
    class Actor
    {
        protected readonly World _world;
        public Position Location = new Position { X = 0, Y = 0 };

        public Actor(World world)
        {
            _world = world;
        }

        public bool CanMove(Position pos)
        {
            return _world.CanMove(pos.X, pos.Y);
        }

        // 객체 생성 시 불리는 Coroutine 진입 함수.
        public virtual IEnumerable<int> CoroEntry()
        {
            yield break;
        }

        // 객체 소멸 시 불리는 Coroutine 진입 함수.
        // 예를 들어 NetworkActor의 Disconnect 시에 호출되어 영속성을 보장해준다.
        public virtual IEnumerable<int> CoroDispose()
        {
            yield break;
        }
    }

    class AiActor : Actor
    {
        Random _random = new Random(DateTime.Now.Millisecond);
        Ai _ai;

        public AiActor(World world, Ai ai)
            : base(world)
        {
            _ai = ai;
        }

        bool IsAlive()
        {
            return false;
        }

        void DoItNow(Command command)
        {
        }

        public IEnumerable<int> CoroAiEntry()
        {
            _world.Actors.Add(this);
            while (IsAlive())
            {
                // 정보를 모으고 AI 에게 주입 - 현재 어떤 정보가 필요한지 확인 불가

                // 판단을 한 다음 - QLearning을 한번 적용해볼까?

                // 액션을 취한다 - 여튼 명령을 내려보자
                DoItNow(_ai.GetAction());

                yield return _random.Next(1000, 5000);
            }
            _world.Actors.Remove(this);
        }
    }

    // Network 연결에 의해 Message 기반으로 수행될 Actor이다.
    // 이 객체는 실제 로직을 포함하지는 않고, Message 처리에 대한 기반 역할만 한다.
    class NetworkActor : Actor
    {
        protected readonly Session _session;
        public NetworkActor(World world, Session session)
            : base(world)
        {
            _session = session;
            PrepareDispatchMap();
        }

        public void SendToNetwork(IMessage message)
        {
            _session.Send(message);
        }

        public override IEnumerable<int> CoroEntry()
        {
            yield break;
        }

        #region Message Dispatch

        private readonly Dictionary<Type, MethodInfo> _dispatchMap = new Dictionary<Type, MethodInfo>();

        private void PrepareDispatchMap()
        {
            // NetworkActor를 상속받는 객체들의 MessageHandler를 모두 DispatchMap에 등록한다.
            // 이 때 이를 static으로 두면 상속 받는 여러 class가 모두 같은 DispatchMap을 가질 수 있기 때문에 이를 instance 영역에 둔다.
            var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
            foreach (var method in GetType().GetMethods(flags))
            {
                var paramInfos = method.GetParameters();
                if (paramInfos == null || paramInfos.Length != 1)
                    continue;
                var paramType = paramInfos[0].ParameterType;
                if (typeof(IMessage).IsAssignableFrom(paramType))
                    _dispatchMap.Add(paramType, method);
            }
        }

        public IEnumerable<int> CoroDispatchEntry()
        {
            while (true)
            {
                // Session의 MessageQueue를 Coroutine의 흐름에서 Dispatch 할 수 있도록 해준다.
                foreach (var message in _session.MessageQueue.Flush())
                {
                    var messageType = message.GetType();
                    if (!_dispatchMap.ContainsKey(messageType))
                        continue;
                    _dispatchMap[messageType].Invoke(this, new object[] { message });
                }
                // yield thread
                yield return 0;
            }
        }

        #endregion
    }
}
