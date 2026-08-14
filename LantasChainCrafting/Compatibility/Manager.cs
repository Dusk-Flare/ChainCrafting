using System.Linq;
using UnityEngine;
using Resource = SextantHorizon.Utils.Resource;
using ResourceTable = SextantHorizon.Utils.ResourceTable;

namespace ChainCrafting.Compatibility
{
    public static class Manager
    {
        public static bool ExternalResources => OpenLockerAPI;
        public static float ExternalResourceRange { get; set; } = 30f;
        private static bool _checkedOpenLockerAPI = false;
        private static bool _openLockerAPI = false;
        public static bool OpenLockerAPI
        {
            get
            {
                if (!_checkedOpenLockerAPI)
                {
                    _openLockerAPI = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("OpenLockerAPI");
                    Plugin.Logger.LogInfo($"Open Locker API {( _openLockerAPI ? "has been" : "has not been" )} detected");
                    _checkedOpenLockerAPI = true;
                }
                return _openLockerAPI;
            }
        }
        public static bool ValidateExternal(ResourceTable resources, Vector3? searchOrigin = null) => resources.All(r => GetLocalPickupCount(r, searchOrigin) >= r.Amount);
        public static int GetLocalPickupCount(Resource resource, Vector3? searchOrigin = null) => GetLocalPickupCount(resource.Type, searchOrigin);
        public static int GetLocalPickupCount(TechType techType, Vector3? searchOrigin = null)
        {
            if (OpenLockerAPI)
            {
                return IOpenLockerAPI.GetLocalPickupCount(techType, searchOrigin);
            }
            return 0;
        }
        public static bool HasRoomForExternalResource(int x, int y, Vector3? searchOrigin = null)
        {
            if (OpenLockerAPI)
            {
                return IOpenLockerAPI.HasRoomForExternalResource(x, y, searchOrigin);
            }
            return false;
        }
        public static bool DepositExternalResource(InventoryItem resource, Vector3? depositOrigin = null)
        {
            if (OpenLockerAPI)
            {
                return IOpenLockerAPI.DepositExternalResource(resource, depositOrigin);
            }
            return false;
        }
        public static bool ConsumeExternalResources(ResourceTable resources, Vector3? consumeOrigin = null) => resources.All(r => ConsumeExternalResources(r, consumeOrigin));
        public static bool ConsumeExternalResources(Resource resource, Vector3? consumeOrigin = null)
        {
            if (OpenLockerAPI)
            {
                return IOpenLockerAPI.ConsumeExternalResources(resource, consumeOrigin);
            }
            return false;
        }
    }
}