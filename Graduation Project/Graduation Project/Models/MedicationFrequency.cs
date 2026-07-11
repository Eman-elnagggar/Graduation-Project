using System.Text.RegularExpressions;

namespace Graduation_Project.Models
{
    /// <summary>
    /// A concrete, schedulable frequency: how many doses a day, how often the day
    /// repeats, and the exact clock times each dose is due.
    /// </summary>
    public class MedicationFrequencySpec
    {
        public string Code { get; set; } = MedicationFrequencies.CustomCode;
        public string Label { get; set; } = string.Empty;
        public int TimesPerDay { get; set; } = 1;
        public int IntervalDays { get; set; } = 1;
        public List<TimeSpan> Times { get; set; } = new();
    }

    public class MedicationFrequencyOption
    {
        public string Code { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        public int TimesPerDay { get; set; }
        public int IntervalDays { get; set; } = 1;
        public IReadOnlyList<TimeSpan> DefaultTimes { get; set; } = Array.Empty<TimeSpan>();

        public MedicationFrequencySpec ToSpec() => new()
        {
            Code = Code,
            Label = Label,
            TimesPerDay = TimesPerDay,
            IntervalDays = IntervalDays,
            Times = DefaultTimes.ToList()
        };
    }

    /// <summary>
    /// The standard frequency catalogue shared by the patient tracker UI, the
    /// prescription importer and the reminder scheduler. The JS copy in
    /// wwwroot/js/patient-medications.js mirrors this list — keep them in sync.
    /// </summary>
    public static class MedicationFrequencies
    {
        public const string CustomCode = "custom";
        public const string AsNeededCode = "as-needed";

        public const int MaxTimesPerDay = 8;
        public const int MaxIntervalDays = 90;

        private static readonly List<MedicationFrequencyOption> _all = new()
        {
            Option("once-daily",      "Once daily",        "Every day", 1, 1, "09:00"),
            Option("twice-daily",     "Twice daily",       "Every day", 2, 1, "09:00", "21:00"),
            Option("three-daily",     "3 times daily",     "Every day", 3, 1, "08:00", "14:00", "20:00"),
            Option("four-daily",      "4 times daily",     "Every day", 4, 1, "08:00", "12:00", "16:00", "20:00"),
            Option("five-daily",      "5 times daily",     "Every day", 5, 1, "08:00", "11:00", "14:00", "17:00", "20:00"),

            Option("every-4-hours",   "Every 4 hours",     "By the clock", 6, 1, "06:00", "10:00", "14:00", "18:00", "22:00", "02:00"),
            Option("every-6-hours",   "Every 6 hours",     "By the clock", 4, 1, "06:00", "12:00", "18:00", "00:00"),
            Option("every-8-hours",   "Every 8 hours",     "By the clock", 3, 1, "06:00", "14:00", "22:00"),
            Option("every-12-hours",  "Every 12 hours",    "By the clock", 2, 1, "08:00", "20:00"),

            Option("every-other-day", "Every other day",   "Less often", 1, 2,  "09:00"),
            Option("every-3-days",    "Every 3 days",      "Less often", 1, 3,  "09:00"),
            Option("weekly",          "Once a week",       "Less often", 1, 7,  "09:00"),
            Option("every-2-weeks",   "Every 2 weeks",     "Less often", 1, 14, "09:00"),
            Option("monthly",         "Once a month",      "Less often", 1, 30, "09:00"),

            Option(AsNeededCode,      "Only as needed",    "Other", 0, 1),
            Option(CustomCode,        "Custom schedule…",  "Other", 1, 1, "09:00"),
        };

        public static IReadOnlyList<MedicationFrequencyOption> All => _all;

        public static MedicationFrequencyOption? Find(string? code) =>
            string.IsNullOrWhiteSpace(code)
                ? null
                : _all.FirstOrDefault(o => string.Equals(o.Code, code, StringComparison.OrdinalIgnoreCase));

        public static MedicationFrequencyOption Default => _all[0];

        /// <summary>
        /// Builds a schedulable spec from whatever the caller supplied. Anything
        /// missing or out of range falls back to the matching catalogue option, so
        /// this never throws and never returns an unschedulable spec.
        /// </summary>
        public static MedicationFrequencySpec Build(
            string? code,
            int? timesPerDay = null,
            int? intervalDays = null,
            IEnumerable<TimeSpan>? times = null)
        {
            var option = Find(code);

            // An unknown code is treated as a custom schedule rather than rejected —
            // the caller's times/counts still drive the result.
            var spec = option?.ToSpec() ?? new MedicationFrequencySpec
            {
                Code = CustomCode,
                Label = "Custom schedule",
                TimesPerDay = 1,
                IntervalDays = 1,
                Times = new List<TimeSpan> { new(9, 0, 0) }
            };

            var isCustom = option == null || option.Code == CustomCode;

            if (isCustom)
            {
                if (timesPerDay.HasValue)
                    spec.TimesPerDay = Math.Clamp(timesPerDay.Value, 0, MaxTimesPerDay);
                if (intervalDays.HasValue)
                    spec.IntervalDays = Math.Clamp(intervalDays.Value, 1, MaxIntervalDays);
            }

            if (spec.TimesPerDay == 0)
            {
                // "As needed" has no scheduled doses — reminders and due slots skip it.
                spec.Times = new List<TimeSpan>();
                return spec;
            }

            var supplied = times?
                .Select(Normalize)
                .Distinct()
                .OrderBy(t => t)
                .ToList() ?? new List<TimeSpan>();

            if (supplied.Count > 0)
            {
                if (supplied.Count > spec.TimesPerDay)
                    supplied = supplied.Take(spec.TimesPerDay).ToList();

                // Too few times supplied: top up from the option's defaults so the
                // dose count always matches TimesPerDay.
                foreach (var fallback in spec.Times)
                {
                    if (supplied.Count >= spec.TimesPerDay) break;
                    if (!supplied.Contains(fallback)) supplied.Add(fallback);
                }

                var hour = 8;
                while (supplied.Count < spec.TimesPerDay && hour < 24)
                {
                    var candidate = new TimeSpan(hour, 0, 0);
                    if (!supplied.Contains(candidate)) supplied.Add(candidate);
                    hour += 2;
                }

                spec.Times = supplied.OrderBy(t => t).ToList();
            }
            else if (spec.Times.Count != spec.TimesPerDay)
            {
                spec.Times = SpreadEvenly(spec.TimesPerDay);
            }

            spec.TimesPerDay = spec.Times.Count;
            return spec;
        }

        /// <summary>Evenly spaces N doses across an 8am–10pm waking day.</summary>
        public static List<TimeSpan> SpreadEvenly(int timesPerDay)
        {
            var count = Math.Clamp(timesPerDay, 1, MaxTimesPerDay);
            if (count == 1) return new List<TimeSpan> { new(9, 0, 0) };

            const int firstHour = 8;
            const int lastHour = 22;
            var step = (double)(lastHour - firstHour) / (count - 1);

            return Enumerable.Range(0, count)
                .Select(i => new TimeSpan((int)Math.Round(firstHour + (step * i)) % 24, 0, 0))
                .Distinct()
                .OrderBy(t => t)
                .ToList();
        }

        /// <summary>
        /// Best-effort read of free-text frequency (e.g. a doctor's "1 tab every 8
        /// hours"). Used when importing prescriptions, which have no structured code.
        /// </summary>
        public static MedicationFrequencySpec Parse(string? freeText)
        {
            var text = (freeText ?? string.Empty).Trim().ToLowerInvariant();
            if (text.Length == 0)
                return Default.ToSpec();

            if (text.Contains("as needed") || text.Contains("as required") || text.Contains("prn"))
                return Find(AsNeededCode)!.ToSpec();

            var everyHours = Regex.Match(text, @"every\s+(\d+)\s*(hour|hr|h)\b");
            if (everyHours.Success && int.TryParse(everyHours.Groups[1].Value, out var hours) && hours > 0)
            {
                var byCode = Find($"every-{hours}-hours");
                if (byCode != null) return byCode.ToSpec();

                var perDay = Math.Clamp((int)Math.Round(24d / hours), 1, MaxTimesPerDay);
                return Build(CustomCode, perDay, 1);
            }

            var everyDays = Regex.Match(text, @"every\s+(\d+)\s*(day|d)\b");
            if (everyDays.Success && int.TryParse(everyDays.Groups[1].Value, out var days) && days > 1)
                return Build(CustomCode, 1, days);

            if (text.Contains("every other day") || text.Contains("alternate day"))
                return Find("every-other-day")!.ToSpec();

            if (text.Contains("month"))
                return Find("monthly")!.ToSpec();

            var everyWeeks = Regex.Match(text, @"every\s+(\d+)\s*week");
            if (everyWeeks.Success && int.TryParse(everyWeeks.Groups[1].Value, out var weeks) && weeks > 0)
                return Build(CustomCode, 1, Math.Clamp(weeks * 7, 1, MaxIntervalDays));

            if (text.Contains("week"))
                return Find("weekly")!.ToSpec();

            // Dose counts: "3 times daily", "3x day", "tid", "twice", "once"…
            var perDayMatch = Regex.Match(text, @"(\d+)\s*(?:x|times?)\b");
            if (perDayMatch.Success && int.TryParse(perDayMatch.Groups[1].Value, out var count) && count > 0)
                return FromCount(count);

            if (text.Contains("once") || Regex.IsMatch(text, @"\bod\b|\bqd\b|\bdaily\b")) return FromCount(1);
            if (text.Contains("twice") || Regex.IsMatch(text, @"\bbd\b|\bbid\b")) return FromCount(2);
            if (text.Contains("thrice") || text.Contains("three") || Regex.IsMatch(text, @"\btds\b|\btid\b")) return FromCount(3);
            if (text.Contains("four") || Regex.IsMatch(text, @"\bqds\b|\bqid\b")) return FromCount(4);
            if (text.Contains("five")) return FromCount(5);

            var loneNumber = Regex.Match(text, @"\b([1-8])\b");
            if (loneNumber.Success && int.TryParse(loneNumber.Groups[1].Value, out var n))
                return FromCount(n);

            return Default.ToSpec();
        }

        private static MedicationFrequencySpec FromCount(int timesPerDay)
        {
            var count = Math.Clamp(timesPerDay, 1, MaxTimesPerDay);
            var match = _all.FirstOrDefault(o =>
                o.IntervalDays == 1 &&
                o.TimesPerDay == count &&
                o.Group == "Every day");

            return match?.ToSpec() ?? Build(CustomCode, count, 1);
        }

        private static TimeSpan Normalize(TimeSpan time)
        {
            var minutes = (int)Math.Round(time.TotalMinutes);
            minutes = ((minutes % 1440) + 1440) % 1440;
            return TimeSpan.FromMinutes(minutes);
        }

        private static MedicationFrequencyOption Option(
            string code, string label, string group, int timesPerDay, int intervalDays, params string[] times) => new()
            {
                Code = code,
                Label = label,
                Group = group,
                TimesPerDay = timesPerDay,
                IntervalDays = intervalDays,
                DefaultTimes = times.Select(TimeSpan.Parse).ToList()
            };
    }
}
