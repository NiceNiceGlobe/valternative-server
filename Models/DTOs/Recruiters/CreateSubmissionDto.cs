namespace ValternativeServer.Models.DTOs.Recruiters
{
    public class CreateSubmissionDto
    {
        public string FileName { get; set; } = null!;
        public int TotalRiders { get; set; }
    }
}