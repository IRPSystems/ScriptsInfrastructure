﻿
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

        #region Methods

        public override void Execute()
		{
			if (RefSensorParam is ZimmerPowerMeter_ParamData powerMeter)
				powerMeter.Channel = RefSensorChannel;

			//Calibrate
			//Get reads

			_getValue = new ScriptStepGetParamValue();
            _getValue.Parameter = GainParam;
			_getValue.Communicator = MCU_Communicator;
			_getValue.SendAndReceive();

			if(GainParam.Value != null)
			{
                prevGain = Convert.ToDouble(GainParam.Value);
            }

			GetReadsMcuAndRefSensor();

            newGain = (prevGain * avgRefSensorRead) / avgMcuRead;

			//Set new gain
            _setValue = new ScriptStepSetParameter();
			_setValue.Parameter = GainParam;
			_setValue.Communicator = MCU_Communicator;
			_setValue.Value = newGain;
			_setValue.Execute();

			//Validate Calibration

			Thread.Sleep(100);

            GetReadsMcuAndRefSensor();

            deviation = Math.Abs(((float)avgRefSensorRead - (float)avgMcuRead) * 100 * 2) /
                         (((float)avgRefSensorRead + (float)avgMcuRead));

			if(deviation > DeviationLimit)
			{
				IsPass = false;
				ErrorMessage = "lo tov";
				return;
			}

			//If succeed save param

			_saveValue = new ScriptStepSetSaveParameter();
			_saveValue.Parameter = GainParam;
			_saveValue.Communicator = MCU_Communicator;
			_saveValue.Value = newGain;
			_saveValue.Execute();
        }

		/// <summary>
		/// Reads and assign params from Mcu & RefSensor
		/// </summary>
		private void GetReadsMcuAndRefSensor()
		{
            _getValue.Parameter = McuParam;
            _getValue.Communicator = MCU_Communicator;

            avgMcuRead = GetAvgRead(_getValue, McuNumOfReadings, McuParam);

            _getValue.Parameter = RefSensorParam;
            _getValue.Communicator = MCU_Communicator;

            avgRefSensorRead = GetAvgRead(_getValue, RefSensorNumOfReadings, RefSensorParam);
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
