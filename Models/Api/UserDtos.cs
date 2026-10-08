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
        public string ContactNumber { get; set; } = string.Empty;

        [Newtonsoft.Json.JsonProperty("isActive")]
        public bool? IsActive { get; set; }

        [Newtonsoft.Json.JsonProperty("is_active")]
        private bool? IsActiveSnake { set { if (value.HasValue) IsActive = value; } }

        [Newtonsoft.Json.JsonProperty("status")]
        private string StatusSetter
        {
            set
            {
                if (!string.IsNullOrEmpty(value))
                    IsActive = value.Equals("Active", StringComparison.OrdinalIgnoreCase);
            }
        }

        public string Role { get; set; } = "Encoder";
    }
    public class GetApplicationUserRequest
    {
        public Guid Id { get; set; }
        public string Role { get; set; } = "Encoder";
        public string SearchTerm { get; set; } = string.Empty;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public bool? IsActive { get; set; } = null;
    }

    public class UpdateApplicationUserRequest
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
    }

    public class UpdateUserStatusRequest
    {
        public bool IsActive { get; set; }
    }
}
