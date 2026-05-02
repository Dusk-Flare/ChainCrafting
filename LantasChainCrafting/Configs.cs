using Nautilus.Handlers;
using Nautilus.Options;
using System;

namespace ChainCrafting
{

    public class CraftingMenu : ModOptions
    {
        public static bool OnHoldEnabled = false;
        public CraftingMenu() : base("Chain Options")
        {

            ModToggleOption OnHold = ModToggleOption.Create("OnHold", "Missing Ingredients On Hold", false, "Enabling this option will switch the \"Missing Crafts\" keybind from Toggle to Hold");
            OnHold.OnChanged += (object sender, ToggleChangedEventArgs ToggleOnChange) =>
            {
               OnHoldEnabled = ToggleOnChange.Value;
            };
            AddItem(OnHold);
        }
    }

    public static class CraftingInputs
    {
        public static bool _MissingCraft = false;

        public static void ToggleCrafts() => MissingCraft = !MissingCraft;

        public static Action OnMissingCraftUpdate;
        public static bool MissingCraft
        {
            get => _MissingCraft;

            set
            {
                if (_MissingCraft != value)
                {
                    _MissingCraft = value;
                    OnMissingCraftUpdate?.Invoke();
                }
            }
        }

        public static GameInput.Button MissingCrafts = EnumHandler.AddEntry<GameInput.Button>("Missing Crafts")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.C)
        .WithControllerBinding(GameInputHandler.Paths.Gamepad.DpadLeft)
        .AvoidConflicts(GameInput.Device.Keyboard)
        .WithCategory(PluginInfo.PLUGIN_NAME);
    }
}
