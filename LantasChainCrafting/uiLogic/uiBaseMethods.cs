using ChainCrafting.Configs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChainCrafting.uiLogic
{
    public static class uiBaseMethods
    {
        public static void OnPointerClick(uGUI_BlueprintsTab self,uGUI_BlueprintEntry entry, ref bool __result)
        {
            TechType techType = self.GetTechType(entry);
            Plugin.Logger.LogInfo($"Clicked on {techType}");
            if (self.IsUnlocked(techType))
            {
                uGUI_CraftingHelper.TreeType = techType;
                CraftingInputs.OnCrftingHelperOpen?.Invoke();
            }
            __result = true;
        }
    }
}
