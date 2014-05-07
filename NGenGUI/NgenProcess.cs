using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Hiale.NgenGui.Helper;

namespace Hiale.NgenGui
{
    public class NgenProcess
    {
        private static readonly Dictionary<string, string> FrameworkDict = new Dictionary<string, string>();
        private readonly List<string> _outputMessages;

       private static readonly object locker = new object();

        public NgenProcess(AssemblyDetails assembly)
        {
            Assembly = assembly;
            _outputMessages = new List<string>();
        }

        public AssemblyDetails Assembly { get; private set; }

        public bool Check()
        {
            return NgenRun(Assembly, NgenAction.Display) == 0;
        }

        public bool Install()
        {
            if (Check())
                return true;
            return NgenRun(Assembly, NgenAction.Install) == 0;
        }

        public bool Uninstall()
        {
            if (!Check())
                return true;
            return NgenRun(Assembly, NgenAction.Uninstall) == 0;
        }

        private int NgenRun(AssemblyDetails assembly, NgenAction action)
        {
            _outputMessages.Clear();
            var arguments = string.Empty;
            switch (action)
            {
                case NgenAction.Install:
                    arguments = "install";
                    break;
                case NgenAction.Uninstall:
                    arguments = "uninstall";
                    break;
                case NgenAction.Update:
                    arguments = "update";
                    break;
                case NgenAction.Display:
                    arguments = "display";
                    break;
            }
            if (action == NgenAction.Install || action == NgenAction.Uninstall || action == NgenAction.Display)
                arguments += " \"" + assembly.AssemblyFile + "\" /nologo";
            else
                arguments += " /nologo";
            //string frameworkPath;
            string frameworkDictKey = assembly.FrameworkVersion + (assembly.CPUVersion == CPUVersion.x64 ? "_64" : "_32");
            string frameworkPath;
           lock (locker)
           {
              if (!FrameworkDict.TryGetValue(frameworkDictKey, out frameworkPath))
              {
                 frameworkPath = GetFrameworkDirectory(assembly.CPUVersion == CPUVersion.x64);
                 FrameworkDict.Add(frameworkDictKey, frameworkPath);
              }
           }
           var nGenPath = frameworkPath + assembly.FrameworkVersion + "\\Ngen.exe";
            if (!File.Exists(nGenPath))
                throw new FileNotFoundException(".NET Framework " + assembly.FrameworkVersion + (assembly.CPUVersion == CPUVersion.x64 ? " (x64)" : string.Empty) +  " seems to be missing.");
            return RunWithRedirect(nGenPath, arguments);
        }

        private int RunWithRedirect(string path, string arguments)
        {
            using (var process = new Process())
            {
                //var process = new Process();
                process.StartInfo.FileName = path;
                process.StartInfo.Arguments = arguments;

                // set up output redirection
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.EnableRaisingEvents = true;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false;
                // see below for output handler
                process.ErrorDataReceived += ProcessDataReceived;
                process.OutputDataReceived += ProcessDataReceived;

                process.Start();

                process.BeginErrorReadLine();
                process.BeginOutputReadLine();

                process.WaitForExit();
                process.ErrorDataReceived -= ProcessDataReceived;
                process.OutputDataReceived -= ProcessDataReceived;
                var emptyNativeImages = false;
                foreach (string msg in _outputMessages)
                {
                    if (emptyNativeImages)
                        emptyNativeImages = false;
                    if (msg.Contains("Administrator permissions"))
                        throw new Exception("Administrator permissions are needed.");
                    if (msg.Contains("Native Images"))
                        emptyNativeImages = true;
                }
                if (emptyNativeImages)
                    return -1;
                return process.ExitCode;
            }
        }

        private void ProcessDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data))
                return;
            _outputMessages.Add(e.Data);
            Debug.WriteLine(e.Data);
        }

        public static string GetFrameworkDirectory(bool x64)
        {
            const string frameworkRegKey = @"SOFTWARE\Microsoft\.NetFramework";
            const string frameworkRegProperty = "InstallRoot";
            return Registry.RegistryWow6432.GetRegKey(Registry.RegHive.HKEY_LOCAL_MACHINE, frameworkRegKey, x64 ? Registry.RegSAM.Wow6464Key : Registry.RegSAM.Wow6432Key, frameworkRegProperty);
        }
    }
}