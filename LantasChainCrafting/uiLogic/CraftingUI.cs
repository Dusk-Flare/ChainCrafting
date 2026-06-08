using ChainCrafting.Configs;
using ChainCrafting.CraftingLogic;
using ChainCrafting.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Resources = ChainCrafting.Utils.Resources;

namespace ChainCrafting.uiLogic
{
    public static class CraftingUI
    {
        public static void ConditionalCraftingStatus(TechType type, List<TooltipIcon> icons, bool displayMissing)
        {
            if (!displayMissing)
            {
                GetCraftingStatus(Resources.ComponentsOf(type).Select(resource => resource with { Amount = resource.Amount * CraftingInputs.CraftCount }).ToList(), new(), icons);
                return;
            }
            Resource target = new(type, CraftingInputs.CraftCount);
            Logic.OrganisedStack(target, out Stack<Resource> stack);
            Validate.CostOfOwned(target, out ResourceTable table);
            Logic.AccountForYields(ref stack);
            Validate.CostOfCraft(stack, out ResourceTable fullCost);
            GetCraftingStatus(fullCost, table, icons);
        }

        public static void ConditionalUpdateIngredients(uGUI_RecipeEntry self, ItemsContainer container, bool ping, bool displayMissing)
        {
            if (!displayMissing) uiBaseMethods.BaseUpdateIngredients(self, container, ping);
            else UpdateIngredients(self, ping);
        }

        public static void UpdateIngredients(uGUI_RecipeEntry self, bool ping)
        {
            Logic.OrganisedStack(new(self.techType, CraftingInputs.CraftCount), out Stack<Resource> craftStack);
            Logic.AccountForYields(ref craftStack);
            Validate.CostOfCraft(craftStack, out ResourceTable entryCost);
            Validate.CostOfOwned(new(self.techType, CraftingInputs.CraftCount), out ResourceTable ownedCost);
            List<Resource> resources = entryCost.ToList();

            if (entryCost != null && entryCost.Contains(self.techType)) entryCost.Remove(self.techType);
            int num = -1;
            int yield = Resources.Yield(self.techType);
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
                bool canCraft = Validate.IsFulfilled(techType, CraftingInputs.CraftCount);
                int count = Resources.PickupCount(techType) + ownedCost.AmountOf(techType);
                int amount = item.Amount;
                int num3 = count / amount;
                if (num < 0 || num3 < num) num = num3;
                uGUI_RecipeItem uGUI_RecipeItem2 = self.items[i];
                bool defaultCheck = count >= amount || !GameModeUtils.RequiresIngredients();
                if (defaultCheck)
                {
                    uGUI_RecipeItem2.text.color = Plugin.availableColor;
                }
                else if (canCraft && item.Craftable)
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
            num *= yield;
            if (num > 0)
            {
                if (self.min != num)
                {
                    self.min = num;
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
            StringBuilder stringBuilder = new();
            for (int i = 0; i < count; i++)
            {
                stringBuilder.Length = 0;
                Resource ingredient = ingredients[i];
                TechType techType = ingredient.Type;
                int pickupCount = Resources.PickupCount(techType) + owned.AmountOf(techType);
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
            return action == TreeAction.Expand || (action == TreeAction.Craft && CrafterLogic.IsCraftRecipeUnlocked(sender.techType) && Validate.IsFulfilled(sender.techType, Resources.Yield(sender.techType) * CraftingInputs.CraftCount));
        }
    }
}
