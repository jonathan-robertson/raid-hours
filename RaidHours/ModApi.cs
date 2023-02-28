using System;
using System.Collections;
using UnityEngine;

namespace RaidHours
{
    public enum GameState
    {
        Build, Raid
    }

    public class ModApi : IModApi
    {
        private static readonly ModLog<ModApi> _log = new ModLog<ModApi>();

        // TODO: update this later to be a variable that can be changed from the admin console
        private static readonly TimeZoneInfo _timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");
        private static readonly TimeTrigger _raidStart = new TimeTrigger(hourOfDay: 19); // 7pm
        private static readonly TimeTrigger _raidStop = new TimeTrigger(hourOfDay: 23); // 11pm

        public Coroutine TimeMonitorCoroutine { get; private set; }
        public static bool DebugMode { get; set; } = false;
        public static int DefaultLandClaimOnlineDurabilityModifier { get; private set; }
        public static int DefaultLandClaimOfflineDurabilityModifier { get; private set; }
        public static GameState CurrentState { get; private set; }
        public static GameState PreviousState { get; private set; }
        public string BuffBuildModeName { get; private set; } = "stateBuildMode";
        public string BuffRaidModeName { get; private set; } = "stateRaidMode";
        public string CVarDefaultDefenseOnlineName { get; private set; } = "RaidHoursDefaultDefenseOnline";
        public string CVarDefaultDefenseOfflineName { get; private set; } = "RaidHoursDefaultDefenseOffline";

        public void InitMod(Mod _modInstance)
        {
            ModEvents.GameStartDone.RegisterHandler(OnGameStartDone);
            ModEvents.PlayerSpawnedInWorld.RegisterHandler(OnPlayerSpawnedInWorld);
            ModEvents.GameShutdown.RegisterHandler(OnGameShutdown);
        }

        private void OnPlayerSpawnedInWorld(ClientInfo _clientInfo, RespawnType _respawnType, Vector3i _pos)
        {
            try
            {
                _log.Trace("OnPlayerSpawnedInWorld");
                if (GameManager.Instance.World.Players.dict.TryGetValue(_clientInfo.entityId, out var player))
                {
                    player.SetCVar(CVarDefaultDefenseOnlineName, DefaultLandClaimOnlineDurabilityModifier);
                    player.SetCVar(CVarDefaultDefenseOfflineName, DefaultLandClaimOfflineDurabilityModifier);
                    CheckAndHandleStateChange(player);
                }
            }
            catch (Exception e)
            {
                _log.Error("Failed OnPlayerSpawnedInWorld", e);
            }
        }

        private void OnGameStartDone()
        {
            try
            {
                _log.Trace("OnGameStartDone");
                TimeMonitorCoroutine = ThreadManager.StartCoroutine(MonitorTime());
            }
            catch (Exception e)
            {
                _log.Error("Failed OnGameStartDone", e);
            }
        }

        private void OnGameShutdown()
        {
            try
            {
                _log.Trace("OnGameShutdown");
                ThreadManager.StopCoroutine(TimeMonitorCoroutine); // TODO: might not be necessary
            }
            catch (Exception e)
            {
                _log.Error("Failed OnGameShutdown", e);
            }
        }

        private IEnumerator MonitorTime()
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

        private void CheckAndHandleStateChange(params EntityPlayer[] players)
        {
            var currentTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZoneInfo);
            CurrentState = _raidStop.MinutesUntil(currentTime) < _raidStart.MinutesUntil(currentTime)
                ? GameState.Raid
                : GameState.Build;

            if (CurrentState != PreviousState)
            {
                HandleStateChange(CurrentState, players);
                PreviousState = CurrentState;
            }
        }

        private void HandleStateChange(GameState newState, params EntityPlayer[] players)
        {
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
                ConnectionManager.Instance.SendPackage(netPackage); // TODO: confirm this sends to all connected clients
                for (var i = 0; i < playerList.Count; i++)
                {
                    _ = playerList[i].Buffs.AddBuff(buff);
                }
            }
            else
            {
                for (var i = 0; i < players.Length; i++)
                {
                    var clientInfo = ConnectionManager.Instance.Clients.ForEntityId(players[i].entityId);
                    if (clientInfo != null)
                    {
                        clientInfo.SendPackage(netPackage);
                        _ = players[i].Buffs.AddBuff(buff);
                    }
                }
            }
            _log.Debug($"Successfully updated all players to {newState} mode.");
        }
    }
}
