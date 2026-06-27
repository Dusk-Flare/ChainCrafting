using ChainCrafting.Configs;
using ChainCrafting.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Resources = ChainCrafting.Utils.Resources;

namespace ChainCrafting.CraftingLogic
{
    internal class Validate
    {
        public static bool IsFulfilled(TechType techType, int count = 1)
        {
            if (!GameModeUtils.RequiresIngredients()) return true;
            if(!Resources.Craftable(techType)) return false;
            Logic.ChainCraft(new(techType, count), out Stack<Resource> craftStack);
            CostOfCraft(craftStack, out ResourceTable entryCost);
            bool craftable = ValidateCraft(entryCost);
            return craftable;
        }

        public static void CostOfCraft(Stack<Resource> craftStack, out ResourceTable entryCost)
        {
            entryCost = new();
            foreach (Resource resource in craftStack)
            {
                int materialCount = resource.Amount;
                int materialYield = resource.Yield;
                if (resource.Amount <= 0) continue;
                foreach (Resource component in resource.Components)
                {
                    if (component.Craftable) continue;
                    entryCost.Add(component with { Amount = (int)Mathf.Ceil((float)materialCount / Mathf.Max(1, materialYield)) * component.Amount });
                }
            }
        }

        public static void CostOfOwned(Resource target, out ResourceTable savedCost)
        {
            savedCost = new();
            Logic.OrganisedStack(target, out Stack<Resource> baseStack);
            Logic.ChainCraft(target, out Stack<Resource> craftStack);
            Logic.AccountForYields(ref baseStack);
            CostOfCraft(baseStack, out ResourceTable baseCost);
            CostOfCraft(craftStack, out ResourceTable ownedCost);
            foreach (Resource resource in baseCost) 
            {
                savedCost.Add(resource - ownedCost.AmountOf(resource.Type)); 
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
