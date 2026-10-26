using System;

namespace FocusBlocker.Models
{
    [Flags]
    public enum DaysOfWeekFlags
    {
        None = 0,
        Monday = 1, Tuesday = 2, Wednesday = 4, Thursday = 8, Friday = 16, Saturday = 32, Sunday = 64,
        Everyday = Monday | Tuesday | Wednesday | Thursday | Friday | Saturday | Sunday
    }

    /// <summary>A recurring time window (e.g. 00:00-22:00 every day) during which blocking is active.</summary>
    public class FocusSession
    {
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public DaysOfWeekFlags ActiveDays { get; set; } = DaysOfWeekFlags.Everyday;
        public bool IsEnabled { get; set; } = true;

        public FocusSession() { }

        public FocusSession(TimeSpan start, TimeSpan end, DaysOfWeekFlags days)
        {
            StartTime = start;
            EndTime = end;
            ActiveDays = days;
        }

        public bool IsActiveAt(DateTime moment)
        {
            if (!IsEnabled) return false;

            if (!DayMatches(moment.DayOfWeek))
            {
                if (StartTime > EndTime)
                {
                    DateTime previousDay = moment.AddDays(-1);
                    if (DayMatches(previousDay.DayOfWeek) && moment.TimeOfDay < EndTime)
                        return true;
                }
                return false;
            }

            TimeSpan now = moment.TimeOfDay;
            return StartTime <= EndTime
                ? now >= StartTime && now < EndTime
                : now >= StartTime || now < EndTime;
        }

        private bool DayMatches(DayOfWeek day)
        {
            DaysOfWeekFlags flag;
            switch (day)
            {
                case DayOfWeek.Monday: flag = DaysOfWeekFlags.Monday; break;
                case DayOfWeek.Tuesday: flag = DaysOfWeekFlags.Tuesday; break;
                case DayOfWeek.Wednesday: flag = DaysOfWeekFlags.Wednesday; break;
                case DayOfWeek.Thursday: flag = DaysOfWeekFlags.Thursday; break;
                case DayOfWeek.Friday: flag = DaysOfWeekFlags.Friday; break;
                case DayOfWeek.Saturday: flag = DaysOfWeekFlags.Saturday; break;
                default: flag = DaysOfWeekFlags.Sunday; break;
            }
            return (ActiveDays & flag) == flag;
        }
    }
}