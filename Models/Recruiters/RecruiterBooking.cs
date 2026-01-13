namespace ValternativeServer.Models.Recruiters
{
    public class RecruiterBooking
    {
        public Guid Id { get; set; }

        public Guid RiderId { get; set; }

        public string RiderName { get; set; } = null!;

        public DateTime BookingDate { get; set; }

        public TimeSpan BookingTime { get; set; }

        public string BookingType { get; set; } = null!;

        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}