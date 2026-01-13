using System;

namespace ValternativeServer.Models.DTOs.Recruiters
{
    public class DeployRiderDto
    {
        public Guid RiderId { get; set; }
        public string? City { get; set; }
        public string BikeRegistration { get; set; } = null!;
    }
}