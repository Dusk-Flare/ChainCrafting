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
using static MainMenuLoadButton;

namespace ChainCrafting
{
    public record Resource(TechType Type, int Amount)
    {
        public int Yield => TechData.GetCraftAmount(Type);
    }

    public static class CraftingLogic
    {
        public static CraftTree.Type craftingTree;
        public static IEnumerator Craft(Crafter crafter, TechType techType)
        {
            ChainCraft(techType, Inventory.main, out Stack<Resource> craftStack);

            while (craftStack.Any())
            {
                Resource item = craftStack.Pop();
                TechType next = item.Type;
                for (int i = 0; i < item.Amount; i++)
                {
                    TechData.GetCraftTime(next, out float craftTime);
                    craftTime = Math.Max(craftTime, 2.7f);
                    if (!Consume(next)) continue;
                    crafter._logic.Craft(next, craftTime);
                    while (crafter.HasCraftedItem()) yield return null;
                }
            }
        }

        public static void IsFuffiled(TechType techType, Inventory inventory, ref bool alreadyPassed)
        {
            Plugin.Logger.LogInfo($"Checking if crafting {techType} is fulfilled.");
            if (alreadyPassed) return;
            ChainCraft(techType, inventory, out Stack<Resource> craftStack);
            CostOfCraft(craftStack, out Dictionary<TechType, int> entryCost);
            ValidateCraft(inventory, entryCost, out bool result);
            alreadyPassed = result;
        }


        public static void ChainCraft(TechType resource, Inventory inventory, out Stack<Resource> craftStack)
        {
            craftStack = new Stack<Resource>();
            CreateStack(resource, ref craftStack, 1);
            OrganizeCraftStack(ref craftStack);
            RemoveOwned(ref craftStack, inventory);
        }


        public static void GetRequirements(Resource resource, out Stack<Resource> stack)
        {
            stack = new Stack<Resource>();
            CreateStack(resource.Type, ref stack, resource.Amount);
            OrganizeCraftStack(ref stack);
        }

        public static void CreateStack(TechType recipe, ref Stack<Resource> stack, int amount)
        {
            if (recipe == TechType.None) return;
            if (!CraftTree.IsCraftable(recipe) || IsInCraftingTree(recipe)) return;
            stack.Push(new Resource(recipe, amount));
            ReadOnlyCollection<Ingredient> component = TechData.GetIngredients(recipe);
            foreach (Ingredient ingredient in component) CreateStack(ingredient.techType, ref stack, ingredient.amount * amount);
        }

        public static void OrganizeCraftStack(ref Stack<Resource> craftStack)
        {
            Dictionary<TechType, int> catalog = new();
            Stack<Resource> tempStack = new();
            while (craftStack.Any())
            {
                Resource resource = craftStack.Pop();
                if (!catalog.ContainsKey(resource.Type))
                {
                    catalog.Add(resource.Type, resource.Amount);
                    tempStack.Push(resource);
                }
                else catalog[resource.Type] += resource.Amount;
            }
            while (tempStack.Any())
            {
                TechType resource = tempStack.Pop().Type;
                craftStack.Push(new Resource(resource, catalog[resource]));
            }
        }

        public static void RemoveOwned(ref Stack<Resource> craftStack, Inventory inventory)
        {
            Dictionary<TechType, int> catalog = new();
            Stack<Resource> tempStack = new();
            TechType target = craftStack.Last().Type;
            while (craftStack.Any())
            {
                Resource item = craftStack.Pop();
                int owned = inventory.GetPickupCount(item.Type);
                int removedCount = Math.Min(item.Amount, owned);
                int itemYield = item.Yield;
                catalog[item.Type] = (int)Math.Ceiling((float)Math.Max(0, item.Amount - removedCount) / itemYield);
                GetRequirements(item, out Stack<Resource> requirements);
                if (item.Type != target && removedCount > 0)
                {
                    while (requirements.Any())
                    {
                        Resource requirement = requirements.Pop();
                        TechType type = requirement.Type;
                        if (catalog.ContainsKey(type)) catalog[type] = (int)Math.Ceiling((float)Math.Max(0, requirement.Amount - removedCount) / requirement.Yield);
                    }
                }
                tempStack.Push(item);
            }
            while (tempStack.Any())
            {
                TechType item = tempStack.Pop().Type;
                craftStack.Push(new Resource(item, catalog[item]));
            }
        }

        public static void CostOfCraft(Stack<Resource> craftStack, out Dictionary<TechType, int> entryCost)
        {
            entryCost = new();
            foreach (Resource resource in craftStack)
            {
                int materialCount = resource.Amount;
                if (materialCount <= 0) continue;

                ReadOnlyCollection<Ingredient> ingredients = TechData.GetIngredients(resource.Type);
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

        public static bool IsInCraftingTree(TechType techType)
        {
            if (craftingTree is CraftTree.Type.None) return false;
            CraftTree tree = CraftTree.GetTree(craftingTree);
            if (tree?.nodes == null) return false;
            foreach (CraftNode node in tree.nodes)
            {
                if (node.techType0 == techType && CraftTree.IsCraftable(techType)) return true;
            }
            return false;
        }

        private static void ValidateCraft(Inventory inventory, Dictionary<TechType, int> entryCost, out bool valid)
        {
            Plugin.Logger.LogInfo("Validating craft requirements.");
            foreach (TechType material in entryCost.Keys)
            {
                Plugin.Logger.LogInfo($"Checking material: {material}. Required: {entryCost[material]}, Owned: {inventory.GetPickupCount(material)}");
                if (entryCost[material] > inventory.GetPickupCount(material))
                {
                    Plugin.Logger.LogInfo($"Player does not have enough {material} to craft the target item. Required: {entryCost[material]}, Owned: {inventory.GetPickupCount(material)}");
                    valid = false;
                    return;
                }
            }
            valid = true;
            return;
        }
    }
}
