namespace ValternativeServer.Models.DTOs.Recruiters
{
    public class UpdateSubmissionReviewDto
    {
        public string Status { get; set; } = null!;
        public string? AdminFeedback { get; set; }
    }
}