namespace ValternativeServer.Models.Recruiters
{
    public class RecruiterSubmission
    {
        public Guid Id { get; set; }

        public Guid RecruiterId { get; set; }
        public Recruiter Recruiter { get; set; } = null!;

        public string FileName { get; set; } = null!;
        public int TotalRiders { get; set; }

        public string Status { get; set; } = "Pending";
        public string? AdminFeedback { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }

        public ICollection<RecruiterRider> Riders { get; set; } = new List<RecruiterRider>();
    }
}