using System.Windows.Controls;

namespace SOLUM_UI.Services
{
    /// <summary>
    /// Shared helper methods for UI form controls and data binding.
    /// </summary>
    public static class FormHelper
    {
        /// <summary>
        /// Binds the "Same as Applicant" CheckBox to synchronize the applicant's address into the emergency contact address.
        /// When checked: copies the applicant address into the emergency contact address and disables manual typing.
        /// While checked: any edits made in the applicant address are immediately mirrored into the emergency contact address.
        /// When unchecked: re-enables manual typing for the emergency contact address.
        /// </summary>
        public static void BindSameAsApplicantAddress(
            CheckBox chkSame,
            TextBox txtApplicantAddress,
            TextBox txtEmergencyAddress)
        {
            if (chkSame == null || txtApplicantAddress == null || txtEmergencyAddress == null) return;

            chkSame.Checked += (s, e) =>
            {
                txtEmergencyAddress.Text = txtApplicantAddress.Text ?? string.Empty;
                txtEmergencyAddress.IsEnabled = false;
            };

            chkSame.Unchecked += (s, e) =>
            {
                txtEmergencyAddress.IsEnabled = true;
            };

            txtApplicantAddress.TextChanged += (s, e) =>
            {
                if (chkSame.IsChecked == true)
                {
                    txtEmergencyAddress.Text = txtApplicantAddress.Text ?? string.Empty;
                }
            };
        }
    }
}
