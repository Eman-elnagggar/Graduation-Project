namespace Graduation_Project.ViewModels.Chat
{
    public class SendReportToDoctorRequest
    {
        public string? DoctorUserId { get; set; }
        public string? AttachmentUrl { get; set; }
        public string? FileName { get; set; }
        public string? Caption { get; set; }
    }
}
