
using DeviceCommunicators.General;
using DeviceCommunicators.MCU;
using DeviceCommunicators.Models;
using DeviceHandler.Models;
using ScriptHandler.Interfaces;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Services;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;

namespace ScriptHandler.Models.ScriptSteps
{
	public class ScriptStepEOLSendSN : ScriptStepBase, IScriptStepWithCommunicator
	{
		#region Properties and Fields

		public DeviceParameterData SN_Param { get; set; }
		public DeviceCommunicator Communicator { get; set; }

		public string SerialNumber { get; set; }
		public string UserSN { get; set; }

		private ScriptStepSetParameter _setValue;
		private ScriptStepGetParamValue _getValue;
		private ScriptStepSetSaveParameter _saveValue;

		#endregion Properties and Fields

		#region Constructor

		public ScriptStepEOLSendSN()
		{
			_totalNumOfSteps = 4;
			UserSN = "E7P-22-00-00032";
		}

		#endregion Constructor

		#region Methods

		public override void Execute()
		{
			//Get SN from UI - Temp userSN

			//remove daash and letters

			_stepsCounter = 1;

			EOLStepSummeryData eolStepSummeryData;

			SerialNumber = Regex.Replace(UserSN, "[A-Za-z ]", "");
			SerialNumber = SerialNumber.Remove(0, 1);
			SerialNumber = SerialNumber.Replace("-", "");

			//Set SN
			_setValue = new ScriptStepSetParameter();
			_setValue.Parameter = SN_Param;
			_setValue.Communicator = Communicator;
			_setValue.Value = SerialNumber;
			_setValue.Execute();
			EOLStepSummerysList.AddRange(_setValue.EOLStepSummerysList);

			if (!_setValue.IsPass)
			{
				ErrorMessage = "Unable to set: " + SN_Param.Name;
				IsPass = false;
				string description = Description;
				if (!string.IsNullOrEmpty(UserTitle))
					description = UserTitle;
				eolStepSummeryData = new EOLStepSummeryData(
					description,
					"",
					isPass: IsPass,
					errorDescription: ErrorMessage);
				EOLStepSummerysList.Add(eolStepSummeryData);
				return;
			}

			_stepsCounter++;

			//Verify SN- get

			_getValue = new ScriptStepGetParamValue();
			_getValue.Parameter = SN_Param;
			_getValue.Communicator = Communicator;			
			_getValue.SendAndReceive(out eolStepSummeryData);
			EOLStepSummerysList.Add(eolStepSummeryData);
			if (_getValue.IsPass)
			{				
				//Validate SN
				if (SN_Param.Value as string == UserSN)
				{
					ErrorMessage = "Wrong SN \r\n"
					+ _getValue.ErrorMessage;
					IsPass = false;

					string description = Description;
					if (!string.IsNullOrEmpty(UserTitle))
						description = UserTitle;
					eolStepSummeryData = new EOLStepSummeryData(
						description,
						"",
						isPass: IsPass,
						errorDescription: ErrorMessage);
					EOLStepSummerysList.Add(eolStepSummeryData);
					return;
				}
			}
			else
			{
				ErrorMessage = "Failed to get the SN parameter";
				IsPass = false;
				eolStepSummeryData = new EOLStepSummeryData(
					Description,
					"",
					isPass: IsPass,
					errorDescription: ErrorMessage);
				EOLStepSummerysList.Add(eolStepSummeryData);
				return;
			}

			_stepsCounter++;

			//If succeed save param

			_saveValue = new ScriptStepSetSaveParameter();
			_saveValue.Parameter = SN_Param;
			_saveValue.Communicator = Communicator;
			_saveValue.Value = Convert.ToDouble(SerialNumber);
			_saveValue.Execute();
			EOLStepSummerysList.AddRange(_saveValue.EOLStepSummerysList);

			if (!_saveValue.IsPass)
			{
				IsPass = false;
				ErrorMessage = "Unable to save SN: " + _saveValue.ErrorMessage;
				return;
			}
			IsPass = true;

			string description = Description;
			if (!string.IsNullOrEmpty(UserTitle))
				description = UserTitle;
			eolStepSummeryData = new EOLStepSummeryData(
				description,
				"",
				isPass: IsPass,
				errorDescription: ErrorMessage);
			EOLStepSummerysList.Add(eolStepSummeryData);
			return;
		}

		protected override void Stop()
		{

		}

		protected override void Generate(
			ScriptNodeBase sourceNode,
			Dictionary<int, ScriptStepBase> stepNameToObject,
			ref List<DeviceCommunicator> usedCommunicatorsList,
			GenerateProjectService generateService,
			DevicesContainer devicesContainer)
		{

		}

		public override void GetRealParamAfterLoad(
			DevicesContainer devicesContainer)
		{
			SN_Param = new MCU_ParamData()
			{
				Name = "Serial Number",
				Cmd = "serialnumber",
				DeviceType = Entities.Enums.DeviceTypesEnum.MCU
			};

			SN_Param = GetRealParam(
				SN_Param,
				devicesContainer);
		}

		#endregion Methods
	}
}
