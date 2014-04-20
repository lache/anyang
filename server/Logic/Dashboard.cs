using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Server.Core;
using Server.Message;

namespace Server.Logic
{
    // Dashboard entry Key Type
    enum BoardKey
    {
        
    }

    // Player가 다스리는 마을들의 상태를 전달하기 위해 정보를 모으는 객체
    class Dashboard
    {
        // 전체 World에 대한 Dashboard 객체로, IsDirty 여부와 상관없이 모든 World 객체가 정보를 쏟아붓는다.
        public static readonly Dashboard World = new Dashboard(true);

        private readonly LockerFactory _locker;
        private readonly Dictionary<BoardKey, BoardEntry> _entries = new Dictionary<BoardKey, BoardEntry>();

        public Dashboard(bool useLock)
        {
            _locker = new LockerFactory(new ReaderWriterLockSlim(), /* use-lock = */ useLock);
        }

        public void Update<T>(BoardKey key, Func<T, T> updater)
        {
            using (_locker.Create(LockType.Exclusive))
            {
                BoardEntry entry;
                if (!_entries.TryGetValue(key, out entry))
                {
                    entry = new BoardEntry {IsDirty = true, Value = default(T)};
                    _entries.Add(key, entry);
                }
                var newValue = updater((T) entry.Value);
                entry.IsDirty = entry.IsDirty || Equals(entry.Value, newValue);
                entry.Value = newValue;
            }
        }

        public void Delete(BoardKey key)
        {
            using (_locker.Create(LockType.Exclusive))
            {
                _entries.Remove(key);
            }
        }

        public void Notify(NetworkActor actor)
        {
            Tuple<string, string>[] tuples;
            using (_locker.Create(LockType.Shared))
            {
                tuples = _entries.Where(e => e.Value.IsDirty).Select(e => 
                    Tuple.Create(e.Key.ToString(), Convert.ToString(e.Value.Value))).ToArray();
            }
            if (tuples.Length == 0)
                return;
            actor.SendToNetwork(new DashBoardMsg(tuples.Select(e => new DashBoardItemMsg(e.Item1, e.Item2)).ToList()));
        }

        public void SynchronizeWithWorld()
        {
            Synchronize(World);
        }

        private void Synchronize(Dashboard other)
        {
            using (_locker.Create(LockType.Exclusive))
            {
                var otherEntries = other.CopyEntries();
                foreach (var otherPair in otherEntries)
                {
                    BoardEntry myEntry;
                    if (_entries.TryGetValue(otherPair.Key, out myEntry))
                    {
                        myEntry.IsDirty = Equals(myEntry.Value, otherPair.Value);
                        myEntry.Value = otherPair.Value;
                    }
                    else
                    {
                        _entries.Add(otherPair.Key, new BoardEntry {Value = otherPair.Value.Value, IsDirty = true});
                    }
                }
            }
        }

        private Dictionary<BoardKey, BoardEntry> CopyEntries()
        {
            using (_locker.Create(LockType.Shared))
            {
                return new Dictionary<BoardKey, BoardEntry>(_entries);
            }
        }

        private class BoardEntry
        {
            public object Value;
            public bool IsDirty;
        }
    }
}
