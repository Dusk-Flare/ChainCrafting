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
            CraftingInputs.CraftCount = 1;
            return false;
        }

        [HarmonyPatch(typeof(uGUI_CraftingMenu))]
        [HarmonyPatch(nameof(uGUI_CraftingMenu.ActionAvailable))]
        [HarmonyPostfix]
        private static void ActionAvailable(uGUI_CraftingMenu __instance, uGUI_CraftingMenu.Node sender, ref bool __result)
        {
            Plugin.Logger.LogInfo($"Checking if aclient is GhostCrafter: {__instance.client is GhostCrafter}, TechType: {sender.techType}, Available: {__result}, Result: {CraftingUI.ActionAvailable(sender)}");
            if (__instance.client is GhostCrafter) __result = CraftingUI.ActionAvailable(sender);
        }
    }
}
