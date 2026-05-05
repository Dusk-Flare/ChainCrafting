using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ChainCrafting.Utils;
using System.Linq;

namespace ChainCrafting.CraftingLogic
{
    public static class Logic
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
                    if (!Consume(next)) continue;
                    crafter._logic.Craft(next, Math.Max(item.CraftTime, 2.7f));
                    while (crafter.HasCraftedItem()) yield return null;
                }
            }
        }

        public static void ChainCraft(TechType resource, Inventory inventory, out Stack<Resource> craftStack)
        {
            craftStack = new Stack<Resource>();
            CreateStack(resource, ref craftStack, 1);
            OrganizeCraftStack(ref craftStack);
            RemoveOwned(ref craftStack, inventory, resource);
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

        public static void RemoveOwned(ref Stack<Resource> craftStack, Inventory inventory, TechType target = TechType.None)
        {
            if(!craftStack.Any()) return;
            Dictionary<TechType, int> catalog = new();
            Stack<Resource> tempStack = new();
            while (craftStack.Any())
            {
                Resource item = craftStack.Pop();
                int owned = inventory.GetPickupCount(item.Type);
                int removedCount = Math.Min(item.Amount, owned);
                int itemYield = item.Yield;
                if(item.Type != target) catalog[item.Type] = (int)Math.Ceiling((float)Math.Max(0, item.Amount - removedCount) / itemYield);
                else catalog[item.Type] = item.Amount;
                GetRequirements(item, out Stack<Resource> requirements);
                if (item.Type != target && removedCount > 0)
                {
                    while (requirements.Any())
                    {
                        Resource requirement = requirements.Pop();
                        TechType type = requirement.Type;
                        int requiredAmount = (int)Math.Ceiling((float)(requirement.Amount - removedCount) / requirement.Yield);
                        if (catalog.ContainsKey(type)) catalog[type] = (int)Math.Ceiling((float)Math.Max(0, requiredAmount));
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
    }
}
