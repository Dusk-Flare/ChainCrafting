using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;

namespace ChainCrafting
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public new static ManualLogSource Logger { get; private set; }

        private static Assembly Assembly { get; } = Assembly.GetExecutingAssembly();

        private void Awake()
        {
            Logger = base.Logger;


            Harmony.CreateAndPatchAll(Assembly, $"{PluginInfo.PLUGIN_GUID}");
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
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
