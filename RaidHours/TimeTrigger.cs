using System;

namespace RaidHours
{
    internal class TimeTrigger
    {
        private readonly DayOfWeek? DayOfWeek;
        private readonly int? HourOfDay;
        private readonly int MinOfHour;

        public TimeTrigger(DayOfWeek? dayOfWeek = null, int? hourOfDay = null, int minOfHour = 0)
        {
            DayOfWeek = dayOfWeek;
            HourOfDay = hourOfDay;
            MinOfHour = minOfHour;
        }

        public int MinutesUntil(DateTime dt)
        {
            if (DayOfWeek.HasValue && HourOfDay.HasValue)
            {
                var days = UntilNextDayOfWeek(dt, DayOfWeek.Value, wrap: true);
                var hours = UntilNextHourOfDay(dt, HourOfDay.Value, wrap: false);
                var mins = UntilNextMinOfHour(dt, MinOfHour, wrap: false);
                if ((days == 0 && hours < 0) || (days == 0 && hours == 0 && mins < 0)) { days = 7; }
                if (hours == 0 && mins < 0) { hours = 24; }
                return (days * 1440) + (hours * 60) + mins;
            }

            if (DayOfWeek.HasValue && !HourOfDay.HasValue)
            {
                var days = UntilNextDayOfWeek(dt, DayOfWeek.Value, wrap: true);
                var mins = UntilNextMinOfHour(dt, MinOfHour, wrap: false);
                if (days == 0 && mins < 0) { days = 7; }
                return (days * 1440) + mins;
            }

            if (!DayOfWeek.HasValue && HourOfDay.HasValue)
            {
                var hours = UntilNextHourOfDay(dt, HourOfDay.Value, wrap: true);
                var mins = UntilNextMinOfHour(dt, MinOfHour, wrap: false);
                if (hours == 0 && mins < 0) { hours = 24; }
                return (hours * 60) + mins;
            }

            return UntilNextMinOfHour(dt, MinOfHour, wrap: true);
        }

        public static int UntilNextDayOfWeek(DateTime dt, DayOfWeek dayOfWeek, bool wrap = true)
        {
            return dt.DayOfWeek <= dayOfWeek ? dayOfWeek - dt.DayOfWeek : dayOfWeek - dt.DayOfWeek + (wrap ? 7 : 0);
        }

        public static int UntilNextHourOfDay(DateTime dt, int hourOfDay, bool wrap = true)
        {
            return dt.Hour <= hourOfDay ? hourOfDay - dt.Hour : hourOfDay - dt.Hour + (wrap ? 24 : 0);
        }

        public static int UntilNextMinOfHour(DateTime dt, int minuteOfHour, bool wrap = true)
        {
            return dt.Minute <= minuteOfHour ? minuteOfHour - dt.Minute : minuteOfHour - dt.Minute + (wrap ? 60 : 0);
        }
    }
}
