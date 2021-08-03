using Rage;
using SceneManager.Managers;

namespace SceneManager.API
{
    public static class Functions
    {
        /// <summary>
        /// Import paths from the Saved Paths folder and load them into the game world.
        /// </summary>
        /// <param name="fileName">The name of the file containing the path (extension excluded).</param>
        /// <param name="filePath">Specify the path from where the file will be loaded from.</param>
        public static void LoadPathsFromFile(string fileName, string filePath = "")
        {
            if(PathManager.ImportedPaths.ContainsKey(fileName))
            {
                Game.LogTrivial($"A file with that name is already loaded.");
                return;
            }

            var importedPaths = PathManager.ImportPathsFromFile(fileName, filePath);
            PathManager.LoadImportedPaths(importedPaths, fileName);
        }

        /// <summary>
        /// Delete paths loaded from <see cref="LoadPathsFromFile"/>.
        /// </summary>
        public static void DeleteLoadedPaths()
        {
            PathManager.DeleteAllPaths();
            PathManager.ImportedPaths.Clear();
        }
    }
}
