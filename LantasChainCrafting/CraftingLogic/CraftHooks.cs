using ChainCrafting.uiLogic;
using ChainCrafting.Utils;
using HarmonyLib;
using UnityEngine;

namespace ChainCrafting.CraftingLogic
{
    [HarmonyPatch]
    public static class CraftHooks
    {
        private static Coroutine CraftRoutine { get; set; }
        [HarmonyPatch(typeof(GhostCrafter))]
        [HarmonyPatch(nameof(GhostCrafter.Craft))]
        [HarmonyPrefix]
        public static bool Craft(GhostCrafter __instance, TechType techType)
        {
            if (!GameModeUtils.RequiresIngredients()) return true;
            CraftRoutine = __instance.StartCoroutine(Logic.Craft(__instance, techType));
            return false;
        }

        [HarmonyPatch(typeof(GhostCrafter))]
        [HarmonyPatch(nameof(GhostCrafter.OnHandHover))]
        [HarmonyPostfix]
        private static void OnHandHover(GhostCrafter __instance, GUIHand hand)
        {
            HandReticle.main.SetText(HandReticle.TextType.Use, "CraftStop", true, GameInput.Button.RightHand);
        }

        [HarmonyPatch(typeof(GhostCrafter))]
        [HarmonyPatch(nameof(GhostCrafter.Initialize))]
        [HarmonyPostfix]
        private static void Initialize(GhostCrafter __instance)
        {
            Interactable interactable = __instance.gameObject.EnsureComponent<Interactable>();
            interactable.RegisterInput(GameInput.Button.RightHand, () =>
            {
                if (CraftRoutine == null) return;
                __instance.StopCoroutine(CraftRoutine);
                __instance.OnStateChanged(false);
                ErrorMessage.AddMessage("Crafting stopped");
            });
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
