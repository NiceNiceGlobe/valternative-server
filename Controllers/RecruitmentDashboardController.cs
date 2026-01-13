using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ValternativeServer.ValternativeDb;

namespace nicenice.Server.Controllers
{
    [ApiController]
    [Route("api/recruitment")]
    public class RecruitmentDashboardController : ControllerBase
    {
        private readonly ValternativeDbContext _context;

        public RecruitmentDashboardController(ValternativeDbContext context)
        {
            _context = context;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var todayStart = DateTime.Today;
            var todayEnd = todayStart.AddDays(1);

            var totalRiders = await _context.RecruiterRiders.CountAsync();

            var expectedToday = await _context.RecruiterRiders
                .CountAsync(r => r.CreatedAt >= todayStart && r.CreatedAt < todayEnd);

            var assistedToday = await _context.RecruiterRiders
                .CountAsync(r =>
                    r.CreatedAt >= todayStart &&
                    r.CreatedAt < todayEnd &&
                    r.Status == "Pending");

            return Ok(new
            {
                totalRiders,
                expectedToday,
                assistedToday,
                remainingToday = expectedToday - assistedToday
            });
        }

        [HttpGet("cities")]
        public async Task<IActionResult> GetCityDistribution()
        {
            var data = await _context.RecruiterRiders
                .GroupBy(r => string.IsNullOrEmpty(r.City) ? "Unknown" : r.City)
                .Select(g => new
                {
                    city = g.Key,
                    count = g.Count()
                })
                .OrderByDescending(x => x.count)
                .ToListAsync();

            return Ok(data);
        }

        [HttpGet("riders")]
        public async Task<IActionResult> GetRiders(
            int page = 1,
            int pageSize = 10,
            string? search = null,
            string? status = null)
        {
            var query = _context.RecruiterRiders.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(r =>
                    r.FullName.Contains(search) ||
                    r.Email.Contains(search) ||
                    r.PhoneNumber.Contains(search) ||
                    r.City.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                query = query.Where(r => r.Status == status);
            }

            var totalCount = await query.CountAsync();

            var riders = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new
                {
                    r.Id,
                    r.FullName,
                    r.Email,
                    r.PhoneNumber,
                    r.City,
                    r.Status,
                    r.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                totalCount,
                page,
                pageSize,
                riders
            });
        }
    }
}