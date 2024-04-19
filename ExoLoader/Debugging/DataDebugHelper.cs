using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExoLoader.Debugging
{
    public class DataDebugHelper
    {
        private static readonly string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        private static readonly string logFileName = "ExoLoader error log.txt";

        public static void PrintDataError(string name, params string[] messages) 
        {
            StreamWriter writer = new StreamWriter(Path.Combine(logFilePath, name) + ".txt", false);
            foreach (string message in messages)
            {
                writer.WriteLine(message);
            }
            writer.Close();
        }
    }
}
