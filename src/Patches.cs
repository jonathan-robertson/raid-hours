using HarmonyLib;
using System;

namespace RaidHours
{
    [HarmonyPatch(typeof(Block), "DamageBlock")]
    internal class Block_DamageBlock_Patch
    {
        private static readonly ModLog<Block_DamageBlock_Patch> _log = new ModLog<Block_DamageBlock_Patch>();

        public static bool Prefix(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, int _damagePoints, int _entityIdThatDamaged, ref int __result)
        {
            try
            {
                if (EjectionManager.OnDamageBlock(_world, _blockPos, _entityIdThatDamaged))
                {
                    __result = 0;
                    return false;
                }
            }
            catch (Exception e)
            {
                _log.Error("Prefix", e);
            }
            return true;
        }
    }

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
                if (ModApi.IsServer
                    && ScheduleManager.CurrentState == GameState.Raid)
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
