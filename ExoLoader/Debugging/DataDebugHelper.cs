using System;
using System.IO;

namespace ExoLoader.Debugging
{
    public class DataDebugHelper
    {
        private static readonly string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");

        public static void PrintDataError(string name, params string[] messages)
        {
            Directory.CreateDirectory(logFilePath);
            StreamWriter writer = new StreamWriter(Path.Combine(logFilePath, name) + ".txt", false);
            foreach (string message in messages)
            {
                writer.WriteLine(message);
                ModLoadingStatus.LogError(message);
            }
            writer.Close();
        }
    }
}
