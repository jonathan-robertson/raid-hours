using System.Collections;
using UnityEngine;

namespace RaidHours
{
    internal class Util
    {
        public static int SafelyGetEntityIdFor(ClientInfo clientInfo)
        {
            return clientInfo != null
                ? clientInfo.entityId
                : GameManager.Instance.persistentLocalPlayer.EntityId;
        }

        public static bool TryGetUserIdFor(ClientInfo clientInfo, out PlatformUserIdentifierAbs userId)
        {
            userId = clientInfo != null
                ? GameManager.Instance.persistentPlayers.GetPlayerDataFromEntityID(clientInfo.entityId)?.UserIdentifier
                : GameManager.Instance.persistentLocalPlayer.UserIdentifier;
            return userId != null;
        }

        public static bool TryGetPlayerIdFromEntityId(int playerEntityId, out PlatformUserIdentifierAbs id)
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

        public static bool IsZombieOrAnimal(WorldBase world, int entityId)
        {
            var entity = world.GetEntity(entityId);
            return entity != null
                && (entity.entityType == EntityType.Zombie || entity.entityType == EntityType.Animal);
        }

        public static bool TryGetLandClaimOwnerRelationship(PlatformUserIdentifierAbs playerId, Vector3i playerBlockPos, out Vector3i landClaimBlockPos, out Relationship relationship)
        {
            if (!TryGetActiveLandClaimContaining(playerBlockPos, out landClaimBlockPos, out var landClaimOwner))
            {
                landClaimBlockPos = Vector3i.zero;
                relationship = Relationship.None;
                return false;
            }

            relationship = GetRelationship(landClaimOwner, playerId);
            return true;
        }

        public static bool IsLandClaimOccupiedByOwnerOrAllies(WorldBase world, Vector3i landClaimPos, PersistentPlayerData owner)
        {
            var players = world.GetPlayers();
            for (var i = 0; i < players.Count; i++)
            {
                if (TryGetPlayerIdFromEntityId(players[i].entityId, out var playerId)
                    && GetRelationship(owner, playerId) != Relationship.None
                    && IsWithinLandClaimAtBlockPos(players[i].GetBlockPosition(), landClaimPos))
                {
                    return true;
                }
            }
            return false;
        }

        public static Relationship GetRelationship(PersistentPlayerData ppData, PlatformUserIdentifierAbs otherPlayerId)
        {
            return PlatformUserIdentifierAbs.Equals(ppData.UserIdentifier, otherPlayerId)
                ? Relationship.Self
                : AreAllies(ppData, otherPlayerId)
                    ? Relationship.Ally
                    : Relationship.None;
        }

        public static bool AreAllies(PersistentPlayerData ppData, PlatformUserIdentifierAbs otherPlayer)
        {
            return ppData.ACL != null && ppData.ACL.Contains(otherPlayer);
        }

        public static bool TryGetActiveLandClaimContaining(Vector3i blockPos, out Vector3i landClaimBlockPos, out PersistentPlayerData landClaimOwner)
        {
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

        public static bool IsWithinLandClaimAtBlockPos(Vector3i blockPos, Vector3i landClaimPos)
        {
            return landClaimPos.x - ModApi.LandClaimRadiusMin - 1 <= blockPos.x
                    && blockPos.x <= landClaimPos.x + ModApi.LandClaimRadiusMin + 1
                && landClaimPos.z - ModApi.LandClaimRadiusMin - 1 <= blockPos.z
                    && blockPos.z <= landClaimPos.z + ModApi.LandClaimRadiusMin + 1;
        }

        public static IEnumerator EjectLater(EntityPlayer player, float delay, string buffName)
        {
            yield return new WaitForSeconds(delay);
            Warp(player, FindPositionOutsideLandClaim(player), buffName);
        }

        public static void EjectEntitiesFromLandClaim(World world, Vector3i landClaimPos, string buffName)
        {
            EntityAlive entity;
            var bounds = new Bounds(landClaimPos, ModApi.LandClaimBoundsSize);
            var entities = world.GetLivingEntitiesInBounds(null, bounds);
            for (var i = 0; i < entities.Count; i++)
            {
                entity = entities[i];
                if (entity is EntityPlayer player)
                {
                    if (!player.IsSpectator)
                    {
                        Eject(player, landClaimPos, buffName);
                    }
                }
                else
                {
                    Eject(entity, landClaimPos);
                }
            }
        }

        public static void Eject(EntityAlive entity, Vector3i landClaimPos)
        {
            if (IsWithinLandClaimAtBlockPos(entity.GetBlockPosition(), landClaimPos))
            {
                entity.SetPosition(FindPositionOutsideLandClaim(entity), true);
            }
        }

        public static void Eject(EntityPlayer player, Vector3i landClaimPos, string buffName)
        {
            if (IsWithinLandClaimAtBlockPos(player.GetBlockPosition(), landClaimPos))
            {
                Warp(player, FindPositionOutsideLandClaim(player), buffName);
            }
        }

        private static Vector3 FindPositionOutsideLandClaim(EntityAlive entity)
        {
            var rand = entity.world.GetGameRandom();
            var normalized = new Vector3(0.5f - rand.RandomFloat, 0f, 0.5f - rand.RandomFloat).normalized;
            var vector = entity.position + (normalized * 5f);
            var num = 20;
            while (TryGetActiveLandClaimContaining(new Vector3i(vector), out var _, out var _) && --num > 0)
            {
                vector += normalized * 5f;
            }
            vector.y = entity.world.GetHeight((int)vector.x, (int)vector.z) + 1;
            return vector;
        }

        public static void EjectPlayersFromClaimedLand(WorldBase world, Vector3i landClaimPos, string buffName)
        {
            EntityPlayer player;
            for (var i = 0; i < world.GetPlayers().Count; i++)
            {
                player = world.GetPlayers()[i];
                if (!player.IsSpectator
                    && IsWithinLandClaimAtBlockPos(player.GetBlockPosition(), landClaimPos))
                {
                    Warp(player, FindPositionOutsideLandClaim(player), buffName);
                }
            }
        }

        public static void Warp(EntityPlayer player, Vector3 destination, string buffName)
        {
            _ = player.Buffs.AddBuff(buffName);
            if (player is EntityPlayerLocal localPlayer)
            {
                localPlayer.TeleportToPosition(destination, false);
            }
            else
            {
                var client = SingletonMonoBehaviour<ConnectionManager>.Instance.Clients.ForEntityId(player.entityId);
                client?.SendPackage(NetPackageManager.GetPackage<NetPackageTeleportPlayer>().Setup(destination, null, false));
            }
            BagDropManager.RefreshBagDropOnLogoutState(player, new Vector3i(destination));
        }
    }
}
