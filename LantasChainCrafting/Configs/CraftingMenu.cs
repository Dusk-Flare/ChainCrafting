using Nautilus.Handlers;
using Nautilus.Options;
using System;

namespace ChainCrafting.Configs
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
}
