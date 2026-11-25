# -*- coding: utf-8 -*-

def get_user_admin_cases():
    return [
        (
            "User Administration / Access Control",
            "Verify Administrator successfully accesses User Administration tab and views user list. (Successful)",
            "1. Sign in as Administrator.\n2. Click the 'User Administration' tab icon in the sidebar.\n3. Verify page title updates to 'User Administration'.\n4. Verify user list, count badge, and toolbar controls are rendered.",
            "Administrator login, Click 'UserAdministration' NavButton",
            "Functional Suitability",
            "Verified: Enforces IsAdmin role check and loads UserAdministrationPage with user registry."
        ),
        (
            "User Administration / Role-Based Access Control",
            "Verify unauthorized access to User Administration module is blocked for Encoder accounts. (Security)",
            "1. Sign in as an Encoder.\n2. Inspect sidebar navigation.\n3. Verify 'User Administration' tab is completely collapsed/hidden.\n4. Verify Encoders cannot navigate to or access user administrative functions.",
            "Encoder session, Inspect sidebar navigation",
            "Security",
            "Verified: MainWindow code-behind enforces UserAdministration NavButton visibility only for Admin roles."
        ),
        (
            "User Administration / Role-Based Access Control",
            "Verify unauthorized access to User Administration is blocked for Barangay Secretary accounts. (Security)",
            "1. Sign in as a Barangay Secretary.\n2. Inspect sidebar navigation and attempt navigation.\n3. Verify 'User Administration' tab is hidden and inaccessible.",
            "Barangay Secretary session, Inspect navigation",
            "Security",
            "Verified: Barangay Secretary role hides administrative user management tools."
        ),
        (
            "User Administration / User List Display",
            "Verify user table renders user initials, full name, role badge, email, contact, and status badge accurately. (Functional Suitability)",
            "1. On User Administration page, inspect the users table.\n2. Verify initial avatar displays user's first letter.\n3. Verify role badges render with distinctive styling (Administrator: burgundy, Encoder: grey, Barangay Secretary: blue).\n4. Verify contact number and status badge (Active: green, Disabled: red) display correctly.",
            "Inspect UserList DataTemplate bindings",
            "Functional Suitability",
            "Verified: Data bindings render AppUser properties accurately in UserList ItemsControl."
        ),
        (
            "User Administration / Search by Name",
            "Verify dynamic filtering of user records by staff first or last name. (Functional Suitability)",
            "1. In the search box, type a staff member's first name (e.g., 'Maria').\n2. Verify the table dynamically filters to matching names in real time.\n3. Type a last name (e.g., 'Santos'); verify table filters accordingly.\n4. Verify user count badge reflects the filtered total.",
            "SearchText: 'Maria', then 'Santos'",
            "Functional Suitability",
            "Verified: SearchBox_TextChanged executes client-side filtering on _allUsers."
        ),
        (
            "User Administration / Search by Email",
            "Verify dynamic filtering of user records by staff email address. (Functional Suitability)",
            "1. In the search box, enter a specific user's email address (e.g., 'encoder@solace.gov.ph').\n2. Verify the user table filters to the specific account.\n3. Clear the search box; verify full user list restores immediately.",
            "SearchText: 'encoder@solace.gov.ph'",
            "Functional Suitability",
            "Verified: Case-insensitive email filtering matches user email substring."
        ),
        (
            "User Administration / Search Empty State",
            "Verify empty state notification when search query yields zero matching users. (Usability)",
            "1. In the search box, enter a non-existent user string (e.g., 'NonExistent999').\n2. Observe user list area.\n3. Verify empty state placeholder is shown and TxtUserCount displays '0'.",
            "SearchText: 'NonExistent999'",
            "Usability",
            "Verified: When filtered items count is 0, EmptyState panel becomes visible."
        ),
        (
            "User Administration / Filter Popup Toggle",
            "Verify clicking the filter button toggles the FilterPopup with Role and Status selectors. (Usability)",
            "1. Click the 'Filter' toggle button (BtnFilter) next to the search box.\n2. Verify FilterPopup opens showing Role filter (CmbRoleFilter) and Status filter (CmbUserStatus).\n3. Click outside or on BtnFilter again; verify popup dismisses.",
            "Click 'BtnFilter' button",
            "Usability",
            "Verified: BtnFilter_Click toggles FilterPopup.IsOpen."
        ),
        (
            "User Administration / Filter by Role",
            "Verify filtering user list by specific role (Encoder, Barangay Secretary, Administrator). (Functional Suitability)",
            "1. Open FilterPopup and select Role = 'Encoder'.\n2. Click 'Apply'.\n3. Verify table displays only Encoder accounts and active filter indicator dot appears.\n4. Select Role = 'Barangay Secretary'; verify only secretaries are shown.",
            "RoleFilter: 'Encoder', then 'BarangaySecretary', Click 'Apply'",
            "Functional Suitability",
            "Verified: FilterPopup filters list by Role tag and activates FilterActiveDot."
        ),
        (
            "User Administration / Filter by Status",
            "Verify filtering user list by account status (Active vs Disabled). (Functional Suitability)",
            "1. Open FilterPopup and select Status = 'Active'.\n2. Click 'Apply'; verify only active accounts are listed.\n3. Re-open filter and select Status = 'Disabled'.\n4. Click 'Apply'; verify only disabled accounts are listed.",
            "StatusFilter: 'Active', then 'Disabled', Click 'Apply'",
            "Functional Suitability",
            "Verified: FilterPopup applies boolean check on user.IsActive."
        ),
        (
            "User Administration / Filter by Status 'ALL'",
            "Verify 'ALL' status filter correctly queries and displays both active and disabled accounts. (Functional Suitability)",
            "1. Open FilterPopup and select Status = 'ALL'.\n2. Click 'Apply'.\n3. Verify both active and disabled users are rendered with their respective badges.",
            "StatusFilter: 'ALL', Click 'Apply'",
            "Functional Suitability",
            "Verified: Setting status to 'ALL' includes all accounts without defaulting disabled users to active."
        ),
        (
            "User Administration / Reset Filters",
            "Verify resetting filters restores the default unfiltered user list. (Usability)",
            "1. Apply custom role and status filters.\n2. Open FilterPopup and click 'Reset' (or clear selections).\n3. Verify table restores full user roster and filter indicator dot disappears.",
            "Click 'Reset' in FilterPopup",
            "Usability",
            "Verified: Clears filter criteria, hides FilterActiveDot, and reloads full user collection."
        ),
        (
            "User Administration / Pagination Navigation",
            "Verify navigating through user pages using Next, Previous, First, and Last page controls. (Usability)",
            "1. Ensure total users exceed page size.\n2. Click 'Next' (BtnNextPage); verify Page 2 loads and summary updates ('Showing X–Y of Z').\n3. Click 'Last' (BtnLastPage); verify final page renders and 'Next' is disabled.\n4. Click 'First' (BtnFirstPage); verify Page 1 reloads.",
            "Click BtnNextPage, BtnLastPage, BtnFirstPage, BtnPrevPage",
            "Usability",
            "Verified: Pagination logic calculates dynamic page counts and updates TxtUsersPageSummary."
        ),
        (
            "User Administration / Open Add User Panel",
            "Verify clicking '+ Add User' opens slide-out panel in create mode with blank fields and temporary password. (Usability)",
            "1. Click '+ Add User' button.\n2. Verify AddUserPanel expands on the right side of the screen.\n3. Verify panel title reads 'Add New User', button reads 'Create Account', and fields are blank.\n4. Verify a 10-character temporary password is automatically generated.",
            "Click '+ Add User' button",
            "Usability",
            "Verified: OpenAddUserPanel() initializes AddUserPanel, calls GenerateNewPassword(), and sets editMode = false."
        ),
        (
            "User Administration / Auto-Generated Password Visibility Toggle",
            "Verify toggling password visibility in Add User panel masks and reveals the generated password. (Usability)",
            "1. In Add User panel, inspect the generated temporary password box.\n2. Click the eye toggle button (BtnTogglePasswordMask).\n3. Verify plain text switches to bullet mask and icon updates.\n4. Click eye toggle again; verify plain text is restored.",
            "Click 'BtnTogglePasswordMask' twice",
            "Usability",
            "Verified: TogglePasswordMask_Click toggles visibility between TxtGeneratedPassword and PwdGeneratedPasswordMasked."
        ),
        (
            "User Administration / Auto-Generated Password Regeneration",
            "Verify clicking regenerate generates a new compliant temporary password and shows toast. (Functional Suitability)",
            "1. In Add User panel, note the current generated password.\n2. Click the regenerate button (BtnRegeneratePassword).\n3. Verify a different 10-character password is generated.\n4. Verify toast notification confirms 'Password Generated'.",
            "Click 'BtnRegeneratePassword'",
            "Functional Suitability",
            "Verified: RegeneratePassword_Click invokes PasswordGenerator.Generate(10) and shows info toast."
        ),
        (
            "User Administration / Auto-Generated Password Copy to Clipboard",
            "Verify clicking copy icon copies the temporary password to the Windows clipboard. (Usability)",
            "1. In Add User panel, click the copy button (BtnCopyPassword).\n2. Open Notepad and press Ctrl+V.\n3. Verify pasted text exactly matches the generated temporary password.\n4. Verify toast notification confirms 'Copied'.",
            "Click 'BtnCopyPassword' -> Paste in Notepad",
            "Usability",
            "Verified: CopyGeneratedPassword_Click calls Clipboard.SetText(_generatedPassword) and displays success toast."
        ),
        (
            "User Administration / Role Selection & Conditional Barangay Field",
            "Verify selecting 'Barangay Secretary' reveals Barangay dropdown while 'Encoder' keeps it hidden. (Functional Suitability)",
            "1. In Add User panel, select Role = 'Encoder'; verify PnlUserBarangay is hidden.\n2. Select Role = 'Barangay Secretary' (or Barangay President).\n3. Verify PnlUserBarangay expands showing CmbUserBarangay with 24 Biñan barangays.\n4. Select Role = 'Encoder' again; verify PnlUserBarangay collapses.",
            "Toggle Role between 'Encoder' and 'Barangay Secretary'",
            "Functional Suitability",
            "Verified: CmbRole_SelectionChanged dynamically toggles PnlUserBarangay.Visibility based on role tag."
        ),
        (
            "User Administration / Input Validation - First Name Required",
            "Verify inline error when First Name is left blank in Add User panel. (Negative / Validation)",
            "1. In Add User panel, leave First Name empty.\n2. Fill Last Name, Email, and Contact Number.\n3. Click 'Create Account'.\n4. Verify ErrFirstName displays 'Required.' and FormErrorBanner appears.",
            "First Name: \"\" (Empty), Click 'Create Account'",
            "Functional Suitability",
            "Verified: Validates string.IsNullOrWhiteSpace(firstName) and displays ErrFirstName.Text = 'Required.'."
        ),
        (
            "User Administration / Input Validation - First Name Spaces Only",
            "Verify inline error when First Name contains only spaces. (Negative / Validation)",
            "1. In First Name field, enter several spaces (e.g., '   ').\n2. Click 'Create Account'.\n3. Verify ErrFirstName displays 'Required.' and submission is blocked.",
            "First Name: \"   \" (Spaces only), Click 'Create Account'",
            "Functional Suitability",
            "Verified: IsNullOrWhiteSpace check flags spaces-only input as invalid."
        ),
        (
            "User Administration / Input Validation - First Name Leading Space",
            "Verify inline error when First Name begins with a leading space. (Negative / Validation)",
            "1. In First Name field, enter ' Maria' (with leading space).\n2. Click 'Create Account'.\n3. Verify ErrFirstName displays 'First name cannot start or end with a space.'",
            "First Name: \" Maria\", Click 'Create Account'",
            "Functional Suitability",
            "Verified: Checks TxtFirstName.Text.StartsWith(' ') and displays specific formatting error."
        ),
        (
            "User Administration / Input Validation - First Name Trailing Space",
            "Verify inline error when First Name ends with a trailing space. (Negative / Validation)",
            "1. In First Name field, enter 'Maria ' (with trailing space).\n2. Click 'Create Account'.\n3. Verify ErrFirstName displays 'First name cannot start or end with a space.'",
            "First Name: \"Maria \", Click 'Create Account'",
            "Functional Suitability",
            "Verified: Checks TxtFirstName.Text.EndsWith(' ') and displays specific formatting error."
        ),
        (
            "User Administration / Input Validation - First Name Numeric Keystroke Blocking",
            "Verify typing numbers into First Name field is blocked in real time. (Negative / Input Filtering)",
            "1. Focus First Name field.\n2. Attempt to type numbers '12345'.\n3. Verify keystrokes are blocked and text box remains empty.",
            "Keystroke input '12345' into TxtFirstName",
            "Reliability",
            "Verified: PreviewTextInput regex ^[a-zA-ZñÑ\\.\\s\\-]+$ blocks numeric characters."
        ),
        (
            "User Administration / Input Validation - First Name Special Character Blocking",
            "Verify typing special symbols into First Name field is blocked in real time. (Negative / Input Filtering)",
            "1. Focus First Name field.\n2. Attempt to type symbols '!@#$%^&*()_+='. \n3. Verify keystrokes are suppressed and not entered into field.",
            "Keystroke input '!@#$%' into TxtFirstName",
            "Reliability",
            "Verified: Disallowed symbols are rejected by PreviewTextInput filter."
        ),
        (
            "User Administration / Input Validation - First Name Allowed Characters",
            "Verify First Name accepts letters, hyphens, spaces, and periods (e.g. 'Ma. Teresa'). (Functional Suitability)",
            "1. In First Name field, enter 'Ma. Teresa-Anne'.\n2. Verify text is accepted without error.",
            "First Name: 'Ma. Teresa-Anne'",
            "Functional Suitability",
            "Verified: Regex allows periods, hyphens, and spaces between letters."
        ),
        (
            "User Administration / Input Validation - Last Name Required",
            "Verify inline error when Last Name is left blank in Add User panel. (Negative / Validation)",
            "1. In Add User panel, leave Last Name empty.\n2. Fill other required fields.\n3. Click 'Create Account'.\n4. Verify ErrLastName displays 'Required.' and FormErrorBanner is visible.",
            "Last Name: \"\" (Empty), Click 'Create Account'",
            "Functional Suitability",
            "Verified: string.IsNullOrWhiteSpace(lastName) flags required error on ErrLastName."
        ),
        (
            "User Administration / Input Validation - Last Name Leading / Trailing Space",
            "Verify inline error when Last Name starts or ends with a space. (Negative / Validation)",
            "1. In Last Name field, enter ' Santos ' (surrounded by spaces).\n2. Click 'Create Account'.\n3. Verify ErrLastName displays 'Last name cannot start or end with a space.'",
            "Last Name: \" Santos \", Click 'Create Account'",
            "Functional Suitability",
            "Verified: Validates StartsWith(' ') and EndsWith(' ') on TxtLastName.Text."
        ),
        (
            "User Administration / Input Validation - Last Name Numeric Blocking",
            "Verify numbers typed into Last Name field are blocked in real time. (Negative / Input Filtering)",
            "1. In Last Name field, attempt to type 'DelaCruz123'.\n2. Verify '123' is rejected and only 'DelaCruz' is typed.",
            "Keystroke 'DelaCruz123' into TxtLastName",
            "Reliability",
            "Verified: PreviewTextInput filter suppresses numeric characters."
        ),
        (
            "User Administration / Input Validation - Last Name Allowed Characters",
            "Verify Last Name accepts compound names with spaces, periods, and hyphens (e.g. 'De la Cruz Jr.'). (Functional Suitability)",
            "1. In Last Name field, enter 'De la Cruz Jr.'.\n2. Verify input is accepted without triggering format error.",
            "Last Name: 'De la Cruz Jr.'",
            "Functional Suitability",
            "Verified: Allowed name regex matches compound surnames."
        ),
        (
            "User Administration / Input Validation - Contact Number Required",
            "Verify inline error when Contact Number is left blank in Add User panel. (Negative / Validation)",
            "1. Leave Contact Number field blank.\n2. Fill names and email.\n3. Click 'Create Account'.\n4. Verify ErrContactNumber displays 'Required.'",
            "Contact Number: \"\" (Empty), Click 'Create Account'",
            "Functional Suitability",
            "Verified: IsNullOrWhiteSpace check triggers ErrContactNumber.Text = 'Required.'."
        ),
        (
            "User Administration / Input Validation - Contact Number Length (< 11 Digits)",
            "Verify inline error when Contact Number has fewer than 11 digits. (Negative / Validation)",
            "1. In Contact Number field, enter 10 digits '0917123456'.\n2. Click 'Create Account'.\n3. Verify ErrContactNumber displays 'Must be 11 digits.'",
            "Contact Number: '0917123456' (10 digits), Click 'Create Account'",
            "Functional Suitability",
            "Verified: Validates contactDigits.Length != 11 and displays 'Must be 11 digits.'."
        ),
        (
            "User Administration / Input Validation - Contact Number Prefix (Must start with 09)",
            "Verify inline error when Contact Number does not start with '09'. (Negative / Validation)",
            "1. In Contact Number field, enter 11 digits starting with 08 (e.g., '08123456789').\n2. Click 'Create Account'.\n3. Verify ErrContactNumber displays 'Must start with 09.'",
            "Contact Number: '08123456789', Click 'Create Account'",
            "Functional Suitability",
            "Verified: Validates !contactDigits.StartsWith('09') and displays 'Must start with 09.'."
        ),
        (
            "User Administration / Input Validation - Contact Number Keystroke Filtering",
            "Verify typing alphabetic characters into Contact Number is blocked in real time. (Negative / Input Filtering)",
            "1. Focus Contact Number field.\n2. Attempt to type letters 'abc' or symbols '!@#'.\n3. Verify keystrokes are blocked.",
            "Keystroke 'abc!@#' in TxtContactNumber",
            "Reliability",
            "Verified: PreviewTextInput validates ^\\d+$ or ^\\+63 pattern."
        ),
        (
            "User Administration / Input Validation - Contact Number Max Length (11 Digits for 09)",
            "Verify typing stops at 11 digits when entering a standard 09 phone number. (Boundary)",
            "1. In Contact Number field, type '09171234567'.\n2. Attempt to type a 12th digit '8'.\n3. Verify the 12th digit is blocked and field remains at 11 digits.",
            "Type 12 digits: '091712345678'",
            "Functional Suitability",
            "Verified: Resulting length > 11 is rejected for 09 prefix."
        ),
        (
            "User Administration / Input Validation - Contact Number +63 Country Code Format",
            "Verify Contact Number allows +63 international prefix up to 13 characters. (Functional Suitability)",
            "1. In Contact Number field, enter '+639171234567' (13 characters).\n2. Verify the number is accepted without triggering format error.",
            "Contact Number: '+639171234567'",
            "Functional Suitability",
            "Verified: Validates ^\\+63\\d{0,10}$ format up to 13 characters."
        ),
        (
            "User Administration / Input Validation - Contact Number Paste Filtering",
            "Verify pasting text with dashes or letters into Contact Number strips non-digits and enforces prefix. (Input Sanitization)",
            "1. Copy '0917-123-4567' to clipboard.\n2. Paste into Contact Number field.\n3. Verify input is formatted cleanly and accepted.",
            "Paste: '0917-123-4567'",
            "Functional Suitability",
            "Verified: Pasting handler extracts digits and verifies 11-digit 09 structure."
        ),
        (
            "User Administration / Input Validation - Email Required",
            "Verify inline error when Email Address is left blank in Add User panel. (Negative / Validation)",
            "1. In Add User panel, leave Email Address empty.\n2. Fill names and contact number.\n3. Click 'Create Account'.\n4. Verify ErrEmail displays 'Required.' and submission is blocked.",
            "Email: \"\" (Empty), Click 'Create Account'",
            "Functional Suitability",
            "Verified: string.IsNullOrWhiteSpace(email) sets ErrEmail.Text = 'Required.'."
        ),
        (
            "User Administration / Input Validation - Email Missing '@'",
            "Verify inline error when Email Address lacks '@' character. (Negative / Validation)",
            "1. In Email Address field, enter 'user.solace.gov.ph'.\n2. Click 'Create Account'.\n3. Verify ErrEmail displays 'Enter a valid email.'",
            "Email: 'user.solace.gov.ph', Click 'Create Account'",
            "Functional Suitability",
            "Verified: Regex ^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$ rejects missing '@'."
        ),
        (
            "User Administration / Input Validation - Email Missing Domain Syntax",
            "Verify inline error when Email Address lacks valid domain syntax. (Negative / Validation)",
            "1. In Email Address field, enter 'user@solace'.\n2. Click 'Create Account'.\n3. Verify ErrEmail displays 'Enter a valid email.'",
            "Email: 'user@solace', Click 'Create Account'",
            "Functional Suitability",
            "Verified: Regex requires top-level domain component."
        ),
        (
            "User Administration / Input Validation - Duplicate Email Detection",
            "Verify rejection and inline prompt when entering an already registered email address. (Negative / Validation)",
            "1. In Email Address field, enter an email that belongs to an existing user.\n2. Fill all other fields.\n3. Click 'Create Account'.\n4. Verify ErrEmail displays 'Email already registered.' and API call is blocked.",
            "Email: existing active user email, Click 'Create Account'",
            "Functional Suitability",
            "Verified: _allUsers.Exists(u => u.Email.ToLower() == email.ToLower()) blocks duplicate registration."
        ),
        (
            "User Administration / Input Validation - Barangay Required for Barangay Secretary",
            "Verify inline error when selecting Barangay Secretary role but leaving Barangay unselected. (Negative / Validation)",
            "1. In Add User panel, select Role = 'Barangay Secretary'.\n2. Leave Barangay dropdown at 'Select Barangay...'.\n3. Click 'Create Account'.\n4. Verify ErrUserBarangay displays 'Barangay is required for Barangay Secretary.'",
            "Role: 'Barangay Secretary', Barangay unselected, Click 'Create Account'",
            "Functional Suitability",
            "Verified: Validates brgy selection for BarangaySecretary role tag."
        ),
        (
            "User Administration / Form Error Banner",
            "Verify FormErrorBanner is displayed when form contains any validation errors. (Usability)",
            "1. In Add User panel, leave mandatory fields blank.\n2. Click 'Create Account'.\n3. Verify FormErrorBanner is visible with text 'Please fix the errors above.' and warning toast is displayed.",
            "Blank mandatory fields, Click 'Create Account'",
            "Usability",
            "Verified: FormErrorBanner.Visibility = Visible and ToastNotification.Show('Incomplete Form') fires."
        ),
        (
            "User Administration / Real-Time Error Clearance",
            "Verify typing valid data clears inline error messages in Add User panel. (Usability)",
            "1. Trigger validation errors on First Name, Last Name, Email, and Contact Number.\n2. Type into any field.\n3. Verify FormErrorBanner and specific field error labels collapse.",
            "Trigger errors -> Type into fields",
            "Usability",
            "Verified: TextChanged event handlers invoke ClearErrors to hide error labels."
        ),
        (
            "User Administration / Confirmation Dialog on Create",
            "Verify clicking 'Create Account' with valid inputs opens ConfirmCreateUserDialog modal. (Usability)",
            "1. Fill all fields with valid data (First Name: 'Elena', Last Name: 'Reyes', Role: 'Encoder', Contact: '09171234567', Email: 'elena.reyes@solace.gov.ph').\n2. Click 'Create Account'.\n3. Verify ConfirmCreateUserDialog opens displaying full name, role, email, and temporary password.",
            "Valid user inputs, Click 'Create Account'",
            "Usability",
            "Verified: Opens ConfirmCreateUserDialog with ConfirmCreateUserDialog.Show modal."
        ),
        (
            "User Administration / Cancel in Confirmation Dialog",
            "Verify clicking Cancel in ConfirmCreateUserDialog aborts user creation and returns to form. (Usability)",
            "1. In ConfirmCreateUserDialog, click 'Cancel' (or close button).\n2. Verify dialog closes, no user is created, and Add User panel remains open with entered data intact.",
            "Click 'Cancel' in ConfirmCreateUserDialog",
            "Usability",
            "Verified: ConfirmCreateUserDialog returns false; AddUserPanel state is preserved."
        ),
        (
            "User Administration / Successful User Creation & Credentials Summary",
            "Verify confirming in ConfirmCreateUserDialog commits user to database and opens AccountCreatedDialog. (Successful)",
            "1. In ConfirmCreateUserDialog, click 'Create Account' to confirm.\n2. Verify system calls UserApiService.CreateApplicationUserAsync.\n3. Verify AccountCreatedDialog opens showing created account details and copy option.\n4. Verify newly created user appears in the user list and success toast displays.",
            "Confirm create in ConfirmCreateUserDialog",
            "Functional Suitability",
            "Verified: API persists user, AccountCreatedDialog opens, and AuditLogService records 'REGISTER_USER'."
        ),
        (
            "User Administration / AccountCreatedDialog Copy Credentials",
            "Verify clicking 'Copy Credentials' in AccountCreatedDialog copies email and password to clipboard. (Usability)",
            "1. In AccountCreatedDialog, click 'Copy Credentials' button.\n2. Verify toast confirms credentials copied.\n3. Paste into text editor; verify email and temporary password are formatted clearly.",
            "Click 'Copy Credentials' in AccountCreatedDialog",
            "Usability",
            "Verified: Formats email and password string and sets to Windows Clipboard."
        ),
        (
            "User Administration / Open Edit User Panel",
            "Verify clicking edit button on a user row opens side panel in edit mode pre-populated with user data. (Successful)",
            "1. Locate a user row and click the pencil edit button.\n2. Verify side panel expands with title 'Edit User' and button 'Save Changes'.\n3. Verify First Name, Last Name, Contact Number, Role, and Status are pre-populated with user data.\n4. Verify Email Address is locked and read-only.",
            "Click Edit button on user row",
            "Usability",
            "Verified: OpenEditUserPanel() sets _editTarget, populates inputs, and locks TxtEmail.IsReadOnly = true."
        ),
        (
            "User Administration / Email Read-Only Enforcement in Edit Mode",
            "Verify Email Address field cannot be modified when editing an existing user. (Security)",
            "1. In Edit User panel, attempt to click and type into the Email Address box.\n2. Verify field is read-only, cursor does not insert text, and field styling indicates disabled state.",
            "Attempt text input in TxtEmail during edit mode",
            "Security",
            "Verified: TxtEmail.IsReadOnly = true prevents modification of registered email identifier."
        ),
        (
            "User Administration / Edit User Name Update",
            "Verify updating First Name or Last Name in Edit User panel updates user and saves to API. (Successful)",
            "1. In Edit User panel, change First Name to 'Elena Maria'.\n2. Click 'Save Changes'.\n3. Verify API receives update, panel collapses, toast confirms 'User Updated', and table displays updated name.",
            "Modified First Name: 'Elena Maria', Click 'Save Changes'",
            "Functional Suitability",
            "Verified: UserApiService.UpdateApplicationUserAsync saves modified names and refreshes list."
        ),
        (
            "User Administration / Edit User Contact Number Update",
            "Verify updating Contact Number in Edit User panel validates and saves changes. (Successful)",
            "1. In Edit User panel, change Contact Number to '09189998877'.\n2. Click 'Save Changes'.\n3. Verify validation passes and updated phone number displays in table row.",
            "Modified Contact: '09189998877', Click 'Save Changes'",
            "Functional Suitability",
            "Verified: Validates contact format and persists updated contact via API."
        ),
        (
            "User Administration / Edit User Status Dropdown",
            "Verify updating user status between Active and Disabled via Edit side panel dropdown. (Functional Suitability)",
            "1. In Edit User panel, change Status dropdown from 'Active' to 'Disabled'.\n2. Click 'Save Changes'.\n3. Verify user status updates to Disabled on API and badge updates in table.",
            "Change Status to 'Disabled', Click 'Save Changes'",
            "Functional Suitability",
            "Verified: Selected status is sent in update request and reflected in user table."
        ),
        (
            "User Administration / Reset User Password from Edit Panel",
            "Verify Administrator can trigger temporary password reset for existing user from Edit User panel. (Security)",
            "1. In Edit User panel, locate 'Password Reset' section (PnlEditPasswordReset).\n2. Click 'Reset User Password' button.\n3. Verify ActionConfirmDialog opens warning that existing password will be overwritten.\n4. Click 'Reset Password'; verify a new temporary password is generated and AccountCreatedDialog displays it.",
            "Click 'Reset User Password' in Edit User panel -> Confirm",
            "Security",
            "Verified: UserApiService.ResetUserPasswordAsync generates new temporary password and sets MustChangePassword = true."
        ),
        (
            "User Administration / Disable User via Quick Action Button",
            "Verify clicking Disable action button on an active user row opens warning dialog and disables user. (Functional Suitability)",
            "1. In user table, locate an Active user row.\n2. Click the 'Disable' quick action button.\n3. Verify ActionConfirmDialog appears with Warning theme (amber/orange).\n4. Click 'Disable' in dialog.\n5. Verify toast confirms account disabled, status badge turns red ('Disabled'), and audit log is created.",
            "Click 'Disable' on user row -> Confirm in ActionConfirmDialog",
            "Functional Suitability",
            "Verified: UserApiService.UpdateUserStatusAsync sets IsActive = false; UI updates badge immediately."
        ),
        (
            "User Administration / Cancel Disable User Action",
            "Verify clicking Cancel in Disable User dialog aborts status change without modifying account. (Reliability)",
            "1. Click 'Disable' on an active user row.\n2. In ActionConfirmDialog, click 'Cancel'.\n3. Verify dialog closes, no API call is made, and user status remains 'Active'.",
            "Click 'Disable' -> Click 'Cancel' in ActionConfirmDialog",
            "Reliability",
            "Verified: Aborting confirmation preserves user active state."
        ),
        (
            "User Administration / Re-enable User via Quick Action Button",
            "Verify clicking Re-enable action button on a disabled user row opens success dialog and re-activates user. (Functional Suitability)",
            "1. In user table, locate a Disabled user row.\n2. Click the 'Re-enable' action button.\n3. Verify ActionConfirmDialog appears with Success theme (green).\n4. Click 'Re-enable' in dialog.\n5. Verify toast confirms account activated, status badge turns green ('Active'), and audit log is created.",
            "Click 'Re-enable' on disabled user -> Confirm in ActionConfirmDialog",
            "Functional Suitability",
            "Verified: UserApiService.UpdateUserStatusAsync sets IsActive = true."
        ),
        (
            "User Administration / Prevent Disabling Logged-in Administrator Account",
            "Verify the logged-in administrator cannot disable their own account. (Security)",
            "1. Locate the row corresponding to the currently logged-in Admin account.\n2. Verify the 'Disable' button is hidden or disabled to prevent self-lockout.",
            "Inspect action buttons on current Admin user row",
            "Security",
            "Verified: Disallow self-disabling to guarantee administrative continuity."
        ),
        (
            "User Administration / User Audit Logs Drawer",
            "Verify clicking Audit Logs button in toolbar opens user administration activity log viewer. (Functional Suitability)",
            "1. On User Administration page, click the 'Audit Logs' button (BtnAuditLogs) in the toolbar.\n2. Verify user activity drawer opens displaying registration, update, status change, and password reset events.",
            "Click 'BtnAuditLogs' button",
            "Functional Suitability",
            "Verified: Opens user administration audit log drawer with filtered USER event stream."
        ),
        (
            "User Administration / Close Add/Edit User Panel",
            "Verify clicking '✕' or 'Cancel' closes panel and clears entered data and validation errors. (Usability)",
            "1. Open Add User panel and type invalid data to trigger errors.\n2. Click '✕' close button or 'Cancel'.\n3. Verify panel slides closed (Width = 0).\n4. Re-open panel; verify form is reset with blank fields and errors cleared.",
            "Click 'ClosePanel_Click' button or 'Cancel'",
            "Usability",
            "Verified: ClosePanel_Click resets _editTarget, clears fields, and hides error highlights."
        ),
        (
            "User Administration / Dynamic Page Size Resizing",
            "Verify user table adjusts page size dynamically based on window height to prevent unnecessary scrollbars. (Usability)",
            "1. Resize window height from standard to maximized.\n2. Verify UserList debounces resize and recalculates dynamic page size.\n3. Verify table displays optimal number of rows for available viewport.",
            "Resize application window vertically",
            "Usability",
            "Verified: UpdateUserDynamicPageSize calculates visible capacity and updates page size."
        ),
        (
            "User Administration / Temporary Password Badge Display",
            "Verify users with MustChangePassword flag display 'Temporary Password' badge in table. (Visual Verification)",
            "1. Locate a user created with a temporary password or whose password was reset.\n2. Inspect the password status column.\n3. Verify amber 'Temporary Password' badge is visible with explanatory tooltip.",
            "Inspect PasswordStatusVisibility and PasswordStatusText bindings",
            "Functional Suitability",
            "Verified: PasswordStatusVisibility displays amber badge when MustChangePassword is true."
        ),
        (
            "User Administration / Network Error Handling on User Load",
            "Verify friendly error message and retry option when user list fails to load from server. (Reliability)",
            "1. Disconnect network or stop API service.\n2. Navigate to User Administration.\n3. Verify error notification is displayed without application crash.\n4. Reconnect network and refresh; verify users load properly.",
            "Simulate API offline during User Administration load",
            "Reliability",
            "Verified: Catches exception, shows user-friendly error notice, and maintains stability."
        ),
        (
            "User Administration / Server Rejection on Create Handling",
            "Verify error banner displays backend error message when user creation fails on the server. (Reliability)",
            "1. In Add User panel, submit valid fields.\n2. Simulate server-side error (e.g. database constraint error).\n3. Verify FormErrorBanner displays the exact server error message and panel remains open for correction.",
            "Simulated server-side create failure",
            "Reliability",
            "Verified: FormErrorBanner.Text binds to response.Errors and displays clear server feedback."
        ),
        (
            "User Administration / Audit Logging on All Staff Mutations",
            "Verify creating, updating, disabling, re-enabling, or resetting password creates audit trail record. (Security)",
            "1. Perform any user management action (e.g., create, update, or toggle status).\n2. Navigate to Audit Logs.\n3. Verify corresponding event is logged with actor name, role, target email, and timestamp.",
            "Perform user mutation -> Inspect Audit Logs",
            "Security",
            "Verified: AuditLogService.LogUserAdmin records audit telemetry for all staff operations."
        ),
        (
            "User Administration / SuperAdmin Role Visibility",
            "Verify SuperAdmin role filters and privileges are only visible when authenticated as SuperAdmin. (Security)",
            "1. Log in as standard Administrator.\n2. Open FilterPopup; verify 'SuperAdmin' role filter option is collapsed.\n3. Log in as SuperAdmin; verify 'SuperAdmin' role options become visible.",
            "Standard Admin session vs SuperAdmin session",
            "Security",
            "Verified: CmbFilterRoleSuperAdmin.Visibility is conditioned on MainWindow.CurrentUserRole == 'SuperAdmin'."
        )
    ]
