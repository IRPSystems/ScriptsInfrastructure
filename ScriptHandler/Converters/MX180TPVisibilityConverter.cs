
using System.Globalization;
using System.Windows.Data;
using System;
using Entities.Models;
using System.Windows;
using DeviceCommunicators.Models;
using DeviceCommunicators.MX180TP;

namespace ScriptHandler.Converter
{
    public class MX180TPVisibilityConverter : IValueConverter
    {

        object IValueConverter.Convert(object value, Type targetType, object ConverterParameter, CultureInfo culture)
        {

            if (value is MX180TP_ParamData)
                return Visibility.Visible;

            return Visibility.Collapsed;

        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.Empty;
        }
    }
}
