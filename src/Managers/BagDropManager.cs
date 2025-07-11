﻿using RaidHours.Utilities;

namespace RaidHours.Managers
{
    /// <summary>
    /// Type that will identify the configured drop option on disconnect or whatnot.
    /// </summary>
    /// <remarks>Copied from EntityPlayerLocal.DropOption</remarks>
    internal enum DropOption
    {
        None, All, Toolbelt, Backpack, DeleteAll
    }

    internal class BagDropManager
    {
        private static readonly ModLog<BagDropManager> _log = new ModLog<BagDropManager>();

        public const string CVAR_BAG_DROP_MODE_NAME = "raidHoursBagDropMode";
        public const string BUFF_DROP_MODE_NAME = "raidHoursDropMode";
        public static DropOption Default { get; set; } = DropOption.None;

        public static void RefreshBagDropOnLogoutState(EntityAlive entityAlive, Vector3i blockPos)
        {
            if (entityAlive is EntityPlayer player)
            {
                if (!player.IsSpectator
                    && Helpers.TryGetPlayerIdFromEntityId(player.entityId, out var playerId)
                    && Helpers.TryGetLandClaimOwnerRelationship(playerId, blockPos, out _, out var relationship)
                    && relationship == Relationship.None)
                {
                    UpdateBagDropOnLogoutState(player, DropOption.Backpack);
                }
                else
                {
                    RestoreDefaultBagDropOnLogoutState(player);
                }
            }
        }

        private static void UpdateBagDropOnLogoutState(EntityPlayer player, DropOption dropOption)
        {
            var bagDropOptionInt = (int)dropOption;
            if (player.GetCVar(CVAR_BAG_DROP_MODE_NAME) == bagDropOptionInt)
            {
                return;
            }
            _log.Trace($"UpdateBagDropOnLogout: {player.entityId} -> {dropOption}");

            if (player.isEntityRemote)
            {
                var prefValue = GameStats.GetInt(EnumGameStats.DropOnQuit);
                var clientInfo = ConnectionManager.Instance.Clients.ForEntityId(player.entityId);
                GameStats.Set(EnumGameStats.DropOnQuit, bagDropOptionInt);
                var netPackage = NetPackageManager.GetPackage<NetPackageGameStats>().Setup(GameStats.Instance);
                clientInfo?.SendPackage(netPackage);
                GameStats.Set(EnumGameStats.DropOnQuit, prefValue);
            }
            else
            {
                GameStats.Set(EnumGameStats.DropOnQuit, bagDropOptionInt); // note: does not support split screen
            }
            player.SetCVar(CVAR_BAG_DROP_MODE_NAME, bagDropOptionInt);
        }

        private static void RestoreDefaultBagDropOnLogoutState(EntityPlayer player)
        {
            if (player.GetCVar(CVAR_BAG_DROP_MODE_NAME) == (int)Default)
            {
                return;
            }
            _log.Trace($"RestoreDefaultBagDropOnLogout: {player.entityId}");

            player.Buffs.RemoveBuff(BUFF_DROP_MODE_NAME);
            if (player.isEntityRemote)
            {
                GameStats.Set(EnumGameStats.DropOnQuit, (int)Default);
                var clientInfo = ConnectionManager.Instance.Clients.ForEntityId(player.entityId);
                clientInfo?.SendPackage(NetPackageManager.GetPackage<NetPackageGameStats>().Setup(GameStats.Instance));
            }
            else
            {
                GameStats.Set(EnumGameStats.DropOnQuit, (int)Default); // note: does not support split screen
            }
            player.SetCVar(CVAR_BAG_DROP_MODE_NAME, 0);
        }
    }
}
