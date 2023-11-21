
using DeviceCommunicators.General;
using DeviceHandler.Models;
using Entities.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ScriptHandler.Enums;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace ScriptHandler.Models.ScriptSteps
{
	public class ScriptStepCompareRange : ScriptStepGetParamValue
	{
		public object Value { get; set; }

		public object ValueLeft { get; set; }

		public object ValueRight { get; set; }

		public ComparationTypesEnum Comparation1 { get; set; }

		public ComparationTypesEnum Comparation2 { get; set; }

		public bool IsBetween2Values { get; set; }
		public bool IsValueWithTolerance { get; set; }

		public ScriptStepCompareRange()
		{
			Template = Application.Current.MainWindow.FindResource("AutoRunTemplate") as DataTemplate;
		}


		public override void Execute()
		{
			IsPass = false;
			string errorHeader = "Compare range:\r\n";
			ErrorMessage = errorHeader + "Failed to get the compared parameter for compare range";

			double paramValue = 0;
			string paramName = "";
			bool res = GetValueAndName(
				out paramValue,
				out paramName,
				Value);
			if (!res)
				return;

			ErrorMessage = errorHeader + "Failed to get the left value parameter for compare range";

			double paramValue_Left = 0;
			string paramName_Left = "";
			res = GetValueAndName(
				out paramValue_Left,
				out paramName_Left,
				ValueLeft);
			if (!res)
				return;

			ErrorMessage = errorHeader + "Failed to get the right value parameter for compare range";

			double paramValue_Right = 0;
			string paramName_Right = "";
			res = GetValueAndName(
				out paramValue_Right,
				out paramName_Right,
				ValueRight);
			if (!res)
				return;

			if (IsBetween2Values)
			{
				Compare_Between2Values(
					paramValue,
					paramName,
					paramValue_Left,
					paramName_Left,
					paramValue_Right,
					paramName_Right,
					errorHeader);
			}
			else
			{
				Compare_ValueWithTolerance(
					paramValue,
					paramName,
					paramValue_Left,
					paramName_Left,
					paramValue_Right,
					paramName_Right,
					errorHeader);
			}

		}

		private void Compare_Between2Values(
			double paramValue,
			string paramName,
			double paramValue_Left,
			string paramName_Left,
			double paramValue_Right,
			string paramName_Right,
			string errorHeader)
		{

		
			ErrorMessage =
				errorHeader + 
				paramName_Left + " " + 
				ScriptNodeCompare.GetComperationDescription(Comparation1) + " " +
				paramName + " " +
				ScriptNodeCompare.GetComperationDescription(Comparation2) + " " +
				paramName_Right;

			Compare(
				true,
				paramValue,
				paramValue_Left,
				Comparation1);
			if(!IsPass) 
			{
				return;
			}

			Compare(
				false,
				paramValue_Right,
				paramValue_Right,
				Comparation2);
		}

		private void Compare_ValueWithTolerance(
			double paramValue,
			string paramName,
			double paramValue_Left,
			string paramName_Left,
			double paramValue_Right,
			string paramName_Right,
			string errorHeader)
		{
			ErrorMessage =
				errorHeader + 
				paramName + " = " +
				paramName_Left + " ± " +
				paramName_Right;

			double lowest = paramValue_Left - paramValue_Right;
			double heighest = paramValue_Left + paramValue_Right;

			if(lowest <= paramValue && paramValue <= heighest) 
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
			else if(value is string str)
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



		private void Compare(
			bool isLeft,
			double paramVal,
			double compareVal,
			ComparationTypesEnum comparation)
		{
			switch (comparation)
			{
				case ComparationTypesEnum.Equal:
					if(isLeft)
						IsPass = compareVal == paramVal;
					else
						IsPass = paramVal == compareVal;
					break;
				case ComparationTypesEnum.NotEqual:
					if (isLeft)
						IsPass = compareVal != paramVal;
					else
						IsPass = paramVal != compareVal;
					break;
				case ComparationTypesEnum.Larger:
					if (isLeft)
						IsPass = compareVal > paramVal;
					else
						IsPass = paramVal > compareVal;
					break;
				case ComparationTypesEnum.LargerEqual:
					if (isLeft)
						IsPass = compareVal >= paramVal;
					else
						IsPass = paramVal >= compareVal;
					break;
				case ComparationTypesEnum.Smaller:
					if (isLeft)
						IsPass = compareVal < paramVal;
					else
						IsPass = paramVal < compareVal;
					break;
				case ComparationTypesEnum.SmallerEqual:
					if (isLeft)
						IsPass = compareVal <= paramVal;
					else
						IsPass = paramVal <= compareVal;
					break;
			}

		}

		private object GetCompareParaValue(
			DeviceParameterData parameter)
		{
			//Parameter = parameter;

			if (parameter != null)
			{
				DeviceFullData deviceFullData =
					DevicesList.ToList().Find((d) => d.Device.DeviceType == parameter.DeviceType);
				Communicator = deviceFullData.DeviceCommunicator;
			}

			bool isOK = SendAndReceive(parameter);
			if (!isOK)
			{
				IsPass = false;
				return 0;
			}

			if (Parameter == null)
				return null;

			return Parameter.Value;
		}

		public override bool IsNotSet(
			DevicesContainer devicesContainer,
			ObservableCollection<InvalidScriptItemData> errorsList)
		{
			if (Value == null)
				return true;

			return false;
		}

		public override void Generate(
			ScriptNodeBase sourceNode,
			Dictionary<int, ScriptStepBase> stepNameToObject,
			ref List<DeviceCommunicator> usedCommunicatorsList,
			GenerateProjectService generateService,
			DevicesContainer devicesContainer)
		{
			Value = (sourceNode as ScriptNodeCompareRange).Value;
			ValueLeft = (sourceNode as ScriptNodeCompareRange).ValueLeft;
			ValueRight = (sourceNode as ScriptNodeCompareRange).ValueRight;
			Comparation1 = (sourceNode as ScriptNodeCompareRange).Comparation1;
			Comparation2 = (sourceNode as ScriptNodeCompareRange).Comparation2;
			IsBetween2Values = (sourceNode as ScriptNodeCompareRange).IsBetween2Values;
			IsValueWithTolerance = (sourceNode as ScriptNodeCompareRange).IsValueWithTolerance;
		}
	}
}
