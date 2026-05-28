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
        private const string SubmitCbcNameNormalized = "cbc (complete blood count)";
        private const string SubmitUrinalysisNameNormalized = "urinalysis";
        private const string SubmitFbgNameNormalized = "fasting blood glucose";
        private static readonly HashSet<string> PairableSubmitNames = new(StringComparer.OrdinalIgnoreCase)
        {
            SubmitCbcNameNormalized,
            SubmitUrinalysisNameNormalized,
            SubmitFbgNameNormalized
        };

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

            var testName = ResolveSubmitTestName(labTest);
            var confirmPayload = BuildConfirmPayload(labTest, request.Values);
            var confirmResponse = await _ocrClient.ConfirmAsync(testName, confirmPayload, cancellationToken);

            var confirmedValues = MergeConfirmValues(confirmResponse, confirmPayload);
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
                TestName = ResolveSubmitTestName(labTest),
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
                var testSources = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var uploadedTestNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var uploadedPairables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var fallbackAddedNames = new List<string>();
                var reportTests = (report.LabTests ?? new List<LabTest>())
                    .Where(t => !string.IsNullOrWhiteSpace(t.ConfirmedJson))
                    .ToList();

                if (reportTests.Count == 0)
                    throw new InvalidOperationException("No confirmed tests to submit.");

                foreach (var test in reportTests)
                {
                    var normalizedValues = GetNormalizedConfirmedValues(test);
                    test.ConfirmedJson = JsonSerializer.Serialize(normalizedValues);
                    if (TryAddResultPayload(test, normalizedValues, results, testSources, "upload", out var normalizedName))
                    {
                        if (!string.IsNullOrWhiteSpace(normalizedName))
                        {
                            uploadedTestNames.Add(normalizedName);
                            if (PairableSubmitNames.Contains(normalizedName))
                                uploadedPairables.Add(normalizedName);
                        }
                    }
                }

                LogUploadedTests(report.ReportID, uploadedTestNames);

                var missingTargets = GetPairingTargets(uploadedPairables);
                if (missingTargets.Count > 0)
                {
                    var fallbackTests = await GetLatestFallbackTestsAsync(patient.PatientID, cancellationToken);
                    foreach (var target in missingTargets)
                    {
                        if (!fallbackTests.TryGetValue(target, out var fallbackTest) || fallbackTest == null)
                            continue;

                        var normalizedValues = GetNormalizedConfirmedValues(fallbackTest);
                        if (TryAddResultPayload(fallbackTest, normalizedValues, results, testSources, "database", out var normalizedName))
                        {
                            if (!string.IsNullOrWhiteSpace(normalizedName))
                                fallbackAddedNames.Add(normalizedName);
                        }
                    }

                    LogFallbackTests(report.ReportID, fallbackAddedNames, missingTargets);
                }
                else
                {
                    LogPairingSkipped(report.ReportID, uploadedTestNames, uploadedPairables);
                }

                if (uploadedPairables.Count > 0)
                    LogPairSources(report.ReportID, testSources);

                if (results.Count == 0)
                    throw new InvalidOperationException("No supported tests to submit. The current submit API accepts CBC, Urinalysis, TSH, Ferritin, Fasting Blood Glucose, HbA1c, Blood Group, HBsAg, and HCV.");

                var submitRequest = new AnalysisSubmitRequest
                {
                    PersonalInformation = await BuildPersonalInfoAsync(patient, cancellationToken),
                    Results = results
                };

                var payloadJson = JsonSerializer.Serialize(submitRequest, SubmitJsonOptions());
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
                ["test_name"] = ResolveSubmitTestName(labTest)
            };

            var confidence = ExtractConfidence(labTest.OcrRawJson);
            payload["confidence"] = FormatSubmitConfidence(confidence);

            var submitKeyMap = GetSubmitKeyMap(labTest);
            foreach (var kvp in confirmedValues)
            {
                var normalizedKey = NormalizeKey(kvp.Key);
                var targetKey = submitKeyMap.TryGetValue(normalizedKey, out var mappedKey) ? mappedKey : kvp.Key;
                if (!IsSubmitFieldAllowed(payload["test_name"].ToString(), targetKey))
                    continue;

                var submitValue = NormalizeSubmitValue(payload["test_name"].ToString(), targetKey, kvp.Value);
                if (ShouldOmitSubmitValue(submitValue))
                    continue;

                payload[targetKey] = submitValue;
            }

            return FinalizeSubmitPayload(payload);
        }

        private static Dictionary<string, string> GetSubmitKeyMap(LabTest labTest) =>
            GetConfirmKeyMap(labTest.TestType, labTest.TestName);

        private static Dictionary<string, object> GetNormalizedConfirmedValues(LabTest test)
        {
            var confirmedValues = DeserializeDictionary(test.ConfirmedJson) ?? new Dictionary<string, object>();
            return NormalizeDictionaryValues(confirmedValues);
        }

        private static string NormalizeSubmitTestName(string? testName) => (testName ?? string.Empty).Trim().ToLowerInvariant();

        private static string ResolveNormalizedSubmitTestName(LabTest labTest) =>
            NormalizeSubmitTestName(ResolveSubmitTestName(labTest));

        private static bool TryAddResultPayload(
            LabTest test,
            Dictionary<string, object> normalizedValues,
            List<Dictionary<string, object>> results,
            Dictionary<string, string> testSources,
            string sourceLabel,
            out string? normalizedName)
        {
            normalizedName = null;
            if (!IsSupportedSubmitTest(test))
                return false;

            var payload = BuildResultPayload(test, normalizedValues);
            normalizedName = NormalizeSubmitTestName(payload.TryGetValue("test_name", out var name)
                ? name?.ToString()
                : ResolveSubmitTestName(test));

            if (!string.IsNullOrWhiteSpace(normalizedName)
                && PairableSubmitNames.Contains(normalizedName)
                && testSources.ContainsKey(normalizedName))
                return false;

            results.Add(payload);

            if (!string.IsNullOrWhiteSpace(normalizedName))
                testSources[normalizedName] = sourceLabel;

            return true;
        }

        private static List<string> GetPairingTargets(IReadOnlyCollection<string> uploadedPairables)
        {
            if (uploadedPairables == null || uploadedPairables.Count == 0)
                return new List<string>();

            var hasUrinalysis = uploadedPairables.Contains(SubmitUrinalysisNameNormalized);
            var hasCbc = uploadedPairables.Contains(SubmitCbcNameNormalized);
            var hasFbg = uploadedPairables.Contains(SubmitFbgNameNormalized);

            var missing = new List<string>();
            if (hasUrinalysis)
            {
                if (!hasCbc)
                    missing.Add(SubmitCbcNameNormalized);
                if (!hasFbg)
                    missing.Add(SubmitFbgNameNormalized);
            }
            else if (hasCbc || hasFbg)
            {
                missing.Add(SubmitUrinalysisNameNormalized);
            }

            return missing;
        }

        private async Task<Dictionary<string, LabTest>> GetLatestFallbackTestsAsync(int patientId, CancellationToken cancellationToken)
        {
            var candidates = await _context.LabTests
                .AsNoTracking()
                .Where(t => t.PatientID == patientId && !string.IsNullOrWhiteSpace(t.ConfirmedJson))
                .OrderByDescending(t => t.UploadDate)
                .ToListAsync(cancellationToken);

            var fallback = new Dictionary<string, LabTest>(StringComparer.OrdinalIgnoreCase);
            foreach (var test in candidates)
            {
                if (!IsSupportedSubmitTest(test))
                    continue;

                var normalizedName = ResolveNormalizedSubmitTestName(test);
                if (!PairableSubmitNames.Contains(normalizedName))
                    continue;

                if (!fallback.ContainsKey(normalizedName))
                    fallback[normalizedName] = test;

                if (fallback.Count == PairableSubmitNames.Count)
                    break;
            }

            return fallback;
        }

        private void LogPairSources(int reportId, IReadOnlyDictionary<string, string> testSources)
        {
            LogSingleSource(reportId, "CBC", SubmitCbcNameNormalized, testSources);
            LogSingleSource(reportId, "Urinalysis", SubmitUrinalysisNameNormalized, testSources);
            LogSingleSource(reportId, "FBG", SubmitFbgNameNormalized, testSources);
        }

        private void LogUploadedTests(int reportId, IReadOnlyCollection<string> uploadedTests)
        {
            var summary = uploadedTests.Count == 0
                ? "none"
                : string.Join(", ", uploadedTests.OrderBy(name => name, StringComparer.OrdinalIgnoreCase));
            _logger.LogInformation("Uploaded tests for report {ReportId}: {Tests}", reportId, summary);
        }

        private void LogFallbackTests(int reportId, IReadOnlyCollection<string> fallbackTests, IReadOnlyCollection<string> missingTargets)
        {
            if (fallbackTests.Count == 0)
            {
                var missing = missingTargets.Count == 0
                    ? "none"
                    : string.Join(", ", missingTargets.OrderBy(name => name, StringComparer.OrdinalIgnoreCase));
                _logger.LogInformation("No fallback tests found for report {ReportId}. Missing targets: {MissingTargets}", reportId, missing);
                return;
            }

            var summary = string.Join(", ", fallbackTests.OrderBy(name => name, StringComparer.OrdinalIgnoreCase));
            _logger.LogInformation("Fallback tests added for report {ReportId}: {Tests}", reportId, summary);
        }

        private void LogPairingSkipped(int reportId, IReadOnlyCollection<string> uploadedTests, IReadOnlyCollection<string> uploadedPairables)
        {
            if (uploadedPairables.Count > 0)
                return;

            var summary = uploadedTests.Count == 0
                ? "none"
                : string.Join(", ", uploadedTests.OrderBy(name => name, StringComparer.OrdinalIgnoreCase));
            _logger.LogInformation("Pairing skipped for report {ReportId}. Standalone tests: {Tests}", reportId, summary);
        }

        private void LogSingleSource(int reportId, string label, string key, IReadOnlyDictionary<string, string> testSources)
        {
            if (testSources.TryGetValue(key, out var source))
            {
                _logger.LogInformation("{TestName} source for report {ReportId}: {Source}", label, reportId, source);
            }
            else
            {
                _logger.LogInformation("{TestName} source for report {ReportId}: missing", label, reportId);
            }
        }

        private static bool IsSubmitFieldAllowed(string? testName, string key)
        {
            var testNormalized = (testName ?? string.Empty).Trim().ToLowerInvariant();
            var allowed = testNormalized switch
            {
                "cbc (complete blood count)" => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "HB", "RBCs_Count", "MCV", "MCH", "RDW", "WBC", "lymphocytes", "platelet_count"
                },
                "urinalysis" => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "Color", "PH", "Specific_Gravity", "Protein", "Glucose", "Ketones", "Blood", "RBCs", "Leukocytes", "Nitrite"
                },
                "tsh (thyroid)" => new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "TSH" },
                "ferritin" => new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Ferritin_value" },
                "fasting blood glucose" => new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "FBG" },
                "hba1c (sugar test)" => new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "HbA1c" },
                "blood group" => new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ABO_Group", "RH_Factor" },
                "hbsag (hepatitis b)" => new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "HBsAg" },
                "hcv (hepatitis c)" => new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "HCV" },
                _ => null
            };

            return allowed?.Contains(key) == true;
        }

        private static bool ShouldOmitSubmitValue(object? value)
        {
            if (value == null)
                return true;

            if (value is string text)
            {
                var trimmed = text.Trim();
                return string.IsNullOrWhiteSpace(trimmed)
                    || trimmed.Equals("none", StringComparison.OrdinalIgnoreCase)
                    || trimmed.Equals("null", StringComparison.OrdinalIgnoreCase);
            }

            if (value is decimal dec)
                return dec < 0;
            if (value is double dbl)
                return dbl < 0;
            if (value is float flt)
                return flt < 0;
            if (value is int i)
                return i < 0;
            if (value is long l)
                return l < 0;

            return false;
        }

        private static object NormalizeSubmitValue(string? testName, string key, object? value)
        {
            if (value is JsonElement element)
                value = AnalysisOcrClientJsonElementToObject(element);

            var keyNormalized = NormalizeKey(key);
            var testNormalized = (testName ?? string.Empty).Trim().ToLowerInvariant();

            if (value is string text)
            {
                var trimmed = text.Trim();
                if (trimmed == "+")
                    return "positive";
                if (trimmed == "-")
                    return MapReactiveResult(testNormalized, keyNormalized, "negative");
                if (trimmed.Equals("not extracted", StringComparison.OrdinalIgnoreCase))
                    return "Not Extracted";
                if (decimal.TryParse(trimmed.Replace(",", string.Empty), out var numeric))
                    return NormalizeSubmitNumber(testNormalized, keyNormalized, numeric);
                return MapReactiveResult(testNormalized, keyNormalized, trimmed);
            }

            if (value is double dbl)
                return NormalizeSubmitNumber(testNormalized, keyNormalized, Convert.ToDecimal(dbl));
            if (value is float flt)
                return NormalizeSubmitNumber(testNormalized, keyNormalized, Convert.ToDecimal(flt));
            if (value is decimal dec)
                return NormalizeSubmitNumber(testNormalized, keyNormalized, dec);
            if (value is int or long)
                return NormalizeSubmitNumber(testNormalized, keyNormalized, Convert.ToDecimal(value));

            return value ?? string.Empty;
        }

        private static object NormalizeSubmitNumber(string testName, string key, decimal value)
        {
            if (testName == "cbc (complete blood count)")
                value = NormalizeCbcNumber(key, value);

            if (testName == "urinalysis")
            {
                if (key is "specific_gravity" && value > 0 && value < 10)
                    value *= 1000;

                if (key is "protein" or "glucose" or "ketones" or "nitrite")
                    return value == 0 ? "negative" : value.ToString(System.Globalization.CultureInfo.InvariantCulture);

                if (key == "blood" && value == 0.01m)
                    return "trace";
            }

            if (key is "rh_factor" or "hbsag")
            {
                if (value == 1) return "positive";
                if (value == 0) return "negative";
            }

            if (key == "hcv")
            {
                if (value == 1) return "positive";
                if (value == 0) return "non-reactive";
            }

            return decimal.Truncate(value) == value
                ? (object)(int)value
                : value;
        }

        private static decimal NormalizeCbcNumber(string key, decimal value)
        {
            return key switch
            {
                "hb" when value > 25m && value <= 200m => value / 10m,
                "mchc" when value > 50m && value <= 500m => value / 10m,
                "wbc" when value > 0m && value < 100m => value * 1000m,
                "platelet_count" when value > 0m && value < 1000m => value * 1000m,
                "lymphocytes" when value > 0m && value < 10m => value * 10m,
                _ => value
            };
        }

        private static string MapReactiveResult(string testName, string key, string value)
        {
            if (testName == "hcv (hepatitis c)" && key == "hcv"
                && value.Equals("negative", StringComparison.OrdinalIgnoreCase))
                return "non-reactive";

            return value;
        }

        private static string FormatSubmitConfidence(string? confidence)
        {
            if (string.IsNullOrWhiteSpace(confidence))
                return "0";

            if (!decimal.TryParse(confidence.Replace(",", string.Empty), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var parsed))
                return confidence;

            if (parsed > 1m)
                parsed /= 100m;

            if (parsed < 0m)
                parsed = 0m;

            return parsed.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static Dictionary<string, object> FinalizeSubmitPayload(Dictionary<string, object> payload)
        {
            var testName = payload.TryGetValue("test_name", out var name)
                ? name?.ToString() ?? string.Empty
                : string.Empty;

            if (testName.Equals("CBC (Complete Blood Count)", StringComparison.OrdinalIgnoreCase)
                && !payload.ContainsKey("RDW"))
            {
                throw new InvalidOperationException(
                    "CBC analysis requires an RDW value. Please add RDW from your lab report in the review step.");
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
                    ["rdw"] = "RDW",
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

        private static Dictionary<string, object> MergeConfirmValues(
            Dictionary<string, object>? apiValues,
            Dictionary<string, object> userPayload)
        {
            var merged = apiValues != null
                ? new Dictionary<string, object>(apiValues, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in userPayload)
            {
                if (IsUsableConfirmValue(kvp.Value))
                    merged[kvp.Key] = kvp.Value;
            }

            return merged;
        }

        private static bool IsUsableConfirmValue(object? value)
        {
            if (value == null)
                return false;

            var text = value.ToString()?.Trim();
            if (string.IsNullOrWhiteSpace(text))
                return false;

            return text != "-1"
                && !text.Equals("none", StringComparison.OrdinalIgnoreCase);
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

            if (response.TestResults != null)
            {
                var hasAbnormal = response.TestResults.Any(test =>
                    test.Any(kvp =>
                        !kvp.Key.Equals("test_name", StringComparison.OrdinalIgnoreCase) &&
                        !kvp.Key.Equals("confidence", StringComparison.OrdinalIgnoreCase) &&
                        IsAbnormalSubmitResult(kvp.Value)));

                if (hasAbnormal)
                    return "Abnormal Values Detected";
            }

            if (response.Alerts != null && response.Alerts.Count > 0)
                return "Some Values Below Normal";

            return "All Values Normal";
        }

        private static bool IsAbnormalSubmitResult(object? value)
        {
            if (value is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.Array)
                {
                    var first = element.EnumerateArray().FirstOrDefault();
                    return first.ValueKind == JsonValueKind.String && !IsNormalSubmitStatus(first.GetString());
                }

                if (element.ValueKind == JsonValueKind.String)
                    return !IsNormalSubmitStatus(element.GetString());
            }

            if (value is IEnumerable<object> items)
            {
                var first = items.FirstOrDefault()?.ToString();
                return !IsNormalSubmitStatus(first);
            }

            return false;
        }

        private static bool IsNormalSubmitStatus(string? status)
        {
            return string.IsNullOrWhiteSpace(status)
                || status.Trim().Equals("Normal", StringComparison.OrdinalIgnoreCase);
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

        private static string ResolveSubmitTestName(LabTest labTest)
        {
            var rawName = !string.IsNullOrWhiteSpace(labTest.TestName)
                ? labTest.TestName
                : labTest.TestType;

            return MapTestNameForSubmit(rawName ?? string.Empty);
        }

        private static bool IsSupportedSubmitTest(LabTest labTest)
        {
            if (string.IsNullOrWhiteSpace(labTest.TestName) && string.IsNullOrWhiteSpace(labTest.TestType))
                return false;

            var normalized = ResolveSubmitTestName(labTest).Trim().ToLowerInvariant();
            return normalized is
                "cbc (complete blood count)" or
                "urinalysis" or
                "tsh (thyroid)" or
                "ferritin" or
                "fasting blood glucose" or
                "hba1c (sugar test)" or
                "blood group" or
                "hbsag (hepatitis b)" or
                "hcv (hepatitis c)";
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

        private static JsonSerializerOptions SubmitJsonOptions() => new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }
}
