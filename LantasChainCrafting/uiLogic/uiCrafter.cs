using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ChainCrafting.uiLogic
{
    public static class CraftingUI
    {
        public static void ConditionalCraftingStatus(IList<Ingredient> ingredients, List<TooltipIcon> icons, bool displayMissing)
        {
            if (!displayMissing)
            {
                GetCraftingStatus(ingredients, icons);
                return;
            }
            Stack<Resource> stack = new();
            foreach (Resource ingredient in ingredients)
            {
                TechType techType = ingredient.Type;
                CraftingLogic.CreateStack(techType, ref stack, ingredient.Amount);
            }
            CraftingLogic.OrganizeCraftStack(ref stack);
            CraftingLogic.CostOfCraft(stack, out Dictionary<TechType, int> fullCost);
            IngredientList(fullCost, out IList<Ingredient> ingredientsList);
            GetCraftingStatus(ingredientsList, icons);
        }

        public static void ConditionalUpdateIngredients(uGUI_RecipeEntry self, ItemsContainer container, bool ping, bool displayMissing)
        {
            if (!displayMissing) BaseUpdateIngredients(self, container, ping);
            else UpdateIngredients(self, container, ping);
        }

        public static void UpdateIngredients(uGUI_RecipeEntry self, ItemsContainer container, bool ping)
        {
            Stack<Resource> craftStack = new();
            CraftingLogic.CreateStack(self.techType, ref craftStack, 1);
            CraftingLogic.OrganizeCraftStack(ref craftStack);
            CraftingLogic.CostOfCraft(craftStack, out Dictionary<TechType, int> entryCost);
            List<Resource> resources = entryCost.Select(entry => new Resource(entry)).ToList();
            if (entryCost != null && entryCost.ContainsKey(self.techType)) entryCost.Remove(self.techType);
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
                CraftingLogic.IsFuffiled(techType, out bool isCraftable);
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

        public static void BaseUpdateIngredients(uGUI_RecipeEntry self, ItemsContainer container, bool ping)
        {
            ReadOnlyCollection<Ingredient> ingredients = TechData.GetIngredients(self.techType);
            int craftAmount = TechData.GetCraftAmount(self.techType);
            int num = -1;
            int num2 = (ingredients != null) ? ingredients.Count : 0;
            while (self.items.Count < num2)
            {
                uGUI_RecipeItem uGUI_RecipeItem = self.pool.Get();
                uGUI_RecipeItem.Initialize();
                self.items.Add(uGUI_RecipeItem);
            }
            while (self.items.Count > num2)
            {
                int index = self.items.Count - 1;
                uGUI_RecipeItem entry = self.items[index];
                self.items.RemoveAt(index);
                self.pool.Release(entry);
            }
            for (int i = 0; i < num2; i++)
            {
                Resource ingredient = ingredients[i];
                TechType techType = ingredient.Type;
                CraftingLogic.IsFuffiled(techType, out bool hasResources);
                int count = container.GetCount(techType);
                int amount = ingredient.Amount;
                int num3 = count / amount;
                if (num < 0 || num3 < num)
                {
                    num = num3;
                }
                uGUI_RecipeItem uGUI_RecipeItem2 = self.items[i];
                bool defaultCheck = count >= amount || !GameModeUtils.RequiresIngredients();
                if (defaultCheck)
                {
                    uGUI_RecipeItem2.text.color = Plugin.availableColor;
                }
                else if (hasResources && ingredient.Craftable)
                {
                    uGUI_RecipeItem2.text.color = Plugin.craftableColor;
                }
                else
                {
                    uGUI_RecipeItem2.text.color = Plugin.unavailableColor;
                }

                uGUI_RecipeItem2.Set(techType, count, amount, ping);
            }
            self.background.SetActive(num2 > 0);
            num *= craftAmount;
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

        public static void GetCraftingStatus(IList<Ingredient> ingredients, List<TooltipIcon> icons)
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
                Ingredient ingredient = ingredients[i];
                TechType techType = ingredient.techType;
                int pickupCount = main.GetPickupCount(techType);
                int amount = ingredient.amount;
                bool flag = pickupCount >= amount || !GameModeUtils.RequiresIngredients();
                bool hasIngredients = false;
                if (CraftTree.IsCraftable(techType))
                {
                    CraftingLogic.IsFuffiled(techType, out bool isCraftable);
                    hasIngredients = isCraftable;
                }
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

        public static void IngredientList(Dictionary<TechType, int> ingredients, out IList<Ingredient> ingredientsList)
        {
            ingredientsList = new List<Ingredient>();
            foreach (TechType material in ingredients.Keys)
            {
                ingredientsList.Add(new Ingredient(material, ingredients[material]));
            }
        }

        public static void IconsFromList(IList<Ingredient> ingredients, out List<TooltipIcon> icons)
        {
            icons = new List<TooltipIcon>();
            foreach (Ingredient ingredient in ingredients)
            {
                TechType techType = ingredient.techType;
                Sprite sprite = SpriteManager.Get(techType);
                string orFallback = Language.main.GetOrFallback(TooltipFactory.techTypeIngredientStrings.Get(techType), techType);
                icons.Add(new TooltipIcon(sprite, orFallback));
            }
        }
    }
}
