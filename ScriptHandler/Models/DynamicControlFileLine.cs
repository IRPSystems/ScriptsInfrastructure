
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;

namespace ScriptHandler.Models
{
	public class DynamicControlFileLine : ObservableObject, ICloneable
	{
		

		public ObservableCollection<DynamicControlData> ValuesList { get; set; }
		public TimeSpan Time { get; set; }
		

		public object Clone()
		{
			DynamicControlFileLine dynamicControlFileLine = 
				MemberwiseClone() as DynamicControlFileLine;

			dynamicControlFileLine.ValuesList = new ObservableCollection<DynamicControlData>();
			foreach (DynamicControlData dynamicControlData in ValuesList)
				dynamicControlFileLine.ValuesList.Add(dynamicControlData.Clone() as DynamicControlData);

			return dynamicControlFileLine;
		}
	}
}
