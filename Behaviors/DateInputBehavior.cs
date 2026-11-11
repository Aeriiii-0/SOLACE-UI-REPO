using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SOLUM_UI.Behaviors
{
    /// <summary>
    /// Attached behavior that automatically formats date input with '/' separators as the user types.
    /// Supports MM/DD/YYYY format. Attach to TextBox.
    /// 
    /// Usage in XAML:
    /// xmlns:behaviors="clr-namespace:SOLUM_UI.Behaviors"
    /// &lt;TextBox behaviors:DateInputBehavior.IsEnabled="True" /&gt;
    /// </summary>
    public static class DateInputBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(DateInputBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEnabledProperty, value);
        }

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                if ((bool)e.NewValue)
                {
                    textBox.PreviewTextInput += OnPreviewTextInput;
                    textBox.PreviewKeyDown += OnPreviewKeyDown;
                    textBox.TextChanged += OnTextChanged;
                }
                else
                {
                    textBox.PreviewTextInput -= OnPreviewTextInput;
                    textBox.PreviewKeyDown -= OnPreviewKeyDown;
                    textBox.TextChanged -= OnTextChanged;
                }
            }
        }

        private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            // Only allow digits
            if (!System.Text.RegularExpressions.Regex.IsMatch(e.Text, "^[0-9]+$"))
            {
                e.Handled = true;
                return;
            }

            // Get current text without slashes
            string currentText = textBox.Text.Replace("/", "");
            int cursorPos = textBox.SelectionStart;
            
            // Count how many digits are before cursor (excluding slashes)
            int digitsBeforeCursor = 0;
            for (int i = 0; i < cursorPos && i < textBox.Text.Length; i++)
            {
                if (char.IsDigit(textBox.Text[i]))
                    digitsBeforeCursor++;
            }

            // Check if we're at max length (8 digits total for MM/DD/YYYY)
            if (currentText.Length >= 8 && textBox.SelectionLength == 0)
            {
                e.Handled = true;
                return;
            }

            // Insert the new digit
            string newText = currentText.Insert(digitsBeforeCursor, e.Text);
            
            // Format with slashes
            string formatted = FormatDate(newText);
            
            // Calculate new cursor position
            int newCursorPos = CalculateCursorPosition(formatted, digitsBeforeCursor + 1);

            // Update textbox
            textBox.Text = formatted;
            textBox.SelectionStart = newCursorPos;
            
            e.Handled = true;
        }

        private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            if (e.Key == Key.Back)
            {
                if (textBox.SelectionLength > 0)
                {
                    // Selection exists, let default behavior handle it
                    return;
                }

                int cursorPos = textBox.SelectionStart;
                if (cursorPos == 0)
                {
                    e.Handled = true;
                    return;
                }

                // If cursor is right after a slash, move back one more to delete the digit
                if (cursorPos > 0 && cursorPos <= textBox.Text.Length && textBox.Text[cursorPos - 1] == '/')
                {
                    cursorPos--;
                }

                if (cursorPos == 0)
                {
                    e.Handled = true;
                    return;
                }

                // Remove the digit at cursor-1
                string currentText = textBox.Text.Replace("/", "");
                
                // Count digits before cursor
                int digitsBeforeCursor = 0;
                for (int i = 0; i < textBox.SelectionStart && i < textBox.Text.Length; i++)
                {
                    if (char.IsDigit(textBox.Text[i]))
                        digitsBeforeCursor++;
                }

                if (digitsBeforeCursor > 0)
                {
                    currentText = currentText.Remove(digitsBeforeCursor - 1, 1);
                    string formatted = FormatDate(currentText);
                    int newCursorPos = CalculateCursorPosition(formatted, digitsBeforeCursor - 1);
                    
                    textBox.Text = formatted;
                    textBox.SelectionStart = newCursorPos;
                }

                e.Handled = true;
            }
            else if (e.Key == Key.Delete)
            {
                if (textBox.SelectionLength > 0)
                {
                    // Selection exists, let default behavior handle it after formatting
                    return;
                }

                int cursorPos = textBox.SelectionStart;
                if (cursorPos >= textBox.Text.Length)
                {
                    e.Handled = true;
                    return;
                }

                // Remove digit at cursor position
                string currentText = textBox.Text.Replace("/", "");
                
                // Count digits before cursor
                int digitsBeforeCursor = 0;
                for (int i = 0; i < cursorPos && i < textBox.Text.Length; i++)
                {
                    if (char.IsDigit(textBox.Text[i]))
                        digitsBeforeCursor++;
                }

                if (digitsBeforeCursor < currentText.Length)
                {
                    currentText = currentText.Remove(digitsBeforeCursor, 1);
                    string formatted = FormatDate(currentText);
                    int newCursorPos = CalculateCursorPosition(formatted, digitsBeforeCursor);
                    
                    textBox.Text = formatted;
                    textBox.SelectionStart = newCursorPos;
                }

                e.Handled = true;
            }
        }

        private static void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            // This handles paste operations
            var textBox = sender as TextBox;
            if (textBox == null) return;

            // Check if this is a paste operation (multiple characters changed at once)
            if (e.Changes.Count == 1)
            {
                var change = System.Linq.Enumerable.First(e.Changes);
                if (change.AddedLength > 1)
                {
                    // Paste detected - format it
                    textBox.TextChanged -= OnTextChanged;
                    
                    string digitsOnly = System.Text.RegularExpressions.Regex.Replace(textBox.Text, "[^0-9]", "");
                    if (digitsOnly.Length > 8)
                        digitsOnly = digitsOnly.Substring(0, 8);
                    
                    string formatted = FormatDate(digitsOnly);
                    int cursorPos = formatted.Length;
                    
                    textBox.Text = formatted;
                    textBox.SelectionStart = cursorPos;
                    
                    textBox.TextChanged += OnTextChanged;
                }
            }
        }

        private static string FormatDate(string digitsOnly)
        {
            if (string.IsNullOrEmpty(digitsOnly))
                return "";

            // Limit to 8 digits
            if (digitsOnly.Length > 8)
                digitsOnly = digitsOnly.Substring(0, 8);

            // Format as MM/DD/YYYY
            string result = "";
            for (int i = 0; i < digitsOnly.Length; i++)
            {
                result += digitsOnly[i];
                
                // Add slash after MM (position 2) and after DD (position 4)
                if (i == 1 && digitsOnly.Length > 2)
                    result += "/";
                else if (i == 3 && digitsOnly.Length > 4)
                    result += "/";
            }

            return result;
        }

        private static int CalculateCursorPosition(string formattedText, int digitIndex)
        {
            // Find the position in formatted text that corresponds to the digit at digitIndex
            int digitsFound = 0;
            for (int i = 0; i < formattedText.Length; i++)
            {
                if (char.IsDigit(formattedText[i]))
                {
                    digitsFound++;
                    if (digitsFound == digitIndex)
                        return i + 1; // Position after this digit
                }
            }
            return formattedText.Length;
        }
    }
}
