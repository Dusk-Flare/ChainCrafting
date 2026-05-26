using Nautilus.Json;
using Nautilus.Options.Attributes;

namespace ChainCrafting.Configs
{
    [Menu("Chain Options")]
    public class CraftingMenu : ConfigFile
    {
        [Toggle("Raw Resources On Hold", Tooltip = "Enabling this option will switch the \"Raw Resources\" keybind from Toggle to Hold")]
        public bool OnHoldEnabled = false;

        [Slider("Max Bulk Craft Amount", 2, 100, Tooltip = "Sets the maximum amount of items that can be crafted in bulk")]
        public int UpperBound = 48;
    }
}
