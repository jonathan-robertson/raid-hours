using HarmonyLib;
using RaidHours.Managers;
using RaidHours.Utilities;
using System;

namespace RaidHours.Patches
{
    [HarmonyPatch(typeof(Block), nameof(Block.DamageBlock))]
    internal class Block_DamageBlock_Patch
    {
        private static readonly ModLog<Block_DamageBlock_Patch> _log = new ModLog<Block_DamageBlock_Patch>();

        public static bool Prefix(WorldBase _world, Vector3i _blockPos, int _entityIdThatDamaged, ref int __result)
        {
            try
            {
                if (ModApi.IsServer && EjectionManager.OnDamageBlock(_world, _blockPos, _entityIdThatDamaged))
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
}
