using ChainCrafting.Configs;
using ChainCrafting.CraftingLogic;
using ChainCrafting.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChainCrafting.uiLogic
{
    public static class uiBaseMethods
    {
        public static void OnPointerClick(uGUI_BlueprintsTab self,uGUI_BlueprintEntry entry, ref bool __result)
        {
            TechType techType = self.GetTechType(entry);
            Plugin.Logger.LogInfo($"Clicked on {techType}");
            if (self.IsUnlocked(techType))
            {
                uGUI_CraftingHelper.TreeType = techType;
                CraftingInputs.OnCrftingHelperOpen?.Invoke();
            }
            __result = true;
        }

        public static void CraftRecipe(TechType techType, bool locked, TooltipData data)
        {
            TooltipFactory.Initialize();
            if (locked)
            {
                TooltipFactory.WriteTitle(data.prefix, Language.main.Get(techType));
                TooltipFactory.WriteDescription(data.prefix, TooltipFactory.stringLockedRecipeHint);
                return;
            }
            string text = Language.main.Get(techType);
            int craftAmount = TechData.GetCraftAmount(techType) * CraftingInputs.CraftCount;
            if (craftAmount > 1)
            {
                text = Language.main.GetFormat("CraftMultipleFormat", text, craftAmount);
            }
            TooltipFactory.WriteTitle(data.prefix, text);
            TooltipFactory.WriteDescription(data.prefix, Language.main.Get(TooltipFactory.techTypeTooltipStrings.Get(techType)));
            CraftingUI.ConditionalCraftingStatus(techType, data.icons, CraftingInputs.RawResourcesEnabled);
            if (!locked)
            {
                bool flag = GameInput.PrimaryDevice == GameInput.Device.Controller;
                if (Validate.IsFulfilled(techType, CraftingInputs.CraftCount))
                {
                    TooltipFactory.WriteAction(data.postfix, TooltipFactory.stringButton0, TooltipFactory.stringCraft);
                }
                bool pin = PinManager.GetPin(techType);
                string key = flag ? TooltipFactory.stringButton2 : TooltipFactory.stringButton1;
                if (pin)
                {
                    TooltipFactory.WriteAction(data.postfix, key, TooltipFactory.stringUnpinRecipe);
                }
                else if (PinManager.Count < PinManager.max)
                {
                    TooltipFactory.WriteAction(data.postfix, key, TooltipFactory.stringPinRecipe);
                }
                if (flag)
                {
                    TooltipFactory.WriteAction(data.postfix, TooltipFactory.stringButton1, TooltipFactory.stringNodeExit);
                }
                string input = "";
                if (CraftingInputs.CanUpCraft) input += GameInput.FormatButton(CraftingInputs.UpCraft);
                if (CraftingInputs.CanUpCraft && CraftingInputs.CanDownCraft) input += " / ";
                if (CraftingInputs.CanDownCraft) input += GameInput.FormatButton(CraftingInputs.DownCraft);
                if (input.Length > 0) TooltipFactory.WriteAction(data.postfix, input, Language.main.Get("ActionChangeCraft_Bind"));
                TooltipFactory.WriteAction(data.postfix, GameInput.FormatButton(CraftingInputs.RawResources), Language.main.Get("ActionRawResources_Bind"));
            }
        }

        public static void BaseUpdateIngredients(uGUI_RecipeEntry self, ItemsContainer container, bool ping)
        {
            ReadOnlyCollection<Ingredient> ingredients = TechData.GetIngredients(self.techType);
            int craftAmount = Resources.Yield(self.techType);
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
                bool hasResources = Validate.IsFulfilled(techType, CraftingInputs.CraftCount);
                int count = container.GetCount(techType);
                int amount = ingredient.Amount;
                int num3 = count / amount;
                if (num < 0 || num3 < num) num = num3;
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
    }
}
