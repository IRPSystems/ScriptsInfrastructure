﻿
using System.Globalization;
using System.Windows.Data;
using System;
using System.Windows;
using DeviceCommunicators.NI_6002;
using DeviceCommunicators.ZimmerPowerMeter;
using DeviceCommunicators.MCU;


namespace ScriptHandler.Converter
{
    public class ATECommandVisibilityConverter : IValueConverter
    {

        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ATE_ParamData param &&
                param.ATECommand != null && param.ATECommand.Count > 0)
            {
                return Visibility.Visible;
            }
                
            return Visibility.Collapsed;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
