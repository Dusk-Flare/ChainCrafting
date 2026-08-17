extern alias OpenLocker;
using OpenLocker::OpenLockerAPI;
using UnityEngine;
using static ChainCrafting.Compatibility.Manager;
using Resource = SextantHorizon.Utils.Resource;

namespace ChainCrafting.Compatibility
{
    internal class IOpenLockerAPI
    {
        public static int GetLocalPickupCount(TechType techType, Vector3? searchOrigin = null)
        {
            return Logic.GetLocalPickupCount(techType, searchOrigin, ExternalResourceRange);
        }
        public static bool HasRoomForExternalResource(int x, int y, Vector3? searchOrigin = null)
        {
            return Logic.HasRoomForLocalResource(x, y, searchOrigin, ExternalResourceRange);
        }
        public static bool DepositExternalResource(InventoryItem resource, Vector3? depositOrigin = null)
        {
            return Logic.DepositLocalResource(resource, depositOrigin, ExternalResourceRange);
        }
        public static bool ConsumeExternalResources(Resource resource, Vector3? consumeOrigin = null)
        {
            return Logic.ConsumeLocalResource(resource.Type, resource.Amount, consumeOrigin, ExternalResourceRange);
        }
    }
}
