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
        internal static string SquattingProtectionWarpName { get; private set; } = "raidHoursSquattingProtectionWarp";

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
                && Util.TryGetLandClaimOwnerRelationship(playerId, blockPos, out _, out var relationship)
                && relationship == Relationship.None)
            {
                _ = ThreadManager.StartCoroutine(Util.EjectLater(player, 0.5f, SquattingProtectionWarpName));
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
            if (Util.IsZombieOrAnimal(world, entityIdThatDamaged)
                && Util.TryGetActiveLandClaimContaining(blockPos, out var landClaimPos, out var landClaimOwner)
                && !Util.IsLandClaimOccupiedByOwnerOrAllies(world, landClaimPos, landClaimOwner))
            {
                _log.Trace($"damage from {entityIdThatDamaged} was prevented");
                if (world.GetAIDirector().BloodMoonComponent.BloodMoonActive)
                {
                    Util.EjectEntitiesFromLandClaim(GameManager.Instance.World, landClaimPos, MobRaidingProtectionWarpName);
                }
                else
                {
                    _ = world.RemoveEntity(entityIdThatDamaged, EnumRemoveEntityReason.Despawned);
                }
                return true;
            }
            return false;
        }
    }
}
