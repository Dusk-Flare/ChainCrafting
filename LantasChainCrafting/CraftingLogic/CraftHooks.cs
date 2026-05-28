using ChainCrafting.Configs;
using ChainCrafting.uiLogic;
using HarmonyLib;

namespace ChainCrafting.CraftingLogic
{
    [HarmonyPatch]
    public static class CraftHooks
    {
        [HarmonyPatch(typeof(GhostCrafter))]
        [HarmonyPatch(nameof(GhostCrafter.Craft))]
        [HarmonyPrefix]
        public static bool Craft(GhostCrafter __instance, TechType techType)
        {
            if (!GameModeUtils.RequiresIngredients()) return true;
            __instance.StartCoroutine(Logic.Craft(__instance, techType));
            return false;
        }

        [HarmonyPatch(typeof(uGUI_CraftingMenu))]
        [HarmonyPatch(nameof(uGUI_CraftingMenu.ActionAvailable))]
        [HarmonyPostfix]
        private static void ActionAvailable(uGUI_CraftingMenu __instance, uGUI_CraftingMenu.Node sender, ref bool __result)
        {
            if (__instance.client is GhostCrafter) __result = CraftingUI.ActionAvailable(sender);
        }
    }
}
