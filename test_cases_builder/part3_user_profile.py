# -*- coding: utf-8 -*-

def get_user_profile_cases():
    return [
        (
            "User Profile / Navigation via TopBar",
            "Verify navigating to User Profile page by clicking 'View Profile' in TopBar menu. (Usability)",
            "1. In the application header, click the profile pill showing the user avatar and name.\n2. In the dropdown menu, click 'View Profile'.\n3. Verify the main viewport navigates to the User Profile page and TopBar title updates to 'User Profile'.",
            "Click TopBar Profile Pill -> Click 'View Profile'",
            "Usability",
            "Verified: MainWindow.NavigateTo('UserProfile') renders UserProfilePage."
        ),
        (
            "User Profile / Navigation via Change Password Shortcut",
            "Verify clicking 'Change Password' in TopBar menu navigates to Profile and scrolls to password section. (Usability)",
            "1. Click TopBar Profile Pill.\n2. Click 'Change Password' in the dropdown.\n3. Verify viewport navigates to UserProfilePage, scrolls to PasswordCard, and expands ChangePasswordFormPanel.",
            "Click TopBar Profile Pill -> Click 'Change Password'",
            "Usability",
            "Verified: Navigates with targetSection = 'password', scrolling directly to PasswordCard."
        ),
        (
            "User Profile / Hero Card Rendering",
            "Verify avatar circle displays uppercase first initial and name/role/email badges reflect active session. (Functional Suitability)",
            "1. Log in as 'Admin User' (email: admin@solace.gov.ph, role: Administrator).\n2. Navigate to User Profile page.\n3. Verify avatar letter circle displays 'A', display name shows 'Admin User', and role pill displays 'Administrator'.",
            "Login session: 'Admin User'",
            "Functional Suitability",
            "Verified: HeroCard binds to MainWindow.FullName, MainWindow.CurrentUserRole, and initials."
        ),
        (
            "User Profile / Assigned Scope Display (Administrator)",
            "Verify Assigned Scope displays 'All barangays' for Administrator accounts. (Functional Suitability)",
            "1. Log in as an Administrator.\n2. Open User Profile page.\n3. Inspect TxtScope in HeroCard and TxtBarangay in AccountDetailsCard.\n4. Verify value displays 'All barangays'.",
            "View profile as Administrator",
            "Functional Suitability",
            "Verified: When CurrentUserBarangay is null/empty for Admin, displays 'All barangays'."
        ),
        (
            "User Profile / Assigned Scope Display (Barangay Secretary)",
            "Verify Assigned Scope displays the specific assigned barangay for Barangay Secretary accounts. (Functional Suitability)",
            "1. Log in as Barangay Secretary assigned to 'Barangay Central'.\n2. Open User Profile page.\n3. Inspect TxtScope and TxtBarangay.\n4. Verify specific barangay name 'Barangay Central' is displayed.",
            "View profile as Barangay Secretary (Barangay Central)",
            "Functional Suitability",
            "Verified: Displays specific assigned barangay for scoped staff accounts."
        ),
        (
            "User Profile / Account Details Read-Only Fields",
            "Verify Full Name, Role, Email Address, and Barangay render as read-only labels. (Functional Suitability)",
            "1. Inspect Account Details section on User Profile page.\n2. Verify Full Name, Role, Email Address, and Barangay are non-editable text labels.\n3. Verify data accurately reflects active authenticated session.",
            "Inspect TxtFullName, TxtRole, TxtEmail, TxtBarangay on UserProfilePage",
            "Functional Suitability",
            "Verified: AccountDetailsCard renders read-only TextBlocks; modifications require explicit 'Edit Profile' dialog."
        ),
        (
            "User Profile / Active Status Indicator",
            "Verify Account Status row displays a green dot indicator and 'Active' text. (Visual Verification)",
            "1. Inspect Account Status row at the bottom of Account Details card.\n2. Verify green circular dot indicator (#22C55E) and text 'Active' are displayed.",
            "Inspect Account Status indicator on UserProfilePage",
            "Usability",
            "Verified: Green indicator confirms account is in good standing and active."
        ),
        (
            "User Profile / Contact Number Display & Fallback",
            "Verify Contact Number displays formatted phone number or em-dash fallback when unset. (Usability)",
            "1. On User Profile, inspect Contact Number field.\n2. If contact number is set, verify clean phone number formatting.\n3. If contact number is empty, verify em-dash ('—') fallback is shown.",
            "Inspect TxtContactNumber on UserProfilePage",
            "Usability",
            "Verified: TxtContactNumber displays MainWindow.ContactNumber or fallback."
        ),
        (
            "User Profile / Session Information Card",
            "Verify Session Info card renders login start timestamp, system version, and environment. (Functional Suitability)",
            "1. Inspect Session Info card at the bottom of the page.\n2. Verify Session Started timestamp matches login time in 'MMM d, yyyy h:mm tt' format.\n3. Verify System Version displays 'SOLACE v1.0.0' and Environment displays 'Production'.",
            "Inspect TxtSessionStart, System Version, and Environment badge",
            "Functional Suitability",
            "Verified: SessionInfoCard binds to session startup metadata."
        ),
        (
            "User Profile / Open Edit Profile Dialog",
            "Verify clicking 'Edit Profile' button opens EditProfileDialog modal with full-window dim overlay. (Usability)",
            "1. On User Profile page, click 'Edit Profile' button (BtnEditProfile).\n2. Verify EditProfileDialog opens centered on the window.\n3. Verify full-window dim overlay (MainWindow.ShowDimOverlay) is active.\n4. Verify First Name, Last Name, Email, and Contact Number are pre-filled.",
            "Click 'Edit Profile' button on UserProfilePage",
            "Usability",
            "Verified: BtnEditProfile_Click opens EditProfileDialog and shows fullWindow dim overlay."
        ),
        (
            "User Profile / Edit Profile - First Name Real-Time Filtering",
            "Verify typing numbers or symbols into First Name in EditProfileDialog is blocked. (Negative / Input Filtering)",
            "1. In EditProfileDialog, focus First Name field.\n2. Attempt to type numbers '123' or symbols '@#$'.\n3. Verify characters are blocked in real time.",
            "Keystroke '123@#$' in TxtFirstName on EditProfileDialog",
            "Reliability",
            "Verified: NameInput_PreviewTextInput regex ^[\\p{L} \\-]+$ blocks disallowed characters."
        ),
        (
            "User Profile / Edit Profile - First Name Required Validation",
            "Verify inline error when First Name is cleared in EditProfileDialog. (Negative / Validation)",
            "1. In EditProfileDialog, clear First Name field.\n2. Click 'Save Changes'.\n3. Verify inline error banner displays 'First name is required.'",
            "Clear First Name, Click 'Save Changes'",
            "Functional Suitability",
            "Verified: ValidateFirstName() flags empty first name and displays error banner."
        ),
        (
            "User Profile / Edit Profile - Last Name Real-Time Filtering",
            "Verify typing numbers or symbols into Last Name in EditProfileDialog is blocked. (Negative / Input Filtering)",
            "1. In EditProfileDialog, focus Last Name field.\n2. Attempt to type numbers '456' or symbols '!%*'.\n3. Verify disallowed keystrokes are suppressed.",
            "Keystroke '456!%*' in TxtLastName on EditProfileDialog",
            "Reliability",
            "Verified: Disallowed characters regex blocks non-letter input."
        ),
        (
            "User Profile / Edit Profile - Last Name Required Validation",
            "Verify inline error when Last Name is cleared in EditProfileDialog. (Negative / Validation)",
            "1. In EditProfileDialog, clear Last Name field.\n2. Click 'Save Changes'.\n3. Verify inline error banner displays 'Last name is required.'",
            "Clear Last Name, Click 'Save Changes'",
            "Functional Suitability",
            "Verified: ValidateLastName() flags empty last name and blocks submission."
        ),
        (
            "User Profile / Edit Profile - Contact Number Format Validation",
            "Verify inline error when Contact Number does not match 09XXXXXXXXX (11 digits). (Negative / Validation)",
            "1. In EditProfileDialog, enter 10 digits '0917123456'.\n2. Click 'Save Changes'.\n3. Verify error banner displays 'Contact number must be an 11-digit mobile number starting with 09 (e.g., 09171234567).'",
            "Contact Number: '0917123456' (10 digits), Click 'Save Changes'",
            "Functional Suitability",
            "Verified: PhoneRegex ^09\\d{9}$ validates 11-digit structure."
        ),
        (
            "User Profile / Edit Profile - Email Immutability",
            "Verify Email field is read-only in EditProfileDialog and cannot be changed. (Security)",
            "1. In EditProfileDialog, click into Email field.\n2. Attempt to modify or delete email text.\n3. Verify field is read-only and keystrokes have no effect.",
            "Attempt text input in TxtEmail on EditProfileDialog",
            "Security",
            "Verified: Email field has IsReadOnly = true and disabled background styling."
        ),
        (
            "User Profile / Edit Profile - Cancel Action",
            "Verify clicking Cancel in EditProfileDialog dismisses modal without modifying user details. (Usability)",
            "1. In EditProfileDialog, change First Name and Contact Number.\n2. Click 'Cancel' (or close '✕' button).\n3. Verify dialog closes, dim overlay lifts, and profile details remain unchanged.",
            "Modify fields -> Click 'Cancel' in EditProfileDialog",
            "Usability",
            "Verified: Confirmed remains false; MainWindow details are not updated."
        ),
        (
            "User Profile / Edit Profile - Successful Update",
            "Verify saving valid name and contact updates profile details on API and updates TopBar. (Successful)",
            "1. In EditProfileDialog, enter First Name 'Maria Elena' and Contact '09179998877'.\n2. Click 'Save Changes'.\n3. Verify API receives update, dialog closes, success toast displays, and HeroCard and TopBar profile pill reflect new name.",
            "First Name: 'Maria Elena', Contact: '09179998877', Click 'Save Changes'",
            "Functional Suitability",
            "Verified: UserApiService updates profile, MainWindow.FullName and TopBar pill refresh immediately."
        ),
        (
            "User Profile / In-Page Change Password - Expand Form",
            "Verify clicking 'Change Password' button expands the in-page password update form. (Usability)",
            "1. On User Profile, locate Password & Security card.\n2. Click 'Change Password' button (BtnChangePassword).\n3. Verify ChangePasswordButtonPanel collapses and ChangePasswordFormPanel expands.\n4. Verify keyboard focus lands on Current Password field.",
            "Click 'Change Password' button on UserProfilePage",
            "Usability",
            "Verified: BtnChangePassword_Click toggles panel visibility and focuses PwdCurrentPassword."
        ),
        (
            "User Profile / In-Page Change Password - Cancel and Collapse Form",
            "Verify clicking 'Cancel' clears password fields and collapses the form. (Usability)",
            "1. With Change Password form expanded, enter characters in all three password boxes.\n2. Click 'Cancel' (BtnCancelChangePassword).\n3. Verify all password fields are cleared, error labels collapse, and ChangePasswordButtonPanel re-appears.",
            "Click 'Cancel' in Change Password form",
            "Usability",
            "Verified: BtnCancelChangePassword_Click invokes ClearChangePasswordForm() and resets panels."
        ),
        (
            "User Profile / In-Page Change Password - Current Password Required",
            "Verify inline error when Current Password is empty upon submitting password change. (Negative / Validation)",
            "1. In Change Password form, leave Current Password blank.\n2. Fill New Password and Confirm Password.\n3. Click 'Update Password'.\n4. Verify ErrCurrentPassword displays 'Current password is required.'",
            "Current Password: \"\" (Empty), Click 'Update Password'",
            "Functional Suitability",
            "Verified: Validates string.IsNullOrWhiteSpace(currentPassword) and displays ErrCurrentPassword."
        ),
        (
            "User Profile / In-Page Change Password - New Password Required",
            "Verify inline error when New Password is empty upon submitting password change. (Negative / Validation)",
            "1. In Change Password form, enter Current Password.\n2. Leave New Password blank.\n3. Click 'Update Password'.\n4. Verify ErrNewPassword displays 'New password is required.'",
            "New Password: \"\" (Empty), Click 'Update Password'",
            "Functional Suitability",
            "Verified: Validates string.IsNullOrWhiteSpace(newPassword) and displays ErrNewPassword."
        ),
        (
            "User Profile / In-Page Change Password - Minimum Length (< 6 Chars)",
            "Verify inline error when New Password is shorter than 6 characters. (Negative / Validation)",
            "1. In Change Password form, enter Current Password.\n2. In New Password and Confirm Password, enter short password '12345' (5 characters).\n3. Click 'Update Password'.\n4. Verify ErrNewPassword displays 'New password must be at least 6 characters.'",
            "New Password: '12345' (5 chars), Click 'Update Password'",
            "Functional Suitability",
            "Verified: Validates newPassword.Length < 6 and displays inline error."
        ),
        (
            "User Profile / In-Page Change Password - Confirm Password Required",
            "Verify inline error when Confirm Password is empty upon submitting password change. (Negative / Validation)",
            "1. Enter Current Password and valid New Password.\n2. Leave Confirm Password blank.\n3. Click 'Update Password'.\n4. Verify ErrConfirmPassword displays 'Confirm password is required.'",
            "Confirm Password: \"\" (Empty), Click 'Update Password'",
            "Functional Suitability",
            "Verified: Validates string.IsNullOrWhiteSpace(confirmPassword) and displays ErrConfirmPassword."
        ),
        (
            "User Profile / In-Page Change Password - Passwords Do Not Match",
            "Verify inline error when New Password and Confirm Password do not match. (Negative / Validation)",
            "1. Enter valid Current Password.\n2. In New Password, enter 'NewSecurePass123!'.\n3. In Confirm Password, enter 'DifferentPass999!'.\n4. Click 'Update Password'.\n5. Verify ErrConfirmPassword displays 'Passwords do not match.'",
            "New: 'NewSecurePass123!', Confirm: 'DifferentPass999!', Click 'Update Password'",
            "Functional Suitability",
            "Verified: Validates newPassword != confirmPassword and displays ErrConfirmPassword."
        ),
        (
            "User Profile / In-Page Change Password - New Password Same as Current",
            "Verify inline error when New Password is identical to Current Password. (Negative / Validation)",
            "1. Enter current password in Current Password.\n2. Enter the exact same password in New Password and Confirm Password.\n3. Click 'Update Password'.\n4. Verify ErrNewPassword displays 'New password must be different from current password.'",
            "New Password identical to Current Password, Click 'Update Password'",
            "Functional Suitability",
            "Verified: Validates currentPassword == newPassword and prevents password reuse."
        ),
        (
            "User Profile / In-Page Change Password - Incorrect Current Password Rejection",
            "Verify system rejection when entering an incorrect Current Password. (Security / Negative)",
            "1. In Current Password, enter an incorrect password ('WrongPassword999!').\n2. Enter matching valid New and Confirm Passwords.\n3. Click 'Update Password'.\n4. Verify API rejects request and ErrCurrentPassword displays error message from server.",
            "Current: 'WrongPassword999!', New: 'NewPass123!', Click 'Update Password'",
            "Security",
            "Verified: AuthApiService.ChangePasswordAsync fails; error routed to ErrCurrentPassword."
        ),
        (
            "User Profile / In-Page Change Password - Real-Time Error Clearance",
            "Verify typing into password fields clears active validation errors in Change Password form. (Usability)",
            "1. Trigger validation errors on Change Password form.\n2. Type into PwdCurrentPassword, PwdNewPassword, or PwdConfirmPassword.\n3. Verify inline error text collapses immediately.",
            "Trigger errors -> Type into password boxes",
            "Usability",
            "Verified: PasswordChanged event handlers clear specific error labels."
        ),
        (
            "User Profile / In-Page Change Password - Successful Password Update",
            "Verify submitting valid passwords updates credentials, displays success banner, and auto-collapses form. (Successful)",
            "1. Enter valid Current Password.\n2. Enter compliant New Password and matching Confirm Password.\n3. Click 'Update Password'.\n4. Verify ChangePasswordSuccessBanner appears, toast confirms success, audit log is written, and form collapses after 2 seconds.",
            "Valid Current + New + Confirm Passwords, Click 'Update Password'",
            "Functional Suitability",
            "Verified: AuthApiService.ChangePasswordAsync succeeds, logs audit event, and resets form."
        ),
        (
            "User Profile / Responsiveness & Scrolling",
            "Verify vertical scrolling through PageScroll without control clipping or overlap on 1024x768 resolution. (Usability)",
            "1. Resize window to 1024x768.\n2. Scroll through User Profile page vertically from top avatar card to bottom session card.\n3. Verify all cards clip neatly to rounded corners (radius 22) and no buttons or labels overlap.",
            "Window resize to 1024x768, Mouse scroll wheel",
            "Usability",
            "Verified: PageScroll ScrollViewer and RoundedClip handlers ensure clean responsive rendering."
        ),
        (
            "User Profile / Card Rounded Corner Clipping",
            "Verify decorative filled circles do not spill past card edges on resize. (Visual Verification)",
            "1. Resize application window across various widths.\n2. Inspect top-right decorative background shapes on HeroCard and PasswordCard.\n3. Verify shapes are clipped strictly to the card's 22px rounded border.",
            "Inspect HeroCardBg and PasswordCardBg clipping",
            "Usability",
            "Verified: RoundedClip_SizeChanged applies RectangleGeometry clip matching card CornerRadius."
        ),
        (
            "User Profile / Height Synchronization Across Cards",
            "Verify Account Details card synchronizes height with Password and Session cards when form is collapsed. (Usability)",
            "1. Observe height of Account Details card and adjacent Password & Session cards in two-column layout.\n2. Verify heights align symmetrically.\n3. Expand Change Password form; verify left column height remains fixed without stretching unnaturally.",
            "Inspect SyncAccountDetailsHeight behavior before and after form expansion",
            "Usability",
            "Verified: SyncAccountDetailsHeight preserves clean layout symmetry and guards against expansion distortion."
        ),
        (
            "User Profile / Audit Logging on Profile Update",
            "Verify editing profile information creates an audit record in the system log. (Security)",
            "1. Complete a profile update via EditProfileDialog.\n2. Navigate to Dashboard -> Recent Activity / View All Logs.\n3. Verify an audit entry is logged with action 'UPDATE_USER' and actor name.",
            "Complete profile update -> Check Audit Logs",
            "Security",
            "Verified: AuditLogService records user profile mutation telemetry."
        ),
        (
            "User Profile / Audit Logging on Password Change",
            "Verify successful in-page password update generates an audit log entry. (Security)",
            "1. Successfully update password via in-page form.\n2. Inspect Audit Logs.\n3. Verify audit log entry 'Password changed by <UserName>' is recorded with timestamp.",
            "Complete password update -> Check Audit Logs",
            "Security",
            "Verified: Services.AuditLogService.Instance.LogSystem records password change event."
        ),
        (
            "User Profile / Password Form Double-Click Guard",
            "Verify 'Update Password' button is disabled during API roundtrip to prevent duplicate requests. (Reliability)",
            "1. Enter valid password values.\n2. Rapidly click 'Update Password' button.\n3. Verify button is immediately disabled (IsEnabled = false) until server response arrives.",
            "Rapid click on 'BtnSaveChangePassword'",
            "Reliability",
            "Verified: BtnSaveChangePassword.IsEnabled = false in try block and true in finally block."
        ),
        (
            "User Profile / Server Outage Graceful Handling",
            "Verify graceful error handling and toast notice when API service is offline during password update. (Reliability)",
            "1. Disconnect backend API.\n2. Click 'Update Password'.\n3. Verify application does not crash, button re-enables, and error toast notification is displayed.",
            "API offline, Click 'Update Password'",
            "Reliability",
            "Verified: Catches network exception and shows friendly error toast."
        ),
        (
            "User Profile / Edit Profile Paste Disallowed Characters Handling",
            "Verify pasting text with numbers or symbols into Edit Profile name fields strips invalid characters. (Input Sanitization)",
            "1. Copy 'Maria123@#$' to clipboard.\n2. Paste into First Name field in EditProfileDialog.\n3. Verify only valid characters 'Maria' are pasted and invalid characters are stripped.",
            "Paste 'Maria123@#$' into TxtFirstName",
            "Functional Suitability",
            "Verified: NameInput_Pasting handler strips disallowed characters using regex."
        ),
        (
            "User Profile / Password Masking Inspection",
            "Verify password characters entered into Current, New, and Confirm password boxes are strictly masked. (Security)",
            "1. In Change Password form, type characters into all three password boxes.\n2. Verify characters appear as black masked bullets without exposing cleartext.",
            "Type in PwdCurrentPassword, PwdNewPassword, PwdConfirmPassword",
            "Security",
            "Verified: WPF PasswordBox ensures character masking in memory and display."
        ),
        (
            "User Profile / TopBar Dropdown Dismissal on Outside Click",
            "Verify clicking outside the Profile dropdown menu automatically closes the popup. (Usability)",
            "1. Click Profile Pill in TopBar to open menu.\n2. Click anywhere on the dashboard or empty space.\n3. Verify ProfileMenu popup dismisses smoothly.",
            "Click Profile Pill -> Click outside menu",
            "Usability",
            "Verified: Popup.StaysOpen = false automatically closes popup on outside mouse click."
        ),
        (
            "User Profile / Keyboard Navigation in Change Password Form",
            "Verify navigating between password fields using Tab key. (Usability)",
            "1. Expand Change Password form.\n2. Focus Current Password box.\n3. Press [Tab] key; verify focus moves to New Password box.\n4. Press [Tab] again; verify focus moves to Confirm Password box.\n5. Press [Tab] again; verify focus moves to 'Update Password' button.",
            "Press [Tab] key sequentially through Change Password form",
            "Usability",
            "Verified: TabIndex and keyboard navigation follow standard accessibility flow."
        )
    ]
