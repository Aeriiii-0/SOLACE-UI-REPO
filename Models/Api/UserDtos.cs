using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOLUM_UI.Models.Api
{
    public class ApplicationUserDTO
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

    }
    public class GetApplicationUserRequest
    {
        public Guid Id { get; set; }
        public string Role { get; set; } = "Encoder";
        public string SearchTerm { get; set; } = string.Empty;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public bool IsActive { get; set; } = true;
    }

    public class UpdateApplicationUserRequest
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

    }
}
