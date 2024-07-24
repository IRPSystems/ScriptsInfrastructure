
using DeviceCommunicators.Models;
using DeviceHandler.Models;
using Entities.Enums;
using LibUsbDotNet.DeviceNotify;
using ScriptHandler.Interfaces;
using System.Collections.ObjectModel;

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

		public override bool IsNotSet(
			DevicesContainer devicesContainer,
			ObservableCollection<InvalidScriptItemData> errorsList)
		{
			if (GainParam == null)
				return true;

			if (CurrentParam == null)
				return true;

			if (SetParameter == null)
				return true;
			if (SetParameter.Parameter == null)
				return true;

			return false;
		}

		#endregion Methods
	}
}
