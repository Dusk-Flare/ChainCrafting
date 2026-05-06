using System;
using System.Collections.Generic;
using ChainCrafting.Utils;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace ChainCrafting.CraftingLogic
{
    internal class Validate
    {
        public static void IsFuffiled(TechType techType, out bool alreadyPassed)
        {
            if (!GameModeUtils.RequiresIngredients())
            {
                alreadyPassed = true;
                return;
            }
            Logic.ChainCraft(techType, out Stack<Resource> craftStack);
            CostOfCraft(craftStack, out ResourceTable entryCost);
            ValidateCraft(entryCost, out alreadyPassed);
        }

        public static void CostOfCraft(Stack<Resource> craftStack, out ResourceTable entryCost)
        {
            entryCost = new();
            foreach (Resource resource in craftStack)
            {
                int materialCount = resource.Amount;
                if (resource.Amount <= 0) continue;

                ReadOnlyCollection<Ingredient> ingredients = TechData.GetIngredients(resource.Type);
                foreach (Resource ingredient in ingredients)
                {
                    if (ingredient.Craftable) continue;
                    entryCost.Add(ingredient.Type, ingredient.Amount * materialCount);
                }
            }
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

        private static void ValidateCraft(ResourceTable entryCost, out bool valid)
        {
            foreach (Resource material in entryCost)
            {
                if (material.Amount > material.PickupCount)
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
