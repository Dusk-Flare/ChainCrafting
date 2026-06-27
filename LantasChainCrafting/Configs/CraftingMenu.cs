using Nautilus.Options;
using UnityEngine;

namespace ChainCrafting.Configs
{

    public class CraftingMenu : ModOptions
    {
        public static bool OnHoldEnabled = false;
        public static int UpperBound = 5;
        public CraftingMenu() : base(Language.main.Get("ConfigTab"))
        {
            ModToggleOption OnHold = ModToggleOption.Create("OnHold", Language.main.Get("ConfigOnHold"), false, Language.main.Get("ConfigOnHoldDesc"));
            OnHold.OnChanged += (sender, ToggleOnChange) => OnHoldEnabled = ToggleOnChange.Value;
            AddItem(OnHold);
            ModSliderOption CraftCount = ModSliderOption.Create("BulkCraft", Language.main.Get("ConfigBulkCraft"), 1, 50, 5, null, "{0:F0}", 1, Language.main.Get("ConfigBulkCraftDesc"));
            CraftCount.OnChanged += (sender, SliderOnChange) => 
            {
                UpperBound = (int)SliderOnChange.Value;
                CraftingInputs.CraftCount = Mathf.Min(CraftingInputs.CraftCount, UpperBound);
            };
            AddItem(CraftCount);
        }
    }
}
