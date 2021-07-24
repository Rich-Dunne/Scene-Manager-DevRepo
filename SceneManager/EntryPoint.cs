using System;
using System.Reflection;
using Rage;
using SceneManager.Utils;
using SceneManager.Menus;
using SceneManager.Managers;

[assembly: Rage.Attributes.Plugin("Scene Manager", Author = "Rich", Description = "Control your scenes with custom AI pathing and traffic barrier management.", PrefersSingleInstance = true)]

namespace SceneManager
{
    [Obfuscation(Exclude = false, Feature = "-rename", ApplyToMembers = false)]
    public class EntryPoint
    {
        [Obfuscation(Exclude = false, Feature = "-rename")]
        internal static void Main()
        {
            if (!DependencyChecker.DependenciesInstalled())
            {
                Game.UnloadActivePlugin();
                return;
            }

            while (Game.IsLoading)
            {
                GameFiber.Yield();
            }

            AppDomain.CurrentDomain.DomainUnload += TerminationHandler;
            Settings.LoadSettings();
            GetAssemblyVersion();
            MenuManager.InitializeMenus();
            Hints.DisplayHintsToOpenMenu();

            GameFiber.StartNew(() => UserInput.HandleKeyPress(), "Handle User Input");

            void GetAssemblyVersion()
            {
                string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                Game.LogTrivial($"Scene Manager V{version} is ready.");
            }
        }

        private static void TerminationHandler(object sender, EventArgs e)
        {
            try
            {
                ExportPathMenu.ExportOnUnload();
            }
            catch(Exception ex)
            {
                Game.LogTrivial($"Autosave error: {ex.Message}");
                Game.DisplayNotification($"~o~Scene Manager ~r~[Error]\n~w~There was a problem autosaving the paths.");
            }
            BarrierMenu.Cleanup();
            PathManager.DeleteAllPaths();

            Game.LogTrivial($"Plugin has shut down.");
        }
    }
}
