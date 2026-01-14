public class CreateRecruiterDto
{
    public string Email { get; set; } = null!;
    public string InitialPassword { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}