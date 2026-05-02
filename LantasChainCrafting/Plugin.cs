using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Nautilus.Handlers;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ChainCrafting
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public new static ManualLogSource Logger { get; private set; }

        private static Assembly Assembly { get; } = Assembly.GetExecutingAssembly();

        public static string available = "<color=#94DE00FF>";
        public static string craftable = "<color=#FFE208FF>";
        public static string unavailable = "<color=#DF4026FF>";
        public static Color availableColor = new(0.58f, 0.87f, 0.0f, 1.0f);
        public static Color craftableColor = new(1.0f, 0.886f, 0.031f, 1.0f);
        public static Color unavailableColor = new(0.87f, 0.25f, 0.15f, 1.0f);
        public static CraftingMenu Menu { get; private set; }

        private void Awake()
        {
            Logger = base.Logger;


            Harmony.CreateAndPatchAll(Assembly, $"{PluginInfo.PLUGIN_GUID}");
            OptionsPanelHandler.RegisterModOptions(Menu = new());
            CraftingInputs.OnMissingCraftUpdate += () =>
            {
                Logger.LogInfo($"Missing Craft: {CraftingInputs.MissingCraft}");
            };
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private void Update()
        {
            if(!GameInput.IsInitialized) return;
            if (CraftingMenu.OnHoldEnabled) CraftingInputs.MissingCraft = GameInput.GetButtonHeld(CraftingInputs.MissingCrafts);
            else if (GameInput.GetButtonDown(CraftingInputs.MissingCrafts)) CraftingInputs.ToggleCrafts();
        }


    }

    [HarmonyPatch]
    public static class ChainCrafting
    {
        [HarmonyPatch(typeof(CrafterLogic))]
        [HarmonyPatch(nameof(CrafterLogic.IsCraftRecipeFulfilled))]
        [HarmonyPostfix]
        public static void Validate(TechType techType, ref bool __result)
        {
            CraftingLogic.IsFuffiled(techType, out bool alreadyPassed);
            __result = alreadyPassed;
        }

        [HarmonyPatch(typeof(CrafterLogic))]
        [HarmonyPatch(nameof(CrafterLogic.ConsumeResources))]
        [HarmonyPostfix]
        public static void ConsumePrefix(ref bool __result)
        {
            __result = true;
        }

        [HarmonyPatch(typeof(GhostCrafter))]
        [HarmonyPatch(nameof(GhostCrafter.Craft))]
        [HarmonyPrefix]
        public static bool Prefix(GhostCrafter __instance, TechType techType)
        {
            if (!GameModeUtils.RequiresIngredients()) return true;
            __instance.StartCoroutine(CraftingLogic.Craft(__instance, techType));
            return false;
        }

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
