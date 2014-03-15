using Server.Forms;
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
        private readonly Coroutine _coro = new Coroutine();
        private readonly World _world;
        private readonly ProgramOptions _options;

        public Program(ProgramOptions options)
        {
            _persistence = new Persistence(options.NonPersistenceWorld);
            _world = new World(_coro, _persistence);
            _options = options;
        }

        public void Run()
        {
            _network.OnConnect += network_OnConnect;
            _network.OnDisconnect += network_OnDisconnect;

            Logger.Write("start nerwork");
            _network.StartServer(40004);

            // 서버 로직의 시작점은 World이다.
            Logger.Write("start logic");
            _coro.AddEntry(_world.CoroEntry);
            _coro.AddEntry(CoroUpdateRealtime);

            if (!_options.Debug)
                _coro.Run();
            else
            {
                _coro.Start();
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

        // 네트워크 세션과 Actor를 연결해준다.
        void network_OnConnect(Session session)
        {
            // 네트워크로 연결된 Actor는 Player이므로 User 객체를 만들어준다.
            var actor = new Player(_world, session);
            actor.Connected = true;
            session.Source = actor;

            _coro.AddEntry(actor.CoroEntry);
            _coro.AddEntry(actor.CoroDispatchEntry);
        }

        void network_OnDisconnect(Session session)
        {
            var actor = session.Source as NetworkActor;
            if (actor != null)
            {
                actor.Connected = false;
            }
        }

        static void Main(string[] args)
        {
            var runningOnMono = Type.GetType("Mono.Runtime") != null;
            if (!runningOnMono)
            {
                ConsoleHelper.Initialize();
            }

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
