namespace ValternativeServer.Models.DTOs.Recruiters
{
    public class AdminRecruiterProfileDto
    {
        public Guid RecruiterId { get; set; }
        public string RecruiterCode { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime JoinedAt { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string LastActive { get; set; } = null!;
    }
}