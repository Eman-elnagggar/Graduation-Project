using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;
using Graduation_Project.ViewModels.Analysis;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project.Services
{
    public class AnalysisService : IAnalysisService
    {
        private readonly AppDbContext _context;
        private readonly AnalysisOcrClient _ocrClient;
        private readonly AnalysisSubmitClient _submitClient;
        private readonly ILogger<AnalysisService> _logger;
        private readonly IWebHostEnvironment _env;

        public AnalysisService(
            AppDbContext context,
            AnalysisOcrClient ocrClient,
            AnalysisSubmitClient submitClient,
            ILogger<AnalysisService> logger,
            IWebHostEnvironment env)
        {
            _context = context;
            _ocrClient = ocrClient;
            _submitClient = submitClient;
            _logger = logger;
            _env = env;
        }

        public async Task<AnalysisUploadResponse> UploadAndExtractAsync(AnalysisUploadRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Image == null)
                throw new InvalidOperationException("Image is required.");

            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.PatientID == request.PatientId, cancellationToken);

            if (patient == null)
                throw new InvalidOperationException("Patient not found.");

            var doctorId = await GetDoctorIdForPatientAsync(request.PatientId, cancellationToken);

            TestReport? report = null;
            if (request.ReportId.HasValue)
            {
                report = await _context.TestReports.FirstOrDefaultAsync(r => r.ReportID == request.ReportId, cancellationToken);
            }
            else
            {
                report = new TestReport
                {
                    PatientID = request.PatientId,
                    DoctorID = doctorId == 0 ? 0 : doctorId,
                    ReportDate = DateTime.UtcNow,
                    AnalysisStatus = AnalysisStatus.WaitingForConfirmation
                };
                _context.TestReports.Add(report);
                await _context.SaveChangesAsync(cancellationToken);
            }

            var ocrTestType = MapTestTypeForOcr(request.TestType);
            var ocrResponse = await _ocrClient.AnalyzeImageAsync(request.Image, ocrTestType, cancellationToken);
            if (ocrResponse == null)
                throw new InvalidOperationException("OCR service unavailable.");

            if (ocrResponse.Values.Count == 0)
                throw new InvalidOperationException("OCR service returned no values.");

            var normalizedValues = NormalizeDictionary(ocrResponse.Values);
            var rawJson = JsonSerializer.Serialize(ocrResponse.Values);
            var normalizedJson = JsonSerializer.Serialize(normalizedValues);

            // Pre-compute image save path before creating the DB record
            string? computedImagePath = null;
            string? computedPhysicalPath = null;
            try
            {
                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "lab-tests", request.PatientId.ToString());
                Directory.CreateDirectory(uploadsDir);
                var ext = Path.GetExtension(request.Image.FileName ?? "").ToLowerInvariant();
                if (!new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".pdf" }.Contains(ext))
                    ext = ".jpg";
                var fileName = $"{Guid.NewGuid():N}{ext}";
                computedImagePath   = $"/uploads/lab-tests/{request.PatientId}/{fileName}";
                computedPhysicalPath = Path.Combine(uploadsDir, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to prepare upload directory for patient {PatientId}.", request.PatientId);
            }

            var labTest = new LabTest
            {
                PatientID = request.PatientId,
                DoctorID = doctorId == 0 ? null : doctorId,
                UploadDate = DateTime.UtcNow,
                ImagePath = computedImagePath,
                TestType = request.TestType,
                TestName = ocrResponse.TestName,
                OcrRawJson = rawJson,
                OcrNormalizedJson = normalizedJson,
                ConfirmedJson = null,
                AnalysisStatus = AnalysisStatus.WaitingForConfirmation,
                ReportID = report?.ReportID
            };

            _context.LabTests.Add(labTest);
            await _context.SaveChangesAsync(cancellationToken);

            // Write the image file to disk after the DB record exists
            if (computedPhysicalPath != null)
            {
                try
                {
                    using var imgStream = request.Image.OpenReadStream();
                    using var fileStream = new FileStream(computedPhysicalPath, FileMode.Create);
                    await imgStream.CopyToAsync(fileStream, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to save image file for lab test {LabTestId}.", labTest.LabTestID);
                    labTest.ImagePath = null;
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }

            return new AnalysisUploadResponse
            {
                LabTestId = labTest.LabTestID,
                ReportId = report?.ReportID,
                Status = labTest.AnalysisStatus ?? AnalysisStatus.WaitingForConfirmation,
                TestName = ocrResponse.TestName ?? request.TestType,
                Confidence = ocrResponse.Confidence,
                ExtractedValues = normalizedValues
            };
        }

        private static string MapTestTypeForOcr(string testType)
        {
            var key = testType.Trim().ToLowerInvariant();
            return key switch
            {
                "cbc" => "CBC (Complete Blood Count)",
                "urinalysis" => "Urinalysis",
                "tsh" => "TSH (Thyroid)",
                "ferritin" => "Ferritin",
                "fbg" => "Fasting Blood Glucose",
                "fastingbloodglucose" => "Fasting Blood Glucose",
                "fasting_blood_glucose" => "Fasting Blood Glucose",
                "hba1c" => "HbA1c (Sugar Test)",
                "bloodgroup" => "Blood Group",
                "hbsag" => "HBsAg (Hepatitis B)",
                "hcv" => "HCV (Hepatitis C)",
                _ => testType
            };
        }

        public async Task<AnalysisUploadResponse> ConfirmAsync(int labTestId, AnalysisConfirmRequest request, CancellationToken cancellationToken = default)
        {
            var labTest = await _context.LabTests.FirstOrDefaultAsync(l => l.LabTestID == labTestId, cancellationToken);
            if (labTest == null)
                throw new InvalidOperationException("Lab test not found.");

            if (labTest.ReportID.HasValue)
            {
                var report = await _context.TestReports.FirstOrDefaultAsync(r => r.ReportID == labTest.ReportID, cancellationToken);
                if (report != null)
                {
                    report.AnalysisStatus = AnalysisStatus.Processing;
                }
            }

            var testName = MapTestNameForSubmit(labTest.TestName ?? labTest.TestType);
            var confirmPayload = BuildConfirmPayload(labTest, request.Values);
            var confirmResponse = await _ocrClient.ConfirmAsync(testName, confirmPayload, cancellationToken);

            var confirmedValues = confirmResponse ?? confirmPayload;
            confirmedValues = RemoveMetadataKeys(confirmedValues);
            confirmedValues = NormalizeDictionaryValues(confirmedValues);

            labTest.ConfirmedJson = JsonSerializer.Serialize(confirmedValues);
            labTest.AnalysisStatus = AnalysisStatus.Processing;
            await _context.SaveChangesAsync(cancellationToken);

            return new AnalysisUploadResponse
            {
                LabTestId = labTest.LabTestID,
                ReportId = labTest.ReportID,
                Status = labTest.AnalysisStatus ?? AnalysisStatus.Processing,
                TestName = labTest.TestName ?? labTest.TestType,
                Confidence = null,
                ExtractedValues = confirmedValues
            };
        }

        public async Task ProcessAnalysisAsync(int labTestId, CancellationToken cancellationToken = default)
        {
            var labTest = await _context.LabTests.FirstOrDefaultAsync(l => l.LabTestID == labTestId, cancellationToken);
            if (labTest == null)
                return;

            if (labTest.AnalysisStatus != AnalysisStatus.Processing)
                return;

            TestReport? report = null;

            try
            {
                if (!labTest.ReportID.HasValue)
                    throw new InvalidOperationException("Report not found for analysis.");

                report = await _context.TestReports
                    .Include(r => r.LabTests)
                    .FirstOrDefaultAsync(r => r.ReportID == labTest.ReportID, cancellationToken);

                if (report == null)
                    throw new InvalidOperationException("Report not found for analysis.");

                var patient = await _context.Patients
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.PatientID == labTest.PatientID, cancellationToken);

                if (patient == null)
                    throw new InvalidOperationException("Patient not found.");

                var results = new List<Dictionary<string, object>>();
                var reportTests = (report.LabTests ?? new List<LabTest>())
                    .Where(t => !string.IsNullOrWhiteSpace(t.ConfirmedJson))
                    .ToList();

                if (reportTests.Count == 0)
                    throw new InvalidOperationException("No confirmed tests to submit.");

                foreach (var test in reportTests)
                {
                    var confirmedValues = DeserializeDictionary(test.ConfirmedJson) ?? new Dictionary<string, object>();
                    var normalizedValues = NormalizeDictionaryValues(confirmedValues);
                    test.ConfirmedJson = JsonSerializer.Serialize(normalizedValues);
                    results.Add(BuildResultPayload(test, normalizedValues));
                }

                var submitRequest = new AnalysisSubmitRequest
                {
                    PersonalInformation = await BuildPersonalInfoAsync(patient, cancellationToken),
                    Results = results
                };

                var payloadJson = JsonSerializer.Serialize(submitRequest);
                _logger.LogWarning("Submit payload for report {ReportId}: {Payload}", report.ReportID, payloadJson);

                var analysisResponse = await _submitClient.SubmitAsync(submitRequest, cancellationToken);
                if (analysisResponse == null)
                    throw new InvalidOperationException("Analysis submit failed.");

                report.AnalysisStatus = AnalysisStatus.Processing;
                report.ReportDate = DateTime.UtcNow;

                labTest.AI_AnalysisJSON = JsonSerializer.Serialize(analysisResponse.TestResults ?? new List<Dictionary<string, object>>());
                labTest.AnalysisStatus = AnalysisStatus.Completed;

                await UpsertReportAsync(labTest, analysisResponse, cancellationToken);

                // Post-processing: update patient DgState & RiskState from result_3
                UpdatePatientFromRisk(patient, analysisResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process analysis for lab test {LabTestId}.", labTestId);
                labTest.AnalysisStatus = AnalysisStatus.Failed;
                if (report != null)
                {
                    report.AnalysisStatus = AnalysisStatus.Failed;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<AnalysisResultResponse?> GetAnalysisResultAsync(int labTestId, CancellationToken cancellationToken = default)
        {
            var labTest = await _context.LabTests
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.LabTestID == labTestId, cancellationToken);

            if (labTest == null)
                return null;

            var report = labTest.ReportID.HasValue
                ? await _context.TestReports.AsNoTracking().FirstOrDefaultAsync(r => r.ReportID == labTest.ReportID, cancellationToken)
                : null;

            var status = ResolveStatus(labTest.AnalysisStatus, report?.AnalysisStatus);

            JsonElement? riskElement = null;
            if (!string.IsNullOrWhiteSpace(report?.RiskJson))
            {
                riskElement = JsonSerializer.Deserialize<JsonElement>(report.RiskJson);
            }

            return new AnalysisResultResponse
            {
                Status = status,
                PersonalInfo = DeserializeDictionary(report?.PersonalInfoJson),
                Tests = DeserializeList(report?.AiResultJson) ?? new List<Dictionary<string, object>>(),
                RiskPrediction = riskElement,
                Report = report?.AISummary,
                Alerts = DeserializeStringList(report?.AlertsJson)
            };
        }

        private async Task UpsertReportAsync(LabTest labTest, AnalysisSubmitResponse response, CancellationToken cancellationToken)
        {
            TestReport report;
            if (labTest.ReportID.HasValue)
            {
                report = await _context.TestReports.FirstOrDefaultAsync(r => r.ReportID == labTest.ReportID, cancellationToken)
                    ?? new TestReport { PatientID = labTest.PatientID, DoctorID = labTest.DoctorID, ReportDate = DateTime.UtcNow };
            }
            else
            {
                report = new TestReport
                {
                    PatientID = labTest.PatientID,
                    DoctorID = labTest.DoctorID,
                    ReportDate = DateTime.UtcNow
                };
                _context.TestReports.Add(report);
                await _context.SaveChangesAsync(cancellationToken);
                labTest.ReportID = report.ReportID;
            }

            report.AnalysisStatus = AnalysisStatus.Completed;
            report.OverallStatus = DetermineOverallStatus(response);
            report.PersonalInfoJson = response.PersonalInfo != null
                ? JsonSerializer.Serialize(response.PersonalInfo)
                : null;
            report.AISummary = response.Report;
            report.AiResultJson = JsonSerializer.Serialize(response.TestResults ?? new List<Dictionary<string, object>>());
            report.RiskJson = response.RiskPrediction.HasValue
                ? response.RiskPrediction.Value.GetRawText()
                : null;
            report.AlertsJson = JsonSerializer.Serialize(response.Alerts ?? new List<string>());
        }

        private static void UpdatePatientFromRisk(Patient patient, AnalysisSubmitResponse response)
        {
            if (!response.RiskPrediction.HasValue)
                return;

            var riskElement = response.RiskPrediction.Value;
            JsonElement target = default;

            if (riskElement.ValueKind == JsonValueKind.Object)
            {
                target = riskElement;
            }
            else if (riskElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in riskElement.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object && item.EnumerateObject().Any())
                        target = item;
                }
            }

            if (target.ValueKind != JsonValueKind.Object)
                return;

            if (target.TryGetProperty("diabetes_status", out var dg))
                patient.DgState = dg.GetString();
            else if (target.TryGetProperty("dg_state", out var dg2))
                patient.DgState = dg2.GetString();

            if (target.TryGetProperty("risk_level", out var rl))
                patient.RiskState = rl.GetString();
            else if (target.TryGetProperty("risk_state", out var rs2))
                patient.RiskState = rs2.GetString();
        }

        private async Task<int> GetDoctorIdForPatientAsync(int patientId, CancellationToken cancellationToken)
        {
            var doctorId = await _context.PatientDoctors
                .Where(pd => pd.PatientID == patientId && pd.Status == "Approved")
                .OrderByDescending(pd => pd.IsPrimary)
                .Select(pd => pd.DoctorID)
                .FirstOrDefaultAsync(cancellationToken);

            return doctorId;
        }

        private static Dictionary<string, object> NormalizeDictionary(Dictionary<string, object> values)
        {
            var normalized = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in values)
            {
                var key = NormalizeKey(kvp.Key);
                normalized[key] = NormalizeValue(kvp.Value);
            }
            return normalized;
        }

        private static string ResolveStatus(string? labTestStatus, string? reportStatus)
        {
            if (string.Equals(reportStatus, AnalysisStatus.Completed, StringComparison.OrdinalIgnoreCase))
                return AnalysisStatus.Completed;
            if (string.Equals(reportStatus, AnalysisStatus.Failed, StringComparison.OrdinalIgnoreCase))
                return AnalysisStatus.Failed;

            return labTestStatus ?? AnalysisStatus.WaitingForConfirmation;
        }

        private static Dictionary<string, object> NormalizeDictionaryValues(Dictionary<string, object> values)
        {
            var normalized = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in values)
            {
                normalized[kvp.Key] = NormalizeValue(kvp.Value);
            }
            return normalized;
        }

        private static string NormalizeKey(string key) => key.Trim().ToLowerInvariant().Replace(" ", "_");

        private static object NormalizeValue(object? value)
        {
            if (value == null)
                return string.Empty;

            if (value is JsonElement element)
                return NormalizeValue(AnalysisOcrClientJsonElementToObject(element));

            if (value is string text)
            {
                var trimmed = text.Trim();
                if (string.Equals(trimmed, "nil", StringComparison.OrdinalIgnoreCase)) return 0m;
                if (string.Equals(trimmed, "trace", StringComparison.OrdinalIgnoreCase)) return 0.01m;
                if (string.Equals(trimmed, "positive", StringComparison.OrdinalIgnoreCase)) return "+";
                if (decimal.TryParse(trimmed.Replace(",", string.Empty), out var numeric)) return numeric;
                return trimmed;
            }

            if (value is double dbl) return Convert.ToDecimal(dbl);
            if (value is float flt) return Convert.ToDecimal(flt);
            if (value is int or long or decimal) return value;

            return value;
        }

        private static object AnalysisOcrClientJsonElementToObject(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString() ?? string.Empty,
                JsonValueKind.Number => element.TryGetDecimal(out var value) ? value : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Object => JsonSerializer.Deserialize<Dictionary<string, object>>(element.GetRawText()) ?? new Dictionary<string, object>(),
                JsonValueKind.Array => JsonSerializer.Deserialize<List<object>>(element.GetRawText()) ?? new List<object>(),
                _ => string.Empty
            };
        }

        private async Task<AnalysisPersonalInfoDto> BuildPersonalInfoAsync(Patient patient, CancellationToken cancellationToken)
        {
            var user = patient.User;
            var fullName = string.Join(" ", new[] { user?.FirstName, user?.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
            var age = user?.DateOfBirth != default
                ? (int?)Math.Floor((DateTime.UtcNow - user!.DateOfBirth).TotalDays / 365.25)
                : null;

            var pregnancy = await _context.PregnancyRecords
                .Where(p => p.PatientID == patient.PatientID)
                .OrderByDescending(p => p.StartDate)
                .FirstOrDefaultAsync(cancellationToken);

            var trimester = patient.GestationalWeeks > 0
                ? (int?)Math.Min(3, Math.Max(1, (int)Math.Ceiling(patient.GestationalWeeks / 12.0)))
                : null;

            var bloodSugarAvg = await _context.PatientBloodSugar
                .Where(b => b.PatientID == patient.PatientID)
                .OrderByDescending(b => b.DateTime)
                .Select(b => b.BloodSugar)
                .Take(7)
                .DefaultIfEmpty()
                .AverageAsync(cancellationToken);

            var bloodPressureAvg = await _context.PatientBloodPressure
                .Where(b => b.PatientID == patient.PatientID)
                .OrderByDescending(b => b.DateTime)
                .Select(b => b.BloodPressure)
                .Take(7)
                .ToListAsync(cancellationToken);

            var (avgSys, avgDia) = CalculateAverageBloodPressure(bloodPressureAvg);

            return new AnalysisPersonalInfoDto
            {
                Name = string.IsNullOrWhiteSpace(fullName) ? user?.UserName ?? "" : fullName,
                Age = age ?? 0,
                Trimester = trimester ?? 0,
                Week = patient.GestationalWeeks > 0 ? patient.GestationalWeeks : 0,
                BabyGender = string.IsNullOrWhiteSpace(pregnancy?.BabyGender) ? "Unknown" : pregnancy!.BabyGender,
                Height = patient.HeightCm > 0 ? (int)Math.Round(patient.HeightCm) : 0,
                Weight = patient.WeightKg > 0 ? (int)Math.Round(patient.WeightKg) : 0,
                Parity = patient.Births,
                RbsAverage = bloodSugarAvg > 0 ? (int)Math.Round(bloodSugarAvg) : 0,
                AvgSystolic = avgSys ?? 0,
                AvgDiastolic = avgDia ?? 0,
                DgState = string.IsNullOrWhiteSpace(patient.DgState) ? "Stable" : patient.DgState,
                RiskState = string.IsNullOrWhiteSpace(patient.RiskState) ? "Low" : patient.RiskState
            };
        }

        private static (int? systolic, int? diastolic) CalculateAverageBloodPressure(IEnumerable<string> readings)
        {
            var values = readings
                .Select(r => r.Split('/', StringSplitOptions.RemoveEmptyEntries))
                .Where(parts => parts.Length == 2)
                .Select(parts => (
                    systolic: int.TryParse(parts[0], out var s) ? (int?)s : null,
                    diastolic: int.TryParse(parts[1], out var d) ? (int?)d : null))
                .Where(v => v.systolic.HasValue && v.diastolic.HasValue)
                .ToList();

            if (values.Count == 0)
                return (null, null);

            var avgSys = (int)Math.Round(values.Average(v => v.systolic!.Value));
            var avgDia = (int)Math.Round(values.Average(v => v.diastolic!.Value));
            return (avgSys, avgDia);
        }

        private static Dictionary<string, object> BuildResultPayload(LabTest labTest, Dictionary<string, object> confirmedValues)
        {
            var payload = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["test_name"] = MapTestNameForSubmit(labTest.TestName ?? labTest.TestType)
            };

            var confidence = ExtractConfidence(labTest.OcrRawJson);
            if (!string.IsNullOrWhiteSpace(confidence))
            {
                payload["confidence"] = confidence;
            }

            foreach (var kvp in confirmedValues)
            {
                payload[kvp.Key] = kvp.Value;
            }

            return payload;
        }

        private static Dictionary<string, object> BuildConfirmPayload(LabTest labTest, Dictionary<string, object> values)
        {
            var payload = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var keyMap = GetConfirmKeyMap(labTest.TestType, labTest.TestName);
            foreach (var kvp in values)
            {
                var normalizedKey = NormalizeKey(kvp.Key);
                var targetKey = keyMap.TryGetValue(normalizedKey, out var mappedKey) ? mappedKey : kvp.Key;
                var normalizedValue = NormalizeValue(kvp.Value);
                // Convert all values to strings for external API compatibility
                payload[targetKey] = normalizedValue?.ToString() ?? string.Empty;
            }

            var confidence = ExtractConfidence(labTest.OcrRawJson);
            if (!string.IsNullOrWhiteSpace(confidence))
            {
                payload["confidence"] = confidence;
            }

            return payload;
        }

        private static Dictionary<string, string> GetConfirmKeyMap(string? testType, string? testName)
        {
            var normalized = (testType ?? testName ?? string.Empty).Trim().ToLowerInvariant();
            return normalized switch
            {
                "cbc" or "cbc (complete blood count)" => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["hb"] = "HB",
                    ["wbc"] = "WBC",
                    ["rbcs_count"] = "RBCs_Count",
                    ["mcv"] = "MCV",
                    ["mch"] = "MCH",
                    ["mchc"] = "MCHC",
                    ["lymphocytes"] = "lymphocytes",
                    ["platelet_count"] = "platelet_count"
                },
                "urinalysis" or "urine analysis" => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["color"] = "Color",
                    ["ph"] = "PH",
                    ["specific_gravity"] = "Specific_Gravity",
                    ["protein"] = "Protein",
                    ["glucose"] = "Glucose",
                    ["ketones"] = "Ketones",
                    ["blood"] = "Blood",
                    ["rbcs"] = "RBCs",
                    ["leukocytes"] = "Leukocytes",
                    ["nitrite"] = "Nitrite"
                },
                "tsh" or "tsh (thyroid)" => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["tsh"] = "TSH"
                },
                "ferritin" => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["ferritin_value"] = "Ferritin_value",
                    ["ferritin"] = "Ferritin_value"
                },
                "fbg" or "fastingbloodglucose" or "fasting blood glucose" or "fasting_blood_glucose" => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["fbg"] = "FBG",
                    ["fasting_blood_glucose"] = "FBG"
                },
                "hba1c" or "hba1c (sugar test)" => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["hba1c"] = "HbA1c"
                },
                "hcv" or "hcv (hepatitis c)" => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["hcv"] = "HCV"
                },
                "hbsag" or "hbsag (hepatitis b)" => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["hbsag"] = "HBsAg"
                },
                "bloodgroup" or "blood group" => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["abo_group"] = "ABO_Group",
                    ["bloodtype"] = "ABO_Group",
                    ["rh_factor"] = "RH_Factor",
                    ["rh"] = "RH_Factor"
                },
                _ => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            };
        }

        private static Dictionary<string, object> RemoveMetadataKeys(Dictionary<string, object> values)
        {
            var cleaned = new Dictionary<string, object>(values, StringComparer.OrdinalIgnoreCase);
            cleaned.Remove("test_name");
            cleaned.Remove("confidence");
            return cleaned;
        }



        private static string DetermineOverallStatus(AnalysisSubmitResponse response)
        {
            if (response.RiskPrediction.HasValue)
            {
                var riskElement = response.RiskPrediction.Value;
                JsonElement target = default;

                if (riskElement.ValueKind == JsonValueKind.Object)
                    target = riskElement;
                else if (riskElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in riskElement.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.Object && item.EnumerateObject().Any())
                        { target = item; break; }
                    }
                }

                if (target.ValueKind == JsonValueKind.Object &&
                    target.TryGetProperty("risk_level", out var rl))
                {
                    var level = (rl.GetString() ?? "").ToLowerInvariant();
                    if (level.Contains("high"))     return "Abnormal Values Detected";
                    if (level.Contains("moderate") || level.Contains("medium"))
                                                    return "Some Values Below Normal";
                    return "All Values Normal";
                }
            }

            if (response.Alerts != null && response.Alerts.Count > 0)
                return "Some Values Below Normal";

            return "All Values Normal";
        }


        private static string MapTestNameForSubmit(string testName)
        {
            var normalized = testName.Trim().ToLowerInvariant();
            return normalized switch
            {
                "cbc" => "CBC (Complete Blood Count)",
                "cbc (complete blood count)" => "CBC (Complete Blood Count)",
                "urinalysis" => "Urinalysis",
                "urine analysis" => "Urinalysis",
                "ferritin" => "Ferritin",
                "tsh (thyroid)" => "TSH (Thyroid)",
                "tsh" => "TSH (Thyroid)",
                "fbg" => "Fasting Blood Glucose",
                "fasting blood glucose" => "Fasting Blood Glucose",
                "hba1c (sugar test)" => "HbA1c (Sugar Test)",
                "hba1c" => "HbA1c (Sugar Test)",
                "blood group" => "Blood Group",
                "bloodgroup" => "Blood Group",
                "hbsag (hepatitis b)" => "HBsAg (Hepatitis B)",
                "hbsag" => "HBsAg (Hepatitis B)",
                "hcv (hepatitis c)" => "HCV (Hepatitis C)",
                "hcv" => "HCV (Hepatitis C)",
                _ => testName
            };
        }

        private static string? ExtractConfidence(string? ocrRawJson)
        {
            if (string.IsNullOrWhiteSpace(ocrRawJson))
                return null;

            try
            {
                using var doc = JsonDocument.Parse(ocrRawJson);
                if (doc.RootElement.TryGetProperty("confidence", out var confidence))
                    return confidence.ToString();
            }
            catch
            {
            }

            return null;
        }

        private static Dictionary<string, object>? DeserializeDictionary(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json, JsonOptions());
        }

        private static List<Dictionary<string, object>>? DeserializeList(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            return JsonSerializer.Deserialize<List<Dictionary<string, object>>>(json, JsonOptions());
        }

        private static List<string> DeserializeStringList(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<string>();
            return JsonSerializer.Deserialize<List<string>>(json, JsonOptions()) ?? new List<string>();
        }

        private static JsonSerializerOptions JsonOptions() => new()
        {
            PropertyNameCaseInsensitive = true
        };
    }
}
