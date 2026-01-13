namespace ValternativeServer.Models.Recruiters
{
    public class RecruiterNotificationSettings
    {
        public Guid Id { get; set; }

        public Guid RecruiterId { get; set; }

        public bool SubmissionStatusUpdates { get; set; } = true;

        public bool WeeklyPerformanceReport { get; set; } = true;

        public bool SystemUpdates { get; set; } = true;

        public Recruiter? Recruiter { get; set; }
    }
}