
using System.Globalization;
using System.Windows.Data;
using System;
using ScriptHandler.Models;

namespace ScriptRunner.Converter
{
	public class NumberToBytesConverter : IValueConverter
	{

		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is ScriptStepCANMessage message))
				return value;

			byte[] payloadBytes = BitConverter.GetBytes(message.Payload);
			string payloadStr = "";
			for (int i = 0; i < message.PayloadLength; i++)
			{
				payloadStr += payloadBytes[i].ToString("X2") + " ";
			}

			return payloadStr;
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return string.Empty;
		}
	}
}
