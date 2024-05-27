
namespace Shared.Core.Misc
{
    public class TimeSpanExtension
    {
        public TimeSpan TimeSpan { get; set; } = TimeSpan.Zero;

        public const long TicksPerMicrosecond = 10;

        internal const long MaxMicroSeconds = long.MaxValue / TicksPerMicrosecond;
        internal const long MinMicroSeconds = long.MinValue / TicksPerMicrosecond;

        public TimeSpanExtension()
        {
        }
        public TimeSpanExtension(TimeSpan timeSpan)
        {
            TimeSpan = timeSpan;
        }

        public int Microseconds => (int)(TimeSpan.Ticks / TicksPerMicrosecond % 1000);


        // Returns the total number of Microseconds rounded to the nearest whole number
        public long TotalMicrosecondsAsLong
        {
            get
            {
                // Do I need this ??
                // double temp = (double) TimeSpan.Ticks / TicksPerMicrosecond;
                // if (temp > MaxMicroSeconds)
                //     throw new OverflowException("");
                //
                // if (temp < MinMicroSeconds)
                //     throw new OverflowException("");

                return TimeSpan.Ticks / TicksPerMicrosecond + (long)Math.Round(TimeSpan.Ticks % TicksPerMicrosecond / (float)TicksPerMicrosecond);
            }
        }

        //might be unsafe but animation clips shouldn't be too long..
        public static TimeSpanExtension FromMicroseconds(long value)
        {
            return new TimeSpanExtension(new TimeSpan(value * TicksPerMicrosecond));
        }


    }
}
