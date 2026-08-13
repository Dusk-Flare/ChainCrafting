using ChainCrafting.Configs;
using ChainCrafting.Utils;
using SextantHorizon.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using Resources = SextantHorizon.Utils.Resources;

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
                        ErrorMessage.AddWarning(Language.main.Get("NotEnoughPowerMessage"));
                        yield break;
                    }
                    if (!Consume(next))
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
                    int count = resource.PickupCount;
                    if(Compatibility.ExternalResources) count += Compatibility.GetLocalPickupCount(resource.Type);
                    OrganisedStack(resource with { Amount = Mathf.Min(count, catalog.AmountOf(resource)) }, out Stack<Resource> componentStack);
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
            if(!GameModeUtils.RequiresIngredients()) return true;
            if (Validate.IsFulfilled(techType))
            {
                if(Compatibility.ExternalResources) ConsumeTotalResources(techType);
                else Inventory.main.ConsumeResourcesForRecipe(techType);
                return true;
            }
            ErrorMessage.AddWarning(Language.main.Get("DontHaveNeededIngredients"));
            return false;
        }

        public static void ConsumeTotalResources(TechType techType)
        {
            if(!GameModeUtils.RequiresIngredients() || !Resources.Craftable(techType) || !Compatibility.ExternalResources) return;
            ResourceTable components = Resources.ComponentsOf(techType);
            ResourceTable ingredients = components.Select(r => r with { Amount = Mathf.Min(r.Amount, r.PickupCount) }).ToList();
            ResourceTable externalIngredients = components.Select(r => r with { Amount = r.Amount - r.PickupCount }).Where(r => r.Amount > 0).ToList();
            if (!ingredients.Any()) return;
            foreach(Resource resource in ingredients)
            {
                TechType techType2 = resource.Type;
                int count = resource.Amount;
                while (count > 0)
                {
                    if (!Inventory.main.DestroyItem(techType2, true))
                    {
                        Plugin.Logger.LogError($"Unable to remove one '{techType2}' from player inventory to craft {techType}.");
                    }
                    uGUI_IconNotifier.main.Play(techType2, uGUI_IconNotifier.AnimationType.To, null);
                    count--;
                }
            }
            if(!Compatibility.ConsumeExternalResources(externalIngredients)) Plugin.Logger.LogError("Failed to consume external resources.");
        }
    }
}
