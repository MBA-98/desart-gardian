using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace GameDayTime
{
    /// <summary>
    /// Drives in-game time within a day and day-to-day transitions.
    /// - Time runs from day start (default 10:00) to end (default 19:00) using delta time.
    /// - You can map that span to X real seconds (e.g. 300 = five real minutes per game day).
    /// - Advance either by sleeping in bed after evening, or automatically at day end.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameDayTimeManager : MonoBehaviour
    {
        public const int DefaultDayStartMinutesFromMidnight = 10 * 60;
        public const int DefaultDayEndMinutesFromMidnight = 19 * 60;

        [Header("Day bounds (game time)")]
        [Tooltip("Day start hour (0–23).")]
        [SerializeField, Range(0, 23)]
        private int dayStartHour = 10;

        [Tooltip("Day start minute (0–59).")]
        [SerializeField, Range(0, 59)]
        private int dayStartMinute = 0;

        [Tooltip("Day end hour (0–23) — active part of the day ends here.")]
        [SerializeField, Range(0, 23)]
        private int dayEndHour = 19;

        [Tooltip("Day end minute (0–59).")]
        [SerializeField, Range(0, 59)]
        private int dayEndMinute = 0;

        [Header("Starting day and cap")]
        [SerializeField]
        private int startingDay = 1;

        [Tooltip("0 = no cap (days keep increasing). > 0 = last playable day (e.g. 10 = ten-day campaign).")]
        [SerializeField]
        private int maxDay = 10;

        [Header("Time scale")]
        [Tooltip("If > 0: gameMinutesPerRealSecond is derived so the full game-day span fits in this many real seconds. Example: 300 = five real minutes per in-game day.")]
        [SerializeField]
        private float realSecondsPerFullGameDay = 0f;

        [Tooltip("Used only when realSecondsPerFullGameDay is 0. Game minutes advanced per one real second.")]
        [SerializeField]
        private float gameMinutesPerRealSecond = 2f;

        [Header("Day advance")]
        [Tooltip("On: after day end, time waits until the player sleeps in bed. Off: advance to next day as soon as day end is reached.")]
        [SerializeField]
        private bool requireSleepToAdvanceDay = false;

        [Header("Time source")]
        [Tooltip("On: use Time.unscaledDeltaTime (ignores timeScale).")]
        [SerializeField]
        private bool useUnscaledDeltaTime;

        [Header("After final day (when maxDay > 0)")]
        [SerializeField]
        private bool lockTimeAfterFinalDaySleep = true;

        [Header("UI (optional — prefer GameDayTimeUI)")]
        [SerializeField]
        private TMP_Text dayLabel;

        [SerializeField]
        private TMP_Text timeLabel;

        [Header("Inspector events")]
        [SerializeField]
        private UnityEvent<int> onDayStarted;

        [SerializeField]
        private UnityEvent<int> onDayEnded;

        [SerializeField]
        private UnityEvent onPlayerSlept;

        [SerializeField]
        private UnityEvent onCampaignComplete;

        /// <summary>Current day number (starts at 1 and increases).</summary>
        public int CurrentDay { get; private set; }

        /// <summary>Last campaign day when greater than zero; 0 means no day cap.</summary>
        public int MaxDay => maxDay;

        /// <summary>Current time as floating minutes from midnight within the day span.</summary>
        public float CurrentTimeMinutesFromMidnight { get; private set; }

        public bool IsWaitingForSleep { get; private set; }
        public bool IsCampaignTimeLocked { get; private set; }
        public bool HasDayEndedToday => IsWaitingForSleep;

        /// <summary>Whether bed sleep is allowed now (game day ended and not yet advanced).</summary>
        public bool CanSleepInBedNow => requireSleepToAdvanceDay && IsWaitingForSleep && !IsCampaignTimeLocked;

        public int DayStartMinutesFromMidnight { get; private set; }
        public int DayEndMinutesFromMidnight { get; private set; }

        /// <summary>Length of the active game day in minutes (start to end).</summary>
        public int GameDayLengthMinutes => Mathf.Max(1, DayEndMinutesFromMidnight - DayStartMinutesFromMidnight);

        /// <summary>Raised when the day number changes.</summary>
        public event Action<int> DayNumberChanged;

        /// <summary>Raised when the displayed clock should refresh (typically each in-game minute).</summary>
        public event Action ClockDisplayChanged;

        private int _lastUiDay = int.MinValue;
        private int _lastUiTotalMinutes = int.MinValue;
        private float _effectiveGameMinutesPerRealSecond;

        private void Awake()
        {
            RebuildDayBounds();
            ApplyRealSecondsPreset();
            CurrentDay = Mathf.Max(1, startingDay);
            if (maxDay > 0)
                CurrentDay = Mathf.Min(CurrentDay, maxDay);

            CurrentTimeMinutesFromMidnight = DayStartMinutesFromMidnight;
            IsWaitingForSleep = false;
            IsCampaignTimeLocked = false;
        }

        private void Start()
        {
            onDayStarted?.Invoke(CurrentDay);
            DayNumberChanged?.Invoke(CurrentDay);
            InvalidateUiState();
            FlushUiIfDirty();
        }

        private void Update()
        {
            if (IsCampaignTimeLocked)
            {
                FlushUiIfDirty();
                return;
            }

            if (requireSleepToAdvanceDay && IsWaitingForSleep)
            {
                FlushUiIfDirty();
                return;
            }

            if (_effectiveGameMinutesPerRealSecond <= 0f)
            {
                FlushUiIfDirty();
                return;
            }

            // Clock: advance in-game minutes from real delta time × configured rate (see ApplyRealSecondsPreset).
            float dt = useUnscaledDeltaTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float next = CurrentTimeMinutesFromMidnight + _effectiveGameMinutesPerRealSecond * dt;

            if (next >= DayEndMinutesFromMidnight)
            {
                CurrentTimeMinutesFromMidnight = DayEndMinutesFromMidnight;
                onDayEnded?.Invoke(CurrentDay);

                if (requireSleepToAdvanceDay)
                {
                    IsWaitingForSleep = true;
                }
                else
                {
                    HandleEveningTransition(fromPlayerSleep: false);
                }
            }
            else
            {
                CurrentTimeMinutesFromMidnight = next;
            }

            NotifyIfDisplayChanged();
            FlushUiIfDirty();
        }

        /// <summary>End of day: advance to next day or complete campaign.</summary>
        /// <remarks>Increments <see cref="CurrentDay"/> when more days remain; resets clock to day start.</remarks>
        private void HandleEveningTransition(bool fromPlayerSleep)
        {
            if (fromPlayerSleep)
                onPlayerSlept?.Invoke();

            bool unlimited = maxDay <= 0;
            bool moreDays = unlimited || CurrentDay < maxDay;

            if (moreDays)
            {
                CurrentDay++;
                CurrentTimeMinutesFromMidnight = DayStartMinutesFromMidnight;
                IsWaitingForSleep = false;
                onDayStarted?.Invoke(CurrentDay);
                DayNumberChanged?.Invoke(CurrentDay);
                InvalidateUiState();
                return;
            }

            onCampaignComplete?.Invoke();
            CurrentTimeMinutesFromMidnight = DayStartMinutesFromMidnight;
            IsWaitingForSleep = false;
            if (lockTimeAfterFinalDaySleep)
                IsCampaignTimeLocked = true;

            InvalidateUiState();
        }

        /// <summary>Called from the bed after day end when requireSleepToAdvanceDay is enabled.</summary>
        public bool SleepInBed()
        {
            if (!requireSleepToAdvanceDay)
                return false;

            if (!IsWaitingForSleep)
                return false;

            HandleEveningTransition(fromPlayerSleep: true);
            NotifyIfDisplayChanged();
            FlushUiIfDirty();
            return true;
        }

        public bool TrySleepToNextDay() => SleepInBed();

        /// <summary>
        /// Resets the calendar to the configured starting day (usually 1), morning time, and clears sleep/campaign lock.
        /// Use after Game Over / Play Again.
        /// </summary>
        public void ResetToStartingDay()
        {
            CurrentDay = Mathf.Max(1, startingDay);
            if (maxDay > 0)
                CurrentDay = Mathf.Min(CurrentDay, maxDay);

            CurrentTimeMinutesFromMidnight = DayStartMinutesFromMidnight;
            IsWaitingForSleep = false;
            IsCampaignTimeLocked = false;

            InvalidateUiState();
            onDayStarted?.Invoke(CurrentDay);
            DayNumberChanged?.Invoke(CurrentDay);
            ClockDisplayChanged?.Invoke();
            FlushUiIfDirty();
        }

        public void SetGameMinutesPerRealSecond(float value)
        {
            gameMinutesPerRealSecond = Mathf.Max(0f, value);
            realSecondsPerFullGameDay = 0f;
            ApplyRealSecondsPreset();
        }

        public float GetGameMinutesPerRealSecond() => _effectiveGameMinutesPerRealSecond;

        /// <summary>Set real-time length of a full game day in seconds (0 = use manual gameMinutesPerRealSecond).</summary>
        public void SetRealSecondsPerFullGameDay(float seconds)
        {
            realSecondsPerFullGameDay = Mathf.Max(0f, seconds);
            ApplyRealSecondsPreset();
        }

        /// <summary>Forces UI and subscribers to refresh even if the displayed value has not ticked yet.</summary>
        private void InvalidateUiState()
        {
            _lastUiDay = int.MinValue;
            _lastUiTotalMinutes = int.MinValue;
        }

        private void NotifyIfDisplayChanged()
        {
            int m = Mathf.FloorToInt(CurrentTimeMinutesFromMidnight);
            if (CurrentDay != _lastUiDay || m != _lastUiTotalMinutes)
                ClockDisplayChanged?.Invoke();
        }

        // Optional direct TMP refs on this component; <see cref="GameDayTimeUI"/> is the preferred HUD.
        private void FlushUiIfDirty()
        {
            int m = Mathf.FloorToInt(CurrentTimeMinutesFromMidnight);
            if (CurrentDay == _lastUiDay && m == _lastUiTotalMinutes && _lastUiDay != int.MinValue)
                return;

            _lastUiDay = CurrentDay;
            _lastUiTotalMinutes = m;

            if (dayLabel != null)
                dayLabel.text = $"Day {CurrentDay}";

            if (timeLabel != null)
                timeLabel.text = FormatTime12Hour(CurrentTimeMinutesFromMidnight);
        }

        public static string FormatTime12Hour(float minutesFromMidnight)
        {
            int total = Mathf.FloorToInt(minutesFromMidnight);
            int h24 = total / 60;
            int min = total % 60;
            string suffix = h24 >= 12 ? "PM" : "AM";
            int h12 = h24 % 12;
            if (h12 == 0)
                h12 = 12;
            return $"{h12}:{min:D2} {suffix}";
        }

        public static string FormatTime24Hour(float minutesFromMidnight)
        {
            int total = Mathf.FloorToInt(minutesFromMidnight);
            int h24 = total / 60;
            int min = total % 60;
            return $"{h24:D2}:{min:D2}";
        }

        private void RebuildDayBounds()
        {
            DayStartMinutesFromMidnight = Mathf.Clamp(dayStartHour * 60 + dayStartMinute, 0, 24 * 60 - 1);
            DayEndMinutesFromMidnight = Mathf.Clamp(dayEndHour * 60 + dayEndMinute, 0, 24 * 60);
            if (DayEndMinutesFromMidnight <= DayStartMinutesFromMidnight)
            {
                DayStartMinutesFromMidnight = DefaultDayStartMinutesFromMidnight;
                DayEndMinutesFromMidnight = DefaultDayEndMinutesFromMidnight;
            }
        }

        private void ApplyRealSecondsPreset()
        {
            if (realSecondsPerFullGameDay > 0f)
            {
                float len = GameDayLengthMinutes;
                _effectiveGameMinutesPerRealSecond = len / realSecondsPerFullGameDay;
            }
            else
            {
                _effectiveGameMinutesPerRealSecond = Mathf.Max(0f, gameMinutesPerRealSecond);
            }
        }

        private void OnValidate()
        {
            RebuildDayBounds();
            startingDay = Mathf.Max(1, startingDay);
            if (maxDay > 0)
                startingDay = Mathf.Min(startingDay, maxDay);

            if (realSecondsPerFullGameDay < 0f)
                realSecondsPerFullGameDay = 0f;
            if (gameMinutesPerRealSecond < 0f)
                gameMinutesPerRealSecond = 0f;

            ApplyRealSecondsPreset();
        }
    }
}
