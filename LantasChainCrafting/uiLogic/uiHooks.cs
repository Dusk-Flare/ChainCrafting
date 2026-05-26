using ChainCrafting.Configs;
using ChainCrafting.CraftingLogic;
using HarmonyLib;
using Nautilus.Handlers;
using System.Collections.Generic;
using UnityEngine;

namespace ChainCrafting.uiLogic
{

    [HarmonyPatch]
    internal class uiHooks
    {
        [HarmonyPatch(typeof(TooltipFactory))]
        [HarmonyPatch(nameof(TooltipFactory.CraftRecipe))]
        [HarmonyPrefix]
        private static bool CraftRecipe(TechType techType, bool locked, TooltipData data)
        {
            uiBaseMethods.CraftRecipe(techType, locked, data);
            return false;
        }

        [HarmonyPatch(typeof(uGUI_RecipeEntry))]
        [HarmonyPatch(nameof(uGUI_RecipeEntry.UpdateIngredients))]
        [HarmonyPrefix]
        private static bool UpdateIngredients(uGUI_RecipeEntry __instance, ItemsContainer container, bool ping)
        {
            CraftingUI.ConditionalUpdateIngredients(__instance, container, ping, CraftingInputs.RawResourcesEnabled);
            return false;
        }

        [HarmonyPatch(typeof(uGUI_PinnedRecipes))]
        [HarmonyPatch(nameof(uGUI_PinnedRecipes.Initialize))]
        [HarmonyPostfix]
        private static void Initialize(uGUI_PinnedRecipes __instance)
        {
            CraftingInputs.OnRawResourcesUpdate += () => __instance.ingredientsDirty = true;
        }

        [HarmonyPatch(typeof(uGUI_PinnedRecipes))]
        [HarmonyPatch(nameof(uGUI_PinnedRecipes.Deinitialize))]
        [HarmonyPostfix]
        private static void Deinitialize(uGUI_PinnedRecipes __instance)
        {
            CraftingInputs.OnRawResourcesUpdate -= () => __instance.ingredientsDirty = true;
        }

        [HarmonyPatch(typeof(uGUI_BlueprintsTab))]
        [HarmonyPatch(nameof(uGUI_BlueprintsTab.OnPointerClick))]
        [HarmonyPrefix]
        private static bool OnPointerClick(int button)
        {
            string myInput0 = GameInput.GetBinding(GameInput.PrimaryDevice, CraftingInputs.CraftingHelper, GameInput.BindingSet.Primary);
            string myInput1 = GameInput.GetBinding(GameInput.PrimaryDevice, CraftingInputs.CraftingHelper, GameInput.BindingSet.Secondary);
            string binding = GameInputHandler.Paths.Mouse.MiddleButton;
            return !((binding == myInput0 || binding == myInput1) && button == 2);
        }

        [HarmonyPatch(typeof(uGUI_BlueprintsTab))]
        [HarmonyPatch(nameof(uGUI_BlueprintsTab.OnPointerEnter))]
        [HarmonyPrefix]
        public static void OnPointerEnter(uGUI_BlueprintsTab __instance, uGUI_BlueprintEntry entry)
        {
            TechType type = __instance.GetTechType(entry);
            Plugin.Logger.LogInfo($"Hovering over {type}");
            Plugin.tempType = type;
        }

        [HarmonyPatch(typeof(uGUI_BlueprintsTab))]
        [HarmonyPatch(nameof(uGUI_BlueprintsTab.OnPointerExit))]
        [HarmonyPrefix]
        public static void OnPointerExit() => Plugin.tempType = TechType.None;

        [HarmonyPatch(typeof(uGUI_PDA))]
        [HarmonyPatch(nameof(uGUI_PDA.Initialize))]
        [HarmonyPostfix]
        private static void PDAInitialize(uGUI_PDA __instance)
        {
            GameObject craftTab = GameObject.Instantiate(__instance.tabLog.gameObject, __instance.transform.Find("Content"));
            craftTab.name = "Crafting Helper";
            GameObject.DestroyImmediate(craftTab.GetComponent<uGUI_LogTab>());
            craftTab.AddComponent<uGUI_CraftingHelper>();

            __instance.tabs.Add(Plugin.CraftingHelper, craftTab.GetComponent<uGUI_PDATab>());
        }

        [HarmonyPatch(typeof(uGUI_CraftingMenu))]
        [HarmonyPatch(nameof(uGUI_CraftingMenu.Open))]
        [HarmonyPostfix]
        private static void Open(uGUI_CraftingMenu __instance)
        {
            CraftingInputs.OnCraftCountUpdate += () => __instance.isDirty = true;
            bool open = __instance.client is GhostCrafter;
            CraftingInputs.GhostCrafterOpen = open;
            if(!open) CraftingInputs.CraftCount = 1;
        }

        [HarmonyPatch(typeof(uGUI_CraftingMenu))]
        [HarmonyPatch(nameof(uGUI_CraftingMenu.Close))]
        [HarmonyPostfix]
        private static void Close(uGUI_CraftingMenu __instance)
        {
            CraftingInputs.OnCraftCountUpdate -= () => __instance.isDirty = true;
            CraftingInputs.GhostCrafterOpen = false;
        }
    }
}
