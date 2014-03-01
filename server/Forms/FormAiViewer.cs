using Server.Core;
using Server.Message;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Server.Forms
{
    public partial class FormAiViewer : Form
    {
        private readonly Network _network = new Network();
        private Session _session;
        private Dictionary<int /* id */, PointF> _posMap = new Dictionary<int, PointF>();
        public FormAiViewer()
        {
            InitializeComponent();
            PrepareDispatchMap();
        }

        private void FormAiViewer_Load(object sender, EventArgs e)
        {
            _session = _network.Connect("localhost", 40004);
            _session.Send(new EnterWorldMsg(":)"));
        }

        private void FormAiViewer_FormClosing(object sender, FormClosingEventArgs e)
        {
            _session.Disconnect();
        }

        private void FormAiViewer_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Color.White);

            foreach (var pair in _posMap)
            {
                g.FillRectangle(Brushes.Black, pair.Value.X, pair.Value.Y, 2, 2);
            }
        }

        void OnWorldInfo(WorldInfoMsg msg)
        {

        }

        void OnSpawn(SpawnMsg msg)
        {
        }

        void OnDespawn(DespawnMsg msg)
        {
        }

        void OnUpdatePosition(UpdatePositionMsg msg)
        {
            if (_posMap.ContainsKey(msg.Id))
                _posMap.Remove(msg.Id);
            _posMap.Add(msg.Id, new PointF((float)msg.X, (float)msg.Y));
        }

        private void sessionTimer_Tick(object sender, EventArgs e)
        {
            DispatchMessage();
            Invalidate();
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

        private void DispatchMessage()
        {
            foreach (var message in _session.MessageQueue.Flush())
            {
                var messageType = message.GetType();
                if (!_dispatchMap.ContainsKey(messageType))
                    continue;
                _dispatchMap[messageType].Invoke(this, new object[] { message });
            }
        }

        #endregion
    }
}
