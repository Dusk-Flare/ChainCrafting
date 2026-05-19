using ChainCrafting.Configs;
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
        [HarmonyPatch(nameof(TooltipFactory.WriteIngredients))]
        [HarmonyPrefix]
        private static bool WriteIngredients(IList<Ingredient> ingredients, List<TooltipIcon> icons)
        {
            CraftingUI.ConditionalCraftingStatus(ingredients, icons, CraftingInputs.MissingCraft);
            return false;
        }

        [HarmonyPatch(typeof(uGUI_RecipeEntry))]
        [HarmonyPatch(nameof(uGUI_RecipeEntry.UpdateIngredients))]
        [HarmonyPrefix]
        private static bool UpdateIngredients(uGUI_RecipeEntry __instance, ItemsContainer container, bool ping)
        {
            CraftingUI.ConditionalUpdateIngredients(__instance, container, ping, CraftingInputs.MissingCraft);
            return false;
        }

        [HarmonyPatch(typeof(uGUI_PinnedRecipes))]
        [HarmonyPatch(nameof(uGUI_PinnedRecipes.Initialize))]
        [HarmonyPostfix]
        private static void Initialize(uGUI_PinnedRecipes __instance)
        {
            CraftingInputs.OnMissingCraftUpdate += () => __instance.ingredientsDirty = true;
        }

        [HarmonyPatch(typeof(uGUI_PinnedRecipes))]
        [HarmonyPatch(nameof(uGUI_PinnedRecipes.Deinitialize))]
        [HarmonyPostfix]
        private static void Deinitialize(uGUI_PinnedRecipes __instance)
        {
            CraftingInputs.OnMissingCraftUpdate -= () => __instance.ingredientsDirty = true;
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
        private static void Initialize_Postfix(uGUI_PDA __instance)
        {
            GameObject craftTab = GameObject.Instantiate(__instance.tabLog.gameObject, __instance.transform.Find("Content"));
            craftTab.name = "Crafting Helper";
            GameObject.DestroyImmediate(craftTab.GetComponent<uGUI_LogTab>());
            craftTab.AddComponent<uGUI_CraftingHelper>();

            __instance.tabs.Add(Plugin.CraftingHelper, craftTab.GetComponent<uGUI_PDATab>());
        }
    }
}
