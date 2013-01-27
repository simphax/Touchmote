using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WiiTUIO
{
    class Launcher
    {

        public static void LaunchAsAdministrator(string file, Action callback)
        {
            try
            {
                // create the ProcessStartInfo using "cmd" as the program to be run,
                // and "/c " as the parameters.
                // Incidentally, /c tells cmd that we want it to execute the command that follows,
                // and then exit.
                System.Diagnostics.ProcessStartInfo procStartInfo =
                    new System.Diagnostics.ProcessStartInfo();

                procStartInfo.WorkingDirectory = System.AppDomain.CurrentDomain.BaseDirectory+"Driver\\";
                
                procStartInfo.FileName = procStartInfo.WorkingDirectory + file;
                Console.WriteLine(procStartInfo.WorkingDirectory);
                Console.WriteLine(procStartInfo.FileName);
                // The following commands are needed to redirect the standard output.
                // This means that it will be redirected to the Process.StandardOutput StreamReader.
                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.UseShellExecute = false;
                // Do not create the black window.
                procStartInfo.CreateNoWindow = true;
                // Now we create a process, assign its ProcessStartInfo and start it
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo = procStartInfo;
                proc.Start();
                // Get the output into a string
                //string result = proc.StandardOutput.ReadToEnd();
                // Display the command output.
                //Console.WriteLine(result);
                proc.WaitForExit();
                callback();
            }
            catch (Exception objException)
            {
                Console.WriteLine(objException.Message);
                // Log the exception
            }
        }

        public static void RestartComputer()
        {
            System.Diagnostics.Process.Start("shutdown.exe", "-r -t 0");
        }
    }
}
