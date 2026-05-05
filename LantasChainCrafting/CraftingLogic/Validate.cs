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
            Inventory inventory = Inventory.main;
            Logic.ChainCraft(techType, inventory, out Stack<Resource> craftStack);
            CostOfCraft(craftStack, out Dictionary<TechType, int> entryCost);
            ValidateCraft(entryCost, out alreadyPassed);
        }

        public static void CostOfCraft(Stack<Resource> craftStack, out Dictionary<TechType, int> entryCost)
        {
            entryCost = new();
            foreach (Resource resource in craftStack)
            {
                int materialCount = resource.Amount;
                if (materialCount <= 0) continue;

                ReadOnlyCollection<Ingredient> ingredients = TechData.GetIngredients(resource.Type);
                foreach (Resource ingredient in ingredients)
                {
                    TechType type = ingredient.Type;
                    if (CraftTree.IsCraftable(type)) continue;
                    int amount = ingredient.Amount * materialCount;
                    if (entryCost.ContainsKey(type)) entryCost[type] += amount;
                    else entryCost[type] = amount;
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

        private static void ValidateCraft(Dictionary<TechType, int> entryCost, out bool valid)
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
