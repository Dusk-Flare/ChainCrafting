using ChainCrafting.uiLogic;
using HarmonyLib;
using SextantHorizon.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ChainCrafting.CraftingLogic
{
    [HarmonyPatch]
    public static class CraftHooks
    {
        private static Coroutine CraftRoutine { get; set; }
        public static Queue<Resource> CraftingQueue { get; private set; } = new();

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
            __instance.gameObject.GetComponent<Interactable>()?.OnHandHover(hand);
        }

        [HarmonyPatch(typeof(GhostCrafter))]
        [HarmonyPatch(nameof(GhostCrafter.Initialize))]
        [HarmonyPostfix]
        private static void Initialize(GhostCrafter __instance)
        {
            Interactable interactable = __instance.gameObject.EnsureComponent<Interactable>();
			Plugin.Logger.LogInfo($"Added Interactable to {__instance.gameObject.name}");
            interactable.RegisterInput(GameInput.Button.RightHand, false, () =>
            {
                if (CraftRoutine == null && !CraftingQueue.Any()) return;
                __instance.StopCoroutine(CraftRoutine);
                CraftingQueue.Clear();
                __instance.OnStateChanged(false);
            });
            interactable.RegisterOnHandHover((_) => HandReticle.main.SetText(HandReticle.TextType.Use, "CraftStop", true, GameInput.Button.RightHand));
        }

        [HarmonyPatch(typeof(uGUI_CraftingMenu))]
        [HarmonyPatch(nameof(uGUI_CraftingMenu.ActionAvailable))]
        [HarmonyPostfix]
        private static void ActionAvailable(uGUI_CraftingMenu __instance, uGUI_CraftingMenu.Node sender, ref bool __result)
        {
            if (__instance.client is GhostCrafter) __result = CraftingUI.ActionAvailable(sender, (__instance.client as GhostCrafter).transform.position);
        }
    }
}
