using System;

namespace Graduation_Project.Services
{
    /// <summary>
    /// An analysis error whose message is written for the patient and is safe to display as-is
    /// (validation / actionable guidance, e.g. "Please add RDW from your lab report").
    /// Anything that is NOT this type is treated as a developer/technical error and is replaced
    /// with a generic friendly message before it reaches the user.
    /// </summary>
    public class AnalysisUserException : InvalidOperationException
    {
        public AnalysisUserException(string message) : base(message) { }
    }

    /// <summary>
    /// Central source of the patient-facing messages for the lab-test analysis flow.
    /// Keeps developer detail (status codes, stack traces, raw .NET messages) out of the UI —
    /// those stay in the logs — while showing users something they can act on.
    /// </summary>
    public static class AnalysisErrorMessages
    {
        public const string ServiceBusy =
            "The analysis service is busy right now. Please wait a moment and tap Retry Analysis.";

        public const string OcrUnreadable =
            "We couldn't read any test values from this image. Please upload a clearer photo of your lab report.";

        public const string OcrUnavailable =
            "The text-reading service is temporarily unavailable. Please try again in a moment.";

        public const string Timeout =
            "This is taking longer than expected. The service may be busy — please try again.";

        public const string Generic =
            "Something went wrong while analyzing your tests. Please try again, and re-upload your tests if the problem continues.";

        /// <summary>
        /// Converts any exception thrown during the analysis flow into a message a patient can
        /// understand. User-authored messages (<see cref="AnalysisUserException"/>) are shown
        /// verbatim; everything else is mapped to a friendly, non-technical message.
        /// </summary>
        public static string ToUserMessage(Exception? ex)
        {
            switch (ex)
            {
                case null:
                    return Generic;
                case AnalysisUserException:
                    return ex.Message;
                case TaskCanceledException:
                case TimeoutException:
                    return Timeout;
            }

            var message = ex.Message ?? string.Empty;

            if (message.Contains("OCR service returned no values", StringComparison.OrdinalIgnoreCase))
                return OcrUnreadable;
            if (message.Contains("OCR", StringComparison.OrdinalIgnoreCase))
                return OcrUnavailable;
            if (message.Contains("analysis service", StringComparison.OrdinalIgnoreCase)
                || message.Contains("unavailable", StringComparison.OrdinalIgnoreCase)
                || message.Contains("timed out", StringComparison.OrdinalIgnoreCase))
                return ServiceBusy;

            return Generic;
        }
    }
}
