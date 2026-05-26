using ChainCrafting.Configs;
using ChainCrafting.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ChainCrafting.CraftingLogic
{
    public static class Logic
    {
        public static IEnumerator Craft(GhostCrafter crafter, TechType techType)
        {
            ChainCraft(techType, CraftingInputs.CraftCount, out Stack<Resource> craftStack);
            while (craftStack.Any())
            {
                Resource item = craftStack.Pop();
                TechType next = item.Type;
                for (int i = 0; i < item.Amount; i++)
                {
                    if(!CrafterLogic.ConsumeEnergy(crafter.powerRelay, 5f)) continue;
                    if (!Consume(next)) continue;
                    crafter.OnStateChanged(true);
                    crafter._logic.Craft(next, Math.Max(item.CraftTime, 2.7f));
                    while (crafter.HasCraftedItem()) yield return null;
                    crafter.OnStateChanged(false);
                }
            }
        }

        public static void ChainCraft(TechType resource, int amount, out Stack<Resource> craftStack)
        {
            craftStack = new Stack<Resource>();
            CreateStack(resource, amount, ref craftStack);
            OrganizeCraftStack(ref craftStack);
            RemoveOwned(ref craftStack, resource);
        }


        public static void GetRequirements(Resource resource, out Stack<Resource> stack)
        {
            stack = new Stack<Resource>();
            CreateStack(resource.Type, resource.Amount, ref stack);
            OrganizeCraftStack(ref stack);
        }

        public static void CreateStack(TechType recipe, int amount, ref Stack<Resource> stack)
        {
            if (recipe == TechType.None) return;
            if (!CraftTree.IsCraftable(recipe)) return;
            stack.Push(new(recipe, amount));
            ReadOnlyCollection<Ingredient> component = TechData.GetIngredients(recipe);
            foreach (Resource ingredient in component) CreateStack(ingredient.Type, ingredient.Amount * amount, ref stack);
        }

        public static void OrganizeCraftStack(ref Stack<Resource> craftStack)
        {
            ResourceTable catalog = new();
            Stack<Resource> tempStack = new();
            while (craftStack.Any())
            {
                Resource resource = craftStack.Pop();
                if (!catalog.Add(resource)) tempStack.Push(resource);
            }
            while (tempStack.Any())
            {
                TechType resource = tempStack.Pop().Type;
                craftStack.Push(catalog[resource]);
            }
        }

        public static void AccountForYields(ref Stack<Resource> craftStack)
        {
            if (!craftStack.Any()) return;
            ResourceTable catalog = new();
            Stack<Resource> tempStack = new();
            while (craftStack.Any())
            {
                Resource item = craftStack.Pop();
                catalog.Set(item);
                GetRequirements(item, out Stack<Resource> requirements);
                while (requirements.Any())
                {
                    Resource requirement = requirements.Pop();
                    TechType type = requirement.Type;
                    int requiredAmount = (int)Math.Ceiling((float)(requirement.Amount) / requirement.Yield);
                    if (catalog.Contains(type)) catalog.Set(type, Math.Max(0, requiredAmount));
                }
                
                tempStack.Push(item);
            }
            while (tempStack.Any())
            {
                TechType item = tempStack.Pop().Type;
                craftStack.Push(catalog[item]);
            }
        }

        public static void RemoveOwned(ref Stack<Resource> craftStack, TechType target = TechType.None)
        {
            if(!craftStack.Any()) return;
            ResourceTable catalog = new();
            Stack<Resource> tempStack = new();
            while (craftStack.Any())
            {
                Resource item = craftStack.Pop();
                int owned = item.PickupCount;
                int removedCount = Math.Min(item.Amount, owned);
                int itemYield = item.Yield;
                if(item.Type != target) catalog.Set(item.Type, (int)Math.Ceiling((float)Math.Max(0, item.Amount - removedCount) / itemYield));
                else catalog.Set(item);
                GetRequirements(item, out Stack<Resource> requirements);
                if (item.Type != target && removedCount > 0)
                {
                    while (requirements.Any())
                    {
                        Resource requirement = requirements.Pop();
                        TechType type = requirement.Type;
                        int requiredAmount = (int)Math.Ceiling((float)(requirement.Amount - removedCount) / requirement.Yield);
                        if (catalog.Contains(type)) catalog.Set(type, Math.Max(0, requiredAmount));
                    }
                }
                tempStack.Push(item);
            }
            while (tempStack.Any())
            {
                TechType item = tempStack.Pop().Type;
                Plugin.Logger.LogInfo(catalog[item]);
                craftStack.Push(catalog[item]);
            }
        }

        public static bool Consume(TechType techType)
        {
            if (Validate.IsFulfilled(techType, 1))
            {
                Inventory.main.ConsumeResourcesForRecipe(techType, null);
                return true;
            }
            ErrorMessage.AddWarning(Language.main.Get("DontHaveNeededIngredients"));
            return false;
        }
    }
}
