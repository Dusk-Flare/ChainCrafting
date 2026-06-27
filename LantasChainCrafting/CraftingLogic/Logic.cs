using BepInEx.Logging;
using ChainCrafting.Configs;
using ChainCrafting.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using UnityEngine;
using Resources = ChainCrafting.Utils.Resources;

namespace ChainCrafting.CraftingLogic
{
    public static class Logic
    {
        public static IEnumerator Craft(GhostCrafter crafter, TechType techType)
        {
            ChainCraft(new(techType, Resources.Yield(techType) * CraftingInputs.CraftCount), out Stack<Resource> craftStack);
            Plugin.Logger.LogInfo($"Crafting {techType}, Yield of {Resources.Yield(techType)} and count of {CraftingInputs.CraftCount}? {craftStack.Any()}");
            CraftingInputs.CraftCount = 1;
            while (craftStack.Any())
            {
                Resource item = craftStack.Pop();
                TechType next = item.Type;
                for (int i = 0; i < item.Amount; i++)
                {
                    if (!CrafterLogic.ConsumeEnergy(crafter.powerRelay, 5f))
                    {
                        ErrorMessage.AddWarning("Not enough power");
                        yield break;
                    }
                    if(!Consume(next))
                    {
                        ErrorMessage.AddWarning(Language.main.Get("DontHaveNeededIngredients"));
                        yield break;
                    }
                    crafter.OnStateChanged(true);
                    crafter._logic.Craft(next, Mathf.Max(item.CraftTime, 2.7f));
                    while (crafter.HasCraftedItem())
                    {
                        if (!crafter._logic.inProgress) crafter.OnStateChanged(false);
                        yield return null;
                    }
                    crafter.OnStateChanged(false);
                }
            }
        }

        public static void ChainCraft(Resource target, out Stack<Resource> craftStack)
        {
            OrganisedStack(target, out craftStack);
            AccountForYields(ref craftStack);
            RemoveOwned(target.Type, ref craftStack);
        }

        public static void OrganisedStack(Resource target, out Stack<Resource> craftStack)
        {
            craftStack = new Stack<Resource>();
            CreateStack(target.Type, target.Amount, ref craftStack);
            OrganizeCraftStack(ref craftStack);
        }

        public static void GetRequirements(Resource resource, out Stack<Resource> stack)
        {
            stack = new Stack<Resource>();
            CreateStack(resource.Type, resource.Amount, ref stack);
            OrganizeCraftStack(ref stack);
        }

        public static void CreateStack(TechType recipe, int amount, ref Stack<Resource> stack)
        {
            if (recipe == TechType.None || amount <= 0) return;
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
            Queue<Resource> processingQueue = new();
            while (craftStack.Any())
            {
                Resource resource = craftStack.Pop();
                catalog.Set(resource);
                tempStack.Push(resource);
            }
            while (tempStack.Any())
            {
                Resource resource = tempStack.Pop();
                foreach(Resource component in resource.Components)
                {
                    if(!component.Craftable) continue;
                    int requiredAmount = (int)Mathf.Ceil((float)catalog.AmountOf(resource) / resource.Yield) * catalog.AmountOf(component);
                    catalog.Subtract(component.Type, Mathf.Max(0, component.Amount - requiredAmount));
                }
                processingQueue.Enqueue(resource);
            }
            while (processingQueue.Any())
            {
                TechType item = processingQueue.Dequeue().Type;
                Resource resource = catalog[item];
                if(resource != null) craftStack.Push(resource);
            }
        }

        public static void RemoveOwned(TechType target, ref Stack<Resource> craftStack)
        {
            if (!craftStack.Any()) return;
            ResourceTable catalog = new();
            Stack<Resource> tempStack = new();
            Queue<Resource> processingQueue = new();
            while (craftStack.Any())
            {
                Resource resource = craftStack.Pop();
                catalog.Set(resource);
                tempStack.Push(resource);
            }
            while(tempStack.Any())
            {
                Resource resource = tempStack.Pop();
                if (resource != target)
                {
                    OrganisedStack(resource with { Amount = resource.PickupCount }, out Stack<Resource> componentStack);
                    AccountForYields(ref componentStack);
                    foreach (Resource item in componentStack) catalog.Subtract(item);
                }
                processingQueue.Enqueue(resource);
            }
            while (processingQueue.Any())
            {
                TechType item = processingQueue.Dequeue().Type;
                Resource resource = catalog[item];
                if (resource != null) craftStack.Push(resource);
            }
        }

        public static bool Consume(TechType techType)
        {
            if (Validate.IsFulfilled(techType))
            {
                Inventory.main.ConsumeResourcesForRecipe(techType);
                return true;
            }
            ErrorMessage.AddWarning(Language.main.Get("DontHaveNeededIngredients"));
            return false;
        }
    }
}
