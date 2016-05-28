using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Linq;
using System.Net;

namespace zeppelin
{
    /// <summary>
    ///Runs zeppelin cmd file and zeppelin web UI cmd file
    /// </summary>
    class Program
    {
        static string startCmd = "/C start ";
        static string terminateCmd = "/C ";
        static string zepServerCmdFile = "mc-zeppelin.cmd";
        static string zepWebUiCmdFile = "startZepplinWebUI.cmd";
        static string cmdRunner = "cmd";
        static string zepEnvHome = "ZEPPELIN_HOME";
        static string binDir = @"\bin";

        static void Main()
        {
            string zeppelinHome = Environment.GetEnvironmentVariable(zepEnvHome);
            string zepBindir = string.Format(zeppelinHome+binDir);
            if (!CheckPath(Path.Combine(zepBindir, zepServerCmdFile)))
            {
                Console.WriteLine("File Not Found : "+ Path.Combine(zepBindir, zepServerCmdFile));
                Console.ReadKey();
                return;
            }

            if (!CheckPath(Path.Combine(zepBindir, zepWebUiCmdFile)))
            {
                Console.WriteLine("File Not Found : " + Path.Combine(zepBindir, zepWebUiCmdFile));
                Console.ReadKey();
                return;
            }

            if (!CheckForRunningService(cmdRunner, zepServerCmdFile))
            {
                Console.WriteLine("Starting Zeppelin Server...");
                StartProcess(zepBindir, startCmd + zepServerCmdFile);
                //Thread.Sleep(5000);
                if (!CheckServerStarted(zepBindir, zepWebUiCmdFile))
                {
                    Console.WriteLine("Error: Couldn't start Server!");
                    Console.ReadKey();
                    return;
                }

            }
            Console.WriteLine("Starting Zeppelin Web UI...");
            StartProcess(zepBindir, terminateCmd + zepWebUiCmdFile, ProcessWindowStyle.Hidden);
           
        }

        static bool CheckServerStarted(string zepBindir,string zepWebUiCmdFile)
        {
            string url = File.ReadLines(Path.Combine(zepBindir, zepWebUiCmdFile)).First().Split(' ')[1];
            int i = 0;
            while(i<20)
            {
                try {
                    WebRequest request = WebRequest.Create(url);
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    if (response != null || response.StatusCode == HttpStatusCode.OK)
                    {
                        return true;
                    }
                }
                catch
                {
                    
                }
                Thread.Sleep(3000);
            }
            return false;
        }

        /// <summary>
        /// Checks File/Folder exist
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        static bool CheckPath(string path)
        {
            if (File.Exists(path))
            {
                return true;
            }
            return false; 

        }

        /// <summary>
        /// Checks existing process
        /// </summary>
        /// <param name="processName"></param>
        /// <param name="cmdFileName"></param>
        /// <returns></returns>
        static bool CheckForRunningService(string processName,string cmdFileName)
        {
            Process[] processes = Process.GetProcessesByName(cmdRunner);
            foreach (Process p in processes)
            {
                if (p.MainWindowTitle.EndsWith(zepServerCmdFile))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Starts process
        /// </summary>
        /// <param name="location"></param>
        /// <param name="arg"></param>
        /// <param name="style"></param>
        static void StartProcess(string location, string arg ,ProcessWindowStyle style=ProcessWindowStyle.Normal)
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo.WorkingDirectory = location;
            process.StartInfo.WindowStyle = style;
            process.StartInfo.FileName = cmdRunner;
            process.StartInfo.Arguments = arg;
           

            process.Start();
           
        }        



        
    }
}
