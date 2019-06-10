using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Threading;

namespace HomeBridgeRunner
{
    public partial class HomeBridgeRunner : ServiceBase
    {
        private readonly string userprofile;
        private Process cmd;
        private Thread thread;
        private object _lock = new object();

        public HomeBridgeRunner()
        {
            InitializeComponent();

            userprofile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        public void RunAsConsole(string[] args)
        {
            Log("Running service as console app");
            this.OnStart(args);
            thread?.Join();
        }

        private void Log(string input)
        {
            string logsDirPath = Path.Combine(userprofile, ".homebridge/logs");

            if (!Directory.Exists(logsDirPath))
            {
                Directory.CreateDirectory(logsDirPath);
            }

            lock (_lock)
            {
                Console.WriteLine(input);
                using (var sw = File.AppendText(Path.Combine(logsDirPath, $"{DateTime.Now.ToString("yyyy-MM-dd")}.txt")))
                {
                    sw.WriteLine(input);
                }
            }
        }

        protected override void OnStart(string[] args)
        {
            thread = new Thread(() =>
            {
                Thread.Sleep(TimeSpan.FromMinutes(1));

                Log("Running homebridge");

                string command = $@"cd /D ""{Path.Combine(userprofile, @"AppData\Roaming\npm\node_modules\homebridge\bin")}"" & node homebridge";
                Log($"Executing command\n{command}");

                cmd = new Process
                {
                    StartInfo =
                    {
                        FileName = "cmd.exe",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        RedirectStandardInput = false,
                        CreateNoWindow = true,
                        Arguments = $"/C {command}"
                    }
                };

                cmd.OutputDataReceived += (obj, outline) =>
                {
                    Log(outline.Data);
                };
                cmd.ErrorDataReceived += (obj, outline) =>
                {
                    Log($"[FATAL]: {outline.Data}");
                };

                cmd.Start();
                cmd.BeginOutputReadLine();
                cmd.BeginErrorReadLine();
                cmd.WaitForExit();
            });

            thread.IsBackground = true;
            thread.Start();
        }

        protected override void OnStop()
        {
            new Thread(() =>
            {
                Log("Stopping homebridge");

                cmd?.StandardInput.Close();
                cmd?.Kill();
                thread?.Abort();
            }).Start();
        }
    }
}
