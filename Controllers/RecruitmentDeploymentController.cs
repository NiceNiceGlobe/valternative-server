using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ValternativeServer.Models.Recruiters;
using ValternativeServer.ValternativeDb;
using ValternativeServer.Models.DTOs.Recruiters;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace nicenice.Server.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/recruitment/deployments")]
    public class RecruitmentDeploymentController : ControllerBase
    {
        private readonly ValternativeDbContext _context;

        public RecruitmentDeploymentController(ValternativeDbContext context)
        {
            _context = context;
        }

        [HttpPost("deploy")]
        public async Task<IActionResult> DeployRider([FromBody] DeployRiderDto dto)
        {
            var rider = await _context.RecruiterRiders
                .FirstOrDefaultAsync(r => r.Id == dto.RiderId);

            if (rider == null)
                return BadRequest("Invalid rider");

            var alreadyDeployed = await _context.DeployedRiders
                .AnyAsync(d => d.RiderId == dto.RiderId);

            if (alreadyDeployed)
                return BadRequest("Rider is already deployed");

            var agentUserId = Guid.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!
            );

            var agentUser = await _context.Users
                .Where(u => u.Id == agentUserId)
                .Select(u => new { u.FirstName, u.LastName })
                .FirstOrDefaultAsync();

            var agentName = $"{agentUser?.FirstName} {agentUser?.LastName}".Trim();

            if (string.IsNullOrWhiteSpace(agentName))
                agentName = "Unknown Agent";

            var deployed = new DeployedRider
            {
                Id = Guid.NewGuid(),
                RiderId = rider.Id,
                City = string.IsNullOrWhiteSpace(dto.City) ? rider.City ?? "Unknown" : dto.City,
                Recruiter = "Recruitment Team",
                DeploymentDate = DateTime.UtcNow,
                BikeRegistration = dto.BikeRegistration,
                AgentUserId = agentUserId,
                Agent = agentName,
                Status = "Deployed",
                CreatedAt = DateTime.UtcNow
            };

            _context.DeployedRiders.Add(deployed);
            rider.Status = "Deployed";

            await _context.SaveChangesAsync();

            return Ok(deployed);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDeployedRider(Guid id, [FromBody] UpdateDeployedRiderDto dto)
        {
            var deployed = await _context.DeployedRiders
                .FirstOrDefaultAsync(d => d.Id == id);

            if (deployed == null)
                return NotFound();

            deployed.City = dto.City ?? deployed.City;
            deployed.BikeRegistration = dto.BikeRegistration ?? deployed.BikeRegistration;
            deployed.Status = dto.Status ?? deployed.Status;

            await _context.SaveChangesAsync();

            return Ok(deployed);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDeployedRider(Guid id)
        {
            var deployed = await _context.DeployedRiders
                .FirstOrDefaultAsync(d => d.Id == id);

            if (deployed == null)
                return NotFound();

            var rider = await _context.RecruiterRiders
                .FirstOrDefaultAsync(r => r.Id == deployed.RiderId);

            if (rider != null)
            {
                rider.Status = "Pending";
            }

            _context.DeployedRiders.Remove(deployed);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetDeployedRiders(
            int page = 1,
            int pageSize = 10,
            string? search = null,
            string? city = null)
        {
            var query = _context.DeployedRiders
                .Include(d => d.Rider)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(d =>
                    d.Rider.FullName.Contains(search) ||
                    d.Rider.Email.Contains(search) ||
                    d.BikeRegistration.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(city))
            {
                query = query.Where(d => d.City == city);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .Join(_context.Users,
                    d => d.AgentUserId,
                    u => u.Id,
                    (d, u) => new
                    {
                        d.Id,
                        RiderName = d.Rider.FullName,
                        d.City,
                        d.Recruiter,
                        d.DeploymentDate,
                        d.BikeRegistration,
                        Agent = u.FirstName + " " + u.LastName,
                        d.Status
                    })
                .OrderByDescending(x => x.DeploymentDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                totalCount,
                page,
                pageSize,
                items
            });
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetDeploymentStats()
        {
            var now = DateTime.UtcNow;

            var startOfWeek = now.Date.AddDays(-(int)now.DayOfWeek);
            var startOfMonth = new DateTime(now.Year, now.Month, 1);

            var totalDeployed = await _context.DeployedRiders.CountAsync();

            var deployedThisWeek = await _context.DeployedRiders
                .CountAsync(d => d.DeploymentDate >= startOfWeek);

            var deployedThisMonth = await _context.DeployedRiders
                .CountAsync(d => d.DeploymentDate >= startOfMonth);

            var totalRecruited = await _context.RecruiterRiders.CountAsync();

            var deploymentRate = totalRecruited == 0
                ? 0
                : Math.Round((double)totalDeployed / totalRecruited * 100, 0);

            return Ok(new
            {
                totalDeployed,
                thisWeek = deployedThisWeek,
                thisMonth = deployedThisMonth,
                deploymentRate
            });
        }

        [HttpGet("by-city")]
        public async Task<IActionResult> GetDeploymentByCity()
        {
            var data = await _context.DeployedRiders
                .GroupBy(d => d.City)
                .Select(g => new
                {
                    city = g.Key,
                    deployed = g.Count()
                })
                .OrderByDescending(x => x.deployed)
                .ToListAsync();

            return Ok(data);
        }

        [HttpGet("top-cities")]
        public async Task<IActionResult> GetTopPerformingCities()
        {
            var topCities = await _context.DeployedRiders
                .GroupBy(d => d.City)
                .Select(g => new
                {
                    city = g.Key,
                    deployed = g.Count()
                })
                .OrderByDescending(x => x.deployed)
                .Take(5)
                .ToListAsync();

            return Ok(topCities);
        }
    }
}