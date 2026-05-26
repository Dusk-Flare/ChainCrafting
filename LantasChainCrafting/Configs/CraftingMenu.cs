using Nautilus.Options;

namespace ChainCrafting.Configs
{

    public class CraftingMenu : ModOptions
    {
        public static bool OnHoldEnabled = false;
        public static int UpperBound = 50;
        public CraftingMenu() : base("Chain Options")
        {
            ModToggleOption OnHold = ModToggleOption.Create("OnHold", "Raw Resources On Hold", false, "Enabling this option will switch the \"Raw Resources\" keybind from Toggle to Hold");
            OnHold.OnChanged += (sender, ToggleOnChange) => OnHoldEnabled = ToggleOnChange.Value;
            AddItem(OnHold);
        }
    }
}
