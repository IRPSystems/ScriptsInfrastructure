
using Entities.Models;
using System.Windows;

namespace ScriptHandler.ValidationRules
{
	public class ParameterWrapper : DependencyObject
	{
		public static readonly DependencyProperty SetParamNodeProperty =
			 DependencyProperty.Register("SetParamNode", typeof(DeviceParameterData),
			 typeof(ParameterWrapper));

		public DeviceParameterData SetParamNode
		{
			get { return (DeviceParameterData)GetValue(SetParamNodeProperty); }
			set { SetValue(SetParamNodeProperty, value); }
		}
	}
}
