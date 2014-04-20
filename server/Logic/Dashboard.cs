using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Server.Core;
using Server.Message;

namespace Server.Logic
{
    // Player가 다스리는 마을들의 상태를 전달하기 위해 정보를 모으는 객체
    class Dashboard
    {
        public static readonly Dashboard Global = new Dashboard(true);
        public static readonly Dashboard Local = new Dashboard(false);

        private readonly LockerFactory _locker;

        private Dashboard(bool useGlobal)
        {
            _locker = new LockerFactory(new ReaderWriterLockSlim(), /* use-lock = */ useGlobal);
        }

        public void Notify(NetworkActor action)
        {
            using (_locker.Create(LockType.Shared))
            {
                
            }
        }
    }
}
