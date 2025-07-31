using System;

namespace KnxModel
{
    public readonly record struct Percent(byte KnxRawValue)
    {
        public double Value => KnxRawValue / 2.55; // Convert 0-255 to 0-100%

        public static Percent FromPercantage(double percentage)
        {
            if (percentage < 0 || percentage > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(percentage), "Percentage must be between 0 and 100.");
            }
            return new Percent((byte)(percentage * 2.55));
        }
    }
}
