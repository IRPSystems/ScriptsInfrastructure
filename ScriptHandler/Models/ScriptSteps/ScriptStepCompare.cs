
using DeviceCommunicators.General;
using DeviceCommunicators.Models;
using DeviceHandler.Interfaces;
using DeviceHandler.Models;
using DeviceHandler.Models.DeviceFullDataModels;
using Entities.Models;
using Newtonsoft.Json;
using ScriptHandler.Enums;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace ScriptHandler.Models
{
	public class ScriptStepCompare: ScriptStepGetParamValue
	{
		public object ValueLeft { get; set; }
		public object ValueRight { get; set; }

		public ComparationTypesEnum Comparation { get; set; }

		public ScriptStepCompare()
		{
			Template = Application.Current.MainWindow.FindResource("AutoRunTemplate") as DataTemplate;
		}

		public override void Execute()
		{
			IsPass = false;

			double leftVal = 0;
			string leftParamName = "";
			if (ValueLeft is DeviceParameterData paramLeft)
			{
				object val = GetCompareParaValue(paramLeft);
				if (val == null)
					return;

				leftVal = Convert.ToDouble(val);
				leftParamName = paramLeft.Name;
			}

			double? rightVal = 0;
			string rightParamName = "";
			if (ValueRight is DeviceParameterData paramRight)
			{
				object val = GetCompareParaValue(paramRight);
				if (val == null)
					return;

				rightVal = Convert.ToDouble(val);
				rightParamName = paramRight.Name;
			}
			else
				rightVal = ValueRight as double?;
			if(rightVal == null)
			{
				return;
			}

			ErrorMessage = leftParamName + " = " + leftVal + "; ";
			if(!string.IsNullOrEmpty(rightParamName))
			{
				ErrorMessage += rightParamName + " = " + rightVal + "; ";
			}
			else
				ErrorMessage += "The value = " + rightVal + "; ";


			Compare(leftVal, (double)rightVal);
		}

		private void Compare(
			double leftVal,
			double rightVal)
		{
			switch (Comparation)
			{
				case ComparationTypesEnum.Equal:
					IsPass = leftVal == rightVal;
					ErrorMessage += " The values not equal";
					break;
				case ComparationTypesEnum.NotEqual:
					IsPass = leftVal != rightVal;
					ErrorMessage += " The values are equal";
					break;
				case ComparationTypesEnum.Larger:
					IsPass = leftVal > rightVal;
					ErrorMessage += " The first value is not larger than the second";
					break;
				case ComparationTypesEnum.LargerEqual:
					IsPass = leftVal >= rightVal;
					ErrorMessage += " The first value is not larger/equal than the second";
					break;
				case ComparationTypesEnum.Smaller:
					IsPass = leftVal < rightVal;
					ErrorMessage += " The first value is not smaller than the second";
					break;
				case ComparationTypesEnum.SmallerEqual:
					IsPass = leftVal <= rightVal;
					ErrorMessage += " The first value is not smaller/equal than the second";
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

			if (parameter is ICalculatedParamete calculated)
			{
				calculated.Calculate();
				if(parameter.Value == null || double.IsNaN((double)parameter.Value))
				{
					IsPass = false;
					return 0;
				}

				return parameter.Value;
			}

			bool isOK = SendAndReceive(parameter);
			if (!isOK)
			{
				IsPass = false;
				return 0;
			}

			return parameter.Value;
		}

		public override bool IsNotSet(
			DevicesContainer devicesContainer,
			ObservableCollection<InvalidScriptItemData> errorsList)
		{
			if (ValueLeft == null)
				return true;

			if (ValueRight == null)
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
			ValueLeft = (sourceNode as ScriptNodeCompare).ValueLeft;
			ValueRight = (sourceNode as ScriptNodeCompare).ValueRight;
			Comparation = (sourceNode as ScriptNodeCompare).Comparation;
		}
	}
}
