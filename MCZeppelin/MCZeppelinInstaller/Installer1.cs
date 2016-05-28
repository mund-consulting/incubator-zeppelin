using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MCZeppelinInstaller
{
    [RunInstaller(true)]
    public partial class ZInstaller : System.Configuration.Install.Installer
    {
        public ZInstaller()
        {
            InitializeComponent();
        }
        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand)]
        protected override void OnBeforeInstall(IDictionary savedState)
        {
            //base.OnBeforeInstall(savedState);
            if (SetJavaHome())
            {
                base.OnBeforeInstall(savedState);
            }
            else
            {
                throw new Exception("Could not find JDK. Install JDK 1.7 or later and try again.");
            }

        }

        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand)]
        public override void Install(IDictionary stateSaver)
        {

            base.Install(stateSaver);
        }

        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand)]
        public override void Commit(IDictionary savedState)
        {
            base.Commit(savedState);

            string installationDir = this.Context.Parameters["targetdir"];
            installationDir = installationDir.Remove(installationDir.Length - 2, 2);
            Environment.SetEnvironmentVariable("ZEPPELIN_HOME", installationDir, EnvironmentVariableTarget.Machine);


            string hadoopHome = Environment.GetEnvironmentVariable("HADOOP_HOME");
            if (hadoopHome == null || !File.Exists(hadoopHome + "/bin/winutils.exe"))
            {
                Environment.SetEnvironmentVariable("HADOOP_HOME", installationDir + "/Hadoop", EnvironmentVariableTarget.Machine);

                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                //startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = "/c " + installationDir + "/Hadoop/bin/winutils.exe chmod 777 /tmp/hive";
                process.StartInfo = startInfo;
                process.Start();

            }

            SetPythonPath(installationDir);

        }

        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand)]
        public override void Rollback(IDictionary savedState)
        {
            base.Rollback(savedState);
        }

        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand)]
        public override void Uninstall(IDictionary savedState)
        {
            base.Uninstall(savedState);
        }


        private void SetPythonPath(string installationDir)
        {
            string pysparkPath = @"\interpreter\spark\pyspark";
            string interpreterPath = installationDir + pysparkPath;
            string[] filePaths = Directory.GetFiles(interpreterPath, "*.zip");
            string pySparkFiles = string.Empty;
            if (filePaths.Length == 0)
            {
                return;
            }
            string pythonPath = Environment.GetEnvironmentVariable("PYTHONPATH");
            List<string> pysparkFilePath = new List<string>();

            if (pythonPath != null)
            {
                foreach (string s in filePaths)
                {

                    if (!pythonPath.Contains(s))
                    {
                        pysparkFilePath.Add(s);
                    }
                    
                }
            }
            else
            {
                pysparkFilePath = filePaths.ToList();

            }



            foreach (string s in pysparkFilePath)
            {
                pySparkFiles += s;
                pySparkFiles += ";";
            }

            

            if (pythonPath == null)
            {
                SetEnvironmentVar("PYTHONPATH", pySparkFiles);

            }
            else
            {
                SetEnvironmentVar("PYTHONPATH", pySparkFiles, true);
            }

        }

        private bool SetJavaHome()
        {
            
            string javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
            if (javaHome == null)
            {
                string jdkPath = GetJavaInstallationPath();
                if (string.IsNullOrEmpty(jdkPath))
                {
                    return false;
                }
                SetEnvironmentVar("JAVA_HOME", jdkPath);
                if (!Environment.GetEnvironmentVariable("path").Contains(jdkPath + @"\bin"))
                {
                    SetEnvironmentVar("path", jdkPath + @"\bin", true);
                }
                return true;
            }

            return true;
        }

        private string GetJavaInstallationPath()
        {
            String jreKey = "SOFTWARE\\JavaSoft\\Java Runtime Environment";
            String jdkKey = "SOFTWARE\\JavaSoft\\Java Development Kit";
            string jdkPath = string.Empty;
            if (!string.IsNullOrEmpty(jdkPath = CheckRegistry(jdkKey)))
            {
                return jdkPath;
            }
            else if (!string.IsNullOrEmpty(jdkPath = CheckRegistry(jreKey)))
            {
                return jdkPath;
            }
            else if (!string.IsNullOrEmpty(jdkPath = JDKpath("javac.exe")))
            {
                return jdkPath;
            }
            else if (!string.IsNullOrEmpty(jdkPath = JDKpath("java.exe")))
            {
                return jdkPath;
            }
            else
            {
                return string.Empty;
            }

        }

        private void SetEnvironmentVar(string varName,string varVal,bool append=false)
        {
            if (append)
            {
                varVal = varVal+";"+Environment.GetEnvironmentVariable(varName);
            }

            Environment.SetEnvironmentVariable(varName, varVal, EnvironmentVariableTarget.Machine);
        }

        private string CheckRegistry(string key)
        {
            
            using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(key))
            {
                if (baseKey != null)
                {
                    String currentVersion = baseKey.GetValue("CurrentVersion").ToString();
                    using (var homeKey = baseKey.OpenSubKey(currentVersion))
                        return homeKey.GetValue("JavaHome").ToString();
                }
                
                    
            }
             
                return null;
        }

        private string JDKpath(String exeFile)
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            //startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/c for %i in ("+exeFile+") do @echo.%~$PATH:i";
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();
            string output = process.StandardOutput.ReadToEnd();
            if (string.IsNullOrEmpty(output))
            {
                return null;
            }
            else if (File.Exists(output))
            {
                output = output.Trim();
                FileVersionInfo info = FileVersionInfo.GetVersionInfo(output);
                if (Convert.ToInt16(info.FileVersion.Split('.')[0]) < 7)
                {
                    return null;
                }
                output = output.Replace(@"\bin\"+exeFile, "");
                return output;
            }
            else
            {
                return null;
            }



        }

    }
}
