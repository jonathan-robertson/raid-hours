using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace RaidHours
{
    public class Settings
    {
        public string TimeZoneString { get; set; } = TimeZoneInfo.Utc.Id;
        public TimeTrigger RaidModeStartTime { get; set; } = new TimeTrigger();
        public TimeTrigger RaidModeStopTime { get; set; } = new TimeTrigger();

        public override string ToString()
        {
            return $"[TimeZoneString: {TimeZoneString}, RaidModeStartTime: {RaidModeStartTime}, RaidModeStopTime: {RaidModeStopTime}]";
        }
    }

    internal class SettingsManager
    {
        private const string NameTimeZoneString = "timeZone";
        private const string NameRaidModeStartTime = "raidModeStartTime";
        private const string NameRaidModeStopTime = "raidModeStopTime";

        private static readonly ModLog<SettingsManager> _log = new ModLog<SettingsManager>();
        private static readonly string filename = Path.Combine(GameIO.GetSaveGameDir(), "raid-hours.xml");

        private static Settings settings;

        public static TimeZoneInfo TimeZoneInfo { get; private set; } = TimeZoneInfo.Utc;
        public static TimeTrigger RaidModeStartTime => settings.RaidModeStartTime;
        public static TimeTrigger RaidModeStopTime => settings.RaidModeStopTime;

        public static string AsString()
        {
            return settings.ToString();
        }

        public static bool SetRaidModeStartTime(TimeTrigger time)
        {
            if (!RaidModeStartTime.Equals(time))
            {
                settings.RaidModeStartTime = time;
                Save();
                return true;
            }
            return false;
        }

        public static bool SetRaidModeStopTime(TimeTrigger time)
        {
            if (!RaidModeStopTime.Equals(time))
            {
                settings.RaidModeStopTime = time;
                Save();
                return true;
            }
            return false;
        }

        public static bool TrySetTimeZone(string timeZoneString)
        {
            if (TryGetTimeZoneInfo(timeZoneString, out var timeZoneInfo))
            {
                TimeZoneInfo = timeZoneInfo;
                settings.TimeZoneString = timeZoneString;
                Save();
                return true;
            }
            return false;
        }

        public static void Load()
        {
            try
            {
                var loadedSettings = new Settings();
                var config = XElement.Load(filename);

                loadedSettings.TimeZoneString = config.Descendants(NameTimeZoneString).First().Value;
                if (TryGetTimeZoneInfo(loadedSettings.TimeZoneString, out var timeZoneInfo))
                {
                    TimeZoneInfo = timeZoneInfo;
                }
                else
                {
                    TimeZoneInfo = TimeZoneInfo.Utc;
                    _log.Error($"Failed to parse the included {NameTimeZoneString} value of '{loadedSettings.TimeZoneString}'; falling back to default time zone of {TimeZoneInfo.Id}");
                    loadedSettings.TimeZoneString = "UTC";
                }

                var startTimeNode = config.Descendants(NameRaidModeStartTime).First();
                if (TryLoadFromNode(startTimeNode, out var startTime))
                {
                    loadedSettings.RaidModeStartTime = startTime;
                }
                else
                {
                    var e = new FileNotFoundException("Invalid start time in file");
                    _log.Error($"Could not load from {filename}", e);
                    throw e;
                }
                var stopTimeNode = config.Descendants(NameRaidModeStopTime).First();
                if (TryLoadFromNode(stopTimeNode, out var stopTime))
                {
                    loadedSettings.RaidModeStopTime = stopTime;
                }
                else
                {
                    var e = new FileNotFoundException("Invalid stop time in file");
                    _log.Error($"Could not load from {filename}", e);
                    throw e;
                }
                settings = loadedSettings;
                _log.Info($"Successfully loaded {filename}");
            }
            catch (FileNotFoundException)
            {
                _log.Info($"No file detected, creating a config with defaults under {filename}");
                settings = new Settings();
                Save();
            }
            catch (Exception e)
            {
                _log.Error($"Failed to load {filename}", e);
            }
        }

        public static bool TryLoadFromNode(XElement element, out TimeTrigger timeTrigger)
        {
            if (element == null)
            {
                timeTrigger = null;
                return false;
            }
            timeTrigger = new TimeTrigger();
            var dayOfWeekString = element.Attribute("d")?.Value;
            if (dayOfWeekString != null && Enum.TryParse<DayOfWeek>(dayOfWeekString, out var dayOfWeek))
            {
                timeTrigger.DayOfWeek = dayOfWeek;
            }
            var hourOfDayString = element.Attribute("h")?.Value;
            if (hourOfDayString != null && int.TryParse(hourOfDayString, out var hourOfDay))
            {
                timeTrigger.HourOfDay = hourOfDay;
            }
            var minOfHourString = element.Attribute("m").Value;
            if (minOfHourString != null && int.TryParse(minOfHourString, out var minOfHour))
            {
                timeTrigger.MinOfHour = minOfHour;
            }
            return true;
        }

        private static void Save()
        {
            try
            {
                new XElement("config",
                    new XElement(NameTimeZoneString, settings.TimeZoneString),
                    new XElement(ConvertToElement(NameRaidModeStartTime, settings.RaidModeStartTime)),
                    new XElement(ConvertToElement(NameRaidModeStopTime, settings.RaidModeStopTime)))
                .Save(filename);
                _log.Info($"Successfully saved {filename}");
            }
            catch (Exception e)
            {
                _log.Error($"Failed to save {filename}", e);
            }
        }

        private static XElement ConvertToElement(string elementName, TimeTrigger timeTrigger)
        {
            var element = new XElement(elementName);
            if (timeTrigger.DayOfWeek.HasValue)
            {
                element.Add(new XAttribute("d", timeTrigger.DayOfWeek.Value));
            }
            if (timeTrigger.HourOfDay.HasValue)
            {
                element.Add(new XAttribute("h", timeTrigger.HourOfDay.Value));
            }
            element.Add(new XAttribute("m", timeTrigger.MinOfHour));
            return element;
        }

        private static bool TryGetTimeZoneInfo(string id, out TimeZoneInfo timeZoneInfo)
        {
            var timeZones = TimeZoneInfo.GetSystemTimeZones();
            for (var i = 0; i < timeZones.Count; i++)
            {
                if (timeZones[i].Id.EqualsCaseInsensitive(id))
                {
                    timeZoneInfo = timeZones[i];
                    return true;
                }
            }
            timeZoneInfo = null;
            return false;
        }
    }
}
