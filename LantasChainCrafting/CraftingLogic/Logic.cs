using BepInEx.Logging;
using ChainCrafting.Configs;
using ChainCrafting.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace ChainCrafting.CraftingLogic
{
    public static class Logic
    {
        public static IEnumerator Craft(GhostCrafter crafter, TechType techType)
        {
            ChainCraft(new(techType, Resources.Yield(techType) * CraftingInputs.CraftCount), out Stack<Resource> craftStack);
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
                    crafter._logic.Craft(next, Math.Max(item.CraftTime, 2.7f));
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
            RemoveOwned(target.Type, ref craftStack);
            StringBuilder builder = new();
            builder.AppendLine($"Creating stack for {target}");
            foreach (Resource item in craftStack) builder.AppendLine(item.ToString());
            Plugin.Logger.LogInfo(builder);
        }

        public static void OrganisedStack(Resource target, out Stack<Resource> craftStack)
        {
            StringBuilder builder = new();
            craftStack = new Stack<Resource>();
            CreateStack(target.Type, target.Amount, ref craftStack);
            builder.AppendLine($"Creating stack for {target}");
            foreach (Resource item in craftStack) builder.AppendLine(item.ToString());
            builder.AppendLine("Organising the Stack:");
            OrganizeCraftStack(ref craftStack);
            foreach (Resource item in craftStack) builder.AppendLine(item.ToString());
            Plugin.Logger.LogInfo(builder.ToString());
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
                    TechType type = component.Type;
                    int requiredAmount = (int)Math.Ceiling((float)(resource.Amount) / resource.Yield) * component.Amount;
                    catalog.Subtract(type, Math.Max(0, component.Amount - requiredAmount));
                }
                processingQueue.Enqueue(resource);
            }
            while (processingQueue.Any())
            {
                TechType item = processingQueue.Dequeue().Type;
                craftStack.Push(catalog[item]);
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
            StringBuilder dot = new();
            while (tempStack.Any())
            {
                Resource resource = tempStack.Pop();
                TechType resourceType = resource.Type;
                int owned = Math.Min(resource.Amount, resource.PickupCount);
                int resourceAmount = (resourceType != target) ? (int)Math.Ceiling((float)(resource.Amount - owned) / resource.Yield) : resource.Amount;
                catalog.Subtract(resourceType, resource.Amount - Math.Min(resource.Amount, resourceAmount));
                dot.AppendLine($"Removed {resource.Amount - Math.Min(resource.Amount, resourceAmount)} from {resource}");
                foreach (Resource component in resource.Components)
                {
                    if(!component.Craftable) continue;
                    TechType type = component.Type;
                    int requiredAmount = resourceAmount * component.Amount;
                    int difference = component.Amount - Math.Min(component.Amount, requiredAmount);

                    dot.AppendLine($"Removed {difference} from {catalog[type]}");
                    catalog.Subtract(type, difference);
                }
                processingQueue.Enqueue(resource);
            }
            Plugin.Logger.LogInfo(dot.ToString());
            while (processingQueue.Any())
            {
                TechType item = processingQueue.Dequeue().Type;
                Resource resource = catalog[item];
                if(resource != null) craftStack.Push(resource);
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
