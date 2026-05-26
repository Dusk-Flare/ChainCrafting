using ChainCrafting.Configs;
using ChainCrafting.CraftingLogic;
using ChainCrafting.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ChainCrafting.uiLogic
{
    public static class CraftingUI
    {
        public static void ConditionalCraftingStatus(TechType type, List<TooltipIcon> icons, bool displayMissing)
        {
            if (!displayMissing)
            {
                GetCraftingStatus(Resource.ComponentsOf(type).Select(resource => resource with { Amount = resource.Amount * CraftingInputs.CraftCount }).ToList(), new(), icons);
                return;
            }
            Stack<Resource> stack = new();
            ResourceTable table = new();
            Logic.CreateStack(type, CraftingInputs.CraftCount, ref stack);
            Logic.OrganizeCraftStack(ref stack);
            foreach (Resource resource in stack) if (resource.Amount < resource.PickupCount) table.Add(resource);
            Logic.RemoveOwned(ref stack, type);
            Validate.CostOfCraft(stack, out ResourceTable fullCost);
            GetCraftingStatus(fullCost, table, icons);
        }

        public static void ConditionalUpdateIngredients(uGUI_RecipeEntry self, ItemsContainer container, bool ping, bool displayMissing)
        {
            if (!displayMissing) uiBaseMethods.BaseUpdateIngredients(self, container, ping);
            else UpdateIngredients(self, container, ping);
        }

        public static void UpdateIngredients(uGUI_RecipeEntry self, ItemsContainer container, bool ping)
        {
            Stack<Resource> craftStack = new();
            Logic.CreateStack(self.techType, CraftingInputs.CraftCount, ref craftStack);
            Logic.OrganizeCraftStack(ref craftStack);
            Logic.AccountForYields(ref craftStack);
            Validate.CostOfCraft(craftStack, out ResourceTable entryCost);
            List<Resource> resources = entryCost.ToList();
            if (entryCost != null && entryCost.Contains(self.techType)) entryCost.Remove(self.techType);
            int negative = -1;
            int yield = TechData.GetCraftAmount(self.techType);
            int totalCraftAmount = resources != null ? resources.Count() : 0;

            while (self.items.Count < totalCraftAmount)
            {
                uGUI_RecipeItem uGUI_RecipeItem = self.pool.Get();
                uGUI_RecipeItem.Initialize();
                self.items.Add(uGUI_RecipeItem);
            }
            while (self.items.Count > totalCraftAmount)
            {
                int index = self.items.Count - 1;
                uGUI_RecipeItem entry = self.items[index];
                self.items.RemoveAt(index);
                self.pool.Release(entry);
            }
            for (int i = 0; i < resources.Count; i++)
            {
                Resource item = resources[i];
                TechType techType = item.Type;
                bool isCraftable = Validate.IsFulfilled(techType, CraftingInputs.CraftCount);
                int count = container.GetCount(techType);
                int amount = item.Amount;
                uGUI_RecipeItem uGUI_RecipeItem2 = self.items[i];
                bool defaultCheck = count >= amount || !GameModeUtils.RequiresIngredients();
                if (defaultCheck)
                {
                    uGUI_RecipeItem2.text.color = Plugin.availableColor;
                }
                else if (isCraftable && item.Craftable)
                {
                    uGUI_RecipeItem2.text.color = Plugin.craftableColor;
                }
                else
                {
                    uGUI_RecipeItem2.text.color = Plugin.unavailableColor;
                }

                uGUI_RecipeItem2.Set(techType, count, amount, ping);
            }
            self.background.SetActive(totalCraftAmount > 0);
            negative *= yield;
            if (negative > 0)
            {
                if (self.min != negative)
                {
                    self.min = negative;
                    self.text.text = string.Format("x{0}", IntStringCache.GetStringForInt(self.min));
                    return;
                }
            }
            else
            {
                self.min = int.MinValue;
                self.text.text = string.Empty;
            }
        }

        public static void GetCraftingStatus(List<Resource> ingredients, ResourceTable owned, List<TooltipIcon> icons)
        {
            if (ingredients == null)
            {
                return;
            }
            int count = ingredients.Count;
            Inventory main = Inventory.main;
            StringBuilder stringBuilder = new();
            for (int i = 0; i < count; i++)
            {
                stringBuilder.Length = 0;
                Resource ingredient = ingredients[i];
                TechType techType = ingredient.Type;
                int pickupCount = main.GetPickupCount(techType) + (owned.Contains(techType) ? owned[techType].Amount : 0);
                int amount = ingredient.Amount;
                bool flag = pickupCount >= amount || !GameModeUtils.RequiresIngredients();
                bool hasIngredients = false;
                if (ingredient.Craftable) hasIngredients = Validate.IsFulfilled(techType, CraftingInputs.CraftCount);
                Sprite sprite = SpriteManager.Get(techType);
                if (flag)
                {
                    stringBuilder.Append(Plugin.available);
                }
                else if (hasIngredients)
                {
                    stringBuilder.Append(Plugin.craftable);
                }
                else
                {
                    stringBuilder.Append(Plugin.unavailable);
                }
                string orFallback = Language.main.GetOrFallback(TooltipFactory.techTypeIngredientStrings.Get(techType), techType);
                stringBuilder.Append(orFallback);
                if (amount > 1)
                {
                    stringBuilder.Append(" x");
                    stringBuilder.Append(amount);
                }
                if (pickupCount > 0 && pickupCount < amount)
                {
                    stringBuilder.Append(" (");
                    stringBuilder.Append(pickupCount);
                    stringBuilder.Append(")");
                }
                stringBuilder.Append("</color>");
                icons.Add(new TooltipIcon(sprite, stringBuilder.ToString()));
            }
        }

        public static bool ActionAvailable(uGUI_CraftingMenu.Node sender)
        {
            TreeAction action = sender.action;
            return action == TreeAction.Expand || (action == TreeAction.Craft && CrafterLogic.IsCraftRecipeUnlocked(sender.techType) && Validate.IsFulfilled(sender.techType, CraftingInputs.CraftCount));
        }
    }
}
