namespace ValternativeServer.Models.DTOs.Recruiters
{
    public class CreateRecruiterRiderDto
    {
        public string FullName { get; set; } = null!;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? City { get; set; }
        public string? Nationality { get; set; }
    }
}