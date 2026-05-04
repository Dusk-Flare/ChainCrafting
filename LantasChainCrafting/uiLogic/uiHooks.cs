using ChainCrafting.Configs;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
