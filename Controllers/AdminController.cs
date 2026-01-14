using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using ValternativeServer.ValternativeDb;
using ValternativeServer.Models.Recruiters;
using ValternativeServer.Models.DTOs.Recruiters;
using ValternativeServer.Models.Auth;

namespace nicenice.Server.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly ValternativeDbContext _context;
        private readonly UserManager<ValternativeUser> _userManager;
        private readonly RoleManager<Role> _roleManager;

        public AdminController(
            ValternativeDbContext context,
            UserManager<ValternativeUser> userManager,
            RoleManager<Role> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetAdminDashboard()
        {
            var submissions = await _context.RecruiterSubmissions.ToListAsync();
            var riders = await _context.RecruiterRiders.ToListAsync();

            var approvedRiders = riders.Count(r => r.Status == "Approved");
            var pendingSubmissions = submissions.Count(s => s.Status == "Pending");

            return Ok(new
            {
                totalRecruiters = await _context.Recruiters.CountAsync(),
                totalSubmissions = submissions.Count,
                pendingReview = pendingSubmissions,
                totalRiders = riders.Count,
                approvalRate = riders.Count == 0
                    ? 0
                    : Math.Round((double)approvedRiders / riders.Count * 100, 2)
            });
        }

        [HttpGet("sidebar-counts")]
        public async Task<IActionResult> GetSidebarCounts()
        {
            return Ok(new
            {
                submissions = await _context.RecruiterSubmissions.CountAsync(),
                recruiters = await _context.Recruiters.CountAsync(),
                riders = await _context.RecruiterRiders.CountAsync()
            });
        }

        [HttpGet("recruiters")]
        public async Task<IActionResult> GetAllRecruiters()
        {
            var data = await _context.Recruiters
                .Include(r => r.User)
                .OrderByDescending(r => r.JoinedAt)
                .Select(r => new
                {
                    r.Id,
                    name = (r.User.FirstName + " " + r.User.LastName).Trim(),
                    r.RecruiterCode,
                    r.Status,
                    r.JoinedAt,
                    totalRiders = _context.RecruiterSubmissions
                        .Where(s => s.RecruiterId == r.Id)
                        .Sum(s => s.TotalRiders)
                })
                .ToListAsync();

            return Ok(data);
        }

        [HttpGet("recruiters/top")]
        public async Task<IActionResult> GetTopRecruiters()
        {
            var data = await _context.Recruiters
                .Include(r => r.User)
                .Select(r => new
                {
                    name = (r.User.FirstName + " " + r.User.LastName).Trim(),
                    active = r.Status == "Active",
                    riders = _context.RecruiterSubmissions
                        .Where(s => s.RecruiterId == r.Id)
                        .Sum(s => s.TotalRiders)
                })
                .OrderByDescending(x => x.riders)
                .Take(5)
                .ToListAsync();

            return Ok(data);
        }

        [HttpPost("recruiters")]
        public async Task<IActionResult> CreateRecruiter([FromBody] CreateRecruiterDto dto)
        {
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
                return BadRequest("User with this email already exists");

            var user = new ValternativeUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                DateOfCreation = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, dto.InitialPassword);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            if (!await _roleManager.RoleExistsAsync("Recruiter"))
            {
                await _roleManager.CreateAsync(new Role { Name = "Recruiter" });
            }

            await _userManager.AddToRoleAsync(user, "Recruiter");

            var recruiter = new Recruiter
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                RecruiterCode = $"REC-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
                Status = "Active",
                JoinedAt = DateTime.UtcNow
            };

            _context.Recruiters.Add(recruiter);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                recruiter.Id,
                recruiter.RecruiterCode,
                recruiter.Status,
                recruiter.JoinedAt
            });
        }

        [HttpGet("submissions")]
        public async Task<IActionResult> GetAllSubmissions()
        {
            var submissions = await _context.RecruiterSubmissions
                .Include(s => s.Recruiter)
                .ThenInclude(r => r.User)
                .OrderByDescending(s => s.UploadedAt)
                .Select(s => new
                {
                    s.Id,
                    s.FileName,
                    s.TotalRiders,
                    s.Status,
                    s.UploadedAt,
                    recruiterName = (s.Recruiter.User.FirstName + " " + s.Recruiter.User.LastName).Trim(),
                    recruiterCode = s.Recruiter.RecruiterCode
                })
                .ToListAsync();

            return Ok(submissions);
        }

        [HttpGet("submissions/{id}")]
        public async Task<IActionResult> GetSubmission(Guid id)
        {
            var submission = await _context.RecruiterSubmissions
                .Include(s => s.Recruiter)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (submission == null)
                return NotFound();

            return Ok(submission);
        }

        [HttpGet("submissions/{id}/riders")]
        public async Task<IActionResult> GetSubmissionRiders(Guid id)
        {
            var riders = await _context.RecruiterRiders
                .Where(r => r.SubmissionId == id)
                .Select(r => new
                {
                    r.Id,
                    r.FullName,
                    r.PhoneNumber,
                    r.City,
                    r.Nationality,
                    r.Status,
                    r.AdminNotes,
                    r.CreatedAt
                })
                .ToListAsync();

            return Ok(riders);
        }

        [HttpPut("riders/{id}")]
        public async Task<IActionResult> UpdateRider(
            Guid id,
            [FromBody] UpdateRecruiterRiderDto dto)
        {
            var rider = await _context.RecruiterRiders
                .FirstOrDefaultAsync(r => r.Id == id);

            if (rider == null)
                return NotFound();

            rider.FullName = dto.FullName ?? rider.FullName;
            rider.Email = dto.Email ?? rider.Email;
            rider.PhoneNumber = dto.PhoneNumber ?? rider.PhoneNumber;
            rider.City = dto.City ?? rider.City;
            rider.Nationality = dto.Nationality ?? rider.Nationality;
            rider.Status = dto.Status ?? rider.Status;

            await _context.SaveChangesAsync();

            return Ok(rider);
        }

        [HttpDelete("riders/{id}")]
        public async Task<IActionResult> DeleteRider(Guid id)
        {
            var rider = await _context.RecruiterRiders
                .FirstOrDefaultAsync(r => r.Id == id);

            if (rider == null)
                return NotFound();

            _context.RecruiterRiders.Remove(rider);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet("riders")]
        public async Task<IActionResult> GetAllRiders()
        {
            var riders = await _context.RecruiterRiders
                .Include(r => r.Submission)
                .ThenInclude(s => s.Recruiter)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.Id,
                    r.FullName,
                    r.PhoneNumber,
                    r.City,
                    r.Nationality,
                    r.Status,
                    r.AdminNotes,
                    submissionFile = r.Submission.FileName,
                    recruiterCode = r.Submission.Recruiter.RecruiterCode
                })
                .ToListAsync();

            return Ok(riders);
        }

        [HttpGet("riders/count")]
        public async Task<IActionResult> GetRidersCount()
        {
            var count = await _context.RecruiterRiders.CountAsync();
            return Ok(new { count });
        }

        [HttpPut("submissions/{id}/review")]
        public async Task<IActionResult> ReviewSubmission(
            Guid id,
            [FromBody] UpdateSubmissionReviewDto dto)
        {
            var submission = await _context.RecruiterSubmissions
                .FirstOrDefaultAsync(s => s.Id == id);

            if (submission == null)
                return NotFound();

            submission.Status = dto.Status;
            submission.AdminFeedback = dto.AdminFeedback;
            submission.ReviewedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet("recruiters/{id}")]
        public async Task<IActionResult> GetRecruiterProfile(Guid id)
        {
            var recruiter = await _context.Recruiters
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (recruiter == null)
                return NotFound();

            return Ok(new AdminRecruiterProfileDto
            {
                RecruiterId = recruiter.Id,
                RecruiterCode = recruiter.RecruiterCode,
                Status = recruiter.Status,
                JoinedAt = recruiter.JoinedAt,
                FullName = $"{recruiter.User.FirstName} {recruiter.User.LastName}".Trim(),
                Email = recruiter.User.Email!,
                PhoneNumber = recruiter.User.PhoneNumber,
                LastActive = "Today, 09:42 AM"
            });
        }

        [HttpPut("recruiters/{id}/profile")]
        public async Task<IActionResult> UpdateRecruiterProfile(
            Guid id,
            [FromBody] UpdateRecruiterProfileDto dto)
        {
            var recruiter = await _context.Recruiters
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (recruiter == null)
                return NotFound();

            if (!string.IsNullOrWhiteSpace(dto.FirstName))
                recruiter.User.FirstName = dto.FirstName;

            if (!string.IsNullOrWhiteSpace(dto.LastName))
                recruiter.User.LastName = dto.LastName;

            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                recruiter.User.Email = dto.Email;
                recruiter.User.UserName = dto.Email;
            }

            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
                recruiter.User.PhoneNumber = dto.PhoneNumber;

            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPost("recruiters/{id}/reset-password")]
        public async Task<IActionResult> ResetRecruiterPassword(
            Guid id,
            [FromBody] ResetRecruiterPasswordDto dto)
        {
            var recruiter = await _context.Recruiters
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (recruiter == null)
                return NotFound();

            var token = await _userManager.GeneratePasswordResetTokenAsync(recruiter.User);
            var result = await _userManager.ResetPasswordAsync(
                recruiter.User,
                token,
                dto.NewPassword
            );

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok();
        }

        [HttpGet("notifications")]
        public async Task<IActionResult> GetAdminNotifications()
        {
            var notifications = await _context.RecruiterSubmissions
                .OrderByDescending(s => s.UploadedAt)
                .Take(5)
                .Select(s => new
                {
                    status = s.Status,
                    fileName = s.FileName,
                    recruiter = s.Recruiter.RecruiterCode,
                    date = s.UploadedAt
                })
                .ToListAsync();

            return Ok(new
            {
                recentActivity = notifications
            });
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentAdmin()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            return Ok(new
            {
                id = user.Id,
                fullName = $"{user.FirstName} {user.LastName}".Trim(),
                email = user.Email,
                role = "Administrator"
            });
        }
    }
}