namespace RaidHours
{
    internal class Settings
    {
        // TODO: load these values from file instead

        public static string TimeZone { get; set; } = "Central Standard Time";
        public static TimeTrigger RaidModeStartTime { get; set; } = new TimeTrigger(hourOfDay: 3, minOfHour: 15); // 7pm
        public static TimeTrigger RaidModeStopTime { get; set; } = new TimeTrigger(hourOfDay: 23); // 11pm
        public static bool AntiZombieRaid { get; set; } = true;
    }
}
