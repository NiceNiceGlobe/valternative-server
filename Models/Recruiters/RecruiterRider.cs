namespace ValternativeServer.Models.Recruiters
{
    public class RecruiterRider
    {
        public Guid Id { get; set; }

        public Guid SubmissionId { get; set; }
        public RecruiterSubmission Submission { get; set; } = null!;

        public string FullName { get; set; } = null!;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? City { get; set; }
        public string? Nationality { get; set; }

        public string Status { get; set; } = "Pending";
        public string? AdminNotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}