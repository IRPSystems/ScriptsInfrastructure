using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace ScriptHandler.Models
{
	public class DynamicControlData : ObservableObject, ICloneable
	{
		public double Value { get; set; }
		public bool IsCurrent { get; set; }

		public object Clone()
		{
			return MemberwiseClone();
		}
	}
}
