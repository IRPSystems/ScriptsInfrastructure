
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

		#endregion Properties

		#region Fields

		private ScriptStepGetParamValue _getValue;
        private ScriptStepSetParameter _setValue;
		private ScriptStepSetSaveParameter _saveValue;

        private double avgMcuRead;
		private double avgRefSensorRead;
        private double prevGain;
        private double newGain;
		private double deviation;
        private double gainMaxLimit;
        private double gainMinLimit;

        private bool _isStopped;

		eState _eState = eState.Init;

        #endregion Fields

        #region Constructor

        public ScriptStepEOLCalibrate()
		{
			_getValue = new ScriptStepGetParamValue();
			_setValue = new ScriptStepSetParameter();
			_saveValue = new ScriptStepSetSaveParameter();

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
            _eState = eState.Init;

            if (RefSensorParam is ZimmerPowerMeter_ParamData powerMeter)
                powerMeter.Channel = RefSensorChannel;

            if (RefSensorParam is NI6002_ParamData niParamData)
            {
                niParamData.Io_port = RefSensorPorts;
                niParamData.shunt_resistor = NIDAQShuntResistor;
                LoggerService.Error(this, "Execute: Daq Port" + niParamData.Io_port.ToString());
            }

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
                        _getValue.SendAndReceive(out eolStepSummeryData);
						EOLStepSummerysList.Add(eolStepSummeryData);
						if (!_getValue.IsPass)
                        {
                            ErrorMessage = "Calibration Error \r\n"
                                  + _getValue.ErrorMessage;
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
                            _eState = eState.StopOrFail;
							break;
                        }
                        LoggerService.Error(this, "ReadSensorsPreCalib: avgRefSensorRead:" + avgRefSensorRead.ToString());
                        _eState = eState.CalculateNewGain;
                        break;

                    case eState.CalculateNewGain:

                        _stepsCounter++;

                        newGain = (prevGain * avgRefSensorRead) / avgMcuRead;

                        if (newGain > gainMaxLimit || newGain < gainMinLimit)
                        {
                            IsPass = false;
                            ErrorMessage = "Calculated gain has exceeded maximum limit\r\n" +
                                            "Max gain limit: " + gainMaxLimit + "\r\n" +
                                            "Min gain limit: " + gainMinLimit + "\r\n" +
                                            "Calculated gain: " + newGain + "\r\n";
                            _eState = eState.StopOrFail;
                            break;
                        }
                        _eState = eState.SetNewGain;
                        break;

                    case eState.SetNewGain:

                        _stepsCounter++;
                        _setValue.Parameter = GainParam;
                        _setValue.Communicator = MCU_Communicator;
                        _setValue.Value = newGain;
                        _setValue.Execute();
						EOLStepSummerysList.AddRange(_setValue.EOLStepSummerysList);

						if (!_setValue.IsPass)
                        {
                            ErrorMessage = "Unable to set: " + GainParam.Name;
                            _eState = eState.StopOrFail;
                            break;
                        }
                        _eState = eState.ReadSensorsPostCalib;
                        break;


                    case eState.ReadSensorsPostCalib:

                        Thread.Sleep(100);

                        _stepsCounter++;

                        if (!GetReadsMcuAndRefSensor())
                        {
                            IsPass = false;
                            _eState = eState.StopOrFail;
                            break;
                        }
                        IsPass = true;
                        _eState = eState.EndSession;
                        break;

                    case eState.StopOrFail:

                        IsPass = false;
                        break;
                }
            }

			eolStepSummeryData = new EOLStepSummeryData(
				Description,
				"",
				isPass: IsPass,
				errorDescription: ErrorMessage);
			EOLStepSummerysList.Add(eolStepSummeryData);

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
				EOLStepSummeryData eolStepSummeryData;
				scriptStepGetParamValue.SendAndReceive(out eolStepSummeryData);
				EOLStepSummerysList.Add(eolStepSummeryData);
				if (!scriptStepGetParamValue.IsPass)
				{
					ErrorMessage = "Calibration Error \r\n"
					 + _getValue.ErrorMessage;
					break;
				}
				if (deviceParameterData.Value != null)
				{
					avgRead += Convert.ToDouble(deviceParameterData.Value);
				}
			}

            avgRead = avgRead / numOfReads;
            return avgRead;
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
			gainMaxLimit = (sourceNode as ScriptNodeEOLCalibrate).GainMax;
            gainMinLimit = (sourceNode as ScriptNodeEOLCalibrate).GainMin;
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
		}

		#endregion Methods
	}
}
