# -*- coding: utf-8 -*-

def get_auth_cases():
    return [
        (
            "Authentication / Sign In",
            "Verify successful authentication with valid administrator credentials and correct dashboard redirection. (Successful)",
            "1. Launch the SOLACE application.\n2. In the Email field, enter a valid Admin email address.\n3. In the Password field, enter the correct password.\n4. Click the 'Sign in securely' button.\n5. Verify system authenticates, sets JWT token, logs audit event, and opens MainWindow.",
            'Email: "admin@solace.gov.ph", Password: "ValidAdminPassword123!", Click \'Sign in securely\' button',
            "Functional Suitability",
            "Verified: Authenticates via AuthApiService, sets MainWindow.CurrentUserRole = 'Administrator', opens MainWindow and logs audit event."
        ),
        (
            "Authentication / Sign In",
            "Verify successful authentication with valid encoder credentials and restricted role initialization. (Successful)",
            "1. Launch the SOLACE application.\n2. In the Email field, enter a valid Encoder account email.\n3. In the Password field, enter the corresponding valid password.\n4. Click the 'Sign in securely' button.\n5. Observe role enforcement in MainWindow.",
            'Email: "encoder1@solace.gov.ph", Password: "EncoderPassword123!", Click \'Sign in securely\' button',
            "Functional Suitability",
            "Verified: Sets MainWindow.CurrentUserRole = 'Encoder' and restricts administrative tabs."
        ),
        (
            "Authentication / Sign In",
            "Verify successful authentication with valid Barangay Secretary credentials and barangay scoping. (Successful)",
            "1. Launch the SOLACE application.\n2. In Email field, enter Barangay Secretary account email.\n3. In Password field, enter valid password.\n4. Click 'Sign in securely'.\n5. Verify system signs in and scopes session to assigned barangay.",
            'Email: "sec_malaban@solace.gov.ph", Password: "SecPassword123!", Click \'Sign in securely\' button',
            "Functional Suitability",
            "Verified: Sets MainWindow.CurrentUserRole = 'BarangaySecretary' and locks Barangay scope to 'Malaban'."
        ),
        (
            "Authentication / Keyboard Navigation",
            "Verify sign-in submission via the 'Enter' key press on password field. (Successful)",
            "1. Enter registered email address into the Email field.\n2. Enter valid password into the Password field.\n3. Press the keyboard 'Enter' key while focused on the input field.\n4. Verify application initiates authentication without requiring a mouse click.",
            'Email: "encoder1@solace.gov.ph", Password: "EncoderPassword123!", Key: [Enter]',
            "Usability",
            "Verified: TxtPassword_KeyDown / TxtPasswordVisible_KeyDown triggers ExecuteSignInAsync on Enter key."
        ),
        (
            "Authentication / Field Validation",
            "Verify inline validation error when attempting to sign in with an empty email field. (Negative / Validation)",
            "1. Leave the Email field blank.\n2. Enter a password into the Password field.\n3. Click the 'Sign in securely' button.\n4. Verify red border highlight and inline error label ('Please enter your email address.') appear below the Email field.",
            'Email: "" (Empty), Password: "Password123!", Click \'Sign in securely\' button',
            "Functional Suitability",
            "Verified: SetEmailError('Please enter your email address.') sets BorderEmail.BorderBrush = ErrorBorderBrush and focuses TxtEmail."
        ),
        (
            "Authentication / Field Validation",
            "Verify inline validation error when email field contains whitespace characters. (Negative / Validation)",
            "1. In the Email field, enter an email address with internal spaces (e.g., 'user name@solace.gov.ph').\n2. Enter a password into the Password field.\n3. Click 'Sign in securely'.\n4. Verify red border highlight and inline error label ('Email address cannot contain spaces.') appear.",
            'Email: "admin user@solace.gov.ph", Password: "Password123!", Click \'Sign in securely\' button',
            "Functional Suitability",
            "Verified: LoginPage validates email.Contains(' ') and shows SetEmailError('Email address cannot contain spaces.')."
        ),
        (
            "Authentication / Field Validation",
            "Verify inline validation error when email address lacks the '@' symbol. (Negative / Validation)",
            "1. In the Email field, enter a string lacking '@' (e.g., 'adminsolace.gov.ph').\n2. Enter a password.\n3. Click 'Sign in securely'.\n4. Verify red border highlight and inline error label ('Please include an \'@\' in the email address.') appear.",
            'Email: "adminsolace.gov.ph", Password: "Password123!", Click \'Sign in securely\' button',
            "Functional Suitability",
            "Verified: LoginPage validates !email.Contains('@') and displays SetEmailError('Please include an \'@\' in the email address.')."
        ),
        (
            "Authentication / Field Validation",
            "Verify inline validation error when email address has invalid domain format. (Negative / Validation)",
            "1. In the Email field, enter a malformed email format (e.g., 'admin@solace').\n2. Enter a password.\n3. Click 'Sign in securely'.\n4. Verify red border highlight and inline error label ('Please enter a valid email address (e.g., name@example.com).') appear.",
            'Email: "admin@solace", Password: "Password123!", Click \'Sign in securely\' button',
            "Functional Suitability",
            "Verified: Regex check ^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$ triggers error message."
        ),
        (
            "Authentication / Field Validation",
            "Verify inline validation error when attempting to sign in with an empty password field. (Negative / Validation)",
            "1. Enter a valid email address into the Email field.\n2. Leave the Password field blank.\n3. Click the 'Sign in securely' button.\n4. Verify red border highlight and inline error label ('Please enter your password.') appear below the Password field.",
            'Email: "admin@solace.gov.ph", Password: "" (Empty), Click \'Sign in securely\' button',
            "Functional Suitability",
            "Verified: SetPasswordError('Please enter your password.') sets BorderPassword.BorderBrush = ErrorBorderBrush."
        ),
        (
            "Authentication / Field Validation",
            "Verify inline validation error when password is less than 6 characters. (Negative / Validation)",
            "1. Enter a valid email address into the Email field.\n2. Enter a short password of 4 characters (e.g., 'pass').\n3. Click 'Sign in securely'.\n4. Verify red border highlight and inline error label ('Password must be at least 6 characters.') appear.",
            'Email: "admin@solace.gov.ph", Password: "pass" (4 characters), Click \'Sign in securely\' button',
            "Functional Suitability",
            "Verified: LoginPage enforces password.Length < 6 check and displays 'Password must be at least 6 characters.'."
        ),
        (
            "Authentication / Field Validation",
            "Verify inline validation errors when both email and password fields are left blank. (Negative / Validation)",
            "1. Leave both Email and Password fields completely blank.\n2. Click the 'Sign in securely' button or press Enter.\n3. Verify inline required error messages appear under both Email and Password fields and focus lands on Email.",
            'Email: "" (Empty), Password: "" (Empty), Click \'Sign in securely\' button',
            "Functional Suitability",
            "Verified: Both SetEmailError and SetPasswordError execute; controlToFocus focuses TxtEmail."
        ),
        (
            "Authentication / Real-Time Validation Clearance",
            "Verify typing into the Email field clears the active red border and inline error label. (Usability)",
            "1. Trigger an inline validation error on the Email field.\n2. Type any character into the Email field.\n3. Verify red border returns to standard grey (#CCCCCC) and ErrEmail collapses immediately.",
            'Trigger error -> Type "a" into TxtEmail',
            "Usability",
            "Verified: TxtEmail_TextChanged calls ClearEmailError() resetting border brush and collapsing ErrEmail."
        ),
        (
            "Authentication / Real-Time Validation Clearance",
            "Verify typing into the Password field clears the active red border and inline error label. (Usability)",
            "1. Trigger an inline validation error on the Password field.\n2. Type any character into the Password field.\n3. Verify red border returns to standard grey (#CCCCCC) and ErrPassword collapses immediately.",
            'Trigger error -> Type "p" into TxtPassword',
            "Usability",
            "Verified: TxtPassword_PasswordChanged calls ClearPasswordError() resetting border brush and collapsing ErrPassword."
        ),
        (
            "Authentication / Error Handling",
            "Verify system rejection when entering incorrect password for an existing account. (Security / Negative)",
            "1. Enter registered email address.\n2. Enter an incorrect password.\n3. Click the 'Sign in securely' button.\n4. Observe error modal dialog showing 'Sign In Failed' with user-friendly credentials error message.",
            'Email: "admin@solace.gov.ph", Password: "WrongPassword999!", Click \'Sign in securely\' button',
            "Security",
            "Verified: FormatHumanReadableAuthError intercepts 401/unauthorized response and renders ErrorModalOverlay."
        ),
        (
            "Authentication / Error Handling",
            "Verify system rejection when entering non-existent or unregistered email address. (Security / Negative)",
            "1. Enter an unregistered email address into the Email field.\n2. Enter an arbitrary password.\n3. Click the 'Sign in securely' button.\n4. Verify error message appears without revealing specific database internal state.",
            'Email: "nonexistent.user@solace.gov.ph", Password: "RandomPassword123!", Click \'Sign in securely\' button',
            "Security",
            "Verified: ErrorModalOverlay displays generic 'Invalid email or password' message."
        ),
        (
            "Authentication / UI Interaction",
            "Verify password field clearing and visibility reset upon dismissing the error modal dialog. (Usability)",
            "1. Enter invalid credentials and trigger error modal.\n2. Click the 'Close' button on the Error Modal.\n3. Verify Password field is cleared, Email field is retained for user convenience, and focus returns to Password.",
            'Click \'Close\' button on ErrorModalOverlay',
            "Usability",
            "Verified: BtnErrorClose_Click clears TxtPassword.Password, keeps TxtEmail.Text, and hides ErrorModalOverlay."
        ),
        (
            "Authentication / UI Controls",
            "Verify toggling password visibility between masked and plain text characters. (Usability)",
            "1. Enter password into the password field (obscured with bullets).\n2. Click the eye icon toggle button (BtnTogglePassword).\n3. Verify plain text is revealed in TxtPasswordVisible and icon switches to eye-off.\n4. Click toggle button again.\n5. Verify text is masked in TxtPassword again.",
            'Password: "SecureSecretPass!", Click \'BtnTogglePassword\' twice',
            "Usability",
            "Verified: BtnTogglePassword_Click synchronizes text between TxtPassword and TxtPasswordVisible and toggles icon geometry."
        ),
        (
            "Authentication / Concurrency & Re-entrancy",
            "Verify prevention of duplicate login submissions on rapid consecutive clicks. (Reliability)",
            "1. Enter valid email and password.\n2. Rapidly double-click or multi-click the 'Sign in securely' button.\n3. Verify button is immediately disabled, loading overlay is activated, and exactly one API login request is executed.",
            'Rapid consecutive clicks on \'BtnLogin\'',
            "Reliability",
            "Verified: _isLoggingIn guard flag immediately rejects re-entrant calls and disables button interaction."
        ),
        (
            "Authentication / UI State & Feedback",
            "Verify loading overlay and visual spinner display during authentication request execution. (Usability)",
            "1. Enter credentials and click 'Sign in securely'.\n2. Observe screen during server roundtrip.\n3. Verify dimming overlay (LoadingOverlay) and spinning progress indicator are shown.\n4. Verify overlay automatically disappears upon completion.",
            'Credentials entered, Click \'Sign in securely\' button',
            "Usability",
            "Verified: LoadingOverlay.Visibility is set to Visible before API call and Collapsed in finally block."
        ),
        (
            "Authentication / Security & Input Sanitization",
            "Verify defense against SQL injection strings in login fields. (Security / Negative)",
            "1. In Email field, enter SQLi string: ' OR '1'='1' --.\n2. In Password field, enter ' OR '1'='1'.\n3. Click 'Sign in securely'.\n4. Verify input is sanitized and rejected safely without database error leakage.",
            'Email: "\' OR \'1\'=\'1\' --", Password: "\' OR \'1\'=\'1\'", Click \'Sign in securely\'',
            "Security",
            "Verified: Email regex rejection or API parameterized query blocks SQLi; error handled gracefully."
        ),
        (
            "Authentication / Security & Input Sanitization",
            "Verify defense against Cross-Site Scripting (XSS) payload injection in login fields. (Security / Negative)",
            "1. In Email field, enter payload: <script>alert(1)</script>.\n2. In Password field, enter arbitrary string.\n3. Click 'Sign in securely'.\n4. Verify email validation blocks string and no script executes.",
            'Email: "<script>alert(1)</script>", Password: "Password123!", Click \'Sign in securely\'',
            "Security",
            "Verified: Regex rejects non-email characters; no HTML/JS execution."
        ),
        (
            "Authentication / Security",
            "Verify account lockout or rejection for deactivated/disabled user accounts. (Security / Negative)",
            "1. Enter email address of a disabled staff member.\n2. Enter valid password for that account.\n3. Click 'Sign in securely'.\n4. Verify authentication is rejected with notification indicating account is disabled.",
            'Email: "disabled.encoder@solace.gov.ph", Password: "OldPassword123!", Click \'Sign in securely\'',
            "Security",
            "Verified: API returns account inactive/disabled error; FormatHumanReadableAuthError displays clear alert."
        ),
        (
            "Authentication / Network Resilience",
            "Verify graceful handling when backend API service is unreachable or offline. (Reliability)",
            "1. Disconnect network or stop backend API server.\n2. Enter valid credentials and click 'Sign in securely'.\n3. Wait for request timeout.\n4. Verify application does not crash and displays friendly 'No Internet Connection' or connection error modal.",
            'Network disconnected / API offline, Click \'Sign in securely\' button',
            "Reliability",
            "Verified: ApiClient.IsNetworkOrConnectionError catches exception and renders humanized connection error."
        ),
        (
            "Authentication / Token Lifecycle & Refresh",
            "Verify seamless JWT token refresh when access token expires during active session. (Reliability)",
            "1. Log in to establish authenticated session with JWT and refresh token.\n2. Allow access token to expire while application is idle.\n3. Trigger an authorized API request (e.g., browse Solo Parent records).\n4. Verify AuthApiService automatically calls refresh-token and updates Bearer token without user re-login.",
            'Expired JWT + Active RefreshToken, API Get Request',
            "Reliability",
            "Verified: ApiClient automatically handles 401 with RefreshTokenAsync and retries original request."
        ),
        (
            "Authentication / Session Termination",
            "Verify complete session termination, token invalidation, and audit logging on user logout. (Security)",
            "1. Log into the system.\n2. In the application TopBar profile menu, click 'Logout'.\n3. Confirm logout prompt in ActionConfirmDialog.\n4. Verify client token cache is cleared, Bearer header is removed, audit log records logout event, and user is returned to LoginPage.",
            'Click TopBar ProfilePill -> Click \'Logout\' -> Confirm in ActionConfirmDialog',
            "Security",
            "Verified: AuthApiService.LogoutAsync clears token, logs audit event, and navigates back to LoginPage."
        ),
        (
            "Authentication / Information Modal",
            "Verify opening, scrolling, and closing the 'Terms and Conditions' modal overlay. (Usability)",
            "1. On the login screen, click the 'Terms and Conditions' hyperlink.\n2. Verify modal opens with legal text regarding Data Privacy Act (RA 10173).\n3. Scroll through content.\n4. Click 'Close' button or '✕' icon.\n5. Verify modal dismisses and returns focus to login form.",
            'Click \'Terms and Conditions\' link, Click \'Close\' / \'✕\'',
            "Usability",
            "Verified: TermsModalOverlay opens, displays Data Privacy Act (RA 10173) provisions, and closes cleanly."
        ),
        (
            "Authentication / Information Modal",
            "Verify 'Forgot password?' hyperlink displays administrator password recovery instructions. (Usability)",
            "1. On the login screen, click the 'Forgot password?' hyperlink.\n2. Verify the 'Password Recovery' modal dialog opens instructing the user to contact their System Administrator (CSWDO / IT Head) for a temporary password reset.\n3. Click 'Understood' to close the dialog.",
            'Click \'Forgot password?\' (LinkForgotPassword) hyperlink, Click \'Understood\'',
            "Usability",
            "Verified: LinkForgotPassword_Click opens ActionConfirmDialog with CSWDO admin recovery instructions."
        ),
        (
            "Authentication / Window Management",
            "Verify custom frameless window controls (Minimize, Maximize, Drag move, Close). (Usability)",
            "1. Click and drag the top title area to move window.\n2. Click '-' minimize button; verify window minimizes to taskbar.\n3. Restore window and click 'Maximize/Restore' button.\n4. Click '✕' close button; verify application terminates cleanly.",
            'Mouse drag on title bar, Click \'BtnMinimize\', \'BtnMaximize\', \'BtnClose\'',
            "Usability",
            "Verified: Custom titlebar controls invoke SystemCommands.MinimizeWindow, MaximizeWindow, and Close."
        ),
        (
            "Authentication / Performance",
            "Verify authentication response time and UI responsiveness under standard operating conditions. (Performance Efficiency)",
            "1. Enter valid credentials under stable network conditions.\n2. Measure duration from clicking 'Sign in securely' until MainWindow dashboard is fully rendered.\n3. Verify total transition completes within acceptable SLA (< 2 seconds).",
            'Valid credentials, Click \'Sign in securely\' button, Performance stopwatch',
            "Performance Efficiency",
            "Verified: Seamless transition with SplashScreen and parallel initialization completes under SLA."
        ),
        (
            "Authentication / Input Sanitization & Trimming",
            "Verify automatic trimming of leading and trailing whitespaces in the Email field. (Functional Suitability)",
            "1. In Email field, enter a valid email preceded and followed by spaces (e.g. ' admin@solace.gov.ph ').\n2. Enter valid password.\n3. Click 'Sign in securely'.\n4. Verify system trims whitespace automatically and authenticates successfully.",
            'Email: " admin@solace.gov.ph ", Password: "ValidAdminPassword123!", Click \'Sign in securely\'',
            "Functional Suitability",
            "Verified: TxtEmail.Text?.Trim() removes surrounding spaces before regex check and API payload submission."
        ),
        (
            "Authentication / Case Sensitivity",
            "Verify email address is treated case-insensitively during user authentication. (Functional Suitability)",
            "1. In Email field, enter valid email in uppercase or mixed casing (e.g. 'ADMIN@SOLACE.GOV.PH').\n2. Enter correct password.\n3. Click 'Sign in securely'.\n4. Verify authentication succeeds regardless of email casing.",
            'Email: "ADMIN@SOLACE.GOV.PH", Password: "ValidAdminPassword123!", Click \'Sign in securely\'',
            "Functional Suitability",
            "Verified: Backend authenticates email with case-insensitive collation."
        ),
        (
            "Authentication / Password Integrity",
            "Verify strict case-sensitivity enforcement on user passwords. (Security / Negative)",
            "1. Enter valid email address.\n2. In Password field, enter valid password with incorrect casing (e.g. all lowercase).\n3. Click 'Sign in securely'.\n4. Verify authentication is rejected with 'Sign In Failed' error dialog.",
            'Email: "admin@solace.gov.ph", Password: "validadminpassword123!", Click \'Sign in securely\'',
            "Security",
            "Verified: Password hash comparison enforces strict byte-level case-sensitivity."
        ),
        (
            "Authentication / Session Persistence",
            "Verify application prompts for fresh authentication upon restart without exposing unauthenticated access. (Security)",
            "1. Exit application completely.\n2. Relaunch application executable.\n3. Verify application opens directly to LoginPage with fields empty and unauthenticated state enforced.",
            'Relaunch SOLUM-UI application',
            "Security",
            "Verified: App.xaml.cs initializes LoginPage on startup; tokens are stored in secure memory session."
        ),
        (
            "Authentication / Application Exit & Session Control",
            "Verify closing the application prompts a confirmation dialog and cancels exit when 'Cancel' is clicked. (Usability)",
            "1. Log in to the application to reach the main window.\n2. Click the window close button ('✕') at the top right (or press Alt+F4).\n3. Verify the 'Exit Application' confirmation modal appears.\n4. Click the Cancel button (or close the modal).\n5. Verify the dialog closes, the window remains open, and the user session remains fully active.",
            'Click window \'✕\' close button, Click \'Cancel\'',
            "Usability",
            "Verified: MainWindow_Closing intercepts event, shows ActionConfirmDialog, and sets e.Cancel = true."
        ),
        (
            "Authentication / Application Exit & Session Control",
            "Verify confirming application exit successfully logs out the user, logs an audit record, and terminates the application. (Functional Suitability)",
            "1. Log in to the application to reach the main window.\n2. Click the window close button ('✕') at the top right.\n3. Verify the 'Exit Application' confirmation modal appears.\n4. Click Exit & Sign Out in the modal.\n5. Verify the system calls the logout API, records a logout audit trail, and safely terminates the application process.",
            'Click window \'✕\' close button, Click \'Exit & Sign Out\'',
            "Functional Suitability",
            "Verified: MainWindow_Closing initiates AuditLogService.LogLogout, clears session, and calls Application.Current.Shutdown."
        ),
        (
            "Authentication / Access Control",
            "Verify disabled user accounts cannot authenticate and are prevented from logging into the application. (Security / Negative)",
            "1. Disable a test user account in User Administration.\n2. Log out of the current Administrator session.\n3. On the Login page, enter the credentials of the disabled user.\n4. Click 'Sign in securely'.\n5. Verify login fails with an unauthorized / account disabled notification and the user is not granted access to the Dashboard.",
            'Email: "disabled_encoder@solace.gov.ph", Password: "Password123!", Click \'Sign in securely\' button',
            "Security",
            "Verified: API returns IsActive = false rejection; user is prevented from proceeding to MainWindow."
        ),
        (
            "Authentication / Responsive Scaling",
            "Verify restoring/un-maximizing Login window maintains minimum 1024x640 bounds and legible Viewbox scaling without shrinking into an unreadable state. (Usability)",
            "1. Launch SOLACE to LoginPage (maximized by default).\n2. Click Restore/Un-maximize button or drag window edges.\n3. Verify window enforces MinWidth=1024, MinHeight=640 and Viewbox remains centered and readable.",
            'Restore LoginPage window from maximized state, attempt to resize below 1024x640',
            "Usability",
            "Verified: LoginPage.xaml enforces MinWidth=1024, MinHeight=640 and bounded Viewbox (MaxWidth=1440, MaxHeight=1000)."
        ),
        (
            "Authentication / Force Password Change",
            "Verify ForceChangePasswordDialog intercepts login when an account is flagged with MustChangePassword. (Security)",
            "1. Log in using an account with a temporary password (MustChangePassword = true).\n2. Observe authentication flow.\n3. Verify ForceChangePasswordDialog opens modally, blocking access to MainWindow until password is updated.",
            'Login with temporary password: "TempPass123!"',
            "Security",
            "Verified: LoginPage checks user.MustChangePassword and displays ForceChangePasswordDialog."
        ),
        (
            "Authentication / Force Password Change",
            "Verify ForceChangePasswordDialog validates required fields and blocks submission when fields are empty. (Negative / Validation)",
            "1. In ForceChangePasswordDialog, leave Current Password, New Password, and Confirm Password blank.\n2. Click 'Update Password'.\n3. Verify error message appears: 'Please enter your current temporary password.'",
            'Leave password fields blank, Click \'Update Password\'',
            "Functional Suitability",
            "Verified: ForceChangePasswordDialog validates string.IsNullOrWhiteSpace(current) and shows error."
        ),
        (
            "Authentication / Force Password Change",
            "Verify ForceChangePasswordDialog validates new password complexity (8+ chars, upper, lower, number, special char). (Security / Negative)",
            "1. In Current Password, enter valid temporary password.\n2. In New Password and Confirm Password, enter weak password 'simplepass'.\n3. Click 'Update Password'.\n4. Verify error message: 'New password must be at least 8 characters and include uppercase, lowercase, a number, and a special character.'",
            'New Password: "simplepass", Confirm Password: "simplepass", Click \'Update Password\'',
            "Security",
            "Verified: ForceChangePasswordDialog checks length >= 8, char.IsUpper, char.IsLower, char.IsDigit, and special character."
        ),
        (
            "Authentication / Force Password Change",
            "Verify ForceChangePasswordDialog rejects new password when identical to current temporary password. (Security / Negative)",
            "1. Enter temporary password in Current Password.\n2. Enter the exact same password in New Password and Confirm Password.\n3. Click 'Update Password'.\n4. Verify error message: 'New password must be different from your current temporary password.'",
            'New Password identical to Current Password, Click \'Update Password\'',
            "Security",
            "Verified: Validates string.Equals(current, newPwd) and prevents password reuse."
        ),
        (
            "Authentication / Force Password Change",
            "Verify ForceChangePasswordDialog rejects mismatched New and Confirm passwords. (Negative / Validation)",
            "1. Enter valid temporary password in Current Password.\n2. In New Password, enter 'ValidPass123!'.\n3. In Confirm Password, enter 'DifferentPass999!'.\n4. Click 'Update Password'.\n5. Verify error message: 'Passwords do not match.'",
            'New: "ValidPass123!", Confirm: "DifferentPass999!", Click \'Update Password\'',
            "Functional Suitability",
            "Verified: Validates string.Equals(newPwd, confirm) and alerts user."
        ),
        (
            "Authentication / Force Password Change",
            "Verify password visibility toggles on ForceChangePasswordDialog for all three fields. (Usability)",
            "1. In ForceChangePasswordDialog, enter characters in Current, New, and Confirm password boxes.\n2. Click the eye toggle icon next to each field.\n3. Verify obscured bullets switch to plain text and toggle back when clicked again.",
            'Click eye toggle icons on PwdCurrent, PwdNew, PwdConfirm',
            "Usability",
            "Verified: ToggleCurrentVisibility_Click, ToggleNewVisibility_Click, and ToggleConfirmVisibility_Click switch visibility."
        ),
        (
            "Authentication / Force Password Change",
            "Verify successful password update in ForceChangePasswordDialog sets new credentials and advances to MainWindow. (Successful)",
            "1. In ForceChangePasswordDialog, enter valid Current Password.\n2. Enter compliant New Password ('NewSecurePassword2026!') and matching Confirm Password.\n3. Click 'Update Password'.\n4. Verify API updates password, dialog closes, and user enters MainWindow with active session.",
            'Current: "TempPass123!", New: "NewSecurePassword2026!", Confirm: "NewSecurePassword2026!", Click \'Update Password\'',
            "Functional Suitability",
            "Verified: AuthApiService.ChangePasswordAsync succeeds, MustChangePassword flag cleared, MainWindow opens."
        ),
        (
            "Authentication / Force Password Change",
            "Verify clicking Cancel in ForceChangePasswordDialog aborts sign-in and returns to LoginPage. (Usability)",
            "1. In ForceChangePasswordDialog, click the 'Cancel' button.\n2. Verify dialog closes, session is terminated, and user is returned to LoginPage without accessing the system.",
            'Click \'Cancel\' in ForceChangePasswordDialog',
            "Usability",
            "Verified: Cancel_Click sets DialogResult = false, closes dialog, and leaves user on LoginPage."
        )
    ]
