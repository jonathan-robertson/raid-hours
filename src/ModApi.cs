using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace RaidHours
{
    public class ModApi : IModApi
    {
        private static readonly ModLog<ModApi> _log = new ModLog<ModApi>();

        internal static int LandClaimSize { get; private set; }
        internal static int LandClaimRadiusMin { get; private set; }
        internal static float LandClaimRadiusMax { get; private set; }
        internal static Vector3 LandClaimBoundsSize { get; private set; }
        public static int LandClaimExpiryHours { get; private set; }

        internal static bool IsServer { get; private set; }

        internal static bool DebugMode { get; set; } = false;

        public void InitMod(Mod _modInstance)
        {
            var harmony = new Harmony(GetType().ToString());
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            ModEvents.GameStartDone.RegisterHandler(OnGameStartDone);
            ModEvents.PlayerSpawnedInWorld.RegisterHandler(OnPlayerSpawnedInWorld);
            ModEvents.GameShutdown.RegisterHandler(OnGameShutdown);
        }

        private void OnGameStartDone()
        {
            try
            {
                IsServer = SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer;
                if (IsServer)
                {
                    _log.Trace("OnGameStartDone");
                    LandClaimSize = GameStats.GetInt(EnumGameStats.LandClaimSize); // 41 is the default, for example
                    LandClaimRadiusMin = LandClaimSize % 2 == 1 ? (LandClaimSize - 1) / 2 : LandClaimSize / 2;
                    LandClaimRadiusMax = (float)Math.Sqrt(Math.Pow(LandClaimRadiusMin, 2) * 2) + 1;
                    LandClaimBoundsSize = new Vector3(LandClaimSize + LandClaimRadiusMin, 255, LandClaimSize + LandClaimRadiusMin);
                    LandClaimExpiryHours = GameStats.GetInt(EnumGameStats.LandClaimExpiryTime) * 24;
                    BagDropManager.Default = (DropOption)GameStats.GetInt(EnumGameStats.DropOnQuit);
                    _log.Debug($"LandClaimSize: {LandClaimSize}, LandClaimRadiusMin: {LandClaimRadiusMin}, LandClaimRadiusMax: {LandClaimRadiusMax}, LandClaimExpiryHours: {LandClaimExpiryHours}, DropOnQuit: {BagDropManager.Default}");
                    ScheduleManager.OnGameStartDone();
                }
            }
            catch (Exception e)
            {
                _log.Error("Failed OnGameStartDone", e);
            }
        }

        private void OnPlayerSpawnedInWorld(ClientInfo _clientInfo, RespawnType _respawnType, Vector3i _pos)
        {
            try
            {
                _log.Trace($"OnPlayerSpawnedInWorld: ({IsServer} && ({_respawnType}))");
                if (IsServer
                    && (_respawnType == RespawnType.JoinMultiplayer  // remote player returns
                        || _respawnType == RespawnType.EnterMultiplayer  // remote player joins for the first time
                        || _respawnType == RespawnType.LoadedGame // local player/host loaded the game
                        || _respawnType == RespawnType.NewGame)) // local player/host created a new game
                {
                    var entityId = Util.SafelyGetEntityIdFor(_clientInfo);
                    if (!GameManager.Instance.World.Players.dict.TryGetValue(entityId, out var player))
                    {
                        _log.Trace($"Player could not be found with entityId of {entityId}. This is not expected.");
                        return;
                    }

                    ScheduleManager.OnPlayerSpawnedInWorld(player);
                    EjectionManager.OnPlayerSpawnedInWorld(player, Util.GetUserIdentifier(_clientInfo), _pos);
                    BagDropManager.RefreshBagDropOnLogoutState(player, _pos);
                }
            }
            catch (Exception e)
            {
                _log.Error("Failed OnPlayerSpawnedInWorld", e);
            }
        }

        private void OnGameShutdown()
        {
            try
            {
                if (IsServer)
                {
                    _log.Trace("OnGameShutdown");
                    ScheduleManager.OnGameShutdown();
                }
            }
            catch (Exception e)
            {
                _log.Error("Failed OnGameShutdown", e);
            }
        }
    }
}
