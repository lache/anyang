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
    }

    class Program
    {
        private readonly Persistence _persistence = new Persistence();
        private readonly Network _network = new Network();
        private readonly Coroutine _coro = new Coroutine();
        private readonly World _world;
        private readonly ProgramOptions _options;

        public Program(ProgramOptions options)
        {
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
            session.Source = actor;
            _coro.AddEntry(actor.CoroEntry);
            _coro.AddEntry(actor.CoroDispatchEntry);

            /*
            var npcData = new NpcData {
                Name = "John",
                WorldId = 1,
                X = 0,
                Y = 12,
                Dir = 0,
                Speed = 0,
                ResourceId = 1,
                MaxHp = 100,
                Hp = 100,
            };
            var roamingPosList = new List<Position> {
                new Position{ X = 0, Y = 29},
                new Position{ X = 0, Y = 12},
            };
            var roamingNpc = new RoamingNpc(_world, null, npcData, roamingPosList);
            _coro.AddEntry(roamingNpc.CoroEntry);
             */
        }

        void network_OnDisconnect(Session session)
        {
            var actor = session.Source as Actor;
            if (actor != null)
            {
                _coro.AddEntry(actor.CoroDispose);
            }
        }

        [DllImport("kernel32")]
        static extern bool AllocConsole();

        static void Main(string[] args)
        {
            AllocConsole();

            var options = new ProgramOptions();
            foreach (var arg in args)
            {
                if (string.Equals(arg, "--debug")) options.Debug = true;
            }
            new Program(options).Run();
        }
    }
}
