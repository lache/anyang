using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace launcher
{
    internal class ClientLauncher
    {
        public static readonly ClientLauncher Instance = new ClientLauncher();

        private readonly ReaderWriterLockSlim _serversLock = new ReaderWriterLockSlim();

        public static string ClientPath
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "anyang"); }
        }

        public static string ClientExecutablePath
        {
            get { return Path.Combine(ClientPath, "HelloCpp.exe"); }
        }

        public void Execute()
        {
            if (!File.Exists(ClientExecutablePath))
                throw new InvalidOperationException("Cannot find a client binary.\nPlease restart this launcher.");

            using (var clientProcess = new Process())
            {
                clientProcess.StartInfo.FileName = ClientExecutablePath;
                clientProcess.StartInfo.WorkingDirectory = ClientPath;
                clientProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                clientProcess.Start();
            }
        }
    }
}