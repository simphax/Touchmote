using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WiiTUIO
{
    class Launcher
    {



        public static void Launch(string relativePath, string file,string arguments, Action callback)
        {
            try
            {
                // create the ProcessStartInfo using "cmd" as the program to be run,
                // and "/c " as the parameters.
                // Incidentally, /c tells cmd that we want it to execute the command that follows,
                // and then exit.
                System.Diagnostics.ProcessStartInfo procStartInfo =
                    new System.Diagnostics.ProcessStartInfo();

                procStartInfo.WorkingDirectory = System.AppDomain.CurrentDomain.BaseDirectory+relativePath+"\\";
                
                procStartInfo.FileName = procStartInfo.WorkingDirectory + file;
                procStartInfo.Arguments = arguments;
                // The following commands are needed to redirect the standard output.
                // This means that it will be redirected to the Process.StandardOutput StreamReader.
                procStartInfo.RedirectStandardOutput = false;
                procStartInfo.UseShellExecute = false;
                // Do not create the black window.
                procStartInfo.CreateNoWindow = false;
                // Now we create a process, assign its ProcessStartInfo and start it
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo = procStartInfo;
                proc.Start();
                // Get the output into a string
                //string result = proc.StandardOutput.ReadToEnd();
                // Display the command output.
                //Console.WriteLine(result);
                proc.WaitForExit();
                if (callback != null)
                {
                    callback();
                }
            }
            catch (Exception objException)
            {
                Console.WriteLine(objException.Message);
                // Log the exception
            }
        }

        public static void RestartComputer()
        {
            System.Diagnostics.ProcessStartInfo procStartInfo =
                    new System.Diagnostics.ProcessStartInfo();

            procStartInfo.RedirectStandardOutput = true;
            procStartInfo.UseShellExecute = false;
            procStartInfo.CreateNoWindow = true;
            procStartInfo.FileName = "shutdown.exe";
            procStartInfo.Arguments = "-r -t 0";
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo = procStartInfo;
            proc.Start();
        }
    }
}
