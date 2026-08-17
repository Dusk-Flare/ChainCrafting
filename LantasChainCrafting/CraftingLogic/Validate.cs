using ChainCrafting.Configs;
using ChainCrafting.Compatibility;
using SextantHorizon.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Resources = SextantHorizon.Utils.Resources;

namespace ChainCrafting.CraftingLogic
{
    internal class Validate
    {
        public static bool IsFulfilled(TechType techType, int count = 1, Vector3? resourceOrigin = null)
        {
            if (!GameModeUtils.RequiresIngredients()) return true;
            if(!Resources.Craftable(techType)) return false;
            Logic.ChainCraft(new(techType, count), out Stack<Resource> craftStack);
            CostOfCraft(craftStack, out ResourceTable entryCost);
            bool isValid = ValidateCraft(entryCost, resourceOrigin);
            Plugin.Logger.LogInfo($"{(isValid ? "Valid" : "Invalid")} craft for {techType} with count {count}. \n\nEntry cost: \n{entryCost}");
            return isValid;
        }

        public static void CostOfCraft(Stack<Resource> craftStack, out ResourceTable entryCost)
        {
            entryCost = [];
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
            savedCost = [];
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

        private static bool ValidateCraft(ResourceTable entryCost, Vector3? resourceOrigin = null)
        {
            if (Manager.ExternalResources)
            {
                ResourceTable externalNeeded = entryCost.Select(r => r with { Amount = r.Amount - r.PickupCount }).Where(r => r.Amount > 0).ToList();
                return entryCost.All(material => material.PickupCount >= material.Amount) || Manager.ValidateExternal(externalNeeded, resourceOrigin);
            }
            return entryCost.All(material => material.PickupCount >= material.Amount);
        }
    }
}
