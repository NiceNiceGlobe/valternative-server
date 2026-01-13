using Microsoft.AspNetCore.Identity;
using System;

namespace ValternativeServer.Models.Auth
{
    public class ValternativeUser : IdentityUser<Guid>
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime? DateOfCreation { get; set; }
    }
}