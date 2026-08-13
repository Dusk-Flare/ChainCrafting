using SextantHorizon.Utils;
using System;
using System.Linq;
using System.Reflection;

namespace ChainCrafting.Utils
{
    public static class Compatibility
    {
        public static bool ExternalResources => OpenLockerAPI;
        public static float ExternalResourceRange { get; set; } = 30f;
        private static bool _checkedOpenLockerLib = false;
        private static bool _openLockerAPI = false;
        public static bool OpenLockerAPI
        {
            get
            {
                if (!_checkedOpenLockerLib)
                {
                    _openLockerAPI = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("OpenLockerAPI");
                    Plugin.Logger.LogInfo($"Open Locker API {( _openLockerAPI ? "has been" : "has not been" )} detected");
                    _checkedOpenLockerLib = true;
                }
                return _openLockerAPI;
            }
        }
        public static bool ValidateExternal(ResourceTable resources) => resources.All(r => GetLocalPickupCount(r) >= r.Amount);
        public static int GetLocalPickupCount(Resource resource) => GetLocalPickupCount(resource.Type);
        public static int GetLocalPickupCount(TechType techType)
        {
            try
            {
                if (OpenLockerAPI)
                {
                    MethodInfo getLocalPickupCount = Reflection.GetMethod("OpenLockerAPI", "OpenLockerAPI.Logic", "GetLocalPickupCount");
                    return (int) getLocalPickupCount.Invoke(null, new object[] { techType, ExternalResourceRange });
                }
            }
            catch (Exception e)
            {
                Plugin.Logger.LogCatch(e);
                Plugin.Logger.LogError("Failed to check for external resource from Open Locker API.");
            }
            return 0;
        }

        public static bool ConsumeExternalResources(ResourceTable resources) => resources.All(ConsumeExternalResources);
        public static bool ConsumeExternalResources(Resource resource)
        {
            try
            {
                if (OpenLockerAPI)
                {
                    MethodInfo consumeLocalResource = Reflection.GetMethod("OpenLockerAPI", "OpenLockerAPI.Logic", "ConsumeLocalResource");
                    bool consumed = (bool)consumeLocalResource.Invoke(null, new object[] { resource.Type, resource.Amount, ExternalResourceRange });
                    if(!consumed) Plugin.Logger.LogError($"Failed to consume {resource}");
                    return consumed;
                }
            }
            catch (Exception e)
            {
                Plugin.Logger.LogCatch(e);
                Plugin.Logger.LogError("Failed to consume external resource from Open Locker API.");
            }
            return false;
        }
    }
}