using System;

[Flags]
public enum TimeOfDayMask : byte
{
    None = 0,
    Morning = 1 << 0,
    Noon = 1 << 1,
    Afternoon = 1 << 2,
    Evening = 1 << 3,
    DeepNight = 1 << 4,
    All = Morning | Noon | Afternoon | Evening | DeepNight,
}
