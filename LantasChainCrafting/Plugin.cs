using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace LantasChainCrafting
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public new static ManualLogSource Logger { get; private set; }

        private static Assembly Assembly { get; } = Assembly.GetExecutingAssembly();

        private void Awake()
        {
            // plugin startup logic
            Logger = base.Logger;

            // register harmony patches, if there are any
            Harmony.CreateAndPatchAll(Assembly, $"{PluginInfo.PLUGIN_GUID}");
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }


    }

    [HarmonyPatch(typeof(Crafter), nameof(Crafter.Craft))]
    public class ChainCrafter
    {
        [HarmonyPrefix]
        public static bool Prefix(Crafter __instance, TechType techType, float duration)
        {
            ChainCrafter chain = new();
            return chain.OnCraft(__instance, techType, duration);
        }
        public bool OnCraft(Crafter crafter, TechType techType, float duration)
        {
            Stack<TechType> craftStack = new();
            ChainCraft(techType, craftStack);
            if (!validateRequirement(craftStack, techType)) return false;

            while (craftStack.Any())
            {
                TechType next = craftStack.Pop();
                TechData.GetCraftTime(next, out duration);
                if (next != techType) crafter.Craft(next, duration);
            }
            return true;
        }

        private void ChainCraft(TechType techType, Stack<TechType> craftStack)
        {
            if (!CraftTree.IsCraftable(techType)) return;
            craftStack.Push(techType);
            ReadOnlyCollection<Ingredient> ingredients = TechData.GetIngredients(techType);
            foreach (Ingredient ingredient in ingredients)
            {
                TechType type = ingredient.techType;
                if (!CraftTree.IsCraftable(type)) continue;
                int amount = ingredient.amount;
                for (int i = 0; i < amount; i++) ChainCraft(type, craftStack);
            }
        }

        private bool validateRequirement(Stack<TechType> craftStack, TechType target)
        {
            Inventory main = Inventory.main;
            Stack<TechType> temp = new();
            Stack<TechType> toRemove = new();
            Dictionary<TechType, int> entry = new();
            Dictionary<TechType, int> entryCost = new();
            while (craftStack.Any())
            {
               TechType nextRecipe = craftStack.Pop();
                if (entry.ContainsKey(nextRecipe)) entry[nextRecipe] += 1;
                else 
                {
                    temp.Push(nextRecipe); 
                    entry[nextRecipe] = 1; 
                }
            }
            while (temp.Any())
            {
                TechType item = temp.Pop();
                int itemCount = entry[item];
                int itemYield = TechData.GetCraftAmount(item);
                int playerOwned = main.GetPickupCount(item);
                int removedCount = Math.Min(itemCount, playerOwned);
                if(item != target && removedCount > 0)
                {
                    entry[item] = (int)Math.Ceiling((float)Math.Max(0, itemCount - removedCount) / itemYield);
                    ChainCraft(item, toRemove);
                    while (toRemove.Any())
                    {
                        TechType nextRecipe = toRemove.Pop();
                        if (nextRecipe == item) continue;
                        int count = entry[nextRecipe];
                        int recipeYield = TechData.GetCraftAmount(nextRecipe);
                        if (entry.ContainsKey(nextRecipe)) entry[nextRecipe] = (int)Math.Ceiling((float)Math.Max(0, count - removedCount)/recipeYield);
                    }
                }
                for (int i = 0; i < entry[item]; i++)
                { 
                    craftStack.Push(item); 
                }
            }
            foreach(TechType material in entry.Keys)
            {
                int materialCount = entry[material];
                if (materialCount <= 0) continue;

                ReadOnlyCollection<Ingredient> ingredients = TechData.GetIngredients(material);
                foreach (Ingredient ingredient in ingredients)
                {
                    TechType type = ingredient.techType;
                    if (CraftTree.IsCraftable(type)) continue;
                    int amount = ingredient.amount * materialCount;
                    if (entryCost.ContainsKey(type)) entryCost[type] += amount;
                    else entryCost[type] = amount;
                }
            }
            foreach (TechType material in entryCost.Keys)
            {
                if (entryCost[material] > main.GetPickupCount(material)) return false;
            }
            return true;
        }
    }
}
