namespace Graduation_Project.Services
{
    public static class DoctorEmailTemplates
    {
        private const string BaseStyle = @"
            body{margin:0;padding:0;background:#0d1117;font-family:'Segoe UI',Arial,sans-serif;}
            .wrap{max-width:580px;margin:40px auto;background:#161b22;border-radius:16px;overflow:hidden;border:1px solid rgba(255,255,255,0.08);}
            .header{padding:36px 40px 28px;text-align:center;}
            .body{padding:8px 40px 36px;}
            .footer{padding:20px 40px;background:#0d1117;text-align:center;font-size:12px;color:#6e7681;}
            h1{margin:0 0 6px;font-size:22px;font-weight:700;}
            p{margin:12px 0;font-size:15px;line-height:1.65;color:#c9d1d9;}
            .note-box{background:rgba(248,113,113,0.08);border:1px solid rgba(248,113,113,0.22);
                      border-radius:10px;padding:16px 18px;margin:20px 0;}
            .note-label{font-size:11px;font-weight:700;text-transform:uppercase;
                        letter-spacing:0.08em;color:#f87171;margin-bottom:6px;}
            .note-text{font-size:14px;color:#e6edf3;line-height:1.6;}
            .btn{display:inline-block;padding:13px 32px;border-radius:10px;
                 font-size:15px;font-weight:700;text-decoration:none;margin-top:8px;}
            .divider{border:none;border-top:1px solid rgba(255,255,255,0.07);margin:24px 0;}";

        public static string Approved(string doctorName)
        {
            return $@"<!doctype html><html><head><meta charset='utf-8'/>
<style>{BaseStyle}</style></head><body>
<div class='wrap'>
  <div class='header' style='background:linear-gradient(135deg,#0a1628 0%,#1e3a8a 60%,#1d4ed8 100%);'>
    <div style='font-size:40px;margin-bottom:10px;'>✅</div>
    <h1 style='color:#e6edf3;'>Registration Approved!</h1>
    <p style='color:#93c5fd;font-size:14px;margin:0;'>NABD نبض · Doctor Portal</p>
  </div>
  <div class='body'>
    <p>Dear <strong style='color:#e6edf3;'>Dr. {doctorName}</strong>,</p>
    <p>We are pleased to inform you that your registration on the <strong style='color:#4f8ef7;'>NABD نبض</strong> platform has been <strong style='color:#4ade80;'>reviewed and approved</strong> by our admin team.</p>
    <p>You can now log in to access your full doctor dashboard, manage your patients, view appointments, and use all platform features.</p>
    <hr class='divider'/>
    <p style='font-size:13px;color:#8b949e;'>If you have any questions, please contact your clinic administrator.</p>
  </div>
  <div class='footer'>© {DateTime.UtcNow.Year} NABD نبض · Healthcare Management Platform</div>
</div>
</body></html>";
        }

        public static string Rejected(string doctorName, string? rejectionNote)
        {
            var noteSection = string.IsNullOrWhiteSpace(rejectionNote)
                ? "<p>No specific reason was provided. Please contact the admin team for further information.</p>"
                : $@"<div class='note-box'>
                       <div class='note-label'>Reason for Rejection</div>
                       <div class='note-text'>{System.Net.WebUtility.HtmlEncode(rejectionNote)}</div>
                     </div>";

            return $@"<!doctype html><html><head><meta charset='utf-8'/>
<style>{BaseStyle}</style></head><body>
<div class='wrap'>
  <div class='header' style='background:linear-gradient(135deg,#1a0a0a 0%,#450a0a 60%,#7f1d1d 100%);'>
    <div style='font-size:40px;margin-bottom:10px;'>⚠️</div>
    <h1 style='color:#e6edf3;'>Registration Not Approved</h1>
    <p style='color:#fca5a5;font-size:14px;margin:0;'>NABD نبض · Doctor Portal</p>
  </div>
  <div class='body'>
    <p>Dear <strong style='color:#e6edf3;'>Dr. {doctorName}</strong>,</p>
    <p>Thank you for registering on <strong style='color:#4f8ef7;'>NABD نبض</strong>. After reviewing your application, our admin team was <strong style='color:#f87171;'>unable to approve</strong> your registration at this time.</p>
    {noteSection}
    <p>If you believe this is an error or you have addressed the issue, please contact your clinic administrator or re-submit your registration with the required documents.</p>
    <hr class='divider'/>
    <p style='font-size:13px;color:#8b949e;'>© {DateTime.UtcNow.Year} NABD نبض · Healthcare Management Platform</p>
  </div>
  <div class='footer'>© {DateTime.UtcNow.Year} NABD نبض · Healthcare Management Platform</div>
</div>
</body></html>";
        }
    }
}
