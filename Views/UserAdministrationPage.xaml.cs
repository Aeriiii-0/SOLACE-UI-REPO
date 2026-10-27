using System;

using System.Collections.Generic;

using System.Linq;

using System.Text.RegularExpressions;

using System.Threading.Tasks;

using System.Windows;

using System.Windows.Controls;

using System.Windows.Input;

using System.Windows.Media;

using SOLUM_UI.Models.Api;

using SOLUM_UI.Services;

using SOLUM_UI.Services.Api;

using SOLUM_UI.Views.Dialogs;

namespace SOLUM_UI

{

    public class AppUser
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; } = "ENCODER";
        public string Barangay { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public string Password { get; set; }
        public string DateAdded { get; set; } = "—";

        public string FullName => $"{FirstName} {LastName}".Trim().ToUpper();

        public string Initials
        {
            get
            {
                string f = string.IsNullOrEmpty(FirstName) ? "" : FirstName[0].ToString().ToUpper();
                string l = string.IsNullOrEmpty(LastName) ? "" : LastName[0].ToString().ToUpper();
                return f + l;
            }
        }

        public string DisplayContact => string.IsNullOrWhiteSpace(ContactNumber) ? "—" : ContactNumber;

        public bool IsAdminUser => string.Equals(Role, "ADMIN", StringComparison.OrdinalIgnoreCase) ||
                                   string.Equals(Role, "ADMINISTRATOR", StringComparison.OrdinalIgnoreCase);

        public bool IsSuperAdminUser => string.Equals(Role, "SUPERADMIN", StringComparison.OrdinalIgnoreCase);

        public bool IsBarangaySecretaryUser =>
            string.Equals(Role, "BARANGAYSECRETARY", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Role, "BARANGAY SECRETARY", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Role, "BARANGAYPRESIDENT", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Role, "BARANGAY PRESIDENT", StringComparison.OrdinalIgnoreCase);

        public bool IsCaseworkerUser => string.Equals(Role, "CASEWORKER", StringComparison.OrdinalIgnoreCase) ||
                                       (Role != null && Role.IndexOf("CASEWORKER", StringComparison.OrdinalIgnoreCase) >= 0);

        public bool IsEncoderUser => string.Equals(Role, "ENCODER", StringComparison.OrdinalIgnoreCase);

        public bool IsPrivilegedUser => IsAdminUser || IsSuperAdminUser;

        public string RoleDisplay
        {
            get
            {
                if (IsSuperAdminUser) return "SUPERADMIN";
                if (IsAdminUser) return "ADMIN";
                if (IsBarangaySecretaryUser) return "BARANGAY SECRETARY";
                if (IsCaseworkerUser) return "CASEWORKER";
                if (IsEncoderUser) return "ENCODER";
                return !string.IsNullOrWhiteSpace(Role) ? Role : "USER";
            }
        }

                public string BarangayDisplay
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Barangay)) return string.Empty;
                if (Barangay.StartsWith("Brgy", StringComparison.OrdinalIgnoreCase) ||
                    Barangay.StartsWith("Barangay", StringComparison.OrdinalIgnoreCase))
                    return Barangay;
                return $"Brgy. {Barangay}";
            }
        }

        public Visibility BarangayVisibility => !string.IsNullOrWhiteSpace(Barangay) ? Visibility.Visible : Visibility.Collapsed;

        public bool HasSpecificBarangay => IsBarangaySecretaryUser && !string.IsNullOrWhiteSpace(Barangay);

        public string ScopeDisplay
        {
            get
            {
                if (IsBarangaySecretaryUser && !string.IsNullOrWhiteSpace(Barangay))
                {
                    if (Barangay.StartsWith("Brgy", StringComparison.OrdinalIgnoreCase) ||
                        Barangay.StartsWith("Barangay", StringComparison.OrdinalIgnoreCase))
                        return Barangay;
                    return $"Brgy. {Barangay}";
                }
                if (IsSuperAdminUser || IsAdminUser || IsCaseworkerUser || IsEncoderUser)
                {
                    return "City-Wide (All Barangays)";
                }
                return !string.IsNullOrWhiteSpace(Barangay) ? $"Brgy. {Barangay}" : "City-Wide";
            }
        }

        public string ScopeIconData => HasSpecificBarangay
            ? "M12,11.5A2.5,2.5 0 0,1 9.5,9A2.5,2.5 0 0,1 12,6.5A2.5,2.5 0 0,1 14.5,9A2.5,2.5 0 0,1 12,11.5M12,2A7,7 0 0,0 5,9C5,14.25 12,22 12,22C12,22 19,14.25 19,9A7,7 0 0,0 12,2Z"
            : "M12,3L2,12H5V20H19V12H22L12,3M12,7.7C13.5,7.7 14.7,8.9 14.7,10.4C14.7,11.9 13.5,13.1 12,13.1C10.5,13.1 9.3,11.9 9.3,10.4C9.3,8.9 10.5,7.7 12,7.7Z";

        private static readonly SolidColorBrush BrgyBadgeBg = CreateFrozenBrush(0xF0, 0xF9, 0xFF);
        private static readonly SolidColorBrush BrgyBadgeBorder = CreateFrozenBrush(0xBA, 0xE6, 0xFD);
        private static readonly SolidColorBrush BrgyBadgeFg = CreateFrozenBrush(0x03, 0x69, 0xA1);
        private static readonly SolidColorBrush BrgyIconBrush = CreateFrozenBrush(0x02, 0x84, 0xC7);

        private static readonly SolidColorBrush CityBadgeBg = CreateFrozenBrush(0xF8, 0xFA, 0xFC);
        private static readonly SolidColorBrush CityBadgeBorder = CreateFrozenBrush(0xE2, 0xE8, 0xF0);
        private static readonly SolidColorBrush CityBadgeFg = CreateFrozenBrush(0x47, 0x55, 0x69);
        private static readonly SolidColorBrush CityIconBrush = CreateFrozenBrush(0x64, 0x74, 0x8B);

        public SolidColorBrush ScopeBadgeBackground => HasSpecificBarangay ? BrgyBadgeBg : CityBadgeBg;
        public SolidColorBrush ScopeBadgeBorder => HasSpecificBarangay ? BrgyBadgeBorder : CityBadgeBorder;
        public SolidColorBrush ScopeTextColor => HasSpecificBarangay ? BrgyBadgeFg : CityBadgeFg;
        public SolidColorBrush ScopeIconBrush => HasSpecificBarangay ? BrgyIconBrush : CityIconBrush;

        public bool HasContact => !string.IsNullOrWhiteSpace(ContactNumber);
        public Visibility HasContactVisibility => HasContact ? Visibility.Visible : Visibility.Collapsed;
        public SolidColorBrush ContactTextColor => HasContact
            ? CreateFrozenBrush(0x33, 0x41, 0x55)
            : CreateFrozenBrush(0x94, 0xA3, 0xB8);

        public bool CanManage
        {
            get
            {
                if (IsPrivilegedUser)
                {
                    return AuthApiService.Instance.IsSuperAdmin ||
                           string.Equals(MainWindow.CurrentUserRole, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
                }
                return true;
            }
        }

        public string ActionDisabledToolTip => CanManage
            ? null
            : "Only SuperAdmins have permission to manage Admin accounts.";

        private static readonly Geometry SuperAdminIcon;
        private static readonly Geometry AdminIcon;
        private static readonly Geometry BarangaySecretaryIcon;
        private static readonly Geometry CaseworkerIcon;
        private static readonly Geometry EncoderIcon;

        private static readonly SolidColorBrush SuperAdminBg = CreateFrozenBrush(0x70, 0x29, 0x43);
        private static readonly SolidColorBrush SuperAdminBorder = CreateFrozenBrush(0x59, 0x1D, 0x33);
        private static readonly SolidColorBrush SuperAdminFg = CreateFrozenBrush(0xFF, 0xFF, 0xFF);

        private static readonly SolidColorBrush AdminBg = CreateFrozenBrush(0xFA, 0xF0, 0xF3);
        private static readonly SolidColorBrush AdminBorder = CreateFrozenBrush(0xDF, 0x9B, 0xB1);
        private static readonly SolidColorBrush AdminFg = CreateFrozenBrush(0x70, 0x29, 0x43);

        private static readonly SolidColorBrush BarangaySecretaryBg = CreateFrozenBrush(0xEA, 0xF2, 0xF8);
        private static readonly SolidColorBrush BarangaySecretaryBorder = CreateFrozenBrush(0xAE, 0xD6, 0xF1);
        private static readonly SolidColorBrush BarangaySecretaryFg = CreateFrozenBrush(0x1B, 0x4F, 0x72);

        private static readonly SolidColorBrush CaseworkerBg = CreateFrozenBrush(0xFD, 0xF6, 0xED);
        private static readonly SolidColorBrush CaseworkerBorder = CreateFrozenBrush(0xF4, 0xD6, 0xB4);
        private static readonly SolidColorBrush CaseworkerFg = CreateFrozenBrush(0x8B, 0x4E, 0x12);

        private static readonly SolidColorBrush DefaultBg = CreateFrozenBrush(0xF5, 0xF2, 0xF4);
        private static readonly SolidColorBrush DefaultBorder = CreateFrozenBrush(0xD8, 0xCE, 0xD3);
        private static readonly SolidColorBrush DefaultFg = CreateFrozenBrush(0x5A, 0x4D, 0x53);

        private static SolidColorBrush CreateFrozenBrush(byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }

        static AppUser()
        {
            SuperAdminIcon = Geometry.Parse("M5,16L3,5L8.5,10L12,4L15.5,10L21,5L19,16H5M19,19A1,1 0 0,1 18,20H6A1,1 0 0,1 5,19V18H19V19Z");
            SuperAdminIcon.Freeze();

            AdminIcon = Geometry.Parse("M12,1L3,5V11C3,16.55 6.84,21.74 12,23C17.16,21.74 21,16.55 21,11V5L12,1M12,3.18L5,6.3V11.22C5,15.54 7.96,19.49 12,20.81C16.04,19.49 19,15.54 19,11.22V6.3L12,3.18Z");
            AdminIcon.Freeze();

            BarangaySecretaryIcon = Geometry.Parse("M12,2L1,7V9H23V7L12,2M3,10V18H6V10H3M8,10V18H11V10H8M13,10V18H16V10H13M18,10V18H21V10H18M1,20V22H23V20H1Z");
            BarangaySecretaryIcon.Freeze();

            CaseworkerIcon = Geometry.Parse("M20,6H16V4C16,2.89 15.1,2 14,2H10C8.9,2 8,2.89 8,4V6H4C2.89,6 2,6.89 2,8V19C2,20.1 2.9,21 4,21H20C21.1,21 22,20.1 22,19V8C22,6.89 21.1,6 20,6M10,4H14V6H10V4M20,19H4V8H20V19Z");
            CaseworkerIcon.Freeze();

            EncoderIcon = Geometry.Parse("M20.71,7.04C21.1,6.65 21.1,6 20.71,5.63L18.37,3.29C18,2.9 17.35,2.9 16.96,3.29L15.12,5.12L18.87,8.87M3,17.25V21H6.75L17.81,9.93L14.06,6.18L3,17.25Z");
            EncoderIcon.Freeze();
        }

        public Geometry RoleIconData => IsSuperAdminUser ? SuperAdminIcon
            : IsAdminUser ? AdminIcon
            : IsBarangaySecretaryUser ? BarangaySecretaryIcon
            : IsCaseworkerUser ? CaseworkerIcon
            : EncoderIcon;

        public SolidColorBrush RoleBadgeColor => IsSuperAdminUser ? SuperAdminBg
            : IsAdminUser ? AdminBg
            : IsBarangaySecretaryUser ? BarangaySecretaryBg
            : IsCaseworkerUser ? CaseworkerBg
            : DefaultBg;

        public SolidColorBrush RoleBadgeBorder => IsSuperAdminUser ? SuperAdminBorder
            : IsAdminUser ? AdminBorder
            : IsBarangaySecretaryUser ? BarangaySecretaryBorder
            : IsCaseworkerUser ? CaseworkerBorder
            : DefaultBorder;

        public SolidColorBrush RoleTextColor => IsSuperAdminUser ? SuperAdminFg
            : IsAdminUser ? AdminFg
            : IsBarangaySecretaryUser ? BarangaySecretaryFg
            : IsCaseworkerUser ? CaseworkerFg
            : DefaultFg;

        public string StatusText => IsActive ? "ACTIVE" : "DISABLED";
        public SolidColorBrush StatusBadgeColor => IsActive
            ? new SolidColorBrush(Color.FromRgb(0xE8, 0xF5, 0xE9))
            : new SolidColorBrush(Color.FromRgb(0xFE, 0xEB, 0xEE));
        public SolidColorBrush StatusBadgeBorder => IsActive
            ? new SolidColorBrush(Color.FromRgb(0xA5, 0xD6, 0xA7))
            : new SolidColorBrush(Color.FromRgb(0xEF, 0x9A, 0x9A));
        public SolidColorBrush StatusTextColor => IsActive
            ? new SolidColorBrush(Color.FromRgb(0x1B, 0x5E, 0x20))
            : new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28));
        public SolidColorBrush StatusDotColor => IsActive
            ? new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32))
            : new SolidColorBrush(Color.FromRgb(0xD3, 0x2F, 0x2F));
        public string ToggleActionToolTip => IsActive ? "Disable User Account" : "Re-enable User Account";
        public string ToggleButtonText => IsActive ? "Disable" : "Re-enable";

        public SolidColorBrush ToggleButtonBorder => IsActive
            ? new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0))
            : new SolidColorBrush(Color.FromRgb(0xA5, 0xD6, 0xA7));

        public SolidColorBrush ToggleButtonBg => IsActive
            ? new SolidColorBrush(Color.FromRgb(0xFA, 0xFA, 0xFA))
            : new SolidColorBrush(Color.FromRgb(0xEB, 0xF8, 0xEE));

        public SolidColorBrush ToggleButtonTextColor => IsActive
            ? new SolidColorBrush(Color.FromRgb(0x61, 0x61, 0x61))
            : new SolidColorBrush(Color.FromRgb(0x1B, 0x5E, 0x20));

        public string ToggleIconData => IsActive
            ? "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,4A8,8 0 0,1 20,12C20,13.85 19.37,15.55 18.31,16.9L7.1,5.69C8.45,4.63 10.15,4 12,4M4,12C4,10.15 4.63,8.45 5.69,7.1L16.9,18.31C15.55,19.37 13.85,20 12,20A8,8 0 0,1 4,12Z"
            : "M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2M10,17L15,12L10,7V17Z";

        public SolidColorBrush ToggleIconBrush => IsActive
            ? new SolidColorBrush(Color.FromRgb(0x75, 0x75, 0x75))
            : new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32));
    }

    public partial class UserAdministrationPage : Page

    {

        private List<AppUser> _allUsers = new List<AppUser>(); 

        private List<AppUser> _filtered = new List<AppUser>();

        private AppUser _editTarget = null;

        private string _generatedPassword = string.Empty;

        private bool _isPasswordMasked = false;

        private const int PageSize = 10;         

        private int _currentPage = 1;                

                private int _totalPages = 1;
        private int _totalCount = 0;

        private static readonly TimeSpan DataFreshnessTtl = TimeSpan.FromMinutes(3);
        private static readonly TimeSpan RefreshCooldown = TimeSpan.FromSeconds(2);
        private DateTime _lastLoadedTime = DateTime.MinValue;
        private DateTime _lastRefreshClickTime = DateTime.MinValue;
        private bool _hasLoadedData = false;
        private bool _isLoading = false;
        private bool _isUpdatingFilters = false;

        public UserAdministrationPage()
        {
            InitializeComponent();

            Loaded += async (s, e) =>
            {
                bool isStale = !_hasLoadedData || (DateTime.UtcNow - _lastLoadedTime) > DataFreshnessTtl;
                if (isStale)
                {
                    await LoadUsersAsync();
                }
            };

            DataObject.AddPastingHandler(TxtFirstName, NamePasteHandler);

            DataObject.AddPastingHandler(TxtLastName, NamePasteHandler);

            DataObject.AddPastingHandler(TxtContactNumber, ContactPasteHandler);

        }

        private void InitRolePermissions()
        {
            bool isSuperAdmin = AuthApiService.Instance.IsSuperAdmin ||
                                string.Equals(MainWindow.CurrentUserRole, "SuperAdmin", StringComparison.OrdinalIgnoreCase);

            if (BtnAddAdmin != null)
                BtnAddAdmin.Visibility = isSuperAdmin ? Visibility.Visible : Visibility.Collapsed;

            _isUpdatingFilters = true;
            try
            {
                if (CmbFilterRoleAdmin != null)
                    CmbFilterRoleAdmin.Visibility = isSuperAdmin ? Visibility.Visible : Visibility.Collapsed;

                if (CmbFilterRoleSuperAdmin != null)
                    CmbFilterRoleSuperAdmin.Visibility = isSuperAdmin ? Visibility.Visible : Visibility.Collapsed;

                if (!isSuperAdmin && CmbRoleFilter != null)
                {
                    var selTag = (CmbRoleFilter.SelectedItem as ComboBoxItem)?.Tag?.ToString();
                    if (selTag == "Admin" || selTag == "SuperAdmin")
                    {
                        CmbRoleFilter.SelectedIndex = 0;
                    }
                }
            }
            finally
            {
                _isUpdatingFilters = false;
            }
        }

        private async Task LoadUsersAsync(bool forceRefresh = false)
        {
            if (_isLoading) return;

            if (!forceRefresh && _hasLoadedData && (DateTime.UtcNow - _lastLoadedTime) <= DataFreshnessTtl)
            {
                ApplyFilters();
                return;
            }

            _isLoading = true;
            if (UsersSkeletonLoader != null) UsersSkeletonLoader.Visibility = Visibility.Visible;
            if (EmptyStatePanel != null) EmptyStatePanel.Visibility = Visibility.Collapsed;
            if (BtnRefreshUsers != null) BtnRefreshUsers.IsEnabled = false;

            InitRolePermissions();

            bool isSuperAdmin = AuthApiService.Instance.IsSuperAdmin ||
                                string.Equals(MainWindow.CurrentUserRole, "SuperAdmin", StringComparison.OrdinalIgnoreCase);

            try
            {
                var rolesToFetch = new List<string>();
                if (isSuperAdmin)
                {
                    rolesToFetch.Add("Admin");
                    rolesToFetch.Add("Encoder");
                    rolesToFetch.Add("BarangaySecretary");
                    rolesToFetch.Add("SuperAdmin");
                }
                else
                {
                    rolesToFetch.Add("Encoder");
                    rolesToFetch.Add("BarangaySecretary");
                }

                var tasks = new List<Task<(string role, bool isActive, BaseResponse<PagedResult<ApplicationUserDTO>> res)>>();

                foreach (var role in rolesToFetch)
                {
                    string r = role;
                    // Active users
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            var res = await UserApiService.Instance.GetApplicationUsersAsync(new GetApplicationUserRequest
                            {
                                Role = r,
                                SearchTerm = string.Empty,
                                Page = 1,
                                PageSize = 100,
                                IsActive = true
                            });
                            return (r, true, res);
                        }
                        catch
                        {
                            return (r, true, (BaseResponse<PagedResult<ApplicationUserDTO>>)null);
                        }
                    }));

                    // Disabled users
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            var res = await UserApiService.Instance.GetApplicationUsersAsync(new GetApplicationUserRequest
                            {
                                Role = r,
                                SearchTerm = string.Empty,
                                Page = 1,
                                PageSize = 100,
                                IsActive = false
                            });
                            return (r, false, res);
                        }
                        catch
                        {
                            return (r, false, (BaseResponse<PagedResult<ApplicationUserDTO>>)null);
                        }
                    }));
                }

                await Task.WhenAll(tasks);

                var userMap = new Dictionary<Guid, AppUser>();

                foreach (var t in tasks)
                {
                    var (queriedRole, isActive, res) = t.Result;
                    if (res?.Succeeded == true && res.Data?.Items != null)
                    {
                        foreach (var dto in res.Data.Items)
                        {
                            if (!userMap.TryGetValue(dto.Id, out var existing))
                            {
                                string assignedRole = !string.IsNullOrWhiteSpace(dto.PrimaryRole)
                                    ? dto.PrimaryRole
                                    : queriedRole;

                                userMap[dto.Id] = new AppUser
                                {
                                    Id = dto.Id,
                                    FirstName = dto.FirstName?.ToUpper(),
                                    LastName = dto.LastName?.ToUpper(),
                                    Email = dto.Email,
                                    ContactNumber = dto.ContactNumber ?? string.Empty,
                                    IsActive = dto.IsActive ?? isActive,
                                    Role = assignedRole.ToUpperInvariant(),
                                    Barangay = dto.Barangay ?? string.Empty
                                };
                            }
                            else
                            {
                                if (!string.IsNullOrWhiteSpace(dto.Barangay) && string.IsNullOrWhiteSpace(existing.Barangay))
                                {
                                    existing.Barangay = dto.Barangay;
                                }
                            }
                        }
                    }
                }

                _allUsers = userMap.Values
                    .OrderBy(u => u.LastName)
                    .ThenBy(u => u.FirstName)
                    .ToList();

                _hasLoadedData = true;
                _lastLoadedTime = DateTime.UtcNow;

                ApplyFilters();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UserAdmin] LoadUsersAsync error: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
                if (UsersSkeletonLoader != null) UsersSkeletonLoader.Visibility = Visibility.Collapsed;
                if (BtnRefreshUsers != null) BtnRefreshUsers.IsEnabled = true;
            }
        }

        private void ApplyFilters()
        {
            string roleTag = (CmbRoleFilter?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "ALL";
            string statusTag = (CmbUserStatus?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "ALL";
            string search = SearchBox?.Text?.Trim() ?? string.Empty;

            IEnumerable<AppUser> query = _allUsers;

            // 1. Role Filter
            if (!string.Equals(roleTag, "ALL", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(roleTag, "Admin", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(u => u.IsAdminUser);
                else if (string.Equals(roleTag, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(u => u.IsSuperAdminUser);
                else if (string.Equals(roleTag, "Encoder", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(u => u.IsEncoderUser || u.IsCaseworkerUser);
                else if (string.Equals(roleTag, "BarangaySecretary", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(u => u.IsBarangaySecretaryUser);
                else
                    query = query.Where(u => string.Equals(u.Role, roleTag, StringComparison.OrdinalIgnoreCase));
            }

            // 2. Status Filter
            if (string.Equals(statusTag, "ACTIVE", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(u => u.IsActive);
            }
            else if (string.Equals(statusTag, "DISABLED", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(u => !u.IsActive);
            }

            // 3. Search Filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u =>
                    (!string.IsNullOrEmpty(u.FirstName) && u.FirstName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrEmpty(u.LastName) && u.LastName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrEmpty(u.FullName) && u.FullName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrEmpty(u.Email) && u.Email.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrEmpty(u.ContactNumber) && u.ContactNumber.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrEmpty(u.Barangay) && u.Barangay.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrEmpty(u.RoleDisplay) && u.RoleDisplay.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                );
            }

            _filtered = query
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ToList();

            _totalCount = _filtered.Count;
            _totalPages = Math.Max(1, (int)Math.Ceiling(_totalCount / (double)PageSize));
            if (_currentPage > _totalPages) _currentPage = _totalPages;
            if (_currentPage < 1) _currentPage = 1;

            RefreshList();
            UpdatePagingControls();
        }

        private void CmbRoleFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || _isUpdatingFilters) return;
            _currentPage = 1;
            ApplyFilters();
        }

        private void CmbUserStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || _isUpdatingFilters) return;
            _currentPage = 1;
            ApplyFilters();
        }

        private async void BtnRefreshUsers_Click(object sender, RoutedEventArgs e)
        {
            if (_isLoading || (DateTime.UtcNow - _lastRefreshClickTime) < RefreshCooldown) return;
            _lastRefreshClickTime = DateTime.UtcNow;

            _lastLoadedTime = DateTime.MinValue;
            _currentPage = 1;
            await LoadUsersAsync(forceRefresh: true);
        }

        private void UpdatePagingControls()
        {
            if (TxtPageInfo != null)
                TxtPageInfo.Text = $"{_currentPage} of {_totalPages}";

            if (TxtUsersSummary != null)
                TxtUsersSummary.Text = $"{_totalCount:N0} total users registered";

            if (TxtUsersPageSummary != null)
            {
                int start = _totalCount == 0 ? 0 : ((_currentPage - 1) * PageSize) + 1;
                int end = Math.Min(_totalCount, _currentPage * PageSize);
                TxtUsersPageSummary.Text = $"Showing {start}–{end} of {_totalCount}";
            }

            if (BtnPrevPage != null)
                BtnPrevPage.IsEnabled = _currentPage > 1;

            if (BtnNextPage != null)
                BtnNextPage.IsEnabled = _currentPage < _totalPages;
        }

        private void BtnPrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage <= 1) return;
            _currentPage--;
            RefreshList();
            UpdatePagingControls();
        }

        private void BtnNextPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage >= _totalPages) return;
            _currentPage++;
            RefreshList();
            UpdatePagingControls();
        }

        private void RefreshList()
        {
            var paged = _filtered
                .Skip((_currentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            UserList.ItemsSource = null;
            UserList.ItemsSource = paged;

            TxtUserCount.Text = " " + _filtered.Count;

            if (EmptyStatePanel != null)
            {
                EmptyStatePanel.Visibility = (_filtered == null || _filtered.Count == 0) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void ClearFilters_Click(object sender, RoutedEventArgs e)
        {
            _isUpdatingFilters = true;
            try
            {
                if (SearchBox != null) SearchBox.Text = string.Empty;
                if (CmbRoleFilter != null) CmbRoleFilter.SelectedIndex = 0;
                if (CmbUserStatus != null) CmbUserStatus.SelectedIndex = 0;
            }
            finally
            {
                _isUpdatingFilters = false;
            }
            _currentPage = 1;
            ApplyFilters();
        }

        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded || _isUpdatingFilters) return;
            _currentPage = 1;
            ApplyFilters();
        }

        private void OpenAddPanel_Click(object sender, RoutedEventArgs e)
        {
            _editTarget = null;
            ResetForm();
            SetPanelMode(false);
            SelectRoleByTag("Caseworker");
            ColPanel.Width = new GridLength(324);
        }

        private void EditUser_Click(object sender, RoutedEventArgs e)

        {

            string tagStr = (sender as Button)?.Tag?.ToString() ?? string.Empty;

            if (!Guid.TryParse(tagStr, out var id)) return;

            AppUser user = _allUsers.Find(u => u.Id == id);

            if (user == null) return;

            if (!user.CanManage)

            {

                ActionConfirmDialog.Warning(

                    "Permission Denied",

                    "Only SuperAdmins have permission to manage Admin accounts.",

                    Window.GetWindow(this));

                return;

            }

            _editTarget = user;

            TxtFirstName.Text = user.FirstName?.ToUpper() ?? string.Empty;

            TxtLastName.Text = user.LastName?.ToUpper() ?? string.Empty;

            TxtEmail.Text = user.Email;

            TxtEmail.IsReadOnly = true;

            TxtContactNumber.Text = user.ContactNumber ?? string.Empty;

            if (CmbEditStatus != null)

            {

                CmbEditStatus.SelectedIndex = user.IsActive ? 0 : 1;

            }

            SetPanelMode(true);

            ClearErrors(null, null);

            string userRoleTag = user.IsBarangaySecretaryUser ? "BarangaySecretary" :
                                 user.IsSuperAdminUser ? "SuperAdmin" :
                                 user.IsAdminUser ? "Admin" : "Caseworker";
            SelectRoleByTag(userRoleTag);

            if (user.IsBarangaySecretaryUser && !string.IsNullOrWhiteSpace(user.Barangay) && CmbUserBarangay != null)
            {
                foreach (var item in CmbUserBarangay.Items)
                {
                    if (item is ComboBoxItem cbItem && string.Equals(cbItem.Content?.ToString(), user.Barangay, StringComparison.OrdinalIgnoreCase))
                    {
                        CmbUserBarangay.SelectedItem = cbItem;
                        break;
                    }
                }
            }

            ColPanel.Width = new GridLength(324);

        }

        private void SelectRoleByTag(string tag)
        {
            if (CmbRole == null) return;
            for (int i = 0; i < CmbRole.Items.Count; i++)
            {
                if (CmbRole.Items[i] is ComboBoxItem item && string.Equals(item.Tag?.ToString(), tag, StringComparison.OrdinalIgnoreCase))
                {
                    CmbRole.SelectedIndex = i;
                    break;
                }
            }
        }

        private void OpenAddAdminPanel_Click(object sender, RoutedEventArgs e)
        {
            _editTarget = null;
            ResetForm();
            SetPanelMode(false);
            SelectRoleByTag("Admin");
            ColPanel.Width = new GridLength(324);
        }



        private void MoreActions_Click(object sender, RoutedEventArgs e)

        {

            if (sender is Button btn && btn.ContextMenu != null)

            {

                btn.ContextMenu.PlacementTarget = btn;

                btn.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;

                btn.ContextMenu.DataContext = btn.DataContext;

                btn.ContextMenu.IsOpen = true;

            }

        }

        private async void ResetPassword_Click(object sender, RoutedEventArgs e)

        {

            AppUser user = (sender as FrameworkElement)?.DataContext as AppUser;

            if (user == null)

            {

                string tagStr = (sender as FrameworkElement)?.Tag?.ToString() ?? string.Empty;

                if (Guid.TryParse(tagStr, out var id))

                {

                    user = _allUsers.Find(u => u.Id == id);

                }

            }

            if (user == null) return;

            if (!user.CanManage)

            {

                ActionConfirmDialog.Warning(

                    "Permission Denied",

                    "Only SuperAdmins have permission to manage Admin accounts.",

                    Window.GetWindow(this));

                return;

            }

            await ExecutePasswordResetAsync(user);

        }

        private async void EditPanelResetPassword_Click(object sender, RoutedEventArgs e)

        {

            if (_editTarget == null) return;

            await ExecutePasswordResetAsync(_editTarget);

        }

        private async Task ExecutePasswordResetAsync(AppUser user)

        {

            if (user == null) return;

            if (!user.CanManage)

            {

                ActionConfirmDialog.Warning(

                    "Permission Denied",

                    "Only SuperAdmins have permission to manage Admin accounts.",

                    Window.GetWindow(this));

                return;

            }

            bool isConfirmed = ActionConfirmDialog.Show(

                title: "Reset Password",

                message: $"Are you sure you want to reset the password for {user.FullName}? A new temporary password will be generated and their existing password will be overwritten.",

                confirmText: "Reset Password",

                cancelText: "Cancel",

                theme: ConfirmThemeType.Warning,

                owner: Window.GetWindow(this),

                customIconData: "M7,14A2,2 0 0,1 5,12A2,2 0 0,1 7,10A2,2 0 0,1 9,12A2,2 0 0,1 7,14M12.65,10C11.83,7.67 9.61,6 7,6A6,6 0 0,0 1,12A6,6 0 0,0 7,18C9.61,18 11.83,16.33 12.65,14H17V18H21V14H23V10H12.65Z");

            if (!isConfirmed) return;

            if (UsersLoadingOverlay != null) UsersLoadingOverlay.IsLoading = true;

            try

            {

                string tempPassword = PasswordGenerator.Generate(10);

                var response = await UserApiService.Instance.ResetUserPasswordAsync(user.Id, tempPassword);

                if (!response.Succeeded)

                {

                    string err = response.Errors != null && response.Errors.Count > 0

                        ? string.Join("\n", response.Errors)

                        : "Failed to reset password on server.";

                    ToastNotification.Show("Reset Failed", err, ToastType.Error);

                    return;

                }

                ToastNotification.Show("Password Reset", "Temporary password generated.", ToastType.Success);

                AuditLogService.Instance.LogUserAdmin("password reset",

                    $"{user.FullName} ({user.Email})",

                    MainWindow.CurrentUserName, MainWindow.CurrentUserRole,

                    user.Id.ToString());

                AccountCreatedDialog.Show(

                    user.FullName,

                    user.Email,

                    tempPassword,

                    user.Role,

                    Window.GetWindow(this),

                    heading: "Password Reset Successfully",

                    subheading: "A new temporary password was generated. Provide it to the employee so they can log in.");

            }

            finally

            {

                if (UsersLoadingOverlay != null) UsersLoadingOverlay.IsLoading = false;

            }

        }

        private async void ToggleStatus_Click(object sender, RoutedEventArgs e)

        {

            AppUser user = (sender as FrameworkElement)?.DataContext as AppUser;

            if (user == null)

            {

                string tagStr = (sender as FrameworkElement)?.Tag?.ToString() ?? string.Empty;

                if (Guid.TryParse(tagStr, out var id))

                {

                    user = _allUsers.Find(u => u.Id == id);

                }

            }

            if (user == null) return;

            if (!user.CanManage)

            {

                ActionConfirmDialog.Warning(

                    "Permission Denied",

                    "Only SuperAdmins have permission to manage Admin accounts.",

                    Window.GetWindow(this));

                return;

            }

            bool newStatus = !user.IsActive;

            string actionVerb = newStatus ? "re-enable" : "disable";

            string actionTitle = newStatus ? "Re-enable User" : "Disable User";

            string confirmText = newStatus ? "Re-enable" : "Disable";

            var theme = newStatus ? ConfirmThemeType.Success : ConfirmThemeType.Warning;

            bool isConfirmed = ActionConfirmDialog.Show(

                title: actionTitle,

                message: $"Are you sure you want to {actionVerb} this user account ({user.FullName})?",

                confirmText: confirmText,

                cancelText: "Cancel",

                theme: theme,

                owner: Window.GetWindow(this));

            if (!isConfirmed) return;

            if (UsersLoadingOverlay != null) UsersLoadingOverlay.IsLoading = true;

            try

            {

                var response = await UserApiService.Instance.UpdateUserStatusAsync(user.Id, newStatus);

                if (!response.Succeeded)

                {

                    string err = response.Errors != null && response.Errors.Count > 0

                        ? string.Join("\n", response.Errors)

                        : $"Unable to {actionVerb} user.";

                    ToastNotification.Show("Status Update Failed", err, ToastType.Warning);

                    return;

                }

                user.IsActive = newStatus;

                RefreshList();

                ToastNotification.Show("Status Updated", $"{user.FullName} was {(newStatus ? "re-enabled" : "disabled")}.", ToastType.Success);

                AuditLogService.Instance.LogUserAdmin($"status changed to {(newStatus ? "Active" : "Disabled")}",

                    $"{user.FullName} ({user.Email})",

                    MainWindow.CurrentUserName, MainWindow.CurrentUserRole,

                    user.Id.ToString());

                _lastLoadedTime = DateTime.MinValue;
                await LoadUsersAsync(forceRefresh: true);

            }

            finally

            {

                if (UsersLoadingOverlay != null) UsersLoadingOverlay.IsLoading = false;

            }

        }

        // CHANGED — Id-based lookup, calls real delete endpoint

        private async void DeleteUser_Click(object sender, RoutedEventArgs e)

        {

            AppUser user = (sender as FrameworkElement)?.DataContext as AppUser;

            if (user == null)

            {

                string tagStr = (sender as FrameworkElement)?.Tag?.ToString() ?? string.Empty;

                if (Guid.TryParse(tagStr, out var id))

                {

                    user = _allUsers.Find(u => u.Id == id);

                }

            }

            if (user == null) return;

            if (!user.CanManage)

            {

                ActionConfirmDialog.Warning(

                    "Permission Denied",

                    "Only SuperAdmins have permission to manage Admin accounts.",

                    Window.GetWindow(this));

                return;

            }

            bool isConfirmed = ActionConfirmDialog.Show(

                title: "Delete User",

                message: $"Are you sure you want to permanently delete {user.FullName}? This action cannot be undone.",

                confirmText: "Delete User",

                cancelText: "Cancel",

                theme: ConfirmThemeType.Danger,

                owner: Window.GetWindow(this));

            if (!isConfirmed) return;

            var response = await UserApiService.Instance.DeleteApplicationUserAsync(user.Id);

            if (!response.Succeeded)

            {

                string err = response.Errors != null && response.Errors.Count > 0

                    ? string.Join("\n", response.Errors)

                    : "Unable to delete user.";

                ToastNotification.Show("Delete Failed", err, ToastType.Warning);

                return;

            }

            if (_editTarget == user) ClosePanel_Click(null, null);

            ToastNotification.Show("User Deleted", user.FullName + " was removed.", ToastType.Warning);

            AuditLogService.Instance.LogUserAdmin("deleted", user.FullName + " (" + user.Email + ")",

                MainWindow.CurrentUserName, MainWindow.CurrentUserRole,

                user.Id.ToString());

            await LoadUsersAsync();

        }

        private void ClosePanel_Click(object sender, RoutedEventArgs e)

        {

            ColPanel.Width = new GridLength(0);

            _editTarget = null;

            ResetForm();

        }

        // CHANGED — now async, branches into Register (create) or Update against real endpoints

        private async void Submit_Click(object sender, RoutedEventArgs e)

        {

            ClearErrors(null, null);

            string firstName = TxtFirstName.Text.Trim().ToUpper();

            string lastName = TxtLastName.Text.Trim().ToUpper();

            string email = TxtEmail.Text.Trim();

            string contactNumber = TxtContactNumber.Text.Trim();

            string password = _generatedPassword;

            bool ok = true;

            if (string.IsNullOrWhiteSpace(firstName))

            {

                ErrFirstName.Text = "Required.";

                ErrFirstName.Visibility = Visibility.Visible;

                ok = false;

            }

            else if (!Regex.IsMatch(firstName, @"^[a-zA-ZñÑ\.\s]+$"))

            {

                ErrFirstName.Text = "Only letters, spaces, and '.' are allowed.";

                ErrFirstName.Visibility = Visibility.Visible;

                ok = false;

            }

            if (string.IsNullOrWhiteSpace(lastName))

            {

                ErrLastName.Text = "Required.";

                ErrLastName.Visibility = Visibility.Visible;

                ok = false;

            }

            else if (!Regex.IsMatch(lastName, @"^[a-zA-ZñÑ\.\s]+$"))

            {

                ErrLastName.Text = "Only letters, spaces, and '.' are allowed.";

                ErrLastName.Visibility = Visibility.Visible;

                ok = false;

            }

            if (string.IsNullOrWhiteSpace(contactNumber))

            {

                ErrContactNumber.Text = "Required.";

                ErrContactNumber.Visibility = Visibility.Visible;

                ok = false;

            }

            else

            {

                bool is09 = Regex.IsMatch(contactNumber, @"^09\d{9}$");

                bool is63 = Regex.IsMatch(contactNumber, @"^\+63\d{9,10}$");

                if (!is09 && !is63)

                {

                    ErrContactNumber.Text = "Must be 11 digits (starts with 09) or start with +63.";

                    ErrContactNumber.Visibility = Visibility.Visible;

                    ok = false;

                }

            }

            // Email & Role validation only matter for CREATE — locked/read-only in edit mode
            if (_editTarget == null)
            {
                string selRole = (CmbRole?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Caseworker";
                if (selRole.Equals("BarangaySecretary", StringComparison.OrdinalIgnoreCase) || selRole.Equals("BarangayPresident", StringComparison.OrdinalIgnoreCase))
                {
                    string brgy = (CmbUserBarangay?.SelectedItem as ComboBoxItem)?.Content?.ToString();
                    if (string.IsNullOrWhiteSpace(brgy) || brgy.StartsWith("Select", StringComparison.OrdinalIgnoreCase))
                    {
                        ErrUserBarangay.Text = "Barangay is required for Barangay Secretary.";
                        ErrUserBarangay.Visibility = Visibility.Visible;
                        ok = false;
                    }
                }

                if (string.IsNullOrWhiteSpace(email))

                { ErrEmail.Text = "Required."; ErrEmail.Visibility = Visibility.Visible; ok = false; }

                else if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))

                { ErrEmail.Text = "Enter a valid email."; ErrEmail.Visibility = Visibility.Visible; ok = false; }

                else if (_allUsers.Exists(u => u.Email.ToLower() == email.ToLower()))

                { ErrEmail.Text = "Email already registered."; ErrEmail.Visibility = Visibility.Visible; ok = false; }

                if (string.IsNullOrWhiteSpace(password))

                {

                    GenerateNewPassword();

                    password = _generatedPassword;

                }

            }

            if (!ok)

            {

                TxtFormError.Text = "Please fix the errors above.";

                FormErrorBanner.Visibility = Visibility.Visible;

                ToastNotification.Show("Incomplete Form", "Fill in all required fields.", ToastType.Warning);

                return;

            }

            if (_editTarget != null)

            {

                bool selectedStatus = (CmbEditStatus?.SelectedItem as ComboBoxItem)?.Tag?.ToString() == "true";

                bool statusChanging = selectedStatus != _editTarget.IsActive;

                string confirmMsg = statusChanging

                    ? $"Are you sure you want to save changes to this user account ({_editTarget.FullName}) and set status to {(selectedStatus ? "Active" : "Disabled")}?"

                    : $"Are you sure you want to save changes to this user account ({_editTarget.FullName})?";

                bool isConfirmed = ActionConfirmDialog.Show(

                    title: "Update User Account",

                    message: confirmMsg,

                    confirmText: "Save Changes",

                    cancelText: "Cancel",

                    theme: ConfirmThemeType.Primary,

                    owner: Window.GetWindow(this),

                    customIconData: "M20.71,7.04C21.1,6.65 21.1,6 20.71,5.63L18.37,3.29C18,2.9 17.35,2.9 16.96,3.29L15.12,5.12L18.87,8.87M3,17.25V21H6.75L17.81,9.93L14.07,6.19L3,17.25Z");

                if (!isConfirmed) return;

                if (UsersLoadingOverlay != null) UsersLoadingOverlay.IsLoading = true;

                try

                {

                    var updateReq = new UpdateApplicationUserRequest

                    {

                        Id = _editTarget.Id,

                        FirstName = firstName,

                        LastName = lastName,

                        ContactNumber = contactNumber

                    };

                    var response = await UserApiService.Instance.UpdateApplicationUserAsync(updateReq);

                if (!response.Succeeded)

                {

                    string err = response.Errors != null && response.Errors.Count > 0

                        ? string.Join("\n", response.Errors)

                        : "Unable to update user.";

                    TxtFormError.Text = err;

                    FormErrorBanner.Visibility = Visibility.Visible;

                    return;

                }

                // Check if account status was toggled in the edit dropdown

                if (statusChanging)

                {

                    var statusResponse = await UserApiService.Instance.UpdateUserStatusAsync(_editTarget.Id, selectedStatus);

                    if (!statusResponse.Succeeded)

                    {

                        string err = statusResponse.Errors != null && statusResponse.Errors.Count > 0

                            ? string.Join("\n", statusResponse.Errors)

                            : "Account details updated, but status update failed.";

                        ToastNotification.Show("Status Update Issue", err, ToastType.Warning);

                    }

                    else

                    {

                        _editTarget.IsActive = selectedStatus;

                        RefreshList();

                        AuditLogService.Instance.LogUserAdmin($"status changed to {(selectedStatus ? "Active" : "Disabled")}",

                            $"{_editTarget.FullName} ({_editTarget.Email})",

                            MainWindow.CurrentUserName, MainWindow.CurrentUserRole,

                            _editTarget.Id.ToString());

                    }

                }

                ToastNotification.Show("User Updated", firstName + " " + lastName + " was updated.", ToastType.Info);

                AuditLogService.Instance.LogUserAdmin("updated",

                    firstName + " " + lastName + " (" + _editTarget.Email + ")",

                    MainWindow.CurrentUserName, MainWindow.CurrentUserRole,

                    _editTarget.Id.ToString());

                ClosePanel_Click(null, null);

                _lastLoadedTime = DateTime.MinValue;
                await LoadUsersAsync(forceRefresh: true);

            }

            finally

            {

                if (UsersLoadingOverlay != null) UsersLoadingOverlay.IsLoading = false;

            }

        }

            else

            {

                string selectedRole = (CmbRole?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Caseworker";

                BaseResponse<RegisterResponse> response;

                if (selectedRole.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase))

                {

                    response = await AuthApiService.Instance.RegisterSuperAdminAsync(email, password, firstName, lastName, contactNumber);

                }

                else if (selectedRole.Equals("Admin", StringComparison.OrdinalIgnoreCase))

                {

                    response = await AuthApiService.Instance.RegisterAdminAsync(email, password, firstName, lastName, contactNumber);

                }

                else if (selectedRole.Equals("BarangaySecretary", StringComparison.OrdinalIgnoreCase) ||
                         selectedRole.Equals("BarangayPresident", StringComparison.OrdinalIgnoreCase))

                {

                    string brgy = (CmbUserBarangay?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty;

                    response = await AuthApiService.Instance.RegisterBarangaySecretaryAsync(email, password, firstName, lastName, contactNumber, brgy);

                }

                else

                {

                    response = await AuthApiService.Instance.RegisterAsync(email, password, firstName, lastName, contactNumber);

                }

                if (!response.Succeeded)

                {

                    string err = response.Errors != null && response.Errors.Count > 0

                        ? string.Join("\n", response.Errors)

                        : "Unable to create user.";

                    TxtFormError.Text = err;

                    FormErrorBanner.Visibility = Visibility.Visible;

                    return;

                }

                AuditLogService.Instance.LogUserAdmin("created",

                    firstName + " " + lastName + " (" + email + ")",

                    MainWindow.CurrentUserName, MainWindow.CurrentUserRole);

                string createdName = firstName + " " + lastName;

                string createdEmail = email;

                string createdPassword = password;

                ClosePanel_Click(null, null);

                _lastLoadedTime = DateTime.MinValue;
                await LoadUsersAsync(forceRefresh: true);

                AccountCreatedDialog.Show(createdName, createdEmail, createdPassword, selectedRole.ToUpper(), Window.GetWindow(this));

            }

        }

        private void GenerateNewPassword()

        {

            _generatedPassword = PasswordGenerator.Generate(10);

            if (TxtGeneratedPassword != null)

                TxtGeneratedPassword.Text = _generatedPassword;

            if (PwdGeneratedPasswordMasked != null)

                PwdGeneratedPasswordMasked.Password = _generatedPassword;

        }

        private void SetPanelMode(bool editMode)

        {

            TxtPanelTitle.Text = editMode ? "Edit User" : "Add New User";

            TxtPanelSub.Text = editMode ? "Update account details." : "Create a new user account.";

            TxtSubmitBtn.Text = editMode ? "Save Changes" : "Create Account";

            if (PnlAccountStatus != null)

                PnlAccountStatus.Visibility = editMode ? Visibility.Visible : Visibility.Collapsed;

            if (PnlEditPasswordReset != null)

                PnlEditPasswordReset.Visibility = editMode ? Visibility.Visible : Visibility.Collapsed;

            if (PnlPassword != null)

                PnlPassword.Visibility = editMode ? Visibility.Collapsed : Visibility.Visible;

            bool isSuperAdmin = AuthApiService.Instance.IsSuperAdmin ||

                                string.Equals(MainWindow.CurrentUserRole, "SuperAdmin", StringComparison.OrdinalIgnoreCase);

            if (CmbRoleItemAdmin != null)

                CmbRoleItemAdmin.Visibility = isSuperAdmin ? Visibility.Visible : Visibility.Collapsed;

            if (CmbRoleItemSuperAdmin != null)

                CmbRoleItemSuperAdmin.Visibility = isSuperAdmin ? Visibility.Visible : Visibility.Collapsed;

            if (CmbRoleItemAdmin != null)
                CmbRoleItemAdmin.Visibility = isSuperAdmin ? Visibility.Visible : Visibility.Collapsed;

            if (CmbRoleItemSuperAdmin != null)
                CmbRoleItemSuperAdmin.Visibility = isSuperAdmin ? Visibility.Visible : Visibility.Collapsed;

            if (CmbRole != null)
            {
                if (editMode)
                {
                    CmbRole.IsEnabled = false;
                }
                else
                {
                    bool isAdminOrHigher = isSuperAdmin ||
                                           AuthApiService.Instance.IsAdmin ||
                                           string.Equals(MainWindow.CurrentUserRole, "Administrator", StringComparison.OrdinalIgnoreCase) ||
                                           string.Equals(MainWindow.CurrentUserRole, "Admin", StringComparison.OrdinalIgnoreCase);

                    CmbRole.IsEnabled = isAdminOrHigher;
                    if (!isAdminOrHigher)
                        CmbRole.SelectedIndex = 0;
                }
            }

        }

        private void ResetForm()

        {

            TxtFirstName.Text = string.Empty;

            TxtLastName.Text = string.Empty;

            TxtEmail.Text = string.Empty;

            TxtEmail.IsReadOnly = false; // re-enable for the next "Add" flow

            TxtContactNumber.Text = string.Empty;

            GenerateNewPassword();

            if (CmbEditStatus != null) CmbEditStatus.SelectedIndex = 0;

            FormErrorBanner.Visibility = Visibility.Collapsed;

            SetPanelMode(false);

        }

        private void ToUpperBox(TextBox tb)

        {

            if (tb == null) return;

            if (tb.Text != tb.Text.ToUpper())

            {

                int caret = tb.SelectionStart;

                tb.Text = tb.Text.ToUpper();

                tb.SelectionStart = Math.Min(caret + 1, tb.Text.Length);

            }

        }

        private void TxtFirstName_TextChanged(object sender, TextChangedEventArgs e)

        {

            ToUpperBox(TxtFirstName);

            ClearErrors(null, null);

        }

        private void TxtLastName_TextChanged(object sender, TextChangedEventArgs e)

        {

            ToUpperBox(TxtLastName);

            ClearErrors(null, null);

        }

        private void ClearErrors(object sender, object e)

        {

            ErrFirstName.Visibility = Visibility.Collapsed;

            ErrLastName.Visibility = Visibility.Collapsed;

            ErrEmail.Visibility = Visibility.Collapsed;

            ErrContactNumber.Visibility = Visibility.Collapsed;

            FormErrorBanner.Visibility = Visibility.Collapsed;

        }

        private void CmbRole_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbRole == null || PnlUserBarangay == null) return;
            string tag = (CmbRole.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? string.Empty;
            bool isSec = tag.Equals("BarangaySecretary", StringComparison.OrdinalIgnoreCase) ||
                         tag.Equals("BarangayPresident", StringComparison.OrdinalIgnoreCase);
            PnlUserBarangay.Visibility = isSec ? Visibility.Visible : Visibility.Collapsed;
            if (ErrUserBarangay != null) ErrUserBarangay.Visibility = Visibility.Collapsed;
        }

        private void TogglePasswordMask_Click(object sender, RoutedEventArgs e)

        {

            _isPasswordMasked = !_isPasswordMasked;

            if (TxtGeneratedPassword != null)

                TxtGeneratedPassword.Visibility = _isPasswordMasked ? Visibility.Collapsed : Visibility.Visible;

            if (PwdGeneratedPasswordMasked != null)

                PwdGeneratedPasswordMasked.Visibility = _isPasswordMasked ? Visibility.Visible : Visibility.Collapsed;

            if (IconPasswordMask != null)

            {

                IconPasswordMask.Data = Geometry.Parse(_isPasswordMasked

                    ? "M11.83,9L15,12.16C15,12.11 15,12.05 15,12A3,3 0 0,0 12,9C11.94,9 11.89,9 11.83,9M7.53,9.8L9.08,11.35C9.03,11.56 9,11.77 9,12A3,3 0 0,0 12,15C12.22,15 12.44,14.97 12.65,14.92L14.2,16.47C13.53,16.8 12.79,17 12,17A5,5 0 0,1 7,12C7,11.21 7.2,10.47 7.53,9.8M2,4.27L4.28,6.55L4.73,7C3.08,8.3 1.78,10 1,12C2.73,16.39 7,19.5 12,19.5C13.55,19.5 15.03,19.2 16.38,18.66L16.81,19.09L19.73,22L21,20.73L3.27,3M12,7A5,5 0 0,1 17,12C17,12.64 16.87,13.26 16.64,13.82L19.57,16.75C20.87,15.39 21.89,13.78 22.46,12C20.73,7.61 16.47,4.5 12,4.5C10.5,4.5 9.09,4.75 7.79,5.2L9.64,7.05C10.37,6.69 11.16,7 12,7Z"

                    : "M12,4.5C7,4.5 2.73,7.61 1,12C2.73,16.39 7,19.5 12,19.5C17,19.5 21.27,16.39 23,12C21.27,7.61 17,4.5 12,4.5M12,17A5,5 0 0,1 7,12A5,5 0 0,1 12,7A5,5 0 0,1 17,12A5,5 0 0,1 12,17M12,9A3,3 0 0,0 9,12A3,3 0 0,0 12,15A3,3 0 0,0 15,12A3,3 0 0,0 12,9Z");

            }

        }

        private void RegeneratePassword_Click(object sender, RoutedEventArgs e)

        {

            GenerateNewPassword();

            ToastNotification.Show("Password Generated", "New temporary password generated.", ToastType.Info);

        }

        private void CopyGeneratedPassword_Click(object sender, RoutedEventArgs e)

        {

            if (!string.IsNullOrEmpty(_generatedPassword))

            {

                Clipboard.SetText(_generatedPassword);

                ToastNotification.Show("Copied", "Temporary password copied to clipboard.", ToastType.Success);

            }

        }

        private void Name_PreviewTextInput(object sender, TextCompositionEventArgs e)

        {

            // Only letters (including Filipino ñ/Ñ), space, and period '.'

            e.Handled = !Regex.IsMatch(e.Text, @"^[a-zA-ZñÑ\.\s]+$");

        }

        private void NamePasteHandler(object sender, DataObjectPastingEventArgs e)

        {

            if (e.DataObject.GetDataPresent(typeof(string)))

            {

                string text = ((string)e.DataObject.GetData(typeof(string))) ?? "";

                if (!Regex.IsMatch(text, @"^[a-zA-ZñÑ\.\s]+$"))

                {

                    e.CancelCommand();

                }

            }

            else

            {

                e.CancelCommand();

            }

        }

        private void ContactNumber_PreviewTextInput(object sender, TextCompositionEventArgs e)

        {

            if (!(sender is TextBox tb)) return;

            // Only '+' and digits are allowed

            if (e.Text != "+" && !char.IsDigit(e.Text, 0))

            {

                e.Handled = true;

                return;

            }

            string current = tb.Text ?? "";

            int selStart = tb.SelectionStart;

            int selLen = tb.SelectionLength;

            string resulting = current.Remove(selStart, selLen).Insert(selStart, e.Text);

            // Must start with '0' or '+'

            if (!resulting.StartsWith("0") && !resulting.StartsWith("+"))

            {

                e.Handled = true;

                return;

            }

            if (resulting.StartsWith("0"))

            {

                if (resulting.Contains("+"))

                {

                    e.Handled = true;

                    return;

                }

                if (resulting.Length >= 2 && resulting[1] != '9')

                {

                    e.Handled = true;

                    return;

                }

                if (!Regex.IsMatch(resulting, @"^\d+$"))

                {

                    e.Handled = true;

                    return;

                }

                if (resulting.Length > 11)

                {

                    e.Handled = true;

                    return;

                }

            }

            else if (resulting.StartsWith("+"))

            {

                if (resulting.IndexOf('+', 1) != -1)

                {

                    e.Handled = true;

                    return;

                }

                if (resulting.Length >= 2 && resulting[1] != '6')

                {

                    e.Handled = true;

                    return;

                }

                if (resulting.Length >= 3 && resulting[2] != '3')

                {

                    e.Handled = true;

                    return;

                }

                if (resulting.Length > 3 && !Regex.IsMatch(resulting.Substring(1), @"^\d+$"))

                {

                    e.Handled = true;

                    return;

                }

                if (resulting.Length > 13)

                {

                    e.Handled = true;

                    return;

                }

            }

        }

        private void ContactPasteHandler(object sender, DataObjectPastingEventArgs e)

        {

            if (e.DataObject.GetDataPresent(typeof(string)))

            {

                string text = ((string)e.DataObject.GetData(typeof(string)))?.Trim() ?? "";

                if (!(sender is TextBox tb)) { e.CancelCommand(); return; }

                string current = tb.Text ?? "";

                int selStart = tb.SelectionStart;

                int selLen = tb.SelectionLength;

                string resulting = current.Remove(selStart, selLen).Insert(selStart, text);

                bool valid09 = Regex.IsMatch(resulting, @"^09\d{0,9}$") && resulting.Length <= 11;

                bool valid63 = Regex.IsMatch(resulting, @"^\+63\d{0,10}$") && resulting.Length <= 13;

                if (!valid09 && !valid63)

                {

                    e.CancelCommand();

                }

            }

            else

            {

                e.CancelCommand();

            }

        }

    }

}

