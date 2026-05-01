using Nautilus.Handlers;
using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChainCrafting
{
    public class CraftingInputs
    {
        public static bool MissingCraft = false;

        public static void toggleCrafts() => MissingCraft = !MissingCraft;

        public static GameInput.Button MissingCrafts = EnumHandler.AddEntry<GameInput.Button>("ToggleMissingCrafts")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.C)
        .WithControllerBinding(GameInputHandler.Paths.Gamepad.DpadLeft)
        .AvoidConflicts(GameInput.Device.Keyboard)
        .WithCategory(PluginInfo.PLUGIN_NAME);
    }
}
