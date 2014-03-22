using NUnit.Framework;
using Server.Core;
using Server.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
            using (var network = new Network { ForDebug = true })
            {
                network.StartServer(port);
                network.Connect("127.0.0.1", port);
            }
        }

        [Test]
        public void MessageTest()
        {
            const int port = 29322;
            using (var network = new Network { ForDebug = true })
            {
                network.StartServer(port);

                var sendMsg = new ChatMsg(1, "name", "메시지");
                var peerSession = network.Connect("127.0.0.1", port);
                peerSession.Send(sendMsg);

                var connectedSession = network.GetConnectedConnection();
                var recvMsg = connectedSession.GetPacket<ChatMsg>();

                Assert.AreEqual(recvMsg.Id, sendMsg.Id);
                Assert.AreEqual(recvMsg.Name, sendMsg.Name);
                Assert.AreEqual(recvMsg.Message, sendMsg.Message);
            }
        }
    }
}
