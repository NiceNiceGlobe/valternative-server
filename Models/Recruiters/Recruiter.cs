using ValternativeServer.Models.Auth;

namespace ValternativeServer.Models.Recruiters
{
    public class Recruiter
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        public ValternativeUser User { get; set; } = null!;

        public string RecruiterCode { get; set; } = null!;

        public string Status { get; set; } = "Active";

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        public ICollection<RecruiterSubmission> Submissions { get; set; } = new List<RecruiterSubmission>();
    }
}