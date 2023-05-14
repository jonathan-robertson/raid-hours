using HarmonyLib;
using System;

namespace RaidHours
{
    [HarmonyPatch(typeof(EntityAlive), "updateCurrentBlockPosAndValue")]
    internal class EntityAlive_updateCurrentBlockPosAndValue_Patches
    {
        private static readonly ModLog<EntityAlive_updateCurrentBlockPosAndValue_Patches> _log = new ModLog<EntityAlive_updateCurrentBlockPosAndValue_Patches>();

        public static void Postfix(EntityAlive __instance, Vector3i ___blockPosStandingOn)
        {
            try
            {
                RaidProtectionManager.OnEntityBlockPositionChanged(__instance, ___blockPosStandingOn);
            }
            catch (Exception e)
            {
                _log.Error($"EntityAlive_updateCurrentBlockPosAndValue_Patches Postfix failed: handle block pos change for {__instance}.", e);
            }
        }
    }
}
