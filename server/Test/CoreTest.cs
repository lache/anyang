using NUnit.Framework;
using Server.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Test
{
    [TestFixture]
    class CoreTest
    {
        [Test]
        public void ConnectionTest()
        {
            const int port = 29321;
            var network = new Network();
            network.StartServer(port);
            var peerSession = network.Connect("127.0.0.1", port);
            peerSession.Disconnect();
        }
    }
}
