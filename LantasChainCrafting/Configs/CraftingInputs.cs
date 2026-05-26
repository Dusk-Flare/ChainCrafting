using BepInEx.Logging;
using Nautilus.Handlers;
using System;

namespace ChainCrafting.Configs
{
    public static class CraftingInputs
    {
        private static bool _rawResourcesEnabled = false;
        private static int _craftCount = 1;

        public static void ToggleCrafts() => RawResourcesEnabled = !RawResourcesEnabled;

        public static Action OnCrftingHelperOpen;
        public static Action OnRawResourcesUpdate;
        public static Action OnCraftCountUpdate;
        public static bool GhostCrafterOpen { get; set; }
        public static bool RawResourcesEnabled
        {
            get => _rawResourcesEnabled;

            set
            {
                if (_rawResourcesEnabled != value)
                {
                    _rawResourcesEnabled = value;
                    OnRawResourcesUpdate?.Invoke();
                }
            }
        }
        public static int CraftCount
        {
            get => _craftCount;
            set
            {
                if(GhostCrafterOpen || value == 1 || value != _craftCount)
                {
                    if(value >= 1 && value <= Plugin.Config.UpperBound)
                    {
                        _craftCount = value;
                        Plugin.Logger.LogInfo($"Craft count updated to {value}");
                        OnCraftCountUpdate?.Invoke();
                    } 
                }
            }
        }
        public static bool CanUpCraft { get => CraftCount < Plugin.Config.UpperBound;  }
        public static bool CanDownCraft { get => CraftCount > 1; }

        public static void Update()
        {
            if (!GameInput.IsInitialized) return;

            if (GameInput.GetButtonDown(CraftingHelper)) OnCrftingHelperOpen?.Invoke();

            if (Plugin.Config.OnHoldEnabled) RawResourcesEnabled = GameInput.GetButtonHeld(RawResources);
            else if (GameInput.GetButtonDown(RawResources)) ToggleCrafts();

            if (uGUI_Tooltip.visible)
            {
                if (GameInput.GetButtonDown(UpCraft)) CraftCount++;
                if (GameInput.GetButtonDown(DownCraft)) CraftCount--;
            } 
            else CraftCount = 1;
        }

        public static GameInput.Button RawResources = EnumHandler.AddEntry<GameInput.Button>("Raw Resources")
            .CreateInput("Raw Resources", "Toggles the display of base ingredients in the crafting menu and pinned recipes")
            .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.C)
            .WithControllerBinding(GameInputHandler.Paths.Gamepad.DpadLeft)
            .AvoidConflicts(GameInput.Device.Keyboard)
            .WithCategory(PluginInfo.PLUGIN_NAME);

        public static GameInput.Button CraftingHelper = EnumHandler.AddEntry<GameInput.Button>("Crafting Helper")
            .CreateInput("Crafting Helper", "Opens the crafting helper menu on your PDA")
            .WithKeyboardBinding(GameInputHandler.Paths.Mouse.MiddleButton)
            .WithControllerBinding(GameInputHandler.Paths.Gamepad.RightStick)
            .AvoidConflicts(GameInput.Device.Keyboard)
            .WithCategory(PluginInfo.PLUGIN_NAME);

        public static GameInput.Button UpCraft = EnumHandler.AddEntry<GameInput.Button>("UpCraft")
            .CreateInput("Increase Craft", "Increases the amount of bulk crafted items")
            .WithKeyboardBinding(GameInputHandler.Paths.Mouse.ScrollUp)
            .WithControllerBinding(GameInputHandler.Paths.Gamepad.DpadUp)
            .AvoidConflicts(GameInput.Device.Keyboard)
            .WithCategory(PluginInfo.PLUGIN_NAME);

        public static GameInput.Button DownCraft = EnumHandler.AddEntry<GameInput.Button>("DownCraft")
            .CreateInput("Lower Craft", "Lowers the amount of bulk crafted items")
            .WithKeyboardBinding(GameInputHandler.Paths.Mouse.ScrollDown)
            .WithControllerBinding(GameInputHandler.Paths.Gamepad.DpadDown)
            .AvoidConflicts(GameInput.Device.Keyboard)
            .WithCategory(PluginInfo.PLUGIN_NAME);
    }
}
