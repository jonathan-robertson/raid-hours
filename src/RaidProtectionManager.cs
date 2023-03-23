using System.Collections;
using UnityEngine;

namespace RaidHours
{
    internal enum Relationship
    {
        None, Ally, Self
    }

    internal class RaidProtectionManager
    {
        private static readonly ModLog<RaidProtectionManager> _log = new ModLog<RaidProtectionManager>();

        internal static string ProtectionName { get; private set; } = "raidHoursRaidProtection";
        internal static string ProtectionWarpName { get; private set; } = "raidHoursRaidProtectionWarp";
        internal static string LoginProtectionWarpName { get; private set; } = "raidHoursLoginProtectionWarp";

        /// <summary>
        /// Eject any player within a non-friendly land claim. Even if raid mode is active, even if this lcb has raid protection disabled, we will still eject becuase (1) this help secure a base if hostiles log out inside the base during raid mode only to log back in during build mode and (2) this prevents players from being trolled with invisible cages during build hours.
        /// </summary>
        /// <param name="player">EntityPlayer in the process of spawning.</param>
        /// <param name="playerId">PlatformUserIdentifierAbs used to determine the relationship between this player and the LCB owner, should one be found.</param>
        /// <param name="blockPos">Vector3i position this player is spawning in at.</param>
        internal static void OnPlayerSpawnedInWorld(EntityPlayer player, PlatformUserIdentifierAbs playerId, Vector3i blockPos)
        {
            _log.Trace($"OnPlayerSpawnedInWorld: {player}, {playerId}, {blockPos}");
            if (TryGetLandClaimOwnerRelationship(playerId, blockPos, out _, out var relationship)
                && relationship == Relationship.None)
            {
                _ = ThreadManager.StartCoroutine(EjectLater(player, 0.5f));
            }
        }

        internal static void OnEntityBlockPositionChanged(EntityAlive entityAlive, Vector3i blockPosStandingOn)
        {
            if (!ModApi.IsServer
                || !SettingsManager.RaidProtectionEnabled
                || !(entityAlive is EntityPlayer player)
                || !TryGetPlayerIdFromEntityId(player.entityId, out var playerId)
                || !TryGetLandClaimOwnerRelationship(playerId, blockPosStandingOn, out var lcbBlockPos, out var relationship))
            {
                return;
            }

            var landClaimActive = IsLandClaimActive(lcbBlockPos, player.world);
            if (relationship == Relationship.Self)
            {
                player.Buffs.SetCustomVar(ProtectionName, landClaimActive ? +1 : -1);
                _ = player.Buffs.AddBuff(ProtectionName);
                return;
            }

            if (landClaimActive
                && ScheduleManager.CurrentState == GameState.Build
                && relationship != Relationship.Ally)
            {
                _ = player.Buffs.AddBuff(ProtectionWarpName);
                Eject(player);
                return;
            }
        }

        private static bool TryGetLandClaimOwnerRelationship(PlatformUserIdentifierAbs playerId, Vector3i playerBlockPos, out Vector3i landClaimBlockPos, out Relationship relationship)
        {
            if (!TryGetLandClaimContaining(playerBlockPos, out landClaimBlockPos, out var landClaimOwner))
            {
                landClaimBlockPos = Vector3i.zero;
                relationship = Relationship.None;
                return false;
            }

            relationship = PlatformUserIdentifierAbs.Equals(landClaimOwner.UserIdentifier, playerId)
                ? Relationship.Self // success
                : AreAllies(landClaimOwner, playerId)
                    ? Relationship.Ally // success
                    : Relationship.None;
            return true;
        }

        private static bool AreAllies(PersistentPlayerData ppData, PlatformUserIdentifierAbs otherPlayer)
        {
            return ppData.ACL != null && ppData.ACL.Contains(otherPlayer);
        }

        private static bool TryGetPlayerIdFromEntityId(int playerEntityId, out PlatformUserIdentifierAbs id)
        {
            var playerData = GameManager.Instance.persistentPlayers.GetPlayerDataFromEntityID(playerEntityId);
            if (playerData != null)
            {
                id = playerData.UserIdentifier;
                return true;
            }
            id = null;
            return false;
        }

        private static bool IsLandClaimActive(Vector3i lcbBlockPos, World world)
        {
            var chunkId = world.ChunkCache.ClusterIdx;
            return world.GetTileEntity(chunkId, lcbBlockPos) is TileEntityLandClaim tileEntityLandClaim && tileEntityLandClaim.ShowBounds;
        }

        private static bool TryGetLandClaimContaining(Vector3i blockPos, out Vector3i landClaimBlockPos, out PersistentPlayerData landClaimOwner)
        {
            foreach (var kvp in GameManager.Instance.persistentPlayers.m_lpBlockMap)
            {
                if (IsWithinLandClaimAtBlockPos(blockPos, kvp.Key))
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

        private static bool IsWithinLandClaimAtBlockPos(Vector3i blockPos, Vector3i landClaimPos)
        {
            return landClaimPos.x - ModApi.LandClaimRadiusMin - 1 <= blockPos.x
                    && blockPos.x <= landClaimPos.x + ModApi.LandClaimRadiusMin + 1
                && landClaimPos.z - ModApi.LandClaimRadiusMin - 1 <= blockPos.z
                    && blockPos.z <= landClaimPos.z + ModApi.LandClaimRadiusMin + 1;
        }

        private static IEnumerator EjectLater(EntityPlayer player, float delay)
        {
            _log.Info($"EjectLater: {player}, delayed for {delay}s");
            yield return new WaitForSeconds(delay);
            _ = player.Buffs.AddBuff(LoginProtectionWarpName);
            Eject(player);
        }

        private static void Eject(EntityPlayer player)
        {
            _log.Info($"Ejecting {player}");
            var rand = player.world.GetGameRandom();
            var normalized = new Vector3(0.5f - rand.RandomFloat, 0f, 0.5f - rand.RandomFloat).normalized;
            var vector = player.position + (normalized * 5f);
            var num = 20;
            while (TryGetLandClaimContaining(new Vector3i(vector), out var _, out var _) && --num > 0)
            {
                vector += normalized * 5f;
            }
            vector.y = player.world.GetHeight((int)vector.x, (int)vector.z) + 1;

            Warp(player, vector);
        }

        private static void Warp(EntityPlayer player, Vector3 destination)
        {
            _log.Info($"Warp {player} to {destination}");
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
