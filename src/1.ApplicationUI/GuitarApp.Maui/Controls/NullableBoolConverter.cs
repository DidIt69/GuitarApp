using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace GuitarApp.Maui.Controls;

public class NullableBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isTrue = value is bool b && b;

        // Si le paramètre contient des chaînes séparées par une virgule (ex: "Arrêter,Écouter")
        if (parameter is string paramString && paramString.Contains(','))
        {
            var parts = paramString.Split(',');
            return isTrue ? parts[0] : parts[1];
        }

        return isTrue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
