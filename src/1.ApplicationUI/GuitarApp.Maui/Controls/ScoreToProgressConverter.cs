using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace GuitarApp.Maui.Converters;

public class ScoreToProgressConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is double d ? Math.Clamp(d / 100.0, 0.0, 1.0) : 0.0;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
