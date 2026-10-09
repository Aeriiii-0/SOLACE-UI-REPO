using System;
using System.Globalization;
using System.Windows.Data;

namespace SOLUM_UI.Converters
{
    [ValueConversion(typeof(string), typeof(string))]
    public class TextToTitleCaseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text)
            {
                TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;
                return textInfo.ToTitleCase(text.ToLower());
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
