using System.Collections;
using UnityEngine;

namespace RaidHours
{
    internal enum Relationship
    {
        None, Ally, Self
    }

    internal class EjectionManager
    {
        private static readonly ModLog<EjectionManager> _log = new ModLog<EjectionManager>();

        internal static string MobRaidingProtectionWarpName { get; private set; } = "raidHoursMobRaidingProtectionWarp";
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
            if (!player.IsSpectator
                && TryGetLandClaimOwnerRelationship(playerId, blockPos, out _, out var relationship)
                && relationship == Relationship.None)
            {
                _ = ThreadManager.StartCoroutine(EjectLater(player, 0.5f));
            }
        }

        /// <summary>
        /// Handle block damage at the given position and return whether it should be nullified.
        /// </summary>
        /// <param name="world">WorldBase used for fetching/filtering through world entities.</param>
        /// <param name="blockPos">Vector3i of the block that was hit.</param>
        /// <param name="entityIdThatDamaged">entityId of the entity that hit the block at the provided position.</param>
        /// <returns>Whether the block's damage should be nullified.</returns>
        internal static bool OnDamageBlock(WorldBase world, Vector3i blockPos, int entityIdThatDamaged)
        {
            _log.Trace($"OnDamageBlock: {blockPos}, {entityIdThatDamaged}");
            if (IsZombieOrAnimal(world, entityIdThatDamaged)
                && TryGetActiveLandClaimContaining(blockPos, out var landClaimPos, out var landClaimOwner)
                && !IsLandClaimOccupiedByOwnerOrAllies(world, landClaimPos, landClaimOwner))
            {
                _log.Trace($"damage from {entityIdThatDamaged} was prevented");
                if (world.GetAIDirector().BloodMoonComponent.BloodMoonActive)
                {
                    EjectEntitiesFromLandClaim(GameManager.Instance.World, landClaimPos);
                }
                else
                {
                    _ = world.RemoveEntity(entityIdThatDamaged, EnumRemoveEntityReason.Despawned);
                    //EjectPlayersFromClaimedLand(world, landClaimPos);
                }
                return true;
            }
            return false;
        }

        private static bool IsZombieOrAnimal(WorldBase world, int entityId)
        {
            _log.Trace($"IsZombieOrAnimal: {entityId}");
            var entity = world.GetEntity(entityId);
            return entity != null
                && (entity.entityType == EntityType.Zombie || entity.entityType == EntityType.Animal);
        }

        private static bool TryGetLandClaimOwnerRelationship(PlatformUserIdentifierAbs playerId, Vector3i playerBlockPos, out Vector3i landClaimBlockPos, out Relationship relationship)
        {
            _log.Trace($"TryGetLandClaimOwnerRelationship: {playerId}, {playerBlockPos}");
            if (!TryGetActiveLandClaimContaining(playerBlockPos, out landClaimBlockPos, out var landClaimOwner))
            {
                landClaimBlockPos = Vector3i.zero;
                relationship = Relationship.None;
                return false;
            }

            relationship = GetRelationship(landClaimOwner, playerId);
            return true;
        }

        private static bool IsLandClaimOccupiedByOwnerOrAllies(WorldBase world, Vector3i landClaimPos, PersistentPlayerData owner)
        {
            _log.Trace($"IsLandClaimOccupiedByOwnerOrAllies: {landClaimPos}, {owner.PlayerName}");
            var players = world.GetPlayers();
            for (var i = 0; i < players.Count; i++)
            {
                if (ModApi.TryGetPlayerIdFromEntityId(players[i].entityId, out var playerId)
                    && GetRelationship(owner, playerId) != Relationship.None
                    && IsWithinLandClaimAtBlockPos(players[i].GetBlockPosition(), landClaimPos))
                {
                    return true;
                }
            }
            return false;
        }

        private static Relationship GetRelationship(PersistentPlayerData ppData, PlatformUserIdentifierAbs otherPlayerId)
        {
            return PlatformUserIdentifierAbs.Equals(ppData.UserIdentifier, otherPlayerId)
                ? Relationship.Self
                : AreAllies(ppData, otherPlayerId)
                    ? Relationship.Ally
                    : Relationship.None;
        }

        private static bool AreAllies(PersistentPlayerData ppData, PlatformUserIdentifierAbs otherPlayer)
        {
            return ppData.ACL != null && ppData.ACL.Contains(otherPlayer);
        }

        private static bool TryGetActiveLandClaimContaining(Vector3i blockPos, out Vector3i landClaimBlockPos, out PersistentPlayerData landClaimOwner)
        {
            _log.Trace($"TryGetActiveLandClaimContaining {blockPos}");
            foreach (var kvp in GameManager.Instance.persistentPlayers.m_lpBlockMap)
            {
                if (ModApi.LandClaimExpiryHours > kvp.Value.OfflineHours // player's land claim must not be expired
                    && IsWithinLandClaimAtBlockPos(blockPos, kvp.Key))
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
            _log.Trace($"EjectLater: {player}, delayed for {delay}s");
            yield return new WaitForSeconds(delay);
            _ = player.Buffs.AddBuff(LoginProtectionWarpName);
            Eject(player);
        }

        private static void Eject(EntityAlive entity)
        {
            _log.Trace($"Ejecting {entity.entityId}");
            var rand = entity.world.GetGameRandom();
            var normalized = new Vector3(0.5f - rand.RandomFloat, 0f, 0.5f - rand.RandomFloat).normalized;
            var vector = entity.position + (normalized * 5f);
            var num = 20;
            while (TryGetActiveLandClaimContaining(new Vector3i(vector), out var _, out var _) && --num > 0)
            {
                vector += normalized * 5f;
            }
            vector.y = entity.world.GetHeight((int)vector.x, (int)vector.z) + 1;

            Warp(entity, vector);
        }

        private static void EjectEntitiesFromLandClaim(World world, Vector3i landClaimPos)
        {
            _log.Trace($"EjectEntitiesFromLandClaim at {landClaimPos}");
            EntityAlive entity;
            var bounds = new Bounds(landClaimPos, ModApi.LandClaimBoundsSize);
            var entities = world.GetLivingEntitiesInBounds(null, bounds);
            for (var i = 0; i < entities.Count; i++)
            {
                entity = entities[i];
                if (entity is EntityPlayer player && player.IsSpectator) { continue; }
                if (IsWithinLandClaimAtBlockPos(entity.GetBlockPosition(), landClaimPos))
                {
                    _ = entity.Buffs.AddBuff(MobRaidingProtectionWarpName);
                    Eject(entity);
                }
            }
        }

        private static void EjectPlayersFromClaimedLand(WorldBase world, Vector3i landClaimPos)
        {
            _log.Trace($"EjectPlayersFromClaimedLand at {landClaimPos}");
            EntityPlayer player;
            for (var i = 0; i < world.GetPlayers().Count; i++)
            {
                player = world.GetPlayers()[i];
                if (!player.IsSpectator
                    && IsWithinLandClaimAtBlockPos(player.GetBlockPosition(), landClaimPos))
                {
                    _ = player.Buffs.AddBuff(MobRaidingProtectionWarpName);
                    Eject(player);
                }
            }
        }

        private static void Warp(EntityAlive entity, Vector3 destination)
        {
            if (entity is EntityZombie || entity is EntityAnimal)
            {
                entity.SetPosition(destination, true);
                return;
            }
            if (entity is EntityPlayer player)
            {
                Warp(player, destination);
                return;
            }
        }

        private static void Warp(EntityPlayer player, Vector3 destination)
        {
            _log.Trace($"Warp {player} to {destination}");
            if (player.isEntityRemote)
            {
                SingletonMonoBehaviour<ConnectionManager>.Instance.Clients.ForEntityId(player.entityId).SendPackage(NetPackageManager.GetPackage<NetPackageTeleportPlayer>().Setup(destination, null, false));
                return;
            }
            if (player != null) // TODO: this seems... off
            {
                player.Teleport(destination, float.MinValue);
                return;
            }
            if (player.AttachedToEntity != null) // TODO: this seems... off
            {
                player.AttachedToEntity.SetPosition(destination, true);
                return;
            }
            player.SetPosition(destination, true); // TODO: this seems... off
        }
    }
}
