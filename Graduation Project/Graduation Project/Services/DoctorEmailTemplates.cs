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
    <div style='margin-bottom:18px;'>
      <img src='cid:nabd-logo' alt='NABD نبض' style='max-width:130px;height:auto;display:block;margin:0 auto;'/>
    </div>
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

        public static string NewPatientWelcome(string patientName, string email, string tempPassword, string loginUrl)
        {
            return $@"<!doctype html><html><head><meta charset='utf-8'/>
<style>
{BaseStyle}
.cred-box{{background:rgba(74,222,128,0.07);border:1px solid rgba(74,222,128,0.22);border-radius:12px;padding:20px 24px;margin:20px 0;}}
.cred-label{{font-size:11px;font-weight:700;text-transform:uppercase;letter-spacing:0.08em;color:#4ade80;margin-bottom:10px;}}
.cred-row{{display:flex;align-items:center;margin-bottom:8px;gap:10px;}}
.cred-key{{font-size:13px;color:#8b949e;min-width:90px;}}
.cred-val{{font-size:14px;font-weight:700;color:#e6edf3;letter-spacing:0.03em;background:rgba(255,255,255,0.05);padding:5px 12px;border-radius:6px;font-family:monospace;}}
.warn-box{{background:rgba(251,191,36,0.08);border:1px solid rgba(251,191,36,0.22);border-radius:10px;padding:14px 18px;margin:20px 0;font-size:13px;color:#fbbf24;}}
.btn{{display:inline-block;padding:13px 36px;border-radius:999px;font-size:15px;font-weight:700;text-decoration:none;margin-top:8px;background:linear-gradient(135deg,#2563eb 0%,#3b82f6 55%,#60a5fa 100%);color:#ffffff !important;box-shadow:0 12px 24px rgba(37,99,235,0.35),0 2px 6px rgba(0,0,0,0.2);}}
</style></head><body>
<div class='wrap'>
  <div class='header' style='background:linear-gradient(135deg,#0a1628 0%,#1e3a8a 60%,#1d4ed8 100%);'>
    <div style='margin-bottom:18px;'>
      <img src='cid:nabd-logo' alt='NABD نبض' style='max-width:130px;height:auto;display:block;margin:0 auto;'/>
    </div>
    <div style='font-size:40px;margin-bottom:10px;'>🩺</div>
    <h1 style='color:#e6edf3;'>Welcome to NABD نبض</h1>
    <p style='color:#93c5fd;font-size:14px;margin:0;'>NABD نبض · Patient Portal</p>
  </div>
  <div class='body'>
    <p>Hello <strong style='color:#e6edf3;'>{System.Net.WebUtility.HtmlEncode(patientName)}</strong>,</p>
    <p>Your account has been created on <strong style='color:#4f8ef7;'>NABD نبض</strong> by our clinic team. Use the credentials below to sign in for the first time.</p>
    <div class='cred-box'>
      <div class='cred-label'>Your Login Credentials</div>
      <div class='cred-row'>
        <span class='cred-key'>Email</span>
        <span class='cred-val'>{System.Net.WebUtility.HtmlEncode(email)}</span>
      </div>
      <div class='cred-row'>
        <span class='cred-key'>Password</span>
        <span class='cred-val'>{System.Net.WebUtility.HtmlEncode(tempPassword)}</span>
      </div>
    </div>
    <div class='warn-box'>⚠️ This is a temporary password. Please change it immediately after signing in.</div>
    <p style='text-align:center;'>
      <a class='btn' href='{loginUrl}'>Sign In Now</a>
    </p>
    <hr class='divider'/>
    <p style='font-size:13px;color:#8b949e;'>If you did not expect this account, please contact your clinic or ignore this email.</p>
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
    <div style='margin-bottom:18px;'>
      <img src='cid:nabd-logo' alt='NABD نبض' style='max-width:130px;height:auto;display:block;margin:0 auto;'/>
    </div>
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
