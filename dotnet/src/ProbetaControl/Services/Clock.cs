using System;

namespace ProbetaControl.Services
{
    /// <summary>"Today" for due-date rules; replaceable in tests.</summary>
    public interface IClock
    {
        DateTime Today { get; }
    }

    public sealed class SystemClock : IClock
    {
        public static SystemClock Instance { get; } = new SystemClock();

        public DateTime Today => DateTime.Today;
    }

    public sealed class FixedClock : IClock
    {
        public FixedClock(DateTime today)
        {
            Today = today.Date;
        }

        public DateTime Today { get; set; }
    }
}
