
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
		public int GainNumOfReadings { get; set; }

		public DeviceParameterData CurrentParam { get; set; }
		public int CurrentNumOfReadings { get; set; }

		public ScriptStepSetParameter RefSensorChannel { get; set; }

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
			GainNumOfReadings = (sourceNode as ScriptNodeEOLCalibrate).GainNumOfReadings;
			CurrentParam = (sourceNode as ScriptNodeEOLCalibrate).CurrentParam;
			CurrentNumOfReadings = (sourceNode as ScriptNodeEOLCalibrate).CurrentNumOfReadings;

			RefSensorChannel = new ScriptStepSetParameter()
			{
				Parameter = (sourceNode as ScriptNodeEOLCalibrate).RefSensorChannel.Parameter,
				Value = (sourceNode as ScriptNodeEOLCalibrate).RefSensorChannel.Value,
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

			if(RefSensorChannel == null)
				return true;
			if(RefSensorChannel.Parameter == null)
				return true;

			return false;
		}

		#endregion Methods
	}
}
