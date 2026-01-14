using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ValternativeServer.ValternativeDb;
using ValternativeServer.Models.Recruiters;
using ValternativeServer.Models.DTOs.Recruiters;
using ValternativeServer.Services;
using Microsoft.AspNetCore.Authorization;

namespace nicenice.Server.Controllers
{
    [Authorize(Roles = "Admin,Recruiter")]
    [ApiController]
    [Route("api/recruiter")]
    public class RecruiterController : ControllerBase
    {
        private readonly ValternativeDbContext _context;
        private readonly IIdentityServices _identityService;
        private readonly RecruiterCsvService _csvService;

        public RecruiterController(
            ValternativeDbContext context,
            IIdentityServices identityService,
            RecruiterCsvService csvService)
        {
            _context = context;
            _identityService = identityService;
            _csvService = csvService;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetRecruiterDashboard()
        {
            var userId = _identityService.GetUserId();
            if (userId == null) return Unauthorized();

            var recruiter = await _context.Recruiters
                .FirstOrDefaultAsync(r => r.UserId == userId);

            if (recruiter == null) return NotFound();

            var submissions = await _context.RecruiterSubmissions
                .Where(s => s.RecruiterId == recruiter.Id)
                .ToListAsync();

            var totalRiders = submissions.Sum(s => s.TotalRiders);
            var approvedRiders = submissions
                .Where(s => s.Status == "Approved")
                .Sum(s => s.TotalRiders);

            return Ok(new
            {
                submissionsCount = submissions.Count,
                totalRiders,
                approvedRiders,
                approvalRate = totalRiders == 0
                    ? 0
                    : (approvedRiders * 100) / totalRiders,
                recentActivity = submissions
                    .OrderByDescending(s => s.UploadedAt)
                    .Take(5)
                    .Select(s => new
                    {
                        s.FileName,
                        s.TotalRiders,
                        s.Status,
                        s.UploadedAt
                    })
            });
        }

        [HttpPost("upload-submission")]
        public async Task<IActionResult> UploadSubmission([FromBody] CreateSubmissionDto dto)
        {
            var userId = _identityService.GetUserId();
            if (userId == null || userId == Guid.Empty)
                return Unauthorized("User not authenticated.");

            var recruiter = await _context.Recruiters
                .FirstOrDefaultAsync(r => r.UserId == userId);

            if (recruiter == null)
                return NotFound("Recruiter profile not found.");

            var submission = new RecruiterSubmission
            {
                Id = Guid.NewGuid(),
                RecruiterId = recruiter.Id,
                FileName = dto.FileName,
                TotalRiders = dto.TotalRiders,
                Status = "Pending",
                UploadedAt = DateTime.UtcNow
            };

            _context.RecruiterSubmissions.Add(submission);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                submissionId = submission.Id,
                submission.Status
            });
        }

        [HttpPost("upload-csv/{submissionId}")]
        public async Task<IActionResult> UploadCsv(Guid submissionId, IFormFile file)
        {
            var userId = _identityService.GetUserId();
            if (userId == null || userId == Guid.Empty)
                return Unauthorized();

            var recruiter = await _context.Recruiters
                .FirstOrDefaultAsync(r => r.UserId == userId);

            if (recruiter == null)
                return NotFound("Recruiter profile not found.");

            var submission = await _context.RecruiterSubmissions
                .FirstOrDefaultAsync(s => s.Id == submissionId && s.RecruiterId == recruiter.Id);

            if (submission == null)
                return NotFound("Submission not found.");

            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            List<ParsedRecruiterRiderDto> parsed;

            if (extension == ".csv")
            {
                parsed = _csvService.Parse(file.OpenReadStream()).ToList();
            }
            else if (extension == ".xlsx" || extension == ".xls")
            {
                parsed = _csvService.ParseExcel(file.OpenReadStream()).ToList();
            }
            else
            {
                return BadRequest("Only CSV, XLSX, and XLS files are supported.");
            }

            if (!parsed.Any())
                return BadRequest("No valid riders found.");

            foreach (var row in parsed)
            {
                _context.RecruiterRiders.Add(new RecruiterRider
                {
                    Id = Guid.NewGuid(),
                    SubmissionId = submission.Id,
                    FullName = row.FullName,
                    Email = row.Email,
                    PhoneNumber = row.PhoneNumber,
                    City = row.City,
                    Nationality = row.Nationality,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                });
            }

            submission.TotalRiders = parsed.Count;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                totalRiders = parsed.Count
            });
        }

        [HttpGet("my-submissions")]
        public async Task<IActionResult> GetMySubmissions()
        {
            var userId = _identityService.GetUserId();
            if (userId == null || userId == Guid.Empty)
                return Unauthorized();

            var recruiter = await _context.Recruiters
                .FirstOrDefaultAsync(r => r.UserId == userId);

            if (recruiter == null)
                return NotFound("Recruiter profile not found.");

            var submissions = await _context.RecruiterSubmissions
                .Where(s => s.RecruiterId == recruiter.Id)
                .OrderByDescending(s => s.UploadedAt)
                .Select(s => new
                {
                    s.Id,
                    s.FileName,
                    s.TotalRiders,
                    s.Status,
                    s.UploadedAt,
                    s.ReviewedAt,
                    s.AdminFeedback
                })
                .ToListAsync();

            return Ok(submissions);
        }

        [HttpGet("submission/{submissionId}/riders")]
        public async Task<IActionResult> GetSubmissionRiders(Guid submissionId)
        {
            var userId = _identityService.GetUserId();
            if (userId == null || userId == Guid.Empty)
                return Unauthorized();

            var recruiter = await _context.Recruiters
                .FirstOrDefaultAsync(r => r.UserId == userId);

            if (recruiter == null)
                return NotFound("Recruiter profile not found.");

            var ownsSubmission = await _context.RecruiterSubmissions
                .AnyAsync(s => s.Id == submissionId && s.RecruiterId == recruiter.Id);

            if (!ownsSubmission)
                return Forbid();

            var riders = await _context.RecruiterRiders
                .Where(r => r.SubmissionId == submissionId)
                .Select(r => new
                {
                    r.Id,
                    r.FullName,
                    r.Email,
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

        [HttpGet("performance/summary")]
        public async Task<IActionResult> GetPerformanceSummary()
        {
            var userId = _identityService.GetUserId();
            if (userId == null || userId == Guid.Empty)
                return Unauthorized();

            var recruiter = await _context.Recruiters
                .FirstOrDefaultAsync(r => r.UserId == userId);

            if (recruiter == null)
                return NotFound();

            var riders = await _context.RecruiterRiders
                .Where(r => _context.RecruiterSubmissions
                    .Any(s => s.Id == r.SubmissionId && s.RecruiterId == recruiter.Id))
                .ToListAsync();

            var total = riders.Count;
            var approved = riders.Count(r => r.Status == "Approved");

            var avgDays = await _context.RecruiterSubmissions
                .Where(s => s.RecruiterId == recruiter.Id && s.ReviewedAt != null)
                .Select(s => (double?)EF.Functions.DateDiffDay(
                    s.UploadedAt,
                    s.ReviewedAt!.Value
                ))
                .AverageAsync() ?? 0;

            return Ok(new
            {
                totalRidersSubmitted = total,
                approvalRate = total == 0 ? 0 : Math.Round((double)approved / total * 100, 2),
                avgProcessingTime = avgDays
            });
        }

        [HttpGet("performance/status-distribution")]
        public async Task<IActionResult> GetStatusDistribution()
        {
            var userId = _identityService.GetUserId();
            if (userId == null || userId == Guid.Empty)
                return Unauthorized();

            var recruiter = await _context.Recruiters
                .FirstOrDefaultAsync(r => r.UserId == userId);

            if (recruiter == null)
                return NotFound();

            var data = await _context.RecruiterRiders
                .Where(r => _context.RecruiterSubmissions
                    .Any(s => s.Id == r.SubmissionId && s.RecruiterId == recruiter.Id))
                .GroupBy(r => r.Status)
                .Select(g => new
                {
                    status = g.Key,
                    count = g.Count()
                })
                .ToListAsync();

            return Ok(data);
        }

        [HttpGet("performance/cities")]
        public async Task<IActionResult> GetTopCities()
        {
            var userId = _identityService.GetUserId();
            if (userId == null || userId == Guid.Empty)
                return Unauthorized();

            var recruiter = await _context.Recruiters
                .FirstOrDefaultAsync(r => r.UserId == userId);

            if (recruiter == null)
                return NotFound();

            var data = await _context.RecruiterRiders
                .Where(r => r.City != null && _context.RecruiterSubmissions
                    .Any(s => s.Id == r.SubmissionId && s.RecruiterId == recruiter.Id))
                .GroupBy(r => r.City)
                .Select(g => new
                {
                    city = g.Key,
                    count = g.Count()
                })
                .OrderByDescending(x => x.count)
                .Take(5)
                .ToListAsync();

            return Ok(data);
        }

        [HttpGet("performance/monthly")]
        public async Task<IActionResult> GetMonthlyPerformance()
        {
            var userId = _identityService.GetUserId();
            if (userId == null || userId == Guid.Empty)
                return Unauthorized();

            var recruiter = await _context.Recruiters
                .FirstOrDefaultAsync(r => r.UserId == userId);

            if (recruiter == null)
                return NotFound();

            var raw = await _context.RecruiterSubmissions
                .Where(s => s.RecruiterId == recruiter.Id)
                .GroupBy(s => new { s.UploadedAt.Year, s.UploadedAt.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    submissions = g.Count(),
                    riders = g.Sum(x => x.TotalRiders)
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            var data = raw.Select(x => new
            {
                month = $"{x.Year}-{x.Month:D2}",
                x.submissions,
                x.riders
            });

            return Ok(data);
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var userId = _identityService.GetUserId();
            if (userId == null || userId == Guid.Empty)
                return Unauthorized();

            var recruiter = await _context.Recruiters
                .FirstOrDefaultAsync(r => r.UserId == userId);

            if (recruiter == null)
                return NotFound("Recruiter profile not found.");

            var user = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new
                {
                    fullName = u.FirstName + " " + u.LastName,
                    email = u.Email,
                    phoneNumber = u.PhoneNumber
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound("User not found.");

            return Ok(new
            {
                recruiterId = recruiter.Id,
                recruiterCode = recruiter.RecruiterCode,
                status = recruiter.Status,
                role = "Rider Recruiter",
                joinedAt = recruiter.JoinedAt,

                name = user.fullName,
                email = user.email,
                phoneNumber = user.phoneNumber
            });
        }

        [HttpGet("submissions/{id}")]
        public async Task<IActionResult> GetSubmission(Guid id)
        {
            var userId = _identityService.GetUserId();
            if (userId == null) return Unauthorized();

            var recruiter = await _context.Recruiters
                .FirstOrDefaultAsync(r => r.UserId == userId);

            if (recruiter == null) return NotFound();

            var submission = await _context.RecruiterSubmissions
                .FirstOrDefaultAsync(s => s.Id == id && s.RecruiterId == recruiter.Id);

            if (submission == null)
                return NotFound("Submission not found");

            return Ok(submission);
        }

        [HttpPost("add-rider")]
        public async Task<IActionResult> AddSingleRider([FromBody] CreateRecruiterRiderDto dto)
        {
            var userId = _identityService.GetUserId();
            if (userId == null || userId == Guid.Empty)
                return Unauthorized();

            var recruiter = await _context.Recruiters
                .FirstOrDefaultAsync(r => r.UserId == userId);

            if (recruiter == null)
                return NotFound("Recruiter profile not found.");

            var submission = await _context.RecruiterSubmissions
                .Where(s => s.RecruiterId == recruiter.Id)
                .OrderByDescending(s => s.UploadedAt)
                .FirstOrDefaultAsync();

            if (submission == null)
            {
                submission = new RecruiterSubmission
                {
                    Id = Guid.NewGuid(),
                    RecruiterId = recruiter.Id,
                    FileName = "Manual Entry",
                    TotalRiders = 0,
                    Status = "Pending",
                    UploadedAt = DateTime.UtcNow
                };

                _context.RecruiterSubmissions.Add(submission);
                await _context.SaveChangesAsync();
            }

            var rider = new RecruiterRider
            {
                Id = Guid.NewGuid(),
                SubmissionId = submission.Id,
                FullName = dto.FullName,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                City = dto.City,
                Nationality = dto.Nationality,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.RecruiterRiders.Add(rider);
            submission.TotalRiders += 1;

            await _context.SaveChangesAsync();

            return Ok(rider);
        }

        [HttpPut("riders/{id}")]
        public async Task<IActionResult> UpdateRider(Guid id, [FromBody] UpdateRecruiterRiderDto dto)
        {
            var userId = _identityService.GetUserId();
            if (userId == null || userId == Guid.Empty)
                return Unauthorized();

            var recruiter = await _context.Recruiters
                .FirstOrDefaultAsync(r => r.UserId == userId);

            if (recruiter == null)
                return NotFound("Recruiter profile not found.");

            var rider = await _context.RecruiterRiders
                .Include(r => r.Submission)
                .FirstOrDefaultAsync(r =>
                    r.Id == id &&
                    r.Submission.RecruiterId == recruiter.Id);

            if (rider == null)
                return NotFound("Rider not found.");

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
            var userId = _identityService.GetUserId();
            if (userId == null || userId == Guid.Empty)
                return Unauthorized();

            var recruiter = await _context.Recruiters
                .FirstOrDefaultAsync(r => r.UserId == userId);

            if (recruiter == null)
                return NotFound("Recruiter profile not found.");

            var rider = await _context.RecruiterRiders
                .Include(r => r.Submission)
                .FirstOrDefaultAsync(r =>
                    r.Id == id &&
                    r.Submission.RecruiterId == recruiter.Id);

            if (rider == null)
                return NotFound("Rider not found.");

            rider.Submission.TotalRiders =
                Math.Max(0, rider.Submission.TotalRiders - 1);

            _context.RecruiterRiders.Remove(rider);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpDelete("submissions/{id}")]
        public async Task<IActionResult> DeleteSubmission(Guid id)
        {
            var userId = _identityService.GetUserId();
            if (userId == null || userId == Guid.Empty)
                return Unauthorized();

            var recruiter = await _context.Recruiters
                .FirstOrDefaultAsync(r => r.UserId == userId);

            if (recruiter == null)
                return NotFound("Recruiter profile not found.");

            var submission = await _context.RecruiterSubmissions
                .FirstOrDefaultAsync(s => s.Id == id && s.RecruiterId == recruiter.Id);

            if (submission == null)
                return NotFound("Submission not found.");

            var riders = _context.RecruiterRiders
                .Where(r => r.SubmissionId == submission.Id);

            _context.RecruiterRiders.RemoveRange(riders);
            _context.RecruiterSubmissions.Remove(submission);

            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet("my-riders")]
        public async Task<IActionResult> GetMyRiders()
        {
            var userId = _identityService.GetUserId();
            if (userId == null || userId == Guid.Empty)
                return Unauthorized();

            var recruiter = await _context.Recruiters
                .FirstOrDefaultAsync(r => r.UserId == userId);

            if (recruiter == null)
                return NotFound("Recruiter profile not found.");

            var riders = await _context.RecruiterRiders
                .Include(r => r.Submission)
                .Where(r => r.Submission.RecruiterId == recruiter.Id)
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
                    submissionFile = r.Submission.FileName
                })
                .ToListAsync();

            return Ok(riders);
        }

        [HttpGet("my-riders/count")]
        public async Task<IActionResult> GetMyRidersCount()
        {
            var userId = _identityService.GetUserId();
            if (userId == null || userId == Guid.Empty)
                return Unauthorized();

            var recruiter = await _context.Recruiters
                .FirstOrDefaultAsync(r => r.UserId == userId);

            if (recruiter == null)
                return NotFound("Recruiter profile not found.");

            var count = await _context.RecruiterRiders
                .Where(r => _context.RecruiterSubmissions
                    .Any(s => s.Id == r.SubmissionId && s.RecruiterId == recruiter.Id))
                .CountAsync();

            return Ok(new { count });
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile(
            [FromBody] UpdateRecruiterProfileDto dto)
        {
            var userId = _identityService.GetUserId();
            if (userId == null || userId == Guid.Empty)
                return Unauthorized();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound("User not found.");

            user.PhoneNumber = dto.PhoneNumber ?? user.PhoneNumber;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                user.PhoneNumber
            });
        }

        [HttpGet("notifications/settings")]
        public async Task<IActionResult> GetNotificationSettings()
        {
            var userId = _identityService.GetUserId();
            if (userId == null) return Unauthorized();

            var recruiter = await _context.Recruiters
                .FirstOrDefaultAsync(r => r.UserId == userId);

            if (recruiter == null) return NotFound();

            var settings = await _context.RecruiterNotificationSettings
                .FirstOrDefaultAsync(x => x.RecruiterId == recruiter.Id);

            if (settings == null)
            {
                settings = new RecruiterNotificationSettings
                {
                    RecruiterId = recruiter.Id
                };

                _context.RecruiterNotificationSettings.Add(settings);
                await _context.SaveChangesAsync();
            }

            return Ok(settings);
        }

        [HttpPut("notifications/settings")]
        public async Task<IActionResult> UpdateNotificationSettings(
            [FromBody] RecruiterNotificationSettings dto)
        {
            var userId = _identityService.GetUserId();
            if (userId == null) return Unauthorized();

            var recruiter = await _context.Recruiters
                .FirstOrDefaultAsync(r => r.UserId == userId);

            if (recruiter == null) return NotFound();

            var settings = await _context.RecruiterNotificationSettings
                .FirstOrDefaultAsync(x => x.RecruiterId == recruiter.Id);

            if (settings == null) return NotFound();

            settings.SubmissionStatusUpdates = dto.SubmissionStatusUpdates;
            settings.WeeklyPerformanceReport = dto.WeeklyPerformanceReport;
            settings.SystemUpdates = dto.SystemUpdates;

            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}