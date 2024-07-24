
using DeviceCommunicators.Models;
using DeviceHandler.Models;
using ScriptHandler.Interfaces;
using System.Collections.ObjectModel;

namespace ScriptHandler.Models.ScriptNodes
{
	public class ScriptNodeEOLCalibrate : ScriptNodeBase
	{
		#region Properties

		public DeviceParameterData GainParam { get; set; }
		public int GainNumOfReadings { get; set; }

		public DeviceParameterData CurrentParam { get; set; }
		public int CurrentNumOfReadings { get; set; }

		public ScriptNodeSetParameter RefSensorChannel { get; set; }

		public double DeviationLimit { get; set; }

		public override string Description 
		{ 
			get
			{
				string description = "Calibrate - " + CurrentParam;
				return description + " - ID:" + ID;
			}
		}

		#endregion Properties

		#region Constructor

		public ScriptNodeEOLCalibrate() 
		{
			Name = "EOL Clibrate";

			RefSensorChannel = new ScriptNodeSetParameter();
		}

		#endregion Constructor

		#region Methods

		public override void PostLoad(
			DevicesContainer devicesContainer,
			IScript currentScript)
		{
			if (GainParam != null)
			{
				GainParam = GetParameter(
					GainParam.DeviceType,
					GainParam,
					devicesContainer);
			}

			if (CurrentParam != null)
			{
				CurrentParam = GetParameter(
					CurrentParam.DeviceType,
					CurrentParam,
					devicesContainer);
			}

			if (RefSensorChannel != null && RefSensorChannel.Parameter != null)
			{
				RefSensorChannel.Parameter = GetParameter(
					RefSensorChannel.Parameter.DeviceType,
					RefSensorChannel.Parameter,
					devicesContainer);
			}
		}

		public override bool IsNotSet(
			DevicesContainer devicesContainer,
			ObservableCollection<InvalidScriptItemData> errorsList)
		{
			if (GainParam == null)
				return true;

			if (CurrentParam == null)
				return true;

			if (RefSensorChannel == null)
				return true;
			if (RefSensorChannel.Parameter == null)
				return true;

			return false;
		}

		#endregion Methods
	}
}
