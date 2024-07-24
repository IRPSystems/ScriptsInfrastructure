
using DeviceCommunicators.Models;
using DeviceHandler.Models;
using Entities.Enums;
using LibUsbDotNet.DeviceNotify;
using ScriptHandler.Interfaces;

namespace ScriptHandler.Models.ScriptNodes
{
	public class ScriptNodeEOLCalibrate : ScriptNodeBase
	{
		#region Properties

		public DeviceParameterData GainParam { get; set; }
		public DeviceParameterData CurrentParam { get; set; }

		public ScriptNodeSetParameter SetParameter { get; set; }

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

			SetParameter = new ScriptNodeSetParameter();
		}

		#endregion Constructor

		#region Methods

		public override void PostLoad(
			DevicesContainer devicesContainer,
			IScript currentScript)
		{
			GainParam = GetParameter(
				GainParam.DeviceType,
				GainParam,
				devicesContainer);

			CurrentParam = GetParameter(
				CurrentParam.DeviceType,
				CurrentParam,
				devicesContainer);

			SetParameter.Parameter = GetParameter(
				SetParameter.Parameter.DeviceType,
				SetParameter.Parameter,
				devicesContainer);
		}

		#endregion Methods
	}
}
