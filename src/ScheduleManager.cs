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

            var wait = new WaitForSeconds(59f); // wait just shy of 1 minute
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
            int onlineModifier, offlineModifier;
            string buff;
            if (newState == GameState.Build)
            {
                onlineModifier = 0;
                offlineModifier = 0;
                buff = BuffBuildModeName;
            }
            else
            {
                onlineModifier = DefaultLandClaimOnlineDurabilityModifier;
                offlineModifier = DefaultLandClaimOfflineDurabilityModifier;
                buff = BuffRaidModeName;
            }

            GameStats.Set(EnumGameStats.LandClaimOnlineDurabilityModifier, onlineModifier);
            GameStats.Set(EnumGameStats.LandClaimOfflineDurabilityModifier, offlineModifier);
            var netPackage = NetPackageManager.GetPackage<NetPackageGameStats>().Setup(GameStats.Instance);
            if (players.Length == 0)
            {
                var playerList = GameManager.Instance.World.Players.list;
                ConnectionManager.Instance.SendPackage(netPackage);
                for (var i = 0; i < playerList.Count; i++)
                {
                    _ = playerList[i].Buffs.AddBuff(buff);
                }
            }
            else
            {
                for (var i = 0; i < players.Length; i++)
                {
                    _ = players[i].Buffs.AddBuff(buff);
                    if (players[i].isEntityRemote)
                    {
                        ConnectionManager.Instance.Clients.ForEntityId(players[i].entityId)?.SendPackage(netPackage);
                    }
                }
            }
            _log.Debug($"Successfully updated player(s) to {newState} mode.");
        }
    }
}
