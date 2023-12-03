
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System;
using System.Collections.ObjectModel;

namespace ScriptHandler.Models
{
	public class DynamicControlFileLine : ObservableObject, ICloneable
	{
		public class DynamicControlData : ObservableObject
		{
			public double Value { get; set; }
			public bool IsCurrent { get; set; }
		}

		public ObservableCollection<DynamicControlData> ValuesList { get; set; }
		public TimeSpan Time { get; set; }
		

		public object Clone()
		{
			return MemberwiseClone();
		}
	}
}
