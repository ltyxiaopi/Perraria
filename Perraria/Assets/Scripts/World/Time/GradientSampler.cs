using System.Collections.Generic;
using UnityEngine;

public static class GradientSampler
{
    public static Color SampleColor(IReadOnlyList<float> atMinutes, IReadOnlyList<Color> values, float minutes)
    {
        int index = FindInterval(atMinutes, values, minutes, out float t);
        int nextIndex = (index + 1) % values.Count;
        return Color.Lerp(values[index], values[nextIndex], t);
    }

    public static float SampleFloat(IReadOnlyList<float> atMinutes, IReadOnlyList<float> values, float minutes)
    {
        int index = FindInterval(atMinutes, values, minutes, out float t);
        int nextIndex = (index + 1) % values.Count;
        return Mathf.Lerp(values[index], values[nextIndex], t);
    }

    private static int FindInterval<TValue>(
        IReadOnlyList<float> atMinutes,
        IReadOnlyList<TValue> values,
        float minutes,
        out float t)
    {
        ValidateInputs(atMinutes, values);

        float normalized = WorldClock.NormalizeMinutes(minutes);
        int lastIndex = atMinutes.Count - 1;

        for (int i = 0; i < lastIndex; i++)
        {
            float start = atMinutes[i];
            float end = atMinutes[i + 1];
            if (normalized < start || normalized >= end)
            {
                continue;
            }

            t = Mathf.InverseLerp(start, end, normalized);
            return i;
        }

        float last = atMinutes[lastIndex];
        float first = atMinutes[0] + WorldClock.MinutesPerDay;
        float wrappedMinutes = normalized < atMinutes[0]
            ? normalized + WorldClock.MinutesPerDay
            : normalized;
        t = Mathf.InverseLerp(last, first, wrappedMinutes);
        return lastIndex;
    }

    private static void ValidateInputs<TValue>(IReadOnlyList<float> atMinutes, IReadOnlyList<TValue> values)
    {
        if (atMinutes == null || values == null || atMinutes.Count == 0 || atMinutes.Count != values.Count)
        {
            throw new System.ArgumentException("Gradient keys and values must be non-empty and have equal length.");
        }
    }
}
