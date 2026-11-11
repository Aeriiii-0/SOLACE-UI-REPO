using System.Collections.Generic;



namespace SOLUM_UI.Models.Api

{

    public class LoginRequest

    {

        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

    }



    public class LoginResponse : BaseResponse

    {

        [Newtonsoft.Json.JsonProperty("id")]

        public string Id { get; set; } = string.Empty;



        [Newtonsoft.Json.JsonProperty("userId")]

        public string UserId { get; set; } = string.Empty;



        public string Token { get; set; } = string.Empty;

        public string RefreshToken { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Barangay { get; set; } = string.Empty;

        public IEnumerable<string> Roles { get; set; } = new List<string>();

        public bool MustChangePassword { get; set; }

        public bool IsPasswordChanged { get; set; }

        public System.DateTime? LastPasswordChangedDate { get; set; }

    }



    public class RegisterResponse : BaseResponse

    {

    }



    public class RegisterUserRequest

    {

        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string ContactNumber { get; set; } = string.Empty;

    }



    public class RegisterBarangaySecretaryRequest : RegisterUserRequest

    {

        public string Barangay { get; set; } = string.Empty;

    }



    public class TokenRequest

    {

        public string ExpiredToken { get; set; } = string.Empty;

        public string RefreshToken { get; set; } = string.Empty;

    }



    public class ChangePasswordRequest

    {

        public string CurrentPassword { get; set; } = string.Empty;

        public string NewPassword { get; set; } = string.Empty;

    }

}

    public class UpdateProfileRequest
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string ContactNumber { get; set; }
    }
