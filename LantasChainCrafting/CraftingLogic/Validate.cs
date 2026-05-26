using ChainCrafting.Configs;
using ChainCrafting.Utils;
using System.Collections.Generic;

namespace ChainCrafting.CraftingLogic
{
    internal class Validate
    {
        public static bool IsFulfilled(TechType techType, int count = 1)
        {
            if (!GameModeUtils.RequiresIngredients()) return true;
            Logic.ChainCraft(techType, count, out Stack<Resource> craftStack);
            CostOfCraft(craftStack, out ResourceTable entryCost);
            bool fuffiled = ValidateCraft(entryCost);
            return fuffiled;
        }

        public static void CostOfCraft(Stack<Resource> craftStack, out ResourceTable entryCost)
        {
            entryCost = new();
            foreach (Resource resource in craftStack)
            {
                int materialCount = resource.Amount;
                if (resource.Amount <= 0) continue;

                foreach (Resource ingredient in resource.Components)
                {
                    if (ingredient.Craftable) continue;
                    entryCost.Add(ingredient.Type, ingredient.Amount * materialCount);
                }
            }
        }

        public static void CostOfOwned(TechType techType, int count, out ResourceTable entryCost)
        {
            entryCost = new();
            Logic.OrganisedStack(techType, count, out Stack<Resource> craftStack);
            foreach (Resource resource in craftStack)
            {
                int materialCount = resource.Amount;
                if (resource.Amount <= 0 || resource.PickupCount < resource.Amount) continue;
                foreach (Resource ingredient in resource.Components)
                {
                    TechType type = ingredient.Type;
                    int amount = ingredient.Amount * materialCount;
                    if (ingredient.Craftable)
                    {
                        Logic.OrganisedStack(type, amount, out Stack<Resource> subStack);
                        CostOfCraft(subStack, out ResourceTable subCost);
                        entryCost.AddAll(subCost);
                        continue;
                    }
                    entryCost.Add(type, amount);
                }
            }
        }

        private static bool ValidateCraft(ResourceTable entryCost)
        {
            foreach (Resource material in entryCost)
            {
                if (material.Amount > material.PickupCount) return false;
            }
            return true;
        }
    }
}
