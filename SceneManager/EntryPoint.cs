using System;
using System.Reflection;
using Rage;
using SceneManager.Utils;
using SceneManager.Menus;

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
            BarrierMenu.Cleanup();
            DeleteAllPaths.Delete();

            Game.LogTrivial($"Plugin has shut down.");
            Game.DisplayNotification($"~o~Scene Manager ~r~[Terminated]\n~w~The plugin has shut down.");
        }
    }
}
