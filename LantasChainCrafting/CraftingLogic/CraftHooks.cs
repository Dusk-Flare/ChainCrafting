using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChainCrafting.CraftingLogic
{
    [HarmonyPatch]
    public static class CraftHooks
    {
        [HarmonyPatch(typeof(CrafterLogic))]
        [HarmonyPatch(nameof(CrafterLogic.IsCraftRecipeFulfilled))]
        [HarmonyPostfix]
        public static void IsCraftRecipeFulfilled(TechType techType, ref bool __result)
        {
            Validate.IsFuffiled(techType, out bool alreadyPassed);
            __result = alreadyPassed;
        }

        [HarmonyPatch(typeof(CrafterLogic))]
        [HarmonyPatch(nameof(CrafterLogic.ConsumeResources))]
        [HarmonyPostfix]
        public static void ConsumeResources(ref bool __result)
        {
            __result = true;
        }

        [HarmonyPatch(typeof(GhostCrafter))]
        [HarmonyPatch(nameof(GhostCrafter.Craft))]
        [HarmonyPrefix]
        public static bool Craft(GhostCrafter __instance, TechType techType)
        {
            if (!GameModeUtils.RequiresIngredients()) return true;
            __instance.StartCoroutine(Logic.Craft(__instance, techType));
            return false;
        }
    }
}
