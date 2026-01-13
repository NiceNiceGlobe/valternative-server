namespace ValternativeServer.Models.DTOs.Recruiters
{
    public class RecruiterPerformanceDto
    {
        public int TotalRidersSubmitted { get; set; }
        public double ApprovalRate { get; set; }
        public double AvgProcessingDays { get; set; }
    }
}