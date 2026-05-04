using Nautilus.Handlers;
using System;

namespace ChainCrafting.Configs
{
    public static class CraftingInputs
    {
        public static bool _MissingCraft = false;

        public static void ToggleCrafts() => MissingCraft = !MissingCraft;

        public static Action OnCrftingHelperOpen;
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

        public static void Update()
        {
            if (!GameInput.IsInitialized) return;
            if (GameInput.GetButtonDown(CraftingHelper)) OnCrftingHelperOpen?.Invoke();
            if (CraftingMenu.OnHoldEnabled) MissingCraft = GameInput.GetButtonHeld(MissingCrafts);
            else if (GameInput.GetButtonDown(MissingCrafts)) ToggleCrafts();
        }


        public static GameInput.Button MissingCrafts = EnumHandler.AddEntry<GameInput.Button>("Missing Crafts")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.C)
        .WithControllerBinding(GameInputHandler.Paths.Gamepad.DpadLeft)
        .AvoidConflicts(GameInput.Device.Keyboard)
        .WithCategory(PluginInfo.PLUGIN_NAME);

        public static GameInput.Button CraftingHelper = EnumHandler.AddEntry<GameInput.Button>("Crafting Helper")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Mouse.MiddleButton)
        .WithControllerBinding(GameInputHandler.Paths.Gamepad.RightStick)
        .AvoidConflicts(GameInput.Device.Keyboard)
        .WithCategory(PluginInfo.PLUGIN_NAME);
    }
}
