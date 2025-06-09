using System.Collections.Generic;

namespace ExoLoader
{
    public static class ModLoadingStatus
    {
        private static readonly List<string> loadErrors = new List<string>();

        public static void LogError(string message)
        {
            loadErrors.Add(message);
        }

        public static bool HasErrors()
        {
            return loadErrors.Count > 0;
        }

        public static List<string> GetErrors()
        {
            return [.. loadErrors];
        }

        public static void ClearErrors()
        {
            loadErrors.Clear();
        }
    }
}
