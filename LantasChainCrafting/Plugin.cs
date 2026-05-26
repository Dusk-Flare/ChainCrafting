using BepInEx;
using BepInEx.Logging;
using ChainCrafting.Configs;
using ChainCrafting.CraftingLogic;
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
        // public static CraftingMenu Menu { get; private set; 
        public static TechType tempType;
        public static PDATab CraftingHelper;
        internal new static CraftingMenu Config = OptionsPanelHandler.RegisterModOptions<CraftingMenu>();
        private void Awake()
        {
            Logger = base.Logger;


            Harmony.CreateAndPatchAll(Assembly, $"{PluginInfo.PLUGIN_GUID}");
            CraftingHelper = EnumHandler.AddEntry<PDATab>("CraftingHelper");
            CraftingInputs.OnCrftingHelperOpen += () =>
            {
                PDA pda = Player.main.GetPDA();
                if (pda == null) return;
                if(pda.isOpen) 
                { 
                    Logger.LogInfo("PDA is already open, opening Crafting Helper tab");
                    if(tempType != TechType.None)
                    { 
                        uGUI_CraftingHelper.TreeType = tempType; 
                    }
                    if(pda.ui.currentTabType != CraftingHelper) pda.ui.OpenTab(CraftingHelper);
                    else pda.Close();
                }
                else
                {
                    Logger.LogInfo("PDA is not open, opening Crafting Helper tab");
                    pda.Open(CraftingHelper);
                }
            };
            CraftingInputs.OnRawResourcesUpdate += () =>
            {
                Logger.LogInfo($"Missing Craft: {CraftingInputs.RawResourcesEnabled}");
            };
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private void Update()
        {
            CraftingInputs.Update();
        }


    }
}
