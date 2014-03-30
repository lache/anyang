﻿using Server.Forms;
using Server.Core;
using Server.Logic;
using Server.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;

namespace Server
{
    class ProgramOptions
    {
        public bool Debug { get; set; }
        public bool NonPersistenceWorld { get; set; }
    }

    class Program
    {
        private readonly Persistence _persistence;
        private readonly Network _network = new Network();
        private readonly Dictionary<int, World> _worlds = new Dictionary<int, World>();
        private readonly ProgramOptions _options;

        public Program(ProgramOptions options)
        {
            _persistence = new Persistence(options.NonPersistenceWorld);
            _options = options;
        }

        public void Run()
        {
            Initializer.Do(InitializePhase.Game);

            _network.OnConnect += network_OnConnect;
            _network.OnDisconnect += network_OnDisconnect;
            _network.AddManualHandler(typeof(EnterWorldMsg), network_OnEnterWorld);

            Logger.Write("start nerwork");
            _network.StartServer(40004);

            // 서버 로직의 시작점은 World이다.
            Logger.Write("start logic");

            var coro = new Coroutine();
            coro.AddEntry(CoroUpdateRealtime);

            // 각 대륙에 대한 World을 만들어준다.
            foreach (var mapFile in Directory.GetFiles("Data", "*.tmx"))
            {
                var world = new World(_persistence, mapFile);
                _worlds.Add(world.Id, world);
            }

            foreach (var world in _worlds.Values)
                world.Start();

            if (!_options.Debug)
                coro.Run();
            else
            {
                coro.Start();
                Application.Run(new FormAiViewer());
            }
        }

        IEnumerable<int> CoroUpdateRealtime()
        {
            while (true)
            {
                // 매 32ms마다 Realtime을 갱신해준다.
                Realtime.Update();
                yield return 32;
            }
        }

        void network_OnConnect(Session session)
        {
        }

        void network_OnDisconnect(Session session)
        {
            var actor = session.Source as NetworkActor;
            if (actor != null)
            {
                actor.Connected = false;
            }
        }

        void network_OnEnterWorld(Session session, IMessage msgObj)
        {
            // 네트워크 세션과 Actor를 연결해준다.
            var msg = (EnterWorldMsg)msgObj;

            // 로그인한 유저의 World를 찾는다.
            var data = _persistence.Find<PlayerData>(e => e.Character.Name == msg.Name);
            if (data == null)
            {
                data = new PlayerData(msg.Name);
                _persistence.Store(data);
            }

            World world;
            if (!_worlds.TryGetValue(data.Move.WorldId, out world))
                return;

            // 네트워크로 연결된 Actor는 Player이므로 User 객체를 만들어준다.
            var actor = new Player(world, session, data);
            actor.Connected = true;
            session.Source = actor;

            world.Coro.AddEntry(actor.CoroEntry);
            world.Coro.AddEntry(actor.CoroDispatchEntry);
        }

        static void Main(string[] args)
        {
            Initializer.Do(InitializePhase.Program);

            var options = new ProgramOptions();
            foreach (var arg in args)
            {
                if (string.Equals(arg, "--debug")) options.Debug = true;
                if (string.Equals(arg, "--no-store")) options.NonPersistenceWorld = true;
                if (string.Equals(arg, "--deploy")) { DoSelfDeploy(); return; }
            }

            new Program(options).Run();
        }

        private static void DoSelfDeploy()
        {
            var deploy = new Tool.ServerDeploy();
            deploy.OnLogReceived += (color, message) =>
                {
                    Console.ForegroundColor = color;
                    Console.WriteLine(message);
                };
            if (!deploy.Execute())
            {
                Environment.ExitCode = 1;
            }
        }
    }

    [Initializer(Platform = InitializePlatform.Microsoft)]
    static class ConsoleHelper
    {
        [DllImport("kernel32")]
        static extern bool AllocConsole();

        public static void Initialize()
        {
            AllocConsole();
        }
    }
}
