using Rage;

namespace SceneManager.Utils
{
    internal static class DeleteAllPaths
    {
        internal static void Delete()
        {
            PathManager.Paths.ForEach(x => x.Delete());
            PathManager.Paths.Clear();
            Game.LogTrivial($"All paths deleted");
            Game.DisplayNotification($"~o~Scene Manager\n~w~All paths deleted.");
        }
    }
}
