
using System.Globalization;
using System.Windows.Data;
using System;
using Entities.Models;
using System.Windows;
using DeviceCommunicators.Models;
using DeviceCommunicators.NI_6002;
using DeviceCommunicators.RigolM300;

namespace ScriptHandler.Converter
{
    public class RigolM300VisibilityConverter : IValueConverter
    {

        object IValueConverter.Convert(object value, Type targetType, object ConverterParameter, CultureInfo culture)
        {

            if (value is RigolM300_ParamData)
                return Visibility.Visible;

            return Visibility.Collapsed;

        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.Empty;
        }
    }
}
