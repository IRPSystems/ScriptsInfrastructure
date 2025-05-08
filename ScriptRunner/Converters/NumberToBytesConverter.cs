
using System.Globalization;
using System.Windows.Data;
using System;
using ScriptHandler.Models;

namespace ScriptRunner.Converter
{
	public class NumberToBytesConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (!(values[0] is ScriptStepCANMessage message))
				return null;

			byte[] payloadBytes = BitConverter.GetBytes(message.Payload);
			string payloadStr = "";
			for (int i = 0; i < message.PayloadLength; i++)
			{
				payloadStr += payloadBytes[i].ToString("X2") + " ";
			}

			return payloadStr;
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
		{
			return null;
		}
	}
}
