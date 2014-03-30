using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Core
{
    public class Coroutine
    {
        public delegate IEnumerable<int> LogicEntryDelegate();

        private readonly List<LogicEntry> _logicEntries = new List<LogicEntry>();
        private readonly List<NewLogicEntry> _newLogicEntries = new List<NewLogicEntry>();
        private readonly List<object> _registersToDelete = new List<object>();

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

            _newLogicEntries.RemoveAll(e => _registersToDelete.Contains(e.Register));
            _logicEntries.RemoveAll(e => _registersToDelete.Contains(e.Register));
            _registersToDelete.Clear();

            foreach (var entry in _newLogicEntries)
            {
                var newEntry = new LogicEntry
                {
                    Enumerator = entry.Delegate().GetEnumerator(),
                    SleepTime = 0,
                    Register = entry.Register
                };
                _logicEntries.Add(newEntry);
            }
            _newLogicEntries.Clear();

            var removals = new List<LogicEntry>();
            foreach (var each in _logicEntries)
            {
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
    }
}
