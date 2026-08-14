using BepInEx.Configuration;
using ChainCrafting.Compatibility;
using Nautilus.Options;
using UnityEngine;

namespace ChainCrafting.Configs
{

    public class CraftingMenu : ModOptions
    {
        private readonly ConfigFile _configFile;
        public static bool OnHoldEnabled = false;
        public static bool ExportResultEnabled = false;
        public static int UpperBound = 5;
        public CraftingMenu(ConfigFile configFile) : base(Language.main.Get("ConfigTab"))
        {
            _configFile = configFile;

            ConfigEntry<bool> onHoldConfig = _configFile.Bind("General.Toggles", Language.main.Get("ConfigOnHold"), false, Language.main.Get("ConfigOnHoldDesc"));
            ModToggleOption OnHold = onHoldConfig.ToModToggleOption();
            OnHold.OnChanged += (sender, ToggleOnChange) => OnHoldEnabled = ToggleOnChange.Value;
            AddItem(OnHold);


            ConfigEntry<float> bulkCraftConfig = _configFile.Bind("General.Sliders", Language.main.Get("ConfigBulkCraft"), 5f, Language.main.Get("ConfigBulkCraftDesc"));
            ModSliderOption CraftCount = bulkCraftConfig.ToModSliderOption(minValue: 1, maxValue: 50, step: 1, floatFormat: "{0:F0}");
            CraftCount.OnChanged += (sender, SliderOnChange) => 
            {
                UpperBound = Mathf.Max(1, (int) SliderOnChange.Value);
                CraftingInputs.CraftCount = Mathf.Min(CraftingInputs.CraftCount, UpperBound);
            };
            AddItem(CraftCount);


            if(Manager.ExternalResources)
            {
                ConfigEntry<float> lockerRangeConfig = _configFile.Bind("General.Sliders", Language.main.Get("ConfigLockerRange"), 30f, Language.main.Get("ConfigLockerRangeDesc"));
                ModSliderOption LockerRange = lockerRangeConfig.ToModSliderOption(minValue: 1, maxValue: 90, step: 1, floatFormat: "{0:F0}");
                LockerRange.OnChanged += (sender, SliderOnChange) => Manager.ExternalResourceRange = SliderOnChange.Value;
                AddItem(LockerRange);
            }
        }
    }
}
