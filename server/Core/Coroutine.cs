using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Server.Core
{
    public class Coroutine
    {
        public delegate IEnumerable<int> LogicEntryDelegate();

        private readonly List<LogicEntry> _logicEntries = new List<LogicEntry>();
        private readonly FlushableArray<NewLogicEntry> _newLogicEntries = new FlushableArray<NewLogicEntry>();
        private readonly FlushableArray<object> _registersToDelete = new FlushableArray<object>();
        private readonly FlushableArray<TransferEntry> _registersToTransfer = new FlushableArray<TransferEntry>();
        private readonly FlushableArray<LogicEntry> _logicEntriesToTransfer = new FlushableArray<LogicEntry>();

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);
        private DateTime _previousTime;
        private readonly int _logicInterval;

        private const int DefaultLogicInterval = 64;

        public Coroutine()
            : this(DefaultLogicInterval)
        {
        }

        public Coroutine(int logicInterval)
        {
            _previousTime = DateTime.Now;
            _logicInterval = logicInterval;
        }

        public void AddEntry(LogicEntryDelegate entry)
        {
            AddEntry(null, entry);
        }

        public void AddEntry(object register, LogicEntryDelegate entry)
        {
            _newLogicEntries.Add(new NewLogicEntry { Delegate = entry, Register = register });
        }

        public void DeleteEntry(object register)
        {
            _registersToDelete.Add(register);
        }

        public void Transfer(Coroutine destination, object register)
        {
            _registersToTransfer.Add(new TransferEntry { Destination = destination, Register = register });
        }

        public void Start()
        {
            var thread = new Thread(Run) { IsBackground = true };
            thread.Start();
        }

        public void Run()
        {
            while (true)
            {
                IterateLogic();
                _resetEvent.WaitOne(_logicInterval, true);
            }
        }

        public void IterateLogic()
        {
            var now = DateTime.Now;
            var delta = (now - _previousTime).Milliseconds;

            var registersToDelete = _registersToDelete.Flush();
            _logicEntries.RemoveAll(e => registersToDelete.Contains(e.Register));

            var registersToTransfer = _registersToTransfer.Flush();
            var newLogicEntries = _newLogicEntries.Flush().Where(e => !registersToDelete.Contains(e.Register));
            foreach (var entry in newLogicEntries)
            {
                if (CheckAndTransferNewEntry(registersToTransfer, entry))
                    continue;

                var newEntry = new LogicEntry
                {
                    Enumerator = entry.Delegate().GetEnumerator(),
                    SleepTime = 0,
                    Register = entry.Register
                };
                _logicEntries.Add(newEntry);
            }

            var removals = new List<LogicEntry>();
            _logicEntries.AddRange(_logicEntriesToTransfer.Flush());
            foreach (var each in _logicEntries)
            {
                if (CheckAndTransferEntry(registersToTransfer, each))
                {
                    removals.Add(each);
                    continue;
                }

                each.SleepTime -= delta;
                if (each.SleepTime >= 0)
                    continue;

                if (!each.Enumerator.MoveNext())
                    removals.Add(each);
                else each.SleepTime = each.Enumerator.Current;
            }

            _logicEntries.RemoveAll(removals.Contains);

            _previousTime = now;
        }

        private static bool CheckAndTransferNewEntry(IEnumerable<TransferEntry> registersToTransfer, NewLogicEntry entry)
        {
            var transfer = false;
            foreach (var transferEntry in registersToTransfer)
            {
                if (transferEntry.Register != entry.Register)
                    continue;

                transferEntry.Destination.AddEntry(entry.Register, entry.Delegate);
                transfer = true;
            }
            return transfer;
        }

        private static bool CheckAndTransferEntry(IEnumerable<TransferEntry> registersToTransfer, LogicEntry entry)
        {
            var transfer = false;
            foreach (var transferEntry in registersToTransfer)
            {
                if (transferEntry.Register != entry.Register)
                    continue;

                transferEntry.Destination._logicEntriesToTransfer.Add(entry);
                transfer = true;
            }
            return transfer;
        }

        private class LogicEntry
        {
            public IEnumerator<int> Enumerator;
            public int SleepTime;
            public object Register;
        }

        private class NewLogicEntry
        {
            public LogicEntryDelegate Delegate;
            public object Register;
        }

        private class TransferEntry
        {
            public object Register;
            public Coroutine Destination;
        }
    }
}
