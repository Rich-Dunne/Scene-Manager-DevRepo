using Rage;
using Rage.Native;

namespace SceneManager 
{
    internal static class PNWUserInput
    {
        public static string GetUserInput(string windowTitle, string defaultText, int maxLength)
        {
            NativeFunction.Natives.DISABLE_ALL_CONTROL_ACTIONS(2);

            NativeFunction.Natives.DISPLAY_ONSCREEN_KEYBOARD(true, windowTitle, 0, defaultText, 0, 0, 0, maxLength);
            Game.DisplayHelp("Enter the filename you would like to save your path as\n~INPUT_FRONTEND_ACCEPT~    Export path\n~INPUT_FRONTEND_CANCEL~    Cancel", true);
            Game.DisplaySubtitle(windowTitle, 100000);

            while (NativeFunction.Natives.UPDATE_ONSCREEN_KEYBOARD<int>() == 0)
            {
                GameFiber.Yield();
            }

            NativeFunction.Natives.ENABLE_ALL_CONTROL_ACTIONS(2);
            Game.DisplaySubtitle("", 5);
            Game.HideHelp();

            return NativeFunction.Natives.GET_ONSCREEN_KEYBOARD_RESULT<string>();
        }
    }
}