using Rage;

namespace SceneManager.Utils
{
    internal static class TogglePaths
    {
        internal static void Toggle(bool disable)
        {
            if (disable)
            {
                PathManager.Paths.ForEach(x => x.DisablePath());
                Game.LogTrivial($"All paths disabled.");
            }
            else
            {
                PathManager.Paths.ForEach(x => x.EnablePath());
                Game.LogTrivial($"All paths enabled.");
            }
        }
    }
}
