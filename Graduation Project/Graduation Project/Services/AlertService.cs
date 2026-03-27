using Graduation_Project.Interfaces;
using Graduation_Project.Models;
using System.Text.Json;

namespace Graduation_Project.Services
{
    /// <summary>
    /// Evaluates patient health data against clinical thresholds and
    /// persists new Alert records for any critical or dangerous conditions.
    /// Deduplicates: at most one alert per (title + message) per calendar day.
    /// </summary>
    public class AlertService
    {
        private readonly IAlert _alertRepository;
        private readonly NotificationService _notificationService;

        public AlertService(IAlert alertRepository, NotificationService notificationService)
        {
            _alertRepository = alertRepository;
            _notificationService = notificationService;
        }

        /// <summary>
        /// Evaluates a single BP/BS reading (used from SaveBloodPressure / SaveBloodSugar)
        /// alongside the rest of the patient's health data.
        /// </summary>
        public void EvaluateAndSaveAlerts(
            int patientId,
            Patient patient,
            PatientBloodPressure? lastBP,
            PatientBloodSugar? lastBS,
            LabTest? lastLabTest,
            Appointment? nextAppointment)
        {
            EvaluateAndSaveAlerts(
                patientId, patient,
                lastBP != null ? new[] { lastBP } : Array.Empty<PatientBloodPressure>(),
                lastBS != null ? new[] { lastBS } : Array.Empty<PatientBloodSugar>(),
                lastLabTest, nextAppointment);
        }

        /// <summary>
        /// Evaluates ALL provided BP and BS readings (used from Index dashboard load).
        /// Only readings recorded TODAY are evaluated to prevent old dismissed alerts
        /// from being re-created on every dashboard refresh.
        /// </summary>
        public void EvaluateAndSaveAlerts(
            int patientId,
            Patient patient,
            IEnumerable<PatientBloodPressure> bpReadings,
            IEnumerable<PatientBloodSugar> bsReadings,
            LabTest? lastLabTest,
            Appointment? nextAppointment)
        {
            var today = DateTime.Today;

            // Only evaluate readings from TODAY on the dashboard path.
            // Readings from previous days were already evaluated (and alerted)
            // at save-time via SaveBloodPressure/SaveBloodSugar.
            // This prevents dismissed alerts from re-appearing on every page refresh.
            var todayBP = bpReadings.Where(r => r?.DateTime.Date == today).ToList();
            var todayBS = bsReadings.Where(r => r?.DateTime.Date == today).ToList();

            // Dedup key = "Title|Message" scoped to today's alerts only.
            // This still prevents the same reading from firing twice on the same day
            // while allowing alerts from new readings to be created.
            var existingKeys = _alertRepository
                .GetByPatientId(patientId)
                .Where(a => a.DateCreated.Date == today)
                .Select(a => $"{a.Title}|{a.Message}")
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var newAlerts = new List<Alert>();

            // ?? Blood Pressure ?????????????????????????????????????????????
            foreach (var bp in todayBP)
            {
                if (bp?.BloodPressure == null || !bp.BloodPressure.Contains('/'))
                    continue;

                var parts = bp.BloodPressure.Split('/');
                if (parts.Length != 2
                    || !int.TryParse(parts[0].Trim(), out int systolic)
                    || !int.TryParse(parts[1].Trim(), out int diastolic))
                    continue;

                if (systolic >= 160 || diastolic >= 110)
                {
                    TryAdd(newAlerts, existingKeys, patientId,
                        "Critically High Blood Pressure",
                        $"Your reading of {bp.BloodPressure} mmHg is dangerously high. Seek medical attention immediately.",
                        AlertTypes.Danger);
                }
                else if (systolic >= 140 || diastolic >= 90)
                {
                    TryAdd(newAlerts, existingKeys, patientId,
                        "High Blood Pressure Detected",
                        $"Your reading of {bp.BloodPressure} mmHg exceeds safe limits. Please contact your doctor.",
                        AlertTypes.Warning);
                }
                else if (systolic < 90 || diastolic < 60)
                {
                    TryAdd(newAlerts, existingKeys, patientId,
                        "Low Blood Pressure Detected",
                        $"Your reading of {bp.BloodPressure} mmHg is below normal (systolic <90 or diastolic <60). If you feel dizzy, contact your doctor.",
                        AlertTypes.Warning);
                }
            }

            // Known blood-pressure condition flag on the patient profile
            if (patient.BloodPressureIssue)
            {
                TryAdd(newAlerts, existingKeys, patientId,
                    "Monitor Your Blood Pressure",
                    "You have a recorded blood pressure condition. Check your readings regularly.",
                    AlertTypes.Warning);
            }

            // ?? Blood Sugar ????????????????????????????????????????????????
            foreach (var bs in todayBS)
            {
                if (bs == null) continue;

                if (bs.BloodSugar >= 200)
                {
                    TryAdd(newAlerts, existingKeys, patientId,
                        "Critically High Blood Sugar",
                        $"Your blood sugar of {bs.BloodSugar} mg/dL is critically high. Seek medical care immediately.",
                        AlertTypes.Danger);
                }
                else if (bs.BloodSugar > 125)
                {
                    TryAdd(newAlerts, existingKeys, patientId,
                        "Elevated Blood Sugar Detected",
                        $"Your fasting blood sugar of {bs.BloodSugar} mg/dL is above the normal range. Consult your doctor.",
                        AlertTypes.Warning);
                }
                else if (bs.BloodSugar < 70)
                {
                    TryAdd(newAlerts, existingKeys, patientId,
                        "Low Blood Sugar Detected",
                        $"Your blood sugar of {bs.BloodSugar} mg/dL is below safe levels (normal: 70–125 mg/dL). Eat something and contact your doctor.",
                        AlertTypes.Danger);
                }
            }

            // ?? Lab Test ???????????????????????????????????????????????????
            if (lastLabTest != null)
            {
                if (lastLabTest.TestReport != null)
                {
                    var report = lastLabTest.TestReport;
                    if (report.OverallStatus == "Abnormal")
                    {
                        TryAdd(newAlerts, existingKeys, patientId,
                            $"Abnormal {lastLabTest.TestType} Result",
                            report.AISummary ?? $"Your {lastLabTest.TestType} test result requires medical review. Please consult your doctor.",
                            AlertTypes.Warning);
                    }
                    else if (report.OverallStatus == "Requires Attention")
                    {
                        TryAdd(newAlerts, existingKeys, patientId,
                            $"{lastLabTest.TestType} Result Needs Attention",
                            report.AISummary ?? $"Your {lastLabTest.TestType} result requires follow-up. Please speak with your doctor.",
                            AlertTypes.Warning);
                    }
                }

                if (!string.IsNullOrWhiteSpace(lastLabTest.AI_AnalysisJSON))
                {
                    EvaluateLabAnalysisJson(newAlerts, existingKeys, patientId,
                        lastLabTest.TestType, lastLabTest.AI_AnalysisJSON);
                }
            }

            // ?? Upcoming Appointment (within 24 hours) ?????????????????????
            if (nextAppointment != null)
            {
                double hoursUntil = (nextAppointment.Date.Date + nextAppointment.Time - DateTime.Now).TotalHours;
                if (hoursUntil is > 0 and <= 24)
                {
                    TryAdd(newAlerts, existingKeys, patientId,
                        "Appointment Tomorrow",
                        $"You have an appointment on {nextAppointment.Date:MMM dd} at {nextAppointment.Time:hh\\:mm}. Don't forget!",
                        AlertTypes.Info);
                }
            }

            // ?? Lifestyle risks during pregnancy ???????????????????????????
            if (patient.Smoking)
            {
                TryAdd(newAlerts, existingKeys, patientId,
                    "Smoking Risk During Pregnancy",
                    "Smoking during pregnancy poses serious risks to your baby. Please speak with your doctor immediately.",
                    AlertTypes.Danger);
            }

            if (patient.AlcoholUse)
            {
                TryAdd(newAlerts, existingKeys, patientId,
                    "Alcohol Use During Pregnancy",
                    "Alcohol use during pregnancy can cause irreversible harm to your baby. Please consult your doctor.",
                    AlertTypes.Danger);
            }

            // ?? Persist ????????????????????????????????????????????????????
            if (newAlerts.Count > 0)
            {
                foreach (var alert in newAlerts)
                    _alertRepository.Add(alert);

                _alertRepository.Save();

                if (!string.IsNullOrWhiteSpace(patient.UserID))
                {
                    foreach (var alert in newAlerts)
                    {
                        var payload = new
                        {
                            alertId = alert.AlertID,
                            title = alert.Title,
                            message = alert.Message,
                            alertType = alert.AlertType,
                            dateCreated = alert.DateCreated,
                            isRead = alert.IsRead
                        };
                        _notificationService.SendAlertAsync(patient.UserID, payload).GetAwaiter().GetResult();
                    }
                }
            }
        }

        // ?????????????????????????????????????????????????????????????????
        private static void EvaluateLabAnalysisJson(
            List<Alert> newAlerts,
            HashSet<string> existingToday,
            int patientId,
            string testType,
            string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                JsonElement array;
                if (root.ValueKind == JsonValueKind.Array)
                    array = root;
                else if (root.ValueKind == JsonValueKind.Object
                    && root.TryGetProperty("results", out var nested)
                    && nested.ValueKind == JsonValueKind.Array)
                    array = nested;
                else
                    return;

                foreach (var item in array.EnumerateArray())
                {
                    string paramName = TryGetString(item, "parameter")
                                    ?? TryGetString(item, "name")
                                    ?? "Unknown Parameter";

                    string status = (TryGetString(item, "status") ?? "").Trim();
                    if (string.IsNullOrWhiteSpace(status)
                        || status.Equals("Normal", StringComparison.OrdinalIgnoreCase))
                        continue;

                    string valueStr = TryGetString(item, "value") ?? "";
                    string unit     = TryGetString(item, "unit")  ?? "";
                    string range    = TryGetString(item, "normalRange")
                                   ?? TryGetString(item, "normal_range")
                                   ?? TryGetString(item, "reference")
                                   ?? "";

                    bool isCritical = status.Equals("Critical", StringComparison.OrdinalIgnoreCase)
                                   || status.Equals("Critically High", StringComparison.OrdinalIgnoreCase)
                                   || status.Equals("Critically Low", StringComparison.OrdinalIgnoreCase);

                    bool isHigh = status.Equals("High", StringComparison.OrdinalIgnoreCase)
                               || status.Equals("Elevated", StringComparison.OrdinalIgnoreCase)
                               || (isCritical && status.Contains("High", StringComparison.OrdinalIgnoreCase));

                    bool isLow = status.Equals("Low", StringComparison.OrdinalIgnoreCase)
                              || (isCritical && status.Contains("Low", StringComparison.OrdinalIgnoreCase));

                    string direction = isHigh ? "high" : isLow ? "low" : "abnormal";
                    string alertType = isCritical ? AlertTypes.Danger : AlertTypes.Warning;

                    string displayValue = !string.IsNullOrWhiteSpace(valueStr) && !string.IsNullOrWhiteSpace(unit)
                        ? $"{valueStr} {unit}"
                        : !string.IsNullOrWhiteSpace(valueStr) ? valueStr : string.Empty;

                    string rangeNote = !string.IsNullOrWhiteSpace(range) ? $" Normal range: {range}." : "";

                    string message = !string.IsNullOrWhiteSpace(displayValue)
                        ? $"Your {testType} test shows {paramName} of {displayValue} — {direction} than expected.{rangeNote} Please consult your doctor."
                        : $"Your {testType} test shows {paramName} is {direction} ({status}).{rangeNote} Please consult your doctor.";

                    TryAdd(newAlerts, existingToday, patientId,
                        $"{testType}: {paramName} {char.ToUpper(direction[0]) + direction[1..]}",
                        message, alertType);
                }
            }
            catch (JsonException) { /* Malformed JSON — skip */ }
        }

        private static string? TryGetString(JsonElement el, string key)
        {
            if (el.TryGetProperty(key, out var prop))
                return prop.ValueKind == JsonValueKind.String ? prop.GetString() : prop.ToString();
            return null;
        }

        private static void TryAdd(
            List<Alert> list,
            HashSet<string> existingKeys,
            int patientId,
            string title,
            string message,
            string alertType)
        {
            string key = $"{title}|{message}";
            if (existingKeys.Contains(key)) return;

            existingKeys.Add(key);
            list.Add(new Alert
            {
                PatientID   = patientId,
                Title       = title,
                Message     = message,
                AlertType   = alertType,
                DateCreated = DateTime.Now,
                IsRead      = false
            });
        }
    }

    /// <summary>
    /// String constants for AlertType — drives CSS class in the view.
    /// </summary>
    public static class AlertTypes
    {
        public const string Danger  = "danger";   // red  — requires immediate action
        public const string Warning = "warning";  // yellow — needs attention soon
        public const string Info    = "info";     // blue  — informational
        public const string Success = "success";  // green — all clear
    }
}
