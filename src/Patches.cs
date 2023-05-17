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
}
