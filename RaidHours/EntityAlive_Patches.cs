using HarmonyLib;
using System;
using UnityEngine;

namespace RaidHours
{
    [HarmonyPatch(typeof(EntityAlive), "updateCurrentBlockPosAndValue")]
    internal class EntityAlive_updateCurrentBlockPosAndValue_Patches
    {
        private static readonly ModLog<EntityAlive_updateCurrentBlockPosAndValue_Patches> _log = new ModLog<EntityAlive_updateCurrentBlockPosAndValue_Patches>();

        /// <summary>
        /// Concurrency-safe dictionary for recording what the last state was.
        /// Used to identify "the moment" player switches from neutral stance to jumping or crouching to simulate single-button reaction
        /// and avoid multiple triggers when player holds key down.
        /// </summary>
        //private static readonly ConcurrentDictionary<int, PlayerState> _prevStates = new ConcurrentDictionary<int, PlayerState>();

        public static void Postfix(EntityAlive __instance, Vector3i ___blockPosStandingOn)
        {
            try
            {
                if (ModApi.IsServer &&
                    Settings.AntiZombieRaid &&
                    ScheduleManager.CurrentState == GameState.Build &&
                    __instance is EntityPlayer player &&
                    TryGetLandClaimOwnerContaining(___blockPosStandingOn, out var owner, out var lcbBlockPos) &&
                    AreHostile(player, owner))
                {
                    //_log.Debug($"Player {__instance} found to be within range of land claim owned by {owner} during build mode and is being warped elsewhere.");
                    Eject(player, lcbBlockPos);
                }
            }
            catch (Exception e)
            {
                _log.Error($"EntityAlive_updateCurrentBlockPosAndValue_Patches Postfix failed: handle block pos change for {__instance}.", e);
            }
        }

        private static bool AreHostile(EntityPlayer player, PersistentPlayerData lcbOwner)
        {
            return lcbOwner.ACL == null
                || !lcbOwner.ACL.Contains(GameManager.Instance.persistentPlayers.GetPlayerDataFromEntityID(player.entityId).UserIdentifier);
        }

        private static bool TryGetLandClaimOwnerContaining(Vector3i pos, out PersistentPlayerData owner, out Vector3i lcbBlockPos)
        {
            foreach (var kvp in GameManager.Instance.persistentPlayers.m_lpBlockMap)
            {
                if (kvp.Key.x - ModApi.LandClaimRadiusMin - 1 <= pos.x && pos.x <= kvp.Key.x + ModApi.LandClaimRadiusMin + 1 &&
                    kvp.Key.z - ModApi.LandClaimRadiusMin - 1 <= pos.z && pos.z <= kvp.Key.z + ModApi.LandClaimRadiusMin + 1)
                {
                    lcbBlockPos = kvp.Key;
                    owner = kvp.Value;
                    return true;
                }
            }
            lcbBlockPos = Vector3i.zero;
            owner = null;
            return false;
        }

        private static void Eject(EntityPlayer player, Vector3i lcbBlockPos)
        {
            var circle = GameManager.Instance.World.GetGameRandom().RandomOnUnitCircle;
            var offset = new Vector3((circle.x + 1) * ModApi.LandClaimRadiusMax, 0, (circle.y + 1) * ModApi.LandClaimRadiusMax);
            //_log.Debug($"offset: {offset}");
            var newPos = lcbBlockPos + offset;
            newPos.y = GameManager.Instance.World.GetHeightAt(newPos.x, newPos.z) + 1; // determine surface height 

            if (!player.isEntityRemote) // local
            {
                if (!player.IsFlyMode.Value)
                {
                    if (player.AttachedToEntity)
                    {
                        player.AttachedToEntity.SetPosition(newPos);
                    }
                    else
                    {
                        player.SetPosition(newPos);
                    }
                    // TODO: warp vehicles also
                    _ = player.Buffs.AddBuff("antiZombieRaidProtection");
                    return;
                }
            }

            var clientInfo = ConnectionManager.Instance.Clients.ForEntityId(player.entityId);
            if (clientInfo != null)
            {
                // TODO: confirm that player is warped with vehicle
                clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageTeleportPlayer>().Setup(newPos, player.rotation, true));
                _ = player.Buffs.AddBuff("antiZombieRaidProtection");
                return;
            }
            else
            {
                _log.Debug($"server thinks entity {player.entityId} being pushed is a player, but couldn't find a client connection for it... could've been due to player disconnection at *just* the right time... still strange.");
            }
        }
    }
}
