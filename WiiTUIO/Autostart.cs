using Microsoft.Win32.TaskScheduler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiiTUIO
{
    class Autostart
    {

        public static bool IsAutostart()
        {
            using (TaskService ts = new TaskService())
            {
                Microsoft.Win32.TaskScheduler.Task task = ts.GetTask("Touchmote");
                return task != null;
            }
        }

        public static bool SetAutostart()
        {
            // Get the service on the local machine
            using (TaskService ts = new TaskService())
            {
                TaskDefinition td = ts.NewTask();
                td.RegistrationInfo.Description = "Autostart Touchmote";

                td.Triggers.Add(new LogonTrigger());

                td.Actions.Add(new ExecAction(System.AppDomain.CurrentDomain.BaseDirectory + "Touchmote.exe", null, System.AppDomain.CurrentDomain.BaseDirectory));
                td.Settings.MultipleInstances = TaskInstancesPolicy.StopExisting;
                td.Principal.RunLevel = TaskRunLevel.Highest;

                ts.RootFolder.RegisterTaskDefinition(@"Touchmote", td);

                return true;

                //ts.RootFolder.DeleteTask("Touchmote");

            }
        }

        public static bool UnsetAutostart()
        {
            // Get the service on the local machine
            using (TaskService ts = new TaskService())
            {
                ts.RootFolder.DeleteTask("Touchmote");
                return true;
            }
        }
    }
}
