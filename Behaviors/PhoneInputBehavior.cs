using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SOLUM_UI.Behaviors
{
    /// <summary>
    /// Behavior that formats phone numbers as 09XX-XXX-XXXX and limits to 11 digits
    /// </summary>
    public static class PhoneInputBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(PhoneInputBehavior),
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
                    textBox.TextChanged += OnTextChanged;
                    DataObject.AddPastingHandler(textBox, OnPaste);
                    textBox.PreviewKeyDown += OnPreviewKeyDown;
                }
                else
                {
                    textBox.PreviewTextInput -= OnPreviewTextInput;
                    textBox.TextChanged -= OnTextChanged;
                    DataObject.RemovePastingHandler(textBox, OnPaste);
                    textBox.PreviewKeyDown -= OnPreviewKeyDown;
                }
            }
        }

        private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            // Only allow digits
            if (!Regex.IsMatch(e.Text, @"^\d+$"))
            {
                e.Handled = true;
                return;
            }

            // Get current text without dashes
            string currentDigits = GetDigitsOnly(textBox.Text);
            
            // Limit to 11 digits
            if (currentDigits.Length >= 11)
            {
                e.Handled = true;
                return;
            }
        }

        private static void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            // Prevent recursive calls
            if (textBox.Tag is bool processing && processing)
                return;

            textBox.Tag = true;

            try
            {
                int originalCaretPosition = textBox.CaretIndex;
                string originalText = textBox.Text;
                
                // Extract digits only
                string digits = GetDigitsOnly(originalText);
                
                // Limit to 11 digits
                if (digits.Length > 11)
                    digits = digits.Substring(0, 11);

                // Format with dashes
                string formatted = FormatPhoneNumber(digits);
                
                if (originalText != formatted)
                {
                    textBox.Text = formatted;
                    
                    // Calculate new caret position
                    int newCaretPosition = CalculateCaretPosition(originalText, formatted, originalCaretPosition);
                    textBox.CaretIndex = newCaretPosition;
                }
            }
            finally
            {
                textBox.Tag = false;
            }
        }

        private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            // Handle backspace and delete
            if (e.Key == Key.Back || e.Key == Key.Delete)
            {
                int caretPos = textBox.CaretIndex;
                string text = textBox.Text;

                // If cursor is on a dash, move back one position
                if (e.Key == Key.Back && caretPos > 0 && caretPos <= text.Length)
                {
                    if (text[caretPos - 1] == '-')
                    {
                        textBox.CaretIndex = caretPos - 1;
                    }
                }
                else if (e.Key == Key.Delete && caretPos < text.Length)
                {
                    if (text[caretPos] == '-')
                    {
                        // Remove the digit after the dash instead
                        if (caretPos + 1 < text.Length)
                        {
                            string digits = GetDigitsOnly(text);
                            int digitIndex = GetDigitIndexAtPosition(text, caretPos + 1);
                            
                            if (digitIndex >= 0 && digitIndex < digits.Length)
                            {
                                digits = digits.Remove(digitIndex, 1);
                                textBox.Tag = true;
                                textBox.Text = FormatPhoneNumber(digits);
                                textBox.CaretIndex = caretPos;
                                textBox.Tag = false;
                                e.Handled = true;
                            }
                        }
                    }
                }
            }
        }

        private static void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string pastedText = (string)e.DataObject.GetData(typeof(string));
                string digits = GetDigitsOnly(pastedText);
                
                if (digits.Length > 11)
                    digits = digits.Substring(0, 11);

                textBox.Tag = true;
                textBox.Text = FormatPhoneNumber(digits);
                textBox.CaretIndex = textBox.Text.Length;
                textBox.Tag = false;

                e.CancelCommand();
            }
        }

        private static string GetDigitsOnly(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            return Regex.Replace(input, @"[^\d]", "");
        }

        private static string FormatPhoneNumber(string digits)
        {
            if (string.IsNullOrEmpty(digits))
                return string.Empty;

            // Format: 09XX-XXX-XXXX
            if (digits.Length <= 4)
            {
                return digits;
            }
            else if (digits.Length <= 7)
            {
                return $"{digits.Substring(0, 4)}-{digits.Substring(4)}";
            }
            else
            {
                int middleLength = digits.Length - 7;
                return $"{digits.Substring(0, 4)}-{digits.Substring(4, 3)}-{digits.Substring(7)}";
            }
        }

        private static int CalculateCaretPosition(string oldText, string newText, int oldCaretPos)
        {
            // Count digits before caret in old text
            int digitsBefore = 0;
            for (int i = 0; i < oldCaretPos && i < oldText.Length; i++)
            {
                if (char.IsDigit(oldText[i]))
                    digitsBefore++;
            }

            // Find position in new text with same number of digits before
            int digitsCount = 0;
            for (int i = 0; i < newText.Length; i++)
            {
                if (char.IsDigit(newText[i]))
                {
                    digitsCount++;
                    if (digitsCount >= digitsBefore)
                    {
                        return i + 1;
                    }
                }
            }

            return newText.Length;
        }

        private static int GetDigitIndexAtPosition(string text, int position)
        {
            int digitIndex = 0;
            for (int i = 0; i < position && i < text.Length; i++)
            {
                if (char.IsDigit(text[i]))
                    digitIndex++;
            }
            return digitIndex;
        }
    }
}
