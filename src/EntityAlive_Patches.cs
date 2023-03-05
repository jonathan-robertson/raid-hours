using HarmonyLib;
using System;
using UnityEngine;

namespace RaidHours
{
    internal enum Relationship
    {
        None, Ally, Self
    }

    [HarmonyPatch(typeof(EntityAlive), "updateCurrentBlockPosAndValue")]
    internal class EntityAlive_updateCurrentBlockPosAndValue_Patches
    {
        private static readonly ModLog<EntityAlive_updateCurrentBlockPosAndValue_Patches> _log = new ModLog<EntityAlive_updateCurrentBlockPosAndValue_Patches>();

        internal static string RaidProtectionName { get; private set; } = "raidHoursRaidProtection";
        internal static string RaidProtectionWarpName { get; private set; } = "raidHoursRaidProtectionWarp";

        public static void Postfix(EntityAlive __instance, Vector3i ___blockPosStandingOn, World ___world)
        {
            try
            {
                if (!ModApi.IsServer ||
                    !SettingsManager.RaidProtectionEnabled ||
                    __instance.entityType != EntityType.Player ||
                    !TryGetLandClaimOwnerRelationship(__instance.entityId, ___blockPosStandingOn, out var lcbBlockPos, out var relationship))
                {
                    return;
                }

                var landClaimActive = IsLandClaimActive(lcbBlockPos, ___world);
                if (relationship == Relationship.Self)
                {
                    var player = __instance as EntityPlayer;
                    player.Buffs.SetCustomVar(RaidProtectionName, landClaimActive ? +1 : -1);
                    _ = player.Buffs.AddBuff(RaidProtectionName);
                    return;
                }

                if (landClaimActive &&
                    ScheduleManager.CurrentState == GameState.Build &&
                    relationship != Relationship.Ally)
                {
                    Eject(__instance as EntityPlayer);
                    return;
                }
            }
            catch (Exception e)
            {
                _log.Error($"EntityAlive_updateCurrentBlockPosAndValue_Patches Postfix failed: handle block pos change for {__instance}.", e);
            }
        }

        private static bool TryGetLandClaimOwnerRelationship(int playerEntityId, Vector3i playerBlockStandingOn, out Vector3i landClaimBlockPos, out Relationship relationship)
        {
            if (!TryGetLandClaimWithin(playerBlockStandingOn, out landClaimBlockPos, out var landClaimOwner))
            {
                landClaimBlockPos = Vector3i.zero;
                relationship = Relationship.None;
                return false;
            }

            relationship = landClaimOwner.EntityId == playerEntityId
                ? Relationship.Self
                : landClaimOwner.ACL != null && landClaimOwner.ACL.Contains(GetPlayerIdFromEntityId(playerEntityId))
                    ? Relationship.Ally
                    : Relationship.None;
            return true;
        }

        private static PlatformUserIdentifierAbs GetPlayerIdFromEntityId(int playerEntityId)
        {
            return GameManager.Instance.persistentPlayers.GetPlayerDataFromEntityID(playerEntityId).UserIdentifier;
        }

        private static bool IsLandClaimActive(Vector3i lcbBlockPos, World world)
        {
            var chunkId = world.ChunkCache.ClusterIdx;
            return world.GetTileEntity(chunkId, lcbBlockPos) is TileEntityLandClaim tileEntityLandClaim && tileEntityLandClaim.ShowBounds;
        }

        private static bool TryGetLandClaimWithin(Vector3i pos, out Vector3i landClaimBlockPos, out PersistentPlayerData landClaimOwner)
        {
            foreach (var kvp in GameManager.Instance.persistentPlayers.m_lpBlockMap)
            {
                if (IsWithinLandClaimAtBlockPos(pos, kvp.Key))
                {
                    landClaimBlockPos = kvp.Key;
                    landClaimOwner = kvp.Value;
                    return true;
                }
            }
            landClaimBlockPos = Vector3i.zero;
            landClaimOwner = null;
            return false;
        }

        private static bool IsWithinLandClaimAtBlockPos(Vector3i pos, Vector3i landClaimPos)
        {
            return landClaimPos.x - ModApi.LandClaimRadiusMin - 1 <= pos.x && pos.x <= landClaimPos.x + ModApi.LandClaimRadiusMin + 1
                && landClaimPos.z - ModApi.LandClaimRadiusMin - 1 <= pos.z && pos.z <= landClaimPos.z + ModApi.LandClaimRadiusMin + 1;
        }

        private static void Eject(EntityPlayer player)
        {
            if (player.IsFlyMode.Value)
            {
                return;
            }

            var rand = player.world.GetGameRandom();
            var normalized = new Vector3(0.5f - rand.RandomFloat, 0f, 0.5f - rand.RandomFloat).normalized;
            var vector = player.position + (normalized * 5f);
            var num = 20;
            while (TryGetLandClaimWithin(new Vector3i(vector), out var _, out var _) && --num > 0)
            {
                vector += normalized * 5f;
            }
            vector.y = player.world.GetHeight((int)vector.x, (int)vector.z) + 1;

            _ = player.Buffs.AddBuff(RaidProtectionWarpName);

            // TODO: also facing player away from LCB... could this be ideal? not possible with remote vehicles

            Warp(player, vector);
        }

        private static void Warp(EntityPlayer player, Vector3 destination)
        {
            if (player.isEntityRemote)
            {
                SingletonMonoBehaviour<ConnectionManager>.Instance.Clients.ForEntityId(player.entityId).SendPackage(NetPackageManager.GetPackage<NetPackageTeleportPlayer>().Setup(destination, null, false));
                return;
            }
            if (player)
            {
                player.Teleport(destination, float.MinValue);
                return;
            }
            if (player.AttachedToEntity != null)
            {
                player.AttachedToEntity.SetPosition(destination, true);
                return;
            }
            player.SetPosition(destination, true);
        }
    }
}
