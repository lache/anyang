using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;

namespace Server.Core
{
    public class Logger
    {
        private const string LogDirectory = "Log";

        private static readonly Logger Instance = new Logger();
        private readonly BlockingCollection<string> _logQueue = new BlockingCollection<string>();

        private Logger()
        {
            if (!Directory.Exists(LogDirectory))
                Directory.CreateDirectory(LogDirectory);

            new Thread(WriteLoop).Start();
        }

        public static void Write(object value)
        {
            Instance.EnqueueMessage(Convert.ToString(value));
        }

        public static void Write(string format, params object[] args)
        {
            var log = args != null && args.Length > 0 ? string.Format(format, args) : format;
            Instance.EnqueueMessage(log);
        }

        public static void Write(Exception e)
        {
            var message = new StringBuilder();
            var exception = e;
            while (exception != null)
            {
                message.AppendLine(exception.GetType() + ": " + exception.Message);
                message.AppendLine(exception.StackTrace);
                exception = exception.InnerException;
            }
            Instance.EnqueueMessage(message.ToString());
        }

        private void EnqueueMessage(string log)
        {
            _logQueue.Add(log);
        }

        private void WriteLoop()
        {
            while (true)
            {
                var log = _logQueue.Take();
                WriteInternal(log);
            }
        }

        private static void WriteInternal(string logLines)
        {
            var logFile = Path.Combine(LogDirectory,
                                       string.Format("{0}.log", DateTime.Now.ToString("yyMMdd")));

            var builder = new StringBuilder();
            foreach (var line in logLines.Replace("\r", "").Split('\n'))
            {
                builder.AppendLine(string.Format("[{0}] {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), line));
            }

            var log = builder.ToString();
            File.AppendAllText(logFile, log, Encoding.UTF8);
            Console.Write(log);
        }
    }
}
