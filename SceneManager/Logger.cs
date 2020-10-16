using Rage;

namespace SceneManager
{
    class Logger
    {
        internal static void Log(string message)
        {
#if DEBUG
            Game.LogTrivialDebug($"{message}");
#else
            Game.LogTrivial($"{message}");
#endif
        }
    }
}
