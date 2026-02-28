namespace Graduation_Project.ViewModels
{
    public class RecentActivityItem
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime DateTime { get; set; }
        public string IconClass { get; set; }       // e.g. "fas fa-heartbeat"
        public string IconBgColor { get; set; }     // e.g. "#e3f2fd"
        public string IconColor { get; set; }       // e.g. "#2196f3"

        /// <summary>
        /// When set, this label is shown instead of the computed RelativeTime.
        /// Use this for items whose "time" has a fixed meaning (e.g. a future appointment date).
        /// </summary>
        public string OverrideTime { get; set; }

        /// <summary>
        /// Returns a human-readable time string relative to now,
        /// or OverrideTime if one has been explicitly provided.
        /// </summary>
        public string RelativeTime
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(OverrideTime))
                    return OverrideTime;

                var diff = DateTime.Now - DateTime;

                // Future dates should never show "Just now" – display the date instead
                if (diff.TotalSeconds < 0) return DateTime.ToString("MMM dd, yyyy");

                if (diff.TotalMinutes < 1) return "Just now";
                if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} minutes ago";
                if (diff.TotalHours < 2) return "1 hour ago";
                if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} hours ago";
                if (diff.TotalDays < 2) return "Yesterday";
                if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} days ago";
                return DateTime.ToString("MMM dd, yyyy");
            }
        }
    }
}
