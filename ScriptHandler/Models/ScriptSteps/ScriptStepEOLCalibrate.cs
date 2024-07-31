
using DeviceCommunicators.Enums;
using DeviceCommunicators.General;
using DeviceCommunicators.Models;
using DeviceHandler.Models;
using ScriptHandler.Interfaces;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection.Metadata;
using System;
using System.Threading;
using ScriptHandler.Enums;
using DeviceCommunicators.ZimmerPowerMeter;

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

		public double DeviationLimit { get; set; }

		public DeviceCommunicator MCU_Communicator { get; set; }
		public DeviceCommunicator RefSensorCommunicator { get; set; }

		#endregion Properties

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

        #region Methods

        public ScriptStepEOLCalibrate()
		{
            _getValue = new ScriptStepGetParamValue();
            _setValue = new ScriptStepSetParameter();
            _saveValue = new ScriptStepSetSaveParameter();
        }

        public override void Execute()
		{
			if (RefSensorParam is ZimmerPowerMeter_ParamData powerMeter)
				powerMeter.Channel = RefSensorChannel;

			//Calibrate
			//Get reads

            _getValue.Parameter = GainParam;
			_getValue.Communicator = MCU_Communicator;
			_getValue.SendAndReceive();
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

			if(!GetReadsMcuAndRefSensor())
			{
				IsPass = false;
				return;
			}

            newGain = (prevGain * avgRefSensorRead) / avgMcuRead;

			if (newGain > gainMaxLimit || newGain < gainMinLimit)
			{
                IsPass = false;
                ErrorMessage = "Calculated gain has exceeded maximum limit\r\n" +
                                "Max gain limit: " + gainMaxLimit + "\r\n" +
                                "Min gain limit: " + gainMinLimit + "\r\n" +
                                "Calculated gain: " + newGain + "\r\n";
                return;
            }

			//Set new gain
            
			_setValue.Parameter = GainParam;
			_setValue.Communicator = MCU_Communicator;
			_setValue.Value = newGain;
			_setValue.Execute();

            if(!_setValue.IsPass)
			{
                ErrorMessage = "Unable to set: " + GainParam.Name;
                return;
            }

            //Validate Calibration

            Thread.Sleep(100);

            if (!GetReadsMcuAndRefSensor())
            {
                IsPass = false;
                return;
            }

            deviation = Math.Abs(((float)avgRefSensorRead - (float)avgMcuRead) * 100 * 2) /
                         (((float)avgRefSensorRead + (float)avgMcuRead));

			//deviation limit temp
			DeviationLimit = 10;

			if(deviation > DeviationLimit)
			{
				IsPass = false;
				ErrorMessage = "Calibration deviation has exceeded maximum limit\r\n" +
                                        "Deviation Result = " + deviation + "%" + "\r\n" +
                                        "Deviation Max Limit" + DeviationLimit + "%";
                return;
			}

			//If succeed save param

			
			_saveValue.Parameter = GainParam;
			_saveValue.Communicator = MCU_Communicator;
			_saveValue.Value = newGain;
			_saveValue.Execute();

			if(!_saveValue.IsPass)
			{
                IsPass = false;
                ErrorMessage = "Calibration - unable to save: " + _saveValue.ErrorMessage;
				return;
            }
			IsPass = true;
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

			if(McuParam.Value == null)
			{
                ErrorMessage = "Unable to get param: " + _getValue.ErrorMessage;
                return false;
			}

            _getValue.Parameter = RefSensorParam;
            _getValue.Communicator = RefSensorCommunicator;

            avgRefSensorRead = GetAvgRead(_getValue, RefSensorNumOfReadings, RefSensorParam);

            if (RefSensorParam.Value == null)
            {
                ErrorMessage = "Unable to get param: " + _getValue.ErrorMessage;
                return false;
            }

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
				scriptStepGetParamValue.SendAndReceive();
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

		}

		public override void Generate(
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
			gainMaxLimit = (sourceNode as ScriptNodeEOLCalibrate).GainMax;
            gainMinLimit = (sourceNode as ScriptNodeEOLCalibrate).GainMin;
            DeviationLimit = (sourceNode as ScriptNodeEOLCalibrate).DeviationLimit;
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
