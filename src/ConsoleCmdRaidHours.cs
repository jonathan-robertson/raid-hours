using System;
using System.Collections.Generic;
using System.Linq;

namespace RaidHours
{
    internal class ConsoleCmdRaidHours : ConsoleCmdAbstract
    {
        private static readonly string[] Commands = new string[] {
            "raidhours",
            "rh"
        };
        private readonly string help;

        public ConsoleCmdRaidHours()
        {
            // tip - windows and linux typically use a different set of standards when it comes to timezones.
            // one example is how CST is handled... windows: "Central Standard Time", linux: "America/Chicago"
            var dict = new Dictionary<string, string>() {
                { "", "show raid-hours mod settings" },
                { "debug", "toggle debug logging mode" },
                { "list", "list available timezones for your operating system" },
                { "fix <user id / player name / entity id>", "fix player's raid hours state; if player is unable to damage claimed land during raid hours, this will re-send the correct raid mode values to the given player" },
                { "set timezone <string>", "set the timezone; use 'list' to get a list of timezones your operating system supports" },
                { "set <start/stop> [d=Monday/Tuesday/...] [h=value] [m=value]", "update the start or stop time with the provided rule... d (day of week), (h hour of day), and m (minute of hour) can all be omitted, but m will default to 0 (i.e. top of the hour). NOTE: h is in 24-hr time, so 17 = 5pm." },
            };

            var i = 1; var j = 1;
            help = $"Usage:\n  {string.Join("\n  ", dict.Keys.Select(command => $"{i++}. {GetCommands()[0]} {command}").ToList())}\nDescription Overview\n{string.Join("\n", dict.Values.Select(description => $"{j++}. {description}").ToList())}";
        }

        public override string[] GetCommands()
        {
            return Commands;
        }

        public override string GetDescription()
        {
            return "Configure or adjust settings for the Raid Hours mod.";
        }

        public override string GetHelp()
        {
            return help;
        }

        public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
        {
            try
            {
                if (_params.Count == 0)
                {
                    SdtdConsole.Instance.Output(SettingsManager.AsString());
                    return;
                }
                switch (_params[0].ToLower())
                {
                    case "list":
                        var timeZones = TimeZoneInfo.GetSystemTimeZones();
                        for (var i = 0; i < timeZones.Count; i++)
                        {
                            SdtdConsole.Instance.Output($"- \"{timeZones[i].Id}\" | {timeZones[i].BaseUtcOffset}");
                        }
                        return;
                    case "debug":
                        ModApi.DebugMode = !ModApi.DebugMode;
                        SdtdConsole.Instance.Output($"Debug Mode has successfully been {(ModApi.DebugMode ? "enabled" : "disabled")}.");
                        return;
                    case "fix":
                        if (_params.Count != 2) { break; }
                        if (!TryGetPlayerFromIdentifier(_params[1], out var player)) { return; }
                        ScheduleManager.OnPlayerSpawnedInWorld(player);
                        SdtdConsole.Instance.Output($"Current raid state has been re-sent to player {player.GetDebugName()}.");
                        return;
                    case "set":
                        if (_params.Count == 1) { break; }
                        switch (_params[1].ToLower())
                        {
                            case "timezone":
                                if (_params.Count == 3)
                                {
                                    var success = SettingsManager.TrySetTimeZone(_params[2].ToLower());
                                    if (success)
                                    {
                                        ScheduleManager.CheckAndHandleStateChange();
                                    }
                                    SdtdConsole.Instance.Output($"Updating timezone {(success ? "succeeded and this change was saved" : "failed; please try the 'list' command to view valid options for your operating system; if the timezone you want to use has spaces in it, be sure to wrap that timezone in quotes.")}.");
                                    return;
                                }
                                break;
                            case "start":
                                if (TryParseTimeTrigger(_params, out var startTimeTrigger))
                                {
                                    var updated = SettingsManager.SetRaidModeStartTime(startTimeTrigger);
                                    if (updated)
                                    {
                                        SdtdConsole.Instance.Output($"Updated Raid Start Time to {startTimeTrigger}.");
                                        ScheduleManager.CheckAndHandleStateChange();
                                    }
                                    else
                                    {
                                        SdtdConsole.Instance.Output($"No change to Raid Start Time was made because the provided input matches its current configuration.");
                                    }
                                    return;
                                }
                                break;
                            case "stop":
                                if (TryParseTimeTrigger(_params, out var stopTimeTrigger))
                                {
                                    var updated = SettingsManager.SetRaidModeStopTime(stopTimeTrigger);
                                    if (updated)
                                    {
                                        SdtdConsole.Instance.Output($"Updated Raid Stop Time to {stopTimeTrigger}.");
                                        ScheduleManager.CheckAndHandleStateChange();
                                    }
                                    else
                                    {
                                        SdtdConsole.Instance.Output($"No change to Raid Stop Time was made because the provided input matches its current configuration.");
                                    }
                                    return;
                                }
                                break;
                        }
                        break;
                }
                SdtdConsole.Instance.Output($"Invald parameter provided; use 'help {Commands[0]}' to learn more.");
            }
            catch (Exception e)
            {
                SdtdConsole.Instance.Output($"Exception encountered: \"{e.Message}\"\n{e.StackTrace}");
            }
        }

        private bool TryGetPlayerFromIdentifier(string identifier, out EntityPlayer entityPlayer)
        {
            var clientInfo2 = ConsoleHelper.ParseParamIdOrName(identifier, true, false);
            if (clientInfo2 == null)
            {
                if (GameManager.IsDedicatedServer || !ConsoleHelper.ParamIsLocalPlayer(identifier, true, false))
                {
                    SdtdConsole.Instance.Output("Target playername or entity/userid id not found.");
                    entityPlayer = default;
                    return false;
                }
                entityPlayer = GameManager.Instance.World.GetPrimaryPlayer();
                return true;
            }
            if (!GameManager.Instance.World.Players.dict.TryGetValue(clientInfo2.entityId, out entityPlayer))
            {
                SdtdConsole.Instance.Output("Target playername or entity/userid id not found.");
                return false;
            }
            return true;
        }

        private bool TryParseTimeTrigger(List<string> _params, out TimeTrigger timeTrigger)
        {
            if (_params.Count == 2)
            {
                timeTrigger = new TimeTrigger();
                return true;
            }
            timeTrigger = new TimeTrigger();
            for (var i = 2; i < _params.Count; i++)
            {
                if (_params[i].Length > 2)
                {
                    switch (_params[i].Substring(0, 2))
                    {
                        case "d=":
                            if (Enum.TryParse<DayOfWeek>(_params[i].Substring(2), out var dayOfWeek))
                            {
                                timeTrigger.DayOfWeek = dayOfWeek;
                                continue;
                            }
                            break;
                        case "h=":
                            if (int.TryParse(_params[i].Substring(2), out var hourOfDay))
                            {
                                timeTrigger.HourOfDay = hourOfDay;
                                continue;
                            }
                            break;
                        case "m=":
                            if (int.TryParse(_params[i].Substring(2), out var minOfHour))
                            {
                                timeTrigger.MinOfHour = minOfHour;
                                continue;
                            }
                            break;
                    }
                }
                SdtdConsole.Instance.Output("Parsing error detected; please try again - avoid spaces around = symbol, properly capitalize days of the week, etc.");
                return false;
            }
            return true;
        }
    }
}
