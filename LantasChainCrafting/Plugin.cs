using BepInEx;
using BepInEx.Logging;
using ChainCrafting.Configs;
using ChainCrafting.uiLogic;
using HarmonyLib;
using Nautilus.Handlers;
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
        public static PDATab CraftingHelper;
        private void Awake()
        {
            Logger = base.Logger;


            Harmony.CreateAndPatchAll(Assembly, $"{PluginInfo.PLUGIN_GUID}");
            OptionsPanelHandler.RegisterModOptions(Menu = new());
            uGUI_CraftingHelper crafthelper = new();
            crafthelper.Awake();
            CraftingHelper = EnumHandler.AddEntry<PDATab>("CraftingHelper");
            CraftingInputs.OnCrftingHelperOpen += () =>
            {
                crafthelper.Open();
            };
            CraftingInputs.OnMissingCraftUpdate += () =>
            {
                Logger.LogInfo($"Missing Craft: {CraftingInputs.MissingCraft}");
            };
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private void Update()
        {
            CraftingInputs.Update();
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
    }
}
