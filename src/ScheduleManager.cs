using System;
using System.Collections;
using UnityEngine;

namespace RaidHours
{
    internal enum GameState
    {
        Build, Raid
    }

    internal class ScheduleManager
    {
        private static readonly ModLog<ScheduleManager> _log = new ModLog<ScheduleManager>();

        public static Coroutine TimeMonitorCoroutine { get; private set; }
        public static int DefaultLandClaimOnlineDurabilityModifier { get; private set; }
        public static int DefaultLandClaimOfflineDurabilityModifier { get; private set; }
        public static GameState CurrentState { get; private set; }
        public static GameState PreviousState { get; private set; }
        public static string BuffBuildModeName { get; private set; } = "raidHoursBuildMode";
        public static string BuffRaidModeName { get; private set; } = "raidHoursRaidMode";
        public static string CVarDefaultDefenseOnlineName { get; private set; } = "raidHoursDefaultDefenseOnline";
        public static string CVarDefaultDefenseOfflineName { get; private set; } = "raidHoursDefaultDefenseOffline";

        internal static void OnPlayerSpawnedInWorld(EntityPlayer player)
        {
            player.SetCVar(CVarDefaultDefenseOnlineName, DefaultLandClaimOnlineDurabilityModifier);
            player.SetCVar(CVarDefaultDefenseOfflineName, DefaultLandClaimOfflineDurabilityModifier);
            HandleStateChange(CurrentState, player);
        }

        internal static void OnGameStartDone()
        {
            SettingsManager.Load();
            TimeMonitorCoroutine = ThreadManager.StartCoroutine(MonitorTime());
        }

        internal static void OnGameShutdown()
        {
            ThreadManager.StopCoroutine(TimeMonitorCoroutine);
            _log.Info("Stopped time monitor coroutine");
        }

        private static IEnumerator MonitorTime()
        {
            // Store default values so we can switch back to them when raid currentTime begins
            DefaultLandClaimOnlineDurabilityModifier = GameStats.GetInt(EnumGameStats.LandClaimOnlineDurabilityModifier);
            DefaultLandClaimOfflineDurabilityModifier = GameStats.GetInt(EnumGameStats.LandClaimOfflineDurabilityModifier);

            var wait = new WaitForSeconds(15f);
            while (true)
            {
                CheckAndHandleStateChange();
                yield return wait;
            }
        }

        internal static void CheckAndHandleStateChange(params EntityPlayer[] players)
        {
            var currentTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, SettingsManager.TimeZoneInfo);

            CurrentState = SettingsManager.RaidModeStopTime.MinutesUntil(currentTime) < SettingsManager.RaidModeStartTime.MinutesUntil(currentTime)
                ? GameState.Raid
                : GameState.Build;

            if (CurrentState != PreviousState)
            {
                HandleStateChange(CurrentState, players);
                PreviousState = CurrentState;
            }
        }

        private static void HandleStateChange(GameState newState, params EntityPlayer[] players)
        {
            _log.Trace($"HandleStateChange: {newState}, {players.Length}");
            var buff = "";
            switch (newState)
            {
                case GameState.Build:
                    GameStats.Set(EnumGameStats.LandClaimOnlineDurabilityModifier, 0);
                    GameStats.Set(EnumGameStats.LandClaimOfflineDurabilityModifier, 0);
                    buff = BuffBuildModeName;
                    break;
                case GameState.Raid:
                    GameStats.Set(EnumGameStats.LandClaimOnlineDurabilityModifier, DefaultLandClaimOnlineDurabilityModifier);
                    GameStats.Set(EnumGameStats.LandClaimOfflineDurabilityModifier, DefaultLandClaimOfflineDurabilityModifier);
                    buff = BuffRaidModeName;
                    break;
            }

            if (players.Length == 0) // target all players (if none provided)
            {
                players = GameManager.Instance.World.Players.list.ToArray();
            }

            var world = GameManager.Instance.World;
            var netPackage = NetPackageManager.GetPackage<NetPackageGameStats>().Setup(GameStats.Instance);
            for (var i = 0; i < players.Length; i++)
            {
                if (players[i].isEntityRemote)
                {
                    // local players automatically see the adjusted GameStats values set above
                    ConnectionManager.Instance.Clients.ForEntityId(players[i].entityId)?.SendPackage(netPackage);
                }
                _ = players[i].Buffs.AddBuff(buff);
                EjectionManager.OnScheduledRaidModeChanged(world, players[i], CurrentState);
                BagDropManager.RefreshBagDropOnLogoutState(players[i], players[i].GetBlockPosition());
            }
            _log.Debug($"Successfully updated player(s) to {newState} mode.");
        }
    }
}
