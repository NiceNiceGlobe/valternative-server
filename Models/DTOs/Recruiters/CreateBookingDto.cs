namespace ValternativeServer.Models.DTOs.Recruiters
{
    public class CreateBookingDto
    {
        public Guid RiderId { get; set; }
        public DateTime BookingDate { get; set; }
        public TimeSpan BookingTime { get; set; }
        public string BookingType { get; set; } = null!;
    }
}