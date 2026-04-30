using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
            CraftingLogic.IsFuffiled(techType, Inventory.main, ref __result);
        }

        [HarmonyPatch(typeof(CrafterLogic))]
        [HarmonyPatch(nameof(CrafterLogic.ConsumeResources))]
        [HarmonyPrefix]
        public static bool ConsumePrefix(ref bool __result)
        {
            __result = true;
            return false;
        }

        [HarmonyPatch(typeof(Crafter))]
        [HarmonyPatch(nameof(Crafter.Craft))]
        [HarmonyPrefix]
        public static bool Prefix(Crafter __instance, TechType techType)
        {
            if (GameModeUtils.RequiresIngredients()) __instance.StartCoroutine(CraftingLogic.Craft(__instance, techType));
            else return true;
            return false;
        }

        [HarmonyPatch(typeof(uGUI_CraftingMenu))]
        [HarmonyPatch(nameof(uGUI_CraftingMenu.Open))]
        [HarmonyPrefix]
        public static bool Open(CraftTree.Type treeType)
        {
            Plugin.Logger.LogInfo("Loading CraftingTree Type.");
            CraftingLogic.craftingTree = treeType;
            Plugin.Logger.LogInfo($"CraftingTree Type: {CraftingLogic.craftingTree}");
            return true;
        }
    }

    /*public static class ChainCrafter
    {
        [HarmonyPatch(typeof(CrafterLogic))]
        [HarmonyPatch(nameof(CrafterLogic.IsCraftRecipeFulfilled))]
        [HarmonyPostfix]
        public static void Postfix(TechType techType, ref bool __result)
        {
            IsFuffiled(techType, ref __result);
        }

        [HarmonyPatch(typeof(CrafterLogic))]
        [HarmonyPatch(nameof(CrafterLogic.ConsumeResources))]
        [HarmonyPrefix]
        public static bool ConsumePrefix(ref bool __result)
        {
            __result = true;
            return false;
        }

        [HarmonyPatch(typeof(Crafter))]
        [HarmonyPatch(nameof(Crafter.Craft))]
        [HarmonyPrefix]
        public static bool Prefix(Crafter __instance, TechType techType)
        {
            __instance.StartCoroutine(Craft(__instance, techType));
            return false;
        }

        public static void IsFuffiled(TechType techType, ref bool alreadyPassed)
        {
            if (alreadyPassed) return;
            Inventory inventory = Inventory.main;
            ChainCraft(techType, out _, out Dictionary<TechType, int> entry);
            CostOfCraft(entry, out Dictionary<TechType, int> entryCost);
            ValidateRequirement(inventory, entryCost, out alreadyPassed);
        }

        private static void ChainCraft(TechType techType, out Stack<TechType> craftStack, out Dictionary<TechType, int> entry)
        {
            Inventory inventory = Inventory.main;
            Stack<TechType> tempStack = new();
            CrudeStackCraft(techType, ref tempStack);
            OrganizeCraftStack(ref tempStack, out entry, out craftStack);
            RemoveOwned(inventory, ref craftStack, ref entry);
        }

        public static bool Consume(TechType techType)
        {
            if (CrafterLogic.IsCraftRecipeFulfilled(techType))
            {
                Inventory.main.ConsumeResourcesForRecipe(techType, null);
                return true;
            }
            ErrorMessage.AddWarning(Language.main.Get("DontHaveNeededIngredients"));
            return false;
        }

        public static IEnumerator Craft(Crafter crafter, TechType techType)
        {
            ChainCraft(techType, out Stack<TechType> craftStack, out _);

            while (craftStack.Any())
            {
                TechType next = craftStack.Pop();
                TechData.GetCraftTime(next, out float craftTime);

                craftTime = Math.Max(craftTime, 2.7f);
                if (!Consume(next)) continue;
                crafter.logic.Craft(next, craftTime);
                while (crafter.HasCraftedItem()) yield return null;
            }
        }

        private static void CrudeStackCraft(TechType techType, ref Stack<TechType> craftStack)
        {
            if (!CraftTree.IsCraftable(techType)) return;
            craftStack.Push(techType);
            ReadOnlyCollection<Ingredient> ingredients = TechData.GetIngredients(techType);
            foreach (Ingredient ingredient in ingredients)
            {
                TechType type = ingredient.techType;
                if (!CraftTree.IsCraftable(type)) continue;
                int amount = ingredient.amount;
                for (int i = 0; i < amount; i++) CrudeStackCraft(type, ref craftStack);
            }
        }

        private static void OrganizeCraftStack(ref Stack<TechType> craftStack, out Dictionary<TechType, int> entry, out Stack<TechType> uniqueStack)
        {
            Stack<TechType> temp = new();
            entry = new();
            while (craftStack.Any())
            {
                TechType item = craftStack.Pop();
                if(entry.ContainsKey(item)) entry[item] += 1;
                else
                {
                    temp.Push(item);
                    entry[item] = 1;
                }
            }
            uniqueStack = new Stack<TechType>(temp);
            while (temp.Any())
            {
                TechType item = temp.Pop();
                for (int i = 0; i < entry[item]; i++) craftStack.Push(item);
            }
        }

        private static void RemoveOwned(Inventory inventory, ref Stack<TechType> craftStack, ref Dictionary<TechType, int> entry)
        {
            TechType target = craftStack.Last();
            Stack<TechType> temp = new(craftStack);
            Stack<TechType> toRemove = new();
            craftStack.Clear();
            while (temp.Any())
            {
                TechType item = temp.Pop();
                int itemCount = entry[item];
                int itemYield = TechData.GetCraftAmount(item);
                int playerOwned = inventory.GetPickupCount(item);
                int removedCount = Math.Min(itemCount, playerOwned);
                if (item != target && removedCount > 0)
                {
                    entry[item] = (int)Math.Ceiling((float)Math.Max(0, itemCount - removedCount) / itemYield);
                    CrudeStackCraft(item, ref toRemove);
                    OrganizeCraftStack(ref toRemove, out _, out _);
                    while (toRemove.Any())
                    {
                        TechType nextRecipe = toRemove.Pop();
                        if (nextRecipe == item) continue;
                        int count = entry[nextRecipe];
                        int recipeYield = TechData.GetCraftAmount(nextRecipe);
                        if (entry.ContainsKey(nextRecipe)) entry[nextRecipe] = (int)Math.Ceiling((float)Math.Max(0, count - removedCount) / recipeYield);
                    }
                }
                for (int i = 0; i < entry[item]; i++)
                {
                    craftStack.Push(item);
                }
            }
        }

        public static void CostOfCraft(Dictionary<TechType, int> entry, out Dictionary<TechType, int> entryCost)
        {
            entryCost = new();
            foreach (TechType material in entry.Keys)
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
        }

        private static void ValidateRequirement(Inventory inventory, Dictionary<TechType, int> entryCost, out bool valid)
        {
            foreach (TechType material in entryCost.Keys)
            {
                if (entryCost[material] > inventory.GetPickupCount(material))
                {
                    valid = false;
                    return;
                }
            }
            valid = true;
            return;
        }


    }*/
}
