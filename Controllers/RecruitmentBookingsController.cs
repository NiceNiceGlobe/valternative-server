using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ValternativeServer.Models.Recruiters;
using ValternativeServer.Models.DTOs.Recruiters;
using ValternativeServer.Services;
using ValternativeServer.ValternativeDb;
using Microsoft.AspNetCore.Authorization;

namespace nicenice.Server.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/recruitment/bookings")]
    public class RecruitmentBookingsController : ControllerBase
    {
        private readonly ValternativeDbContext _context;
        private readonly IEmailService _emailService;

        public RecruitmentBookingsController(
            ValternativeDbContext context,
            IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var today = DateTime.Today;
            var next7Days = today.AddDays(7);

            var totalBookings = await _context.RecruiterBookings.CountAsync();

            var todayBookings = await _context.RecruiterBookings
                .CountAsync(b => b.BookingDate.Date == today);

            var upcomingBookings = await _context.RecruiterBookings
                .CountAsync(b =>
                    b.BookingDate.Date > today &&
                    b.BookingDate.Date <= next7Days);

            var pendingBookings = await _context.RecruiterBookings
                .CountAsync(b => b.Status == "Pending");

            return Ok(new
            {
                totalBookings,
                todayBookings,
                upcomingBookings,
                pendingBookings
            });
        }

        [HttpGet("today")]
        public async Task<IActionResult> GetTodayBookings()
        {
            var today = DateTime.Today;

            var bookings = await _context.RecruiterBookings
                .Where(b => b.BookingDate.Date == today)
                .OrderBy(b => b.BookingTime)
                .Select(b => new
                {
                    b.Id,
                    b.RiderId,
                    b.RiderName,
                    b.BookingDate,
                    b.BookingTime,
                    b.BookingType,
                    b.Status
                })
                .ToListAsync();

            return Ok(bookings);
        }

        [HttpGet("upcoming")]
        public async Task<IActionResult> GetUpcomingBookings()
        {
            var today = DateTime.Today;
            var next7Days = today.AddDays(7);

            var bookings = await _context.RecruiterBookings
                .Where(b =>
                    b.BookingDate.Date > today &&
                    b.BookingDate.Date <= next7Days)
                .OrderBy(b => b.BookingDate)
                .ThenBy(b => b.BookingTime)
                .Select(b => new
                {
                    b.Id,
                    b.RiderId,
                    b.RiderName,
                    b.BookingDate,
                    b.BookingTime,
                    b.BookingType,
                    b.Status
                })
                .ToListAsync();

            return Ok(bookings);
        }

        [HttpGet("calendar")]
        public async Task<IActionResult> GetCalendarBookings()
        {
            var bookings = await _context.RecruiterBookings
                .Select(b => new
                {
                    id = b.Id,
                    title = $"{b.RiderName} - {b.BookingType}",
                    start = b.BookingDate.Add(b.BookingTime),
                    status = b.Status
                })
                .ToListAsync();

            return Ok(bookings);
        }

        [HttpGet]
        public async Task<IActionResult> GetBookings(
            int page = 1,
            int pageSize = 10,
            string? search = null,
            string? status = null)
        {
            var query = _context.RecruiterBookings.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(b => b.RiderName.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                query = query.Where(b => b.Status == status);
            }

            var totalCount = await query.CountAsync();

            var bookings = await query
                .OrderByDescending(b => b.BookingDate)
                .ThenByDescending(b => b.BookingTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new
                {
                    b.Id,
                    b.RiderId,
                    b.RiderName,
                    b.BookingDate,
                    b.BookingTime,
                    b.BookingType,
                    b.Status,
                    b.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                totalCount,
                page,
                pageSize,
                bookings
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDto dto)
        {
            var rider = await _context.RecruiterRiders
                .Where(r => r.Id == dto.RiderId)
                .Select(r => new { r.Id, r.FullName, r.Email })
                .FirstOrDefaultAsync();

            if (rider == null)
                return BadRequest("Invalid rider");

            var booking = new RecruiterBooking
            {
                RiderId = rider.Id,
                RiderName = rider.FullName,
                BookingDate = dto.BookingDate.Date,
                BookingTime = dto.BookingTime,
                BookingType = dto.BookingType,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.RecruiterBookings.Add(booking);
            await _context.SaveChangesAsync();

            var year = DateTime.UtcNow.Year;

            var html = $@"
        <div style='max-width:600px;margin:0 auto;font-family:Segoe UI,Tahoma,Geneva,Verdana,sans-serif;
                    color:#1f2937;border:1px solid #e5e7eb;border-radius:8px;overflow:hidden;background:#ffffff;'>

            <div style='background:#0a58ca;color:#ffffff;padding:22px;text-align:center;'>
                <h2 style='margin:0;font-weight:600;'>Valternative Booking Confirmation</h2>
                <p style='margin:6px 0 0;font-size:14px;'>
                    {booking.BookingDate:dd MMM yyyy}
                </p>
            </div>

            <div style='padding:24px;line-height:1.6;'>
                <p style='margin:0 0 12px;'>
                    Hi <strong>{rider.FullName}</strong>,
                </p>

                <p style='margin:0 0 20px;'>
                    A booking has been scheduled for you with <strong>Valternative</strong>.
                    Please find the details below:
                </p>

                <div style='background:#f0f7ff;border-left:4px solid #0a58ca;
                            padding:16px;margin:20px 0;border-radius:4px;'>

                    <p style='margin:0 0 8px;'><strong>Booking Type:</strong> {booking.BookingType}</p>
                    <p style='margin:0 0 8px;'><strong>Date:</strong> {booking.BookingDate:dd MMM yyyy}</p>
                    <p style='margin:0 0 8px;'><strong>Time:</strong> {booking.BookingTime}</p>
                </div>

                <p style='margin:20px 0;'>
                    Please ensure you are available and on time.
                </p>

                <p style='margin:0;'>
                    Kind regards,<br/>
                    <strong>Valternative Team</strong>
                </p>
            </div>

            <div style='background:#f9fafb;padding:14px;text-align:center;
                        font-size:12px;color:#6b7280;'>
                © {year} Valternative. All rights reserved.
            </div>
        </div>";

            await _emailService.SendEmailAsync(
                rider.Email,
                "Valternative Booking Confirmation",
                html
            );

            return Ok(booking);
        }

        [HttpGet("by-rider/{riderId}")]
        public async Task<IActionResult> GetByRider(Guid riderId)
        {
            return Ok(await _context.RecruiterBookings
                .Where(b => b.RiderId == riderId)
                .OrderBy(b => b.BookingDate)
                .ToListAsync());
        }

        [HttpGet("riders/dropdown")]
        public async Task<IActionResult> GetRidersForDropdown()
        {
            var riders = await _context.RecruiterRiders
                .OrderBy(r => r.FullName)
                .Select(r => new
                {
                    r.Id,
                    r.FullName
                })
                .ToListAsync();

            return Ok(riders);
        }
    }
}