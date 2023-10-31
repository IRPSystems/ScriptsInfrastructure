
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System;

namespace ScriptHandler.Models
{
	public class DynamicControlFileLine : ObservableObject, ICloneable
	{
		public List<double> ValuesList { get; set; }
		public TimeSpan Time { get; set; }

		public object Clone()
		{
			return MemberwiseClone();
		}
	}
}
