using DeviceHandler.Models;
using ScriptHandler.Enums;
using ScriptHandler.Interfaces;
using System.Collections.ObjectModel;

namespace ScriptHandler.Models.ScriptNodes
{
	public class ScriptNodeDelay : ScriptNodeBase
	{
		public ScriptNodeDelay()
		{
			Name = "Delay";
			IntervalUnite = TimeUnitsEnum.sec;
		}

		private int _interval;
		public int Interval
		{
			get => _interval;
			set
			{
				_interval = value;
				OnPropertyChanged("Interval");
				OnPropertyChanged("Description");
				OnPropertyChanged("PassNextDescription");
			}
		}

		private TimeUnitsEnum _intervalUnite;
		public TimeUnitsEnum IntervalUnite 
		{
			get => _intervalUnite;
			set
			{
				_intervalUnite = value;
				OnPropertyChanged("IntervalUnite");
				OnPropertyChanged("Description");
				OnPropertyChanged("PassNextDescription");
			}
		}

		public override string Description
		{
			get
			{
				return "Delay " + _interval + " " + IntervalUnite + " - ID:" + ID;
			}
		}
		public override void PostLoad(
			DevicesContainer devicesContainer,
			IScript currentScript)
		{
			if (IntervalUnite == Enums.TimeUnitsEnum.None)
				IntervalUnite = Enums.TimeUnitsEnum.sec;
		}


		public override bool IsNotSet(
			DevicesContainer devicesContainer,
			ObservableCollection<InvalidScriptItemData> errorsList)
		{
			if (Interval <= 0)
				return true;

			return false;
		}
	}
}
