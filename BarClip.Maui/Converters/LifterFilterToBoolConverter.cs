using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using BarClip.Data.Schema;
using BarClip.Models.Requests; // for LifterFilter enum

namespace BarClip.Maui.Converters
{
    public class LifterFilterToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Checks if the enum value matches the radio button
            return value?.ToString() == parameter?.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Returns the enum value corresponding to the selected radio button
            return Enum.Parse(typeof(LifterFilter), parameter.ToString());
        }
    }
}