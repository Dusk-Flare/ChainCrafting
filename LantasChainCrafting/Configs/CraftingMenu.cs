using Nautilus.Options;

namespace ChainCrafting.Configs
{

    public class CraftingMenu : ModOptions
    {
        public static bool OnHoldEnabled = false;
        public CraftingMenu() : base("Chain Options")
        {
            ModToggleOption OnHold = ModToggleOption.Create("OnHold", "Missing Ingredients On Hold", false, "Enabling this option will switch the \"Missing Crafts\" keybind from Toggle to Hold");
            OnHold.OnChanged += (sender, ToggleOnChange) => OnHoldEnabled = ToggleOnChange.Value;
            AddItem(OnHold);
        }
    }
}
