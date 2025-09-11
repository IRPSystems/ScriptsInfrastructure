
using DeviceCommunicators.Models;
using DeviceHandler.Models;
using ScriptHandler.Enums;
using ScriptHandler.Interfaces;
using System.Collections.ObjectModel;

namespace ScriptHandler.Models.ScriptNodes
{
    public class ScriptNodeEOLCalibrate : ScriptNodeBase
	{
		#region Properties

		public DeviceParameterData GainParam { get; set; }
		public double GainMin { get; set; }
		public double GainMax { get; set; }

		public DeviceParameterData McuParam { get; set; }
		public int McuNumOfReadings { get; set; }

		public DeviceParameterData RefSensorParam { get; set; }
		public int RefSensorNumOfReadings { get; set; }
		public EolRefSensorChannelsEnum RefSensorChannel { get; set; }

		public EolRefSensorPortsEnum RefSensorPorts { get; set; }
		public double NIDAQShuntResistor { get; set; }

		public double DeviationLimit { get; set; }

		public override string Description 
		{ 
			get
			{
				string description = "Calibrate - " + McuParam;
				return description + " - ID:" + ID;
			}
		}

		#endregion Properties

		#region Constructor

		public ScriptNodeEOLCalibrate() 
		{
			Name = "EOL Calibrate";
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

			if (McuParam != null)
			{
				McuParam = GetParameter(
					McuParam.DeviceType,
					McuParam,
					devicesContainer);
			}

			if (RefSensorParam != null)
			{
				RefSensorParam = GetParameter(
					RefSensorParam.DeviceType,
					RefSensorParam,
					devicesContainer);
			}

		}

		public override bool IsNotSet(
			DevicesContainer devicesContainer,
			ObservableCollection<InvalidScriptItemData> errorsList)
		{
			if (GainParam == null)
				return true;

			if (McuParam == null)
				return true;

			if (RefSensorParam == null)
				return true;

			return false;
		}

		#endregion Methods
	}
}
