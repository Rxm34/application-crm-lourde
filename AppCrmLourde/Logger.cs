using System;
using System.IO;

namespace AppCrmLourde
{
    public static class Logger
    {
        public static void Log(string message)
        {
            var manager = SessionManager.ManagerConnecte;
            string managerInfo = manager != null ? $"{manager.NomMan} ({manager.MailMan})" : "Manager inconnu";
            string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {managerInfo} : {message}";

            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs.txt");
            System.IO.File.AppendAllText(path, logMessage + Environment.NewLine);
        }
    }

}
