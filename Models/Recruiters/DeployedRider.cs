using System;

namespace ValternativeServer.Models.Recruiters
{
    public class DeployedRider
    {
        public Guid Id { get; set; }

        public Guid RiderId { get; set; }
        public RecruiterRider Rider { get; set; } = null!;

        public string City { get; set; } = null!;

        public string Recruiter { get; set; } = null!;

        public DateTime DeploymentDate { get; set; }

        public string BikeRegistration { get; set; } = null!;

        public Guid AgentUserId { get; set; }

        public string Agent { get; set; } = null!;

        public string Status { get; set; } = "Deployed";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}