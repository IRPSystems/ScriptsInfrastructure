
using DeviceCommunicators.General;
using DeviceCommunicators.Models;
using DeviceHandler.Models;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection.Metadata;

namespace ScriptHandler.Models.ScriptSteps
{
	public class ScriptStepEOLCalibrate : ScriptStepBase
	{
		#region Properties

		public DeviceParameterData GainParam { get; set; }
		public DeviceParameterData CurrentParam { get; set; }

		public ScriptStepSetParameter SetParameter { get; set; }

		public double DeviationLimit { get; set; }

		public DeviceCommunicator MCU_Communicator { get; set; }
		public DeviceCommunicator RefSensorCommunicator { get; set; }

		#endregion Properties

		#region Methods

		public override void Execute()
		{

		}

		protected override void Stop()
		{

		}

		public override void Generate(
			ScriptNodeBase sourceNode,
			Dictionary<int, ScriptStepBase> stepNameToObject,
			ref List<DeviceCommunicator> usedCommunicatorsList,
			GenerateProjectService generateService,
			DevicesContainer devicesContainer)
		{
			GainParam = (sourceNode as ScriptNodeEOLCalibrate).GainParam;
			CurrentParam = (sourceNode as ScriptNodeEOLCalibrate).CurrentParam;

			SetParameter = new ScriptStepSetParameter()
			{
				Parameter = (sourceNode as ScriptNodeEOLCalibrate).SetParameter.Parameter,
				Value = (sourceNode as ScriptNodeEOLCalibrate).SetParameter.Value,
			};

			DeviationLimit = (sourceNode as ScriptNodeEOLCalibrate).DeviationLimit;
		}

		public override bool IsNotSet(
			DevicesContainer devicesContainer,
			ObservableCollection<InvalidScriptItemData> errorsList)
		{
			if (GainParam == null)
				return true;

			if (CurrentParam == null) 
				return true;

			if(SetParameter == null)
				return true;
			if(SetParameter.Parameter == null)
				return true;

			return false;
		}

		#endregion Methods
	}
}
