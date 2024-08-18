﻿
using DeviceCommunicators.General;
using DeviceCommunicators.Models;
using DeviceHandler.Interfaces;
using DeviceHandler.Models;
using DeviceHandler.Models.DeviceFullDataModels;
using Entities.Models;
using Newtonsoft.Json.Linq;
using ScriptHandler.Enums;
using ScriptHandler.Interfaces;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Services;
using Services.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ScriptHandler.Models
{
	public class ScriptStepCompareWithTolerance : ScriptStepGetParamValue
	{
		#region Properties

		

		public object CompareValue{ get; set; }

		public double Tolerance { get; set; }

		public ComparationTypesEnum Comparation { get; set; }

		public bool IsValueTolerance { get; set; }
		public bool IsPercentageTolerance { get; set; }


		#endregion Properties



		#region Constructor

		public ScriptStepCompareWithTolerance()
		{
			if (Application.Current != null)
			{
				Application.Current.Dispatcher.Invoke(() =>
				{
					Template = Application.Current.MainWindow.FindResource("DelayTemplate") as DataTemplate;
				});
			}
		}

		#endregion Constructor


		#region Methods

		public override void Execute()
		{
			IsPass = false;
			string errorHeader = "Compare range:\r\n";
			string errorMessage = errorHeader + "Failed to get the compared parameter for compare range\r\n\r\n";

			_stepsCounter = 1;

			double paramValue = 0;
			string paramName = "";
			bool res = GetValueAndName(
				out paramValue,
				out paramName,
				Parameter);
			if (!res)
			{
				ErrorMessage = errorMessage + ErrorMessage;
				IsPass = false;
				return;
			}

			ErrorMessage = errorHeader + "Failed to get the left value parameter for compare range";

			_stepsCounter++;

			double paramValue_Left = 0;
			string paramName_Left = "";
			res = GetValueAndName(
				out paramValue_Left,
				out paramName_Left,
				CompareValue);
			if (!res)
			{
				ErrorMessage = errorMessage + ErrorMessage;
				IsPass = false;
				return;
			}

			ErrorMessage = errorHeader + "Failed to get the right value parameter for compare range";

			_stepsCounter++;


			Compare_ValueWithTolerance(
					paramValue,
					paramName,
					paramValue_Left,
					paramName_Left,
					Tolerance,
					errorHeader);
			

			EOLStepSummeryData eolStepSummeryData = new EOLStepSummeryData(
				Description,
				"",
				isPass: IsPass,
				errorDescription: ErrorMessage);
			EOLStepSummerysList.Add(eolStepSummeryData);

		}

		private void Compare_ValueWithTolerance(
			double paramValue,
			string paramName,
			double paramValue_Left,
			string paramName_Left,
			double tolerance,
			string errorHeader)
		{
			ErrorMessage =
				errorHeader +
				paramName + " = " +
				paramName_Left + " ± " +
				tolerance;

			double lowest = paramValue_Left - tolerance;
			double heighest = paramValue_Left + tolerance;

			if (lowest <= paramValue && paramValue <= heighest)
				IsPass = true;
			else
				IsPass = false;

		}


		private bool GetValueAndName(
			out double paramValue,
			out string paramName,
			object value)
		{
			paramValue = 0;
			paramName = "";

			if (value is DeviceParameterData param)
			{
				object val = GetCompareParaValue(param);
				if (val == null)
					return false;

				paramValue = Convert.ToDouble(val);

				paramName =
					"(\"" + param + "\" = " + paramValue + ")";

			}
			else if (value is string str)
			{
				paramName = str;
				double d;
				bool res = double.TryParse(str, out d);
				if (res == false)
					return false;

				paramValue = d;

			}
			else if (value is double d)
			{
				paramName = d.ToString();
				paramValue = d;
			}

			return true;
		}

		private object GetCompareParaValue(
			DeviceParameterData parameter)
		{
			//Parameter = parameter;

			if (parameter != null)
			{
				DeviceFullData deviceFullData =
					DevicesContainer.DevicesFullDataList.ToList().Find((d) => d.Device.DeviceType == parameter.DeviceType);
				Communicator = deviceFullData.DeviceCommunicator;
			}

			EOLStepSummeryData eolStepSummeryData;
			bool isOK = SendAndReceive(parameter, out eolStepSummeryData);
			EOLStepSummerysList.Add(eolStepSummeryData);
			if (!isOK)
			{
				IsPass = false;
				return 0;
			}

			if (parameter == null)
				return null;

			return parameter.Value;
		}




		public override bool IsNotSet(
			DevicesContainer devicesContainer,
			ObservableCollection<InvalidScriptItemData> errorsList)
		{
			if (Parameter == null)
				return true;

			return false;
		}

		protected override void Generate(
			ScriptNodeBase sourceNode,
			Dictionary<int, ScriptStepBase> stepNameToObject,
			ref List<DeviceCommunicator> usedCommunicatorsList,
			GenerateProjectService generateService,
			DevicesContainer devicesContainer)
		{
			Parameter = (sourceNode as ScriptNodeCompareWithTolerance).Parameter;
			CompareValue = (sourceNode as ScriptNodeCompareWithTolerance).CompareValue;
			Tolerance = (sourceNode as ScriptNodeCompareWithTolerance).Tolerance;
			Comparation = (sourceNode as ScriptNodeCompareWithTolerance).Comparation;
			IsValueTolerance = (sourceNode as ScriptNodeCompareWithTolerance).IsValueTolerance;
			IsPercentageTolerance = (sourceNode as ScriptNodeCompareWithTolerance).IsPercentageTolerance;
		}

		public override void GetRealParamAfterLoad(
			DevicesContainer devicesContainer)
		{
			base.GetRealParamAfterLoad(devicesContainer);

			if (CompareValue is DeviceParameterData)
			{
				if (CompareValue is ICalculatedParamete)
					return;

				CompareValue = GetRealParam(
					CompareValue as DeviceParameterData,
					devicesContainer);
			}
		}

		#endregion Methods
	}
}
