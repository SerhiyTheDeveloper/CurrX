using System.IO;

namespace CurrX.Infrastructure
{
    internal static class AppPaths
    {
        public static string GetDatabasePath()
        {
            string localData = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);
            string appFolder = Path.Combine(localData, "CurrX");
            Directory.CreateDirectory(appFolder);
            return Path.Combine(appFolder, "oop-data.db");
        }
    }
}
