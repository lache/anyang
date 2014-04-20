using System;
using System.Threading;

namespace Server.Core
{
    public enum LockType
    {
        Shared, Exclusive
    }

    public class LockerFactory
    {
        private readonly ReaderWriterLockSlim _lock;
        private readonly bool _locking;

        public LockerFactory(ReaderWriterLockSlim @lock, bool locking)
        {
            _lock = @lock;
            _locking = locking;
        }

        public Locker Create(LockType type)
        {
            return _locking ? (Locker)new DisposableLocker(_lock, type) : new EmptyLocker(_lock, type);
        }

        public abstract class Locker : IDisposable
        {
            protected readonly ReaderWriterLockSlim Lock;
            protected readonly LockType Type;

            protected Locker(ReaderWriterLockSlim @lock, LockType type)
            {
                Lock = @lock;
                Type = type;
            }

            public abstract void Dispose();
        }

        private class DisposableLocker : Locker
        {
            public DisposableLocker(ReaderWriterLockSlim @lock, LockType type)
                : base(@lock, type)
            {
                switch (Type)
                {
                    case LockType.Shared:
                        Lock.EnterReadLock();
                        break;
                    case LockType.Exclusive:
                        Lock.EnterWriteLock();
                        break;
                }
            }

            public override void Dispose()
            {
                switch (Type)
                {
                    case LockType.Shared:
                        Lock.ExitReadLock();
                        break;
                    case LockType.Exclusive:
                        Lock.ExitWriteLock();
                        break;
                }
            }
        }

        private class EmptyLocker : Locker
        {
            public EmptyLocker(ReaderWriterLockSlim @lock, LockType type) 
                : base(@lock, type)
            {
            }

            public override void Dispose()
            {
            }
        }
    }

    public static class LockExtension
    {
        public static void DoRead(this ReaderWriterLockSlim locker, Action action)
        {
            locker.EnterReadLock();
            try
            {
                action();
            }
            finally
            {
                locker.ExitReadLock();
            }
        }

        public static T DoRead<T>(this ReaderWriterLockSlim locker, Func<T> action)
        {
            locker.EnterReadLock();
            try
            {
                return action();
            }
            finally
            {
                locker.ExitReadLock();
            }
        }

        public static void DoWrite(this ReaderWriterLockSlim locker, Action action)
        {
            locker.EnterWriteLock();
            try
            {
                action();
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }

        public static T DoWrite<T>(this ReaderWriterLockSlim locker, Func<T> action)
        {
            locker.EnterWriteLock();
            try
            {
                return action();
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }

        public static void DoLock(this SpinLock locker, Action action, int maximumRetryCount = 4096)
        {
            var retryCount = 0;
            var acquired = false;
            while (!acquired)
            {
                locker.Enter(ref acquired);
                if (++retryCount > maximumRetryCount)
                    throw new InvalidOperationException("lock timeout");
            }

            action();
            locker.Exit();
        }
    }
}
