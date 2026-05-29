namespace TheCup_Infrastructure.Enviroment
{
    public class FolderUtilities
    {
        public static string TournamentJsonFilePath(Guid tournamentId)
        {
            string programDataFolder = GetProgramDataFolder();
            return System.IO.Path.Combine(programDataFolder, $"{tournamentId}.json");
        }

        public static string GetProgramDataFolder()
        {
            string programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string appFolderPath = System.IO.Path.Combine(programDataPath, "TheCup");
            if (!System.IO.Directory.Exists(appFolderPath))
            {
                System.IO.Directory.CreateDirectory(appFolderPath);
            }

            return appFolderPath;
        }
    }
}
