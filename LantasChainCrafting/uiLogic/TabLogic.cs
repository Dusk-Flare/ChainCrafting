using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace ChainCrafting.uiLogic
{
    [HarmonyPatch(typeof(uGUI_PDA))]
    internal class TabLogic
    {
        /*[HarmonyPatch(nameof(uGUI_PDA.Initialize))]
        [HarmonyPrefix]
        private static void PreInitialize()
        {
            if (uGUI_PDA.regularTabs.Contains(Plugin.CraftingHelper)) return;
            uGUI_PDA.regularTabs.Add(Plugin.CraftingHelper);
        }

        [HarmonyPatch(nameof(uGUI_PDA.Initialize))]
        [HarmonyPostfix]
        private static void PostInitialize(uGUI_PDA __instance)
        {
            GameObject logTab = __instance.tabLog.gameObject;
            GameObject helper = GameObject.Instantiate(logTab, __instance.transform.Find("Content"));
            helper.name = "CraftingHelper";
            GameObject.DestroyImmediate(helper.GetComponent<uGUI_LogTab>());
            helper.AddComponent<uGUI_CraftingHelper>();
            //__instance.tabs.Add(Plugin.CraftingHelper, helper.GetComponent<uGUI_PDATab>()); 

            helper.GetComponent<uGUI_CraftingHelper>(); 
        }*/
    }
}
