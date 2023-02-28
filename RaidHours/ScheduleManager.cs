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

        private static TimeZoneInfo _timeZoneInfo;

        public static Coroutine TimeMonitorCoroutine { get; private set; }
        public static int DefaultLandClaimOnlineDurabilityModifier { get; private set; }
        public static int DefaultLandClaimOfflineDurabilityModifier { get; private set; }
        public static GameState CurrentState { get; private set; }
        public static GameState PreviousState { get; private set; }
        public static string BuffBuildModeName { get; private set; } = "stateBuildMode";
        public static string BuffRaidModeName { get; private set; } = "stateRaidMode";
        public static string CVarDefaultDefenseOnlineName { get; private set; } = "RaidHoursDefaultDefenseOnline";
        public static string CVarDefaultDefenseOfflineName { get; private set; } = "RaidHoursDefaultDefenseOffline";

        internal static void OnPlayerSpawnedInWorld(ClientInfo _clientInfo)
        {
            // local players
            if (_clientInfo == null)
            {
                var localPlayers = GameManager.Instance.World.GetLocalPlayers();
                for (var i = 0; i < localPlayers.Count; i++)
                {
                    localPlayers[i].SetCVar(CVarDefaultDefenseOnlineName, DefaultLandClaimOnlineDurabilityModifier);
                    localPlayers[i].SetCVar(CVarDefaultDefenseOfflineName, DefaultLandClaimOfflineDurabilityModifier);
                }
                HandleStateChange(CurrentState, localPlayers.ToArray());
                return;
            }

            // remote players
            if (GameManager.Instance.World.Players.dict.TryGetValue(_clientInfo.entityId, out var player))
            {
                player.SetCVar(CVarDefaultDefenseOnlineName, DefaultLandClaimOnlineDurabilityModifier);
                player.SetCVar(CVarDefaultDefenseOfflineName, DefaultLandClaimOfflineDurabilityModifier);
                HandleStateChange(CurrentState, player);
                return;
            }
        }

        internal static void OnGameStartDone()
        {
            _timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(Settings.TimeZone);
            TimeMonitorCoroutine = ThreadManager.StartCoroutine(MonitorTime());
        }

        internal static void OnGameShutdown()
        {
            ThreadManager.StopCoroutine(TimeMonitorCoroutine);
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

        private static void CheckAndHandleStateChange(params EntityPlayer[] players)
        {
            _log.Trace($"CheckAndHandleStateChange: {players.Length}");
            var currentTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZoneInfo);
            CurrentState = Settings.RaidModeStopTime.MinutesUntil(currentTime) < Settings.RaidModeStartTime.MinutesUntil(currentTime)
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
                ConnectionManager.Instance.SendPackage(netPackage); // TODO: confirm this sends to all connected clients and local players
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
