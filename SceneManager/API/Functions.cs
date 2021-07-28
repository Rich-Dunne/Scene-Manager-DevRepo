namespace SceneManager.API
{
    public static class Functions
    {
        /// <summary>
        /// Import paths from the Saved Paths folder and load them into the game world.
        /// </summary>
        /// <param name="fileName">The name of the file containing the path (extension excluded).</param>
        public static void LoadPaths(string fileName)
        {
            var importedPaths = Managers.PathManager.ImportPathsFromFile(fileName);
            Managers.PathManager.LoadImportedPaths(importedPaths, fileName);
        }
    }
}
