using HarmonyLib;
using RaidHours.Managers;
using RaidHours.Utilities;
using System;

namespace RaidHours.Patches
{
    [HarmonyPatch(typeof(EntityAlive), "updateCurrentBlockPosAndValue")]
    internal class EntityAlive_updateCurrentBlockPosAndValue_Patch
    {
        private static readonly ModLog<EntityAlive_updateCurrentBlockPosAndValue_Patch> _log = new ModLog<EntityAlive_updateCurrentBlockPosAndValue_Patch>();

        /// <summary>
        /// Patch responsible for 'intercepting' crouch/jump controls if the given player is standing on a quantum block. 
        /// </summary>
        /// <param name="__instance">EntityAlive instance to check from.</param>
        public static void Postfix(EntityAlive __instance, Vector3i ___blockPosStandingOn)
        {
            try
            {
                if (ModApi.IsServer)
                {
                    BagDropManager.RefreshBagDropOnLogoutState(__instance, ___blockPosStandingOn);
                }
            }
            catch (Exception e)
            {
                _log.Error("Postfix", e);
            }
        }
    }
}
