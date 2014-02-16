﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Redmine.Net.Api;
using Redmine.Net.Api.Types;
using System.Net;

namespace deploy
{
    public abstract class Deploy
    {
        public delegate void LogReceived(Color color, string message);

        public event LogReceived OnLogReceived;

        public bool Execute(string history)
        {
            try
            {
                ExecuteInternal(history);
                Log(Color.White, " * Complete.");
                return true;
            }
            catch (ProcessException exception)
            {
                Log(Color.Red, exception.Message);
                Log(Color.Red, exception.StackTrace);
                Log(Color.Gray, exception.Result.Stdout);
                Log(Color.Gray, exception.Result.Stderr);
            }
            catch (Exception exception)
            {
                Log(Color.Red, exception.Message);
                Log(Color.Red, exception.StackTrace);
            }
            Log(Color.Red, " * Error.");
            return false;
        }

        protected abstract void ExecuteInternal(string history);

        protected void Log(Color color, string format, params object[] args)
        {
            Log(color, 0, format, args);
        }

        protected void Log(Color color, int level, string format, params object[] args)
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

        protected void FireLogReceived(Color color, string message)
        {
            var handler = OnLogReceived;
            if (handler != null) handler(color, message);
        }

        protected static ProcessResult BuildProject(string solutionPath, string configuration)
        {
            const string defaultDevenvPath = @"C:\Program Files (x86)\Microsoft Visual Studio 12.0\Common7\IDE\devenv.com";
            const string expressDevenvPath = @"C:\Program Files (x86)\Microsoft Visual Studio 12.0\Common7\IDE\WDExpress.exe";
            var devenvPath = defaultDevenvPath;
            var devenvPathInEnv = Environment.GetEnvironmentVariable("VS120COMNTOOLS");
            if (!string.IsNullOrWhiteSpace(devenvPathInEnv))
                devenvPath = Path.GetFullPath(Path.Combine(devenvPathInEnv, "..", "IDE", "devenv.com"));

            if (!File.Exists(devenvPath))
            {
                devenvPath = expressDevenvPath;
                if (!string.IsNullOrWhiteSpace(devenvPathInEnv))
                    devenvPath = Path.GetFullPath(Path.Combine(devenvPathInEnv, "..", "IDE", "WDExpress.exe"));

                if (!File.Exists(devenvPath))
                    throw new InvalidOperationException("Cannot find VS120 devenv.com");
            }
            return ExecuteProcess(devenvPath, string.Format(@"{0} /build ""{1}""", solutionPath, configuration), "Build Error");
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

            foreach (var srcFile in Directory.GetFiles(srcPath, srcPattern))
            {
                var destFile = srcFile.Replace(srcPath, destPath);
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
            var result = new List<string>();
            foreach (var each in Directory.GetFiles(targetPath, "*.*", SearchOption.AllDirectories))
            {
                var filePath = each.Substring(targetPath.Length + 1);
                var length = new FileInfo(each).Length;
                var hashValue = Hash(each);
                result.Add(string.Join(",", filePath, length, hashValue));
            }
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

        protected static void WriteDeployHistory(string deployTitle, string history)
        {
            // Redmine 사용 잠정 보류
            return;
            /*
            const string host = "http://xelina.co.kr:8888/redmine";
            const string apiKey = "44668d8262a25c7b6ccbe94163cff8d39da51795";

            var manager = new RedmineManager(host, apiKey);
            var text = string.Format(@"
h2. {2} 배포 [{0}]

{1}
", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), history, deployTitle);

            const string projectName = "mmo";
            const string deployPageTitle = "Deploy History";
            WikiPage historyWiki;
            try
            {
                historyWiki = manager.GetWikiPage(projectName, new NameValueCollection(), deployPageTitle);
                historyWiki.Text += text;
            }
            catch (Exception)
            {
                historyWiki = new WikiPage { Title = deployPageTitle, Text = text };
            }

            try
            {
                manager.CreateOrUpdateWikiPage(projectName, deployPageTitle, historyWiki);
            }
// ReSharper disable EmptyGeneralCatchClause
            catch
// ReSharper restore EmptyGeneralCatchClause
            {
            }
             */
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
            const string scpPath = @"tools\7z.exe";
            if (File.Exists(outputFile))
                File.Delete(outputFile);

            return ExecuteProcess(scpPath, string.Format(@"a -r {0} {1}", outputFile, sourceDirectory), "Cannot zip file: " + outputFile);
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
                var formdataTemplate = "\r\n--" + boundary + "\r\nContent-Disposition:  form-data; name=\"{0}\";\r\n\r\n{1}";
                var headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\n Content-Type: application/octet-stream\r\n\r\n";
                memStream.Write(boundaryBytes, 0, boundaryBytes.Length);

                var header = string.Format(headerTemplate, "target", file);
                var headerbytes = Encoding.UTF8.GetBytes(header);
                memStream.Write(headerbytes, 0, headerbytes.Length);

                using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    var buffer = new byte[4096];
                    var bytesRead = 0;
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
}
