using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ChainCrafting.Utils
{
    public static class Compatibility
    {
        private static bool _checkedMRInventoryStack = false;
        private static bool _mrInventoryStack = false;
        public static bool MRInventoryStack
        {
            get
            {
                if (!_checkedMRInventoryStack)
                {
                    _mrInventoryStack = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("mades.redo.inventorystacking");
                    Plugin.Logger.LogInfo($"Mades Redo Inventory Stacking {( _mrInventoryStack ? "has been" : "has not been" )} detected");
                    _checkedMRInventoryStack = true;
                }
                return _mrInventoryStack;
            }
        }
        private static bool _checkedInventoryStacking = false;
        private static bool _inventoryStacking = false;
        public static bool InventoryStacking
        {
            get
            {
                if (!_checkedInventoryStacking)
                {
                    _inventoryStacking = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.Complot69.virtualstack");
                    Plugin.Logger.LogInfo($"Inventory Resource Stacking {(_inventoryStacking ? "has been" : "has not been")} detected");
                    _checkedInventoryStacking = true;
                }
                return _inventoryStacking;
            }
        }

        public static Assembly GetAssembly(string assemblyName)
        {
            try
            {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assembly.FullName.StartsWith(assemblyName)) return assembly;
                }
            }
            catch (Exception e)
            {
                Plugin.Logger.LogError(e.ToString());
            }
            Plugin.Logger.LogError($"Failed to find assembly {assemblyName}");
            return null;
        }

        public static Type GetType(string assemblyName, string typeName)
        {
            try
            {
                Assembly assembly = GetAssembly(assemblyName);
                return assembly.GetType(typeName);
            }
            catch (Exception e)
            {
                Plugin.Logger.LogError(e.ToString());
            }
            Plugin.Logger.LogError($"Failed to find assembly {assemblyName} for type {typeName}");
            return null;
        }

        public static MethodInfo GetMethod(string assemblyName, string typeName, string methodName)
        {
            try
            {
                Type type = GetType(assemblyName, typeName);
                return type.GetMethod(methodName);
            }
            catch(Exception e)
            {
                Plugin.Logger.LogError(e.ToString());
            }
            Plugin.Logger.LogError($"Failed to find method {methodName} in type {typeName} for assembly {assemblyName}");
            return null;
        }

        public static FieldInfo GetField(string assemblyName, string typeName, string fieldName)
        {
            try
            {
                Assembly assembly = GetAssembly(assemblyName);
                Type type = assembly.GetType(typeName);
                return AccessTools.Field(type, fieldName);
            }
            catch(Exception e)
            {
                Plugin.Logger.LogError(e.ToString());
            }
            Plugin.Logger.LogError($"Failed to find field {fieldName} in type {typeName} for assembly {assemblyName}");
            return null;
        }

        public static int ResourceCount(TechType techType)
        {
            Inventory inventory = Inventory.main;
            try
            {
                if (MRInventoryStack)
                {
                    Type stackType = GetType("MR_InventoryStacking", "MR_InventoryStacking.MRStackData");
                    FieldInfo amountField = AccessTools.Field(stackType, "amount");
                    int units = 0;
                    foreach (InventoryItem item in inventory.container)
                    {
                        if (item.techType != techType) continue;
                        Pickupable pickupable = item.item;
                        Component stack = pickupable.GetComponent(stackType);
                        if (stack == null) return inventory.GetPickupCount(techType);
                        int amount = (int) amountField.GetValue(stack);
                        units += stack != null && amount >= 1 ? amount : 1;
                    }
                    return units;
                }
            } 
            catch(Exception e) 
            {
                Plugin.Logger.LogError(e.ToString());
                Plugin.Logger.LogError("Failed to get stack size from MR Inventory Stacking.");
            }
            try
            {
                if (InventoryStacking)
                {
                    Type mainSaveData = GetType("PruebaDificultad", "InventoryStacks.ModSaveData");
                    Type virtualStackPlugin = GetType("PruebaDificultad", "InventoryStacks.VirtualStackPlugin");
                    PropertyInfo saveData = virtualStackPlugin?.GetProperty("MainSaveData");
                    FieldInfo extras = mainSaveData?.GetField("extras");
                    Dictionary<TechType, int> table = (Dictionary<TechType, int>) extras.GetValue(saveData.GetValue(null));
                    if (table.TryGetValue(techType, out int count)) return count + inventory.GetPickupCount(techType);
                }
            }
            catch (Exception e)
            {
                Plugin.Logger.LogError(e.ToString());
                Plugin.Logger.LogError("Failed to get stack size from InventoryStacking.");
            }
            return inventory.GetPickupCount(techType);
        }
    }
}