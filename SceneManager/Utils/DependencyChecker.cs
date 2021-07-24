using Rage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace SceneManager.Utils
{
    internal class DependencyChecker
    {
        internal static bool DependenciesInstalled()
        {
            if (!InputManagerChecker() || !CheckRNUIVersion() || !IniFilePresent())
            {
                return false;
            }

            return true;
        }

        private static bool CheckRNUIVersion()
        {
            var directory = Directory.GetCurrentDirectory();
            var exists = File.Exists(directory + @"\RAGENativeUI.dll");
            if (!exists)
            {
                Game.LogTrivial($"RNUI was not found in the user's GTA V directory.");
                Game.DisplayNotification($"~o~Scene Manager ~r~[Error]\n~w~RAGENativeUI.dll was not found in your GTA V directory.  Please install RAGENativeUI and try again.");
                return false;
            }

            var userVersion = Assembly.LoadFrom(directory + @"\RAGENativeUI.dll").GetName().Version;
            Version requiredMinimumVersion = new Version("1.7.0.0");
            if (userVersion >= requiredMinimumVersion)
            {
                Game.LogTrivial($"User's RNUI version: {userVersion}");
                return true;
            }
            else
            {
                Game.DisplayNotification($"~o~Scene Manager~r~[Error]\n~w~Your RAGENativeUI.dll version is below 1.7.  Please update RAGENativeUI and try again.");
                return false;
            }

        }

        private static bool InputManagerChecker()
        {
            var directory = Directory.GetCurrentDirectory();
            var exists = File.Exists(directory + @"\InputManager.dll");
            if (!exists)
            {
                Game.LogTrivial($"InputManager was not found in the user's GTA V directory.");
                Game.DisplayNotification($"~o~Scene Manager ~r~[Error]\n~w~InputManager.dll was not found in your GTA V directory.  Please install InputManager.dll and try again.");
                return false;
            }
            return true;
        }

        private static bool IniFilePresent()
        {
            var exists = File.Exists("Plugins/SceneManager.ini");
            if (!exists)
            {
                Game.LogTrivial($"SceneManager.ini was not found in the Plugins folder.");
                Game.DisplayNotification($"~o~Scene Manager ~r~[Error]\n~w~SceneManager.ini was not found in your Plugins folder.  Please install SceneManager.ini and try again.");
            }
            return exists;
        }
    }
}
