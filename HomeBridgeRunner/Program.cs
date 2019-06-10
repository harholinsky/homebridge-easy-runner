using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Reflection;
using System.ServiceProcess;

namespace HomeBridgeRunner
{
    static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += LogUnhandledException;

            if (Environment.UserInteractive)
            {
                // we only care about the first two characters
                string arg = args.Length > 0 ? args[0].ToLowerInvariant().Substring(0, 2) : null;

                switch (arg)
                {
                    case "/i":
                        ConfigureService(true);
                        break;
                    case "/u":  // uninstall
                        ConfigureService(false);
                        break;
                    case "/r":
                        new HomeBridgeRunner().RunAsConsole(args);
                        break;
                    default:  // unknown option
                        Console.WriteLine("Argument not recognized: {0}", args[0]);
                        Console.WriteLine("/i - install");
                        Console.WriteLine("/u - uninstall");
                        Console.WriteLine("/r - run service as console app");
                        break;
                }
            }
            else
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                new HomeBridgeRunner()
                };
                ServiceBase.Run(ServicesToRun);
            }
        }

        private static void ConfigureService(bool install)
        {
            try
            {
                var fileName = Assembly.GetEntryAssembly().Location;
                var args = new List<string> { fileName };
                if (!install)
                {
                    args.Add("/u");
                }
                ManagedInstallerClass.InstallHelper(args.ToArray());
                Console.WriteLine("Service was successfully {0}", install ? "installed" : "uninstalled");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public static void LogUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            if (args.IsTerminating)
                Console.WriteLine("[FATAL]: Unhandled exception occured.\n {0}", args.ExceptionObject);
            else
                Console.WriteLine("Unhandled exception occured.\n {0}", args.ExceptionObject);
        }

    }
}
