using ComputerUtils.ADB;
using ComputerUtils.ConsoleUi;
using ComputerUtils.Logging;
using ComputerUtils.Updating;
using Iteedee.ApkReader;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiUserADB
{
    class Program
    {
        static void Main(string[] args)
        {
            SetupExeptionHandlers();
            if (args.Length >= 1 && args[0] == "--update") MultiUserADBInterface.updater.Update();
            Logger.logFile = AppDomain.CurrentDomain.BaseDirectory + "Log.log";
            Logger.LogRaw("\nStarting " + MultiUserADBInterface.updater.AppName + " version " + MultiUserADBInterface.updater.version);
            MultiUserADBInterface i = new MultiUserADBInterface();
            i.Start();
        }

        public static void SetupExeptionHandlers()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            HandleExeption((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                HandleExeption(e.Exception, "TaskScheduler.UnobservedTaskException");
                e.SetObserved();
            };
        }

        public static void HandleExeption(Exception e, string source)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Logger.Log("An unhandled exception has occured:\n" + e.ToString(), LoggingType.Crash);
            Console.WriteLine("\n\nAn unhandled exception has occured. Check the log for more info and send it to ComputerElite for the (probably) bug to get fix. Press any key to close out.");
            Console.ReadKey();
            Logger.Log("Exiting cause of unhandled exception.");
            Environment.Exit(0);
        }
    }

    class MultiUserADBInterface
    {
        public static Updater updater = new Updater("1.0.0", "http://github.com/ComputerElite/MultiUserADBInterface", "Multi User ADB Interface");
        public static ADBInteractor interactor = new ADBInteractor();
        public static string exe = AppDomain.CurrentDomain.BaseDirectory;

        public void Start()
        {
            updater.UpdateAssistant();
            Console.WriteLine();
            Console.WriteLine("Welcome to " + MultiUserADBInterface.updater.AppName + " version " + MultiUserADBInterface.updater.version + ". Make sure you have your Quest plugged in via USB and developer mode enabled.");
            while (true)
            {
                string choice = ConsoleUiController.ShowMenu(new string[] { "Install APK", "Uninstall app", "Exit" });
                switch(choice)
                {
                    case "1":
                        InstallAPK();
                        break;
                    case "2":
                        UninstallAPK();
                        break;
                    case "3":
                        System.Environment.Exit(0);
                        break;
                }
            }
        }

        public void InstallAPK()
        {
            string apk = ConsoleUiController.QuestionString("Drag and drop apk to install: ").Replace("\"", "");
            Logger.Log(apk + " selected");
            foreach(AndroidUser u in SelectUsers("install the apk"))
            {
                string package = GetAPKPackageName(apk);
                Logger.Log("Trying to uninstall " + package + " from user " + u.id);
                interactor.Uninstall(package, u);
                Logger.Log("Installing " + apk + " to user " + u.id);
                Console.WriteLine("Installing APK to user " + u.name + ". This may take a few minutes.");
                interactor.ForceInstallAPK(apk, u);
            }
            Logger.Log("Finished");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Finished");
            Console.ForegroundColor = ConsoleColor.White;
        }

        public string GetAPKPackageName(String apk)
        {
            Logger.Log("Getting apk package name");
            Stopwatch s = Stopwatch.StartNew();
            ApkReader apkReader = new ApkReader();
            ApkInfo info = new ApkInfo();
            try
            {
                // Extract files and read info with APKReader
                ZipArchive a = ZipFile.OpenRead(apk);
                a.GetEntry("AndroidManifest.xml").ExtractToFile(exe + "Androidmanifest.xml", true);
                a.GetEntry("resources.arsc").ExtractToFile(exe + "resources.arsc", true);
                info = apkReader.extractInfo(File.ReadAllBytes(exe + "AndroidManifest.xml"), File.ReadAllBytes(exe + "resources.arsc"));
            }
            catch (Exception e)
            {
                
            };
            s.Stop();
            Logger.Log("Got APK package name (" + info.packageName + ") in " + s.ElapsedMilliseconds + " ms");
            return info.packageName;
        }

        public void UninstallAPK()
        {
            string package = ConsoleUiController.QuestionString("package to uninstall (e. g. com.beatgames.beatsaber): ").Replace("\"", "");
            Logger.Log(package + " selected");
            foreach (AndroidUser u in SelectUsers("uninstall the package"))
            {
                Logger.Log("Uninstalling " + package + " to user " + u.id);
                Console.WriteLine("Uninstalling package on user " + u.name);
                interactor.Uninstall(package, u);
            }
            Logger.Log("Finished");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Finished");
            Console.ForegroundColor = ConsoleColor.White;
        }

        public List<AndroidUser> SelectUsers(string action = "")
        {
            List<string> selection = new List<string>();
            Console.WriteLine("Select the user(s) for which you want to " + action);
            foreach (AndroidUser u in interactor.GetUsers()) selection.Add(u.name + " (" + u.id + ")");
            selection.Add("All Users");
            string choice = ConsoleUiController.ShowMenu(selection.ToArray(), "User");
            List<AndroidUser> users = new List<AndroidUser>();
            if (Convert.ToInt32(choice) >= selection.Count)
            {
                users = interactor.GetUsers();
            } else
            {
                users.Add(interactor.GetUsers()[Convert.ToInt32(choice) - 1]);
            }
            return users;
        }
    }
}
