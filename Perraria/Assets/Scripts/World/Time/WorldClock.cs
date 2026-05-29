using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class WorldClock : MonoBehaviour
{
    public const float DayLengthSeconds = 24f * 60f;
    public const float MinutesPerDay = 24f * 60f;
    public const float DefaultStartMinutes = 360f;

    public static WorldClock Instance { get; private set; }

    [SerializeField] private float _gameMinutesPerSecond = 1f;
    [SerializeField] private float _startGameMinutes = DefaultStartMinutes;

    public float CurrentGameMinutes { get; private set; }
    public TimeOfDay CurrentTime { get; private set; }

    public event Action<TimeOfDay, TimeOfDay> OnTimeOfDayChanged;

    private void Awake()
    {
        Instance = this;
        SetTime(_startGameMinutes);
    }

    private void Update()
    {
        if (Time.timeScale <= 0f)
        {
            return;
        }

        SetTime(CurrentGameMinutes + _gameMinutesPerSecond * Time.deltaTime);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void SetTime(float minutes)
    {
        TimeOfDay previous = CurrentTime;
        CurrentGameMinutes = NormalizeMinutes(minutes);
        CurrentTime = GetTimeOfDay(CurrentGameMinutes);

        if (CurrentTime != previous)
        {
            OnTimeOfDayChanged?.Invoke(previous, CurrentTime);
        }
    }

    public static TimeOfDay GetTimeOfDay(float minutes)
    {
        float normalized = NormalizeMinutes(minutes);
        if (normalized >= 360f && normalized < 600f)
        {
            return TimeOfDay.Morning;
        }

        if (normalized >= 600f && normalized < 840f)
        {
            return TimeOfDay.Noon;
        }

        if (normalized >= 840f && normalized < 1080f)
        {
            return TimeOfDay.Afternoon;
        }

        if (normalized >= 1080f && normalized < 1320f)
        {
            return TimeOfDay.Evening;
        }

        return TimeOfDay.DeepNight;
    }

    public static float NormalizeMinutes(float minutes)
    {
        float normalized = minutes % MinutesPerDay;
        return normalized < 0f ? normalized + MinutesPerDay : normalized;
    }
}
