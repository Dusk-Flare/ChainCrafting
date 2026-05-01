using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace ChainCrafting
{
    public record Resource(TechType Type, int Amount)
    {
        public int Yield => TechData.GetCraftAmount(Type);
    }

    public static class CraftingLogic
    {
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

        public static void ConditionalCraftingStatus(IList<Ingredient> ingredients, List<TooltipIcon> icons, bool displayMissing)
        {
            if (!displayMissing)
            { 
                GetCraftingStatus(ingredients, icons); 
                return; 
            }
            Stack<Resource> stack = new();
            foreach (Ingredient ingredient in ingredients)
            {
                TechType techType = ingredient.techType;
                AddToStack(techType, ingredient.amount, ref stack);
            }
            RemoveOwned(ref stack, Inventory.main);
            CostOfCraft(stack, out Dictionary<TechType, int> fullCost);
            IngredientList(fullCost, out IList<Ingredient> ingredientsList);
            IconsFromList(ingredientsList, out List<TooltipIcon> newIcons);
            Plugin.Logger.LogInfo("Full cost of craft:");
            foreach (Ingredient ingredient in ingredientsList)
            {
                Plugin.Logger.LogInfo($"TechType: {ingredient.techType}, Amount: {ingredient.amount}");
            }
            Plugin.Logger.LogInfo("Crafting status of full cost end.");
            GetCraftingStatus(ingredientsList, newIcons);
        }

        public static void GetCraftingStatus(IList<Ingredient> ingredients, List<TooltipIcon> icons)
        {
            if (ingredients == null)
            {
                return;
            }
            int count = ingredients.Count;
            Inventory main = Inventory.main;
            StringBuilder stringBuilder = new();
            for (int i = 0; i < count; i++)
            {
                stringBuilder.Length = 0;
                Ingredient ingredient = ingredients[i];
                TechType techType = ingredient.techType;
                int pickupCount = main.GetPickupCount(techType);
                int amount = ingredient.amount;
                bool flag = pickupCount >= amount || !GameModeUtils.RequiresIngredients();
                bool hasIngredients = false;
                if(CraftTree.IsCraftable(techType))
                { 
                    CraftingLogic.IsFuffiled(techType, out bool isCraftable); 
                    hasIngredients = isCraftable;
                }
                Sprite sprite = SpriteManager.Get(techType);
                if (flag)
                {
                    stringBuilder.Append(Plugin.available);
                }
                else if (hasIngredients)
                {
                    stringBuilder.Append(Plugin.craftable);
                }
                else
                {
                    stringBuilder.Append(Plugin.unavailable);
                }
                string orFallback = Language.main.GetOrFallback(TooltipFactory.techTypeIngredientStrings.Get(techType), techType);
                stringBuilder.Append(orFallback);
                if (amount > 1)
                {
                    stringBuilder.Append(" x");
                    stringBuilder.Append(amount);
                }
                if (pickupCount > 0 && pickupCount < amount)
                {
                    stringBuilder.Append(" (");
                    stringBuilder.Append(pickupCount);
                    stringBuilder.Append(")");
                }
                stringBuilder.Append("</color>");
                icons.Add(new TooltipIcon(sprite, stringBuilder.ToString()));
            }
        }

        public static void IngredientList(Dictionary<TechType, int> ingredients, out IList<Ingredient> ingredientsList)
        {
            ingredientsList = new List<Ingredient>();
            foreach (TechType material in ingredients.Keys)
            {
                ingredientsList.Add(new Ingredient(material, ingredients[material]));
            }
        }

        public static void IconsFromList(IList<Ingredient> ingredients, out List<TooltipIcon> icons)
        {
            icons = new List<TooltipIcon>();
            foreach (Ingredient ingredient in ingredients)
            {
                TechType techType = ingredient.techType;
                Sprite sprite = SpriteManager.Get(techType);
                icons.Add(new TooltipIcon(sprite, techType.AsString()));
            }
        }

        public static void AddToStack(TechType techType, int ammount, ref Stack<Resource> resourceStack)
        {
            CreateStack(techType, ref resourceStack, ammount);
            OrganizeCraftStack(ref resourceStack);
        }

        public static void IsFuffiled(TechType techType, out bool alreadyPassed)
        {
            if (!GameModeUtils.RequiresIngredients()) 
            { 
                alreadyPassed = true; 
                return; 
            }
            Inventory inventory = Inventory.main;
            ChainCraft(techType, inventory, out Stack<Resource> craftStack);
            CostOfCraft(craftStack, out Dictionary<TechType, int> entryCost);
            ValidateCraft(inventory, entryCost, out alreadyPassed);
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
            if (!CraftTree.IsCraftable(recipe)) return;
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

        public static bool IsInCraftingTree(TechType techType, CraftTree.Type craftingTree)
        {
            if (craftingTree is CraftTree.Type.None) return false;
            CraftTree tree = CraftTree.GetTree(craftingTree);
            CraftNode node = tree.nodes;
            HashSet<TechType> recipies = new();
            IEnumerator<CraftNode> enumerator = node.Traverse();
            while (enumerator.MoveNext())
            {
                recipies.Add(enumerator.Current.techType0);
            }
            return recipies.Contains(techType);
        }

        private static void ValidateCraft(Inventory inventory, Dictionary<TechType, int> entryCost, out bool valid)
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
    }
}
