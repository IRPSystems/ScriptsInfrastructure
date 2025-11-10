
using DeviceCommunicators.General;
using DeviceCommunicators.Models;
using DeviceHandler.Models;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System;
using System.Threading;
using DeviceCommunicators.ZimmerPowerMeter;
using ScriptHandler.Enums;
using DeviceCommunicators.NI_6002;
using Services.Services;
using Entities.Enums;
using System.Reflection.Metadata;
using System.Diagnostics;
using Newtonsoft.Json;
using DeviceHandler.Models.DeviceFullDataModels;
namespace ScriptHandler.Models.ScriptSteps
{
    public class ScriptStepEOLCalibrate : ScriptStepBase
	{
		#region Properties

		public DeviceParameterData GainParam { get; set; }

		public DeviceParameterData McuParam { get; set; }
		public int McuNumOfReadings { get; set; }

		public DeviceParameterData RefSensorParam { get; set; }
        public int RefSensorNumOfReadings { get; set; }
		public int RefSensorChannel { get; set; }

		public int RefSensorPorts { get; set; }
		public double NIDAQShuntResistor { get; set; }

		public double DeviationLimit { get; set; }

		public DeviceCommunicator MCU_Communicator { get; set; }
		public DeviceCommunicator RefSensorCommunicator { get; set; }

        [JsonIgnore]
        public DevicesContainer DevicesContainer { get; set; }
        public double GainMaxLimit {  get; set; }
		public double GainMinLimit { get; set; }

		#endregion Properties

		#region Fields

		private ScriptStepGetParamValue _getValue;
        private ScriptStepSetParameter _setValue;

        public double AvgMcuRead => avgMcuRead;
        public double AvgRefSensorRead => avgRefSensorRead;
        public double PrevGain => prevGain;
        public double NewGain => newGain;

        private double avgMcuRead;
		private double avgRefSensorRead;
        private double prevGain;
        private double newGain;
		//private double deviation;
        

        private bool _isStopped;

		eState _eState = eState.Init;

        #endregion Fields

        #region Constructor

        public ScriptStepEOLCalibrate()
		{
			_getValue = new ScriptStepGetParamValue();
			_setValue = new ScriptStepSetParameter();

			_totalNumOfSteps = 9;
		}

		#endregion Constructor

		#region Methods

		public enum eState
		{
			Init,
			ReadGain,
			ReadSensorsPreCalib,
            ReadSensorsPostCalib,
			CalculateNewGain,
			SetNewGain,
            StopOrFail,
            EndSession
        }


		public override void Execute()
		{
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
			
            try
            {
				_eState = eState.Init;
				_isStopped = false;
				IsExecuted = true;
				IsError = false;

				_getValue = new ScriptStepGetParamValue();
				_setValue = new ScriptStepSetParameter();

				_getValue.EOLReportsSelectionData = EOLReportsSelectionData;
				_setValue.EOLReportsSelectionData = EOLReportsSelectionData;
				SetCommunicator();

				if (RefSensorParam is ZimmerPowerMeter_ParamData powerMeter)
					powerMeter.Channel = RefSensorChannel;

				if (RefSensorParam is NI6002_ParamData niParamData)
				{
					niParamData.Io_port = RefSensorPorts;
					niParamData.shunt_resistor = NIDAQShuntResistor;
					LoggerService.Error(this, "Execute: Daq Port" + niParamData.Io_port.ToString());
				}

				string description = Description;
				if (!string.IsNullOrEmpty(UserTitle))
					description = UserTitle;

				EOLStepSummeryData eolStepSummeryData = null;
				while (_eState != eState.EndSession && _eState != eState.StopOrFail)
				{

					switch (_eState)
					{
						case eState.Init:
							_stepsCounter = 1;
							_eState = eState.ReadGain;
							break;

						case eState.ReadGain:

							_getValue.Parameter = GainParam;
							_getValue.Communicator = MCU_Communicator;
							_getValue.SendAndReceive(out eolStepSummeryData, description);
							EOLStepSummerysList.Add(eolStepSummeryData);
							if (!_getValue.IsPass)
							{
								ErrorMessage = "Calibration Error \r\n"
									  + _getValue.ErrorMessage;
								IsError = true;
								IsPass = false;
								return;
							}

							if (GainParam.Value != null)
							{
								prevGain = Convert.ToDouble(GainParam.Value);
							}

							_stepsCounter++;
							_eState = eState.ReadSensorsPreCalib;
							break;

						case eState.ReadSensorsPreCalib:

							_stepsCounter++;

							if (!GetReadsMcuAndRefSensor())
							{
								IsPass = false;
								IsError = true;
								_eState = eState.StopOrFail;
								break;
							}
							LoggerService.Error(this, "ReadSensorsPreCalib: avgRefSensorRead:" + avgRefSensorRead.ToString());
							_eState = eState.CalculateNewGain;
							break;

						case eState.CalculateNewGain:

							_stepsCounter++;

							newGain = (prevGain * avgRefSensorRead) / avgMcuRead;

							if (newGain > GainMaxLimit || newGain < GainMinLimit)
							{
								IsPass = false;
								ErrorMessage = "Calculated gain has exceeded maximum limit\r\n" +
												"Max gain limit: " + GainMaxLimit + "\r\n" +
												"Min gain limit: " + GainMinLimit + "\r\n" +
												"Calculated gain: " + newGain + "\r\n" +
												"Avg MCU Read: " + avgMcuRead + "\r\n" +
												"Avg Ref Read: " + avgRefSensorRead;
								_eState = eState.StopOrFail;
								break;
							}
							_eState = eState.SetNewGain;
							break;

						case eState.SetNewGain:

							_stepsCounter++;
							_setValue.Parameter = GainParam;
							_setValue.Communicator = MCU_Communicator;
							_setValue.DevicesContainer = DevicesContainer;
							_setValue.Value = newGain;
							
							if (!string.IsNullOrEmpty(UserTitle))
								_setValue.Description = UserTitle;
							_setValue.Execute();
							EOLStepSummerysList.AddRange(_setValue.EOLStepSummerysList);

							if (!_setValue.IsPass)
							{
								ErrorMessage = "Unable to set: " + GainParam.Name;
								_eState = eState.StopOrFail;
								IsError = true;
								IsPass = false;
								break;
							}
							IsPass = true;
							_eState = eState.EndSession;
							break;

						case eState.StopOrFail:

							IsPass = false;
							break;
					}
					Thread.Sleep(1);
				}

				AddToEOLSummary();
			}
            finally
            {
				//finished derived class execute method
				IsError = (_getValue.IsError ?? false) || (_setValue.IsError ?? false);
                stopwatch.Stop();
                ExecutionTime = stopwatch.Elapsed;
            }

            return;
        }

		/// <summary>
		/// Reads and assign params from Mcu & RefSensor
		/// </summary>
		private bool GetReadsMcuAndRefSensor()
		{
            _getValue.Parameter = McuParam;
            _getValue.Communicator = MCU_Communicator;

            avgMcuRead = GetAvgRead(_getValue, McuNumOfReadings, McuParam);

			if (_isStopped)
			{
				return false;
			}

			if(McuParam.Value == null)
			{
                ErrorMessage = "Unable to get param: " + _getValue.ErrorMessage;
                return false;
			}

            _getValue.Parameter = RefSensorParam;
            _getValue.Communicator = RefSensorCommunicator;

			_stepsCounter++;

			avgRefSensorRead = GetAvgRead(_getValue, RefSensorNumOfReadings, RefSensorParam);

            if (_isStopped)
            {
                return false;
            }

            if (RefSensorParam.Value == null)
            {
                ErrorMessage = "Unable to get param: " + _getValue.ErrorMessage;
                return false;
            }

			_stepsCounter++;

			return true;
        }


        /// <summary>
        /// Reads a given params the amount of times that is required and calculates the average
        /// </summary>
        private double GetAvgRead(ScriptStepGetParamValue scriptStepGetParamValue, int numOfReads, DeviceParameterData deviceParameterData)
		{
			double avgRead = 0;
			for (int i = 0; i < numOfReads; i++)
			{
				if(_isStopped)
				{
					break;
				}

				string description = Description;
				if (!string.IsNullOrEmpty(UserTitle))
					description = UserTitle;

				EOLStepSummeryData eolStepSummeryData;
				scriptStepGetParamValue.SendAndReceive(out eolStepSummeryData, description);
                Thread.Sleep(50);
				EOLStepSummerysList.Add(eolStepSummeryData);
				if (!scriptStepGetParamValue.IsPass)
				{
					ErrorMessage = "Calibration Error \r\n"
					 + _getValue.ErrorMessage;
					break;
				}
				if (deviceParameterData.Value != null)
				{
					avgRead += Math.Abs(Convert.ToDouble(deviceParameterData.Value));
				}

				System.Threading.Thread.Sleep(1);
			}

            avgRead = avgRead / numOfReads;
            return avgRead;
        }
		private void SetCommunicator()
		{
			if(DevicesContainer != null)
			{
                if (McuParam != null && McuParam.IsInCANBus)
				{
					DeviceFullData devicefulldata = DevicesContainer.GetDeviceFullData(McuParam);
					MCU_Communicator = devicefulldata?.DeviceCommunicator;
                }
				if (RefSensorParam != null && RefSensorParam.IsInCANBus)
				{
					DeviceFullData devicefulldata = DevicesContainer.GetDeviceFullData(RefSensorParam);
					RefSensorCommunicator = devicefulldata?.DeviceCommunicator;
                }
            }
		}

        protected override void Stop()
		{
			_eState = eState.StopOrFail;
            if (_isStopped)
                return;

            _isStopped = true;
        }

		protected override void Generate(
			ScriptNodeBase sourceNode,
			Dictionary<int, ScriptStepBase> stepNameToObject,
			ref List<DeviceCommunicator> usedCommunicatorsList,
			GenerateProjectService generateService,
			DevicesContainer devicesContainer)
		{
			GainParam = (sourceNode as ScriptNodeEOLCalibrate).GainParam;
			McuParam = (sourceNode as ScriptNodeEOLCalibrate).McuParam;
			McuNumOfReadings = (sourceNode as ScriptNodeEOLCalibrate).McuNumOfReadings;
            RefSensorParam = (sourceNode as ScriptNodeEOLCalibrate).RefSensorParam;
            RefSensorNumOfReadings = (sourceNode as ScriptNodeEOLCalibrate).RefSensorNumOfReadings;
			RefSensorChannel = (int)(sourceNode as ScriptNodeEOLCalibrate).RefSensorChannel;
			RefSensorPorts = (int)(sourceNode as ScriptNodeEOLCalibrate).RefSensorPorts;
			NIDAQShuntResistor = (sourceNode as ScriptNodeEOLCalibrate).NIDAQShuntResistor;
			GainMaxLimit = (sourceNode as ScriptNodeEOLCalibrate).GainMax;
            GainMinLimit = (sourceNode as ScriptNodeEOLCalibrate).GainMin;
			DevicesContainer = devicesContainer; 
		}

		public override bool IsNotSet(
			DevicesContainer devicesContainer,
			ObservableCollection<InvalidScriptItemData> errorsList)
		{
			if (GainParam == null)
				return true;

			if (McuParam == null) 
				return true;

			if(RefSensorParam == null)
				return true;

			return false;
		}

		public override void GetRealParamAfterLoad(
			DevicesContainer devicesContainer)
		{
			GainParam = GetRealParam(
						GainParam,
						devicesContainer);

			McuParam = GetRealParam(
				McuParam,
				devicesContainer);

			RefSensorParam = GetRealParam(
				RefSensorParam,
				devicesContainer);

			DevicesContainer = devicesContainer; 
		}

        public override List<DeviceTypesEnum> GetUsedDevices()
        {
            List<DeviceTypesEnum> UsedDevices = new List<DeviceTypesEnum>();

            if (McuParam is DeviceParameterData deviceParameter)
            {
                UsedDevices.Add(deviceParameter.DeviceType);
            }
            if (RefSensorParam is DeviceParameterData deviceParameter2)
            {
                UsedDevices.Add(deviceParameter2.DeviceType);
            }
            if (GainParam is DeviceParameterData deviceParameter3)
            {
                UsedDevices.Add(deviceParameter3.DeviceType);
            }
            return UsedDevices;
        }

		public override List<string> GetReportHeaders()
		{
			List<string> headers = base.GetReportHeaders();
			if(GainParam != null)
			{
                string stepDescription = headers[0].Trim('\"');

                string description =
                        $"{stepDescription}\r\nGet {GainParam?.Name}";
                headers.Add($"\"{description}\"");

                description =
                        $"{stepDescription}\r\nGet {McuParam?.Name}";
                headers.Add($"\"{description}\"");

                description =
                        $"{stepDescription}\r\nGet {RefSensorParam?.Name}";
                headers.Add($"\"{description}\"");

                description =
                        $"{stepDescription}\r\nSet {GainParam?.Name}";
                headers.Add($"\"{description}\"");
            }

			return headers;
		}

		public override List<string> GetReportValues()
		{
			List<string> values = base.GetReportValues();

			try
			{
				EOLStepSummeryData stepSummeryData =
					EOLStepSummerysList.Find((e) =>
						!string.IsNullOrEmpty(e.Description) && e.Description.Contains(GainParam.Name));

				if (stepSummeryData != null)
					values.Add(stepSummeryData.TestValue.ToString());
				else
					values.Add("");


				stepSummeryData =
					EOLStepSummerysList.Find((e) =>
						!string.IsNullOrEmpty(e.Description) && e.Description.Contains(McuParam.Name));

				if (stepSummeryData != null)
					values.Add(stepSummeryData.TestValue.ToString());
				else
					values.Add("");


				stepSummeryData =
					EOLStepSummerysList.Find((e) =>
						!string.IsNullOrEmpty(e.Description) && e.Description.Contains(RefSensorParam.Name));

				if (stepSummeryData != null)
					values.Add(stepSummeryData.TestValue.ToString());
				else
					values.Add("");

				stepSummeryData =
				   EOLStepSummerysList.Find((e) =>
					   e.Step is ScriptStepSetParameter);

				if (stepSummeryData != null)
					values.Add(stepSummeryData.TestValue.ToString());
				else
					values.Add("");
				return values;
			}
			catch (Exception ex)
			{
				LoggerService.Error(this, $"{ex}");
				return null;
			}


		}

		#endregion Methods
	}
}
