﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Server.Tool
{
    public abstract class Deploy
    {
        public delegate void LogReceived(ConsoleColor color, string message);

        public event LogReceived OnLogReceived;

        public bool Execute()
        {
            try
            {
                ExecuteInternal();
                Log(ConsoleColor.White, " * Complete.");
                return true;
            }
            catch (ProcessException exception)
            {
                Log(ConsoleColor.Red, exception.Message);
                Log(ConsoleColor.Red, exception.StackTrace);
                Log(ConsoleColor.Gray, exception.Result.Stdout);
                Log(ConsoleColor.Gray, exception.Result.Stderr);
            }
            catch (Exception exception)
            {
                Log(ConsoleColor.Red, exception.Message);
                Log(ConsoleColor.Red, exception.StackTrace);
            }
            Log(ConsoleColor.Red, " * Error.");
            return false;
        }

        protected abstract void ExecuteInternal();

        protected void Log(ConsoleColor color, string format, params object[] args)
        {
            Log(color, 0, format, args);
        }

        protected void Log(ConsoleColor color, int level, string format, params object[] args)
        {
            var message = args != null && args.Length > 0 ? string.Format(format, args) : format;
            if (level == 0)
                FireLogReceived(color, message);
            else
            {
                var space = new string(' ', level);
                foreach (var line in message.Split('\r', '\n').Where(line => !string.IsNullOrWhiteSpace(line)))
                {
                    FireLogReceived(color, space + line.Trim());
                }
            }
        }

        protected void FireLogReceived(ConsoleColor color, string message)
        {
            var handler = OnLogReceived;
            if (handler != null) handler(color, message);
        }

        protected static void CopyFiles(string src, string destPath)
        {
            var srcPath = Path.GetDirectoryName(src);
            var srcPattern = Path.GetFileName(src);

            if (srcPath == null || srcPattern == null)
                throw new InvalidOperationException("Invalid Source: " + src);

            if (!Directory.Exists(srcPath))
                throw new InvalidOperationException("No Source Directory: " + srcPath);

            CreateDirectory(new DirectoryInfo(destPath));

            var pathRegex = new Regex(Regex.Escape(srcPath));
            foreach (var srcFile in Directory.GetFiles(srcPath, srcPattern))
            {
                var destFile = pathRegex.Replace(srcFile, destPath, 1);
                CopyFile(srcFile, destFile);
            }
        }

        protected static void CopyFile(string srcFile, string destFile)
        {
            var srcFileInfo = new FileInfo(srcFile);
            var destFileInfo = new FileInfo(destFile);
            if (!srcFileInfo.Exists)
                throw new InvalidOperationException("Cannot find the file: " + srcFile);

            if (destFileInfo.Exists
                && srcFileInfo.Length == destFileInfo.Length
                && Math.Abs(srcFileInfo.LastWriteTime.ToFileTime() - destFileInfo.LastWriteTime.ToFileTime()) < 1000)
                return;

            File.Copy(srcFile, destFile, true);
        }

        protected static void MakeHash(string targetPath)
        {
            var result = Directory.GetFiles(targetPath, "*.*", SearchOption.AllDirectories).Select(e =>
                string.Join(",", e.Substring(targetPath.Length + 1), new FileInfo(e).Length, Hash(e))).ToList();
            File.WriteAllLines(Path.Combine(targetPath, "files"), result.ToArray(), Encoding.UTF8);
        }

        protected static string Hash(string filePath)
        {
            var result = new StringBuilder();
            var fileBytes = File.ReadAllBytes(filePath);
            var hashBytes = (new MD5CryptoServiceProvider()).ComputeHash(fileBytes);

            foreach (var each in hashBytes)
                result.Append(each.ToString("X2"));

            return result.ToString();
        }

        protected static ProcessResult ExecuteProcessRedirect(string fileName, string argument, string errorMessage = "")
        {
            using (var process = new Process())
            {
                process.StartInfo.FileName = fileName;
                process.StartInfo.Arguments = argument;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.Start();

                var result = new ProcessResult { Stdout = process.StandardOutput.ReadToEnd() };
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    result.Stderr = process.StandardError.ReadToEnd();
                    throw new ProcessException(errorMessage, result);
                }
                return result;
            }
        }

        protected static ProcessResult ExecuteProcess(string fileName, string argument, string errorMessage = "")
        {
            using (var process = new Process())
            {
                process.StartInfo.FileName = fileName;
                process.StartInfo.Arguments = argument;
                process.StartInfo.CreateNoWindow = false;
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
                process.Start();

                var result = new ProcessResult { Stdout = "Ok." };
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    result.Stderr = "Error!";
                    throw new ProcessException(errorMessage, result);
                }
                return result;
            }
        }

        protected static void CreateDirectory(DirectoryInfo directory)
        {
            if (directory.Parent != null) CreateDirectory(directory.Parent);
            if (!directory.Exists) directory.Create();
        }

        protected static void ModifyXml(string xmlPath, string xpath, string newValue)
        {
            var doc = new XmlDocument();
            doc.Load(xmlPath);

            var node = doc.SelectSingleNode(xpath) as XmlAttribute;
            if (node != null)
            {
                node.Value = newValue;
                doc.Save(xmlPath);
            }
        }

        protected static ProcessResult Zip(string sourceDirectory, string outputFile)
        {
            var runningOnMono = Type.GetType("Mono.Runtime") != null;
            var zipPath = runningOnMono 
                ? @"7z"
                : Directory.GetFiles(Path.GetFullPath(Path.Combine("..", "..", "..")), "7z.exe", SearchOption.AllDirectories)[0];
            if (File.Exists(outputFile))
                File.Delete(outputFile);

            return ExecuteProcess(zipPath, string.Format(@"a -r {0} {1}", outputFile, sourceDirectory), "Cannot zip file: " + outputFile);
        }

        protected static string PostFile(string file)
        {
            const string url = "http://dist.mmo.pe.kr/patch_a/";
            var boundary = "----------------------------" + DateTime.Now.Ticks.ToString("x");
            var httpWebRequest = (HttpWebRequest) WebRequest.Create(url);
            httpWebRequest.ContentType = "multipart/form-data; boundary=" + boundary;
            httpWebRequest.Method = "POST";
            httpWebRequest.KeepAlive = true;
            httpWebRequest.Credentials = CredentialCache.DefaultCredentials;
            using (var memStream = new MemoryStream())
            {
                var boundaryBytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");
                const string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\n Content-Type: application/octet-stream\r\n\r\n";
                memStream.Write(boundaryBytes, 0, boundaryBytes.Length);

                var header = string.Format(headerTemplate, "target", file);
                var headerbytes = Encoding.UTF8.GetBytes(header);
                memStream.Write(headerbytes, 0, headerbytes.Length);

                using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    var buffer = new byte[4096];
                    int bytesRead;
                    while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        memStream.Write(buffer, 0, bytesRead);
                    }
                    memStream.Write(boundaryBytes, 0, boundaryBytes.Length);
                }

                httpWebRequest.ContentLength = memStream.Length;
                using (var requestStream = httpWebRequest.GetRequestStream())
                {
                    var tempBuffer = memStream.ToArray();
                    requestStream.Write(tempBuffer, 0, tempBuffer.Length);
                }
            }

            var webResponse = httpWebRequest.GetResponse();
            using (var stream = webResponse.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }

    public class ProcessException : InvalidOperationException
    {
        public ProcessResult Result { get; private set; }

        public ProcessException(string message, ProcessResult result)
            : base(message)
        {
            Result = result;
        }
    }

    public class ProcessResult
    {
        public string Stdout { get; set; }
        public string Stderr { get; set; }
    }

    public class ServerDeploy : Deploy
    {
        protected override void ExecuteInternal()
        {
            const string deployList = @"./*.dll,server/
                               ./Server.exe,server/
                               ./Data/*,server/Data/";
            foreach (var pair in deployList.Split('\r', '\n').Where(e => !string.IsNullOrWhiteSpace(e))
                                           .Select(e => e.Trim().Replace('/', Path.DirectorySeparatorChar).Split(',')))
            {
                CopyFiles(pair[0], pair[1]);
            }
            Log(ConsoleColor.Yellow, " * Copy Ok.");

            MakeHash("server");

            Log(ConsoleColor.Yellow, " * Zip Server.");
            var zipResult = Zip("server", "server.zip");
            Log(ConsoleColor.Cyan, 4, zipResult.Stdout.Trim());
            Log(ConsoleColor.Yellow, " * Zip Ok.");

            Log(ConsoleColor.Yellow, " * Upload Start.");
            var uploadResult = PostFile("server.zip");

            Log(ConsoleColor.Cyan, 4, uploadResult.Trim());
            Log(ConsoleColor.Yellow, " * Upload Ok.");
        }
    }
}
