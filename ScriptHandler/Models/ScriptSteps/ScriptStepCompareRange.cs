
using DeviceCommunicators.DBC;
using DeviceCommunicators.General;
using DeviceCommunicators.Models;
using DeviceHandler.Interfaces;
using DeviceHandler.Models;
using DeviceHandler.Models.DeviceFullDataModels;
using Entities.Enums;
using ScriptHandler.Enums;
using ScriptHandler.Interfaces;
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
		#region Properties


		private DeviceParameterData _value;
		public DeviceParameterData Value
		{
			get => _value;
			set
			{
				_value = value;
				if (Parameter_ExtraData != null)
					Parameter_ExtraData.Parameter = value;
				OnPropertyChanged(nameof(Parameter));
			}
		}

		private object _valueLeft;
		public object ValueLeft
		{
			get => _valueLeft;
			set
			{
				_valueLeft = value;
				if (_valueLeft is DeviceParameterData && CompareValue_ExtraData != null)
					CompareValue_ExtraData.Parameter = _valueLeft as DeviceParameterData;
				OnPropertyChanged(nameof(ValueLeft));
			}
		}


		private object _valueRight;
		public object ValueRight
		{
			get => _valueRight;
			set
			{
				_valueRight = value;
				if (_valueRight is DeviceParameterData && RightValue_ExtraData != null)
					RightValue_ExtraData.Parameter = _valueRight as DeviceParameterData;
				OnPropertyChanged(nameof(ValueRight));
			}
		}

		public ComparationTypesEnum Comparation1 { get; set; }

		public ComparationTypesEnum Comparation2 { get; set; }

		public bool IsBetween2Values { get; set; }
		public bool IsValueWithTolerance { get; set; }

		public bool IsValueTolerance { get; set; }
		public bool IsPercentageTolerance { get; set; }



		public ExtraDataForParameter Parameter_ExtraData { get; set; }
		public ExtraDataForParameter CompareValue_ExtraData { get; set; }
		public ExtraDataForParameter RightValue_ExtraData { get; set; }

		#endregion Properties

		#region Constructor

		public ScriptStepCompareRange()
		{
			if (Application.Current != null)
				Template = Application.Current.MainWindow.FindResource("AutoRunTemplate") as DataTemplate;
			_totalNumOfSteps = 6;
		}

		#endregion Constructor

		#region Methodes

		public override void Execute()
		{
			ErrorMessage = string.Empty;
			IsPass = false;
			IsExecuted = true;
			string errorHeader = "Compare range:\r\n";
			string errorMessage = errorHeader + "Failed to get the compared parameter for compare range\r\n\r\n";

			_stepsCounter = 1;

			string units = "";
			double paramValue = 0;
			string paramName = "";
			bool res = GetValueAndName(
				out paramValue,
				out paramName,
				out units,
				Value,
				"parameter");
			if (!res)
			{
				ErrorMessage = errorMessage + ErrorMessage;
				IsPass = false;
				return;
			}

			string mainUnits = units;

			ErrorMessage = errorHeader + "Failed to get the left value parameter for compare range";

			_stepsCounter++;

			double paramValue_Left = 0;
			string paramName_Left = "";
			res = GetValueAndName(
				out paramValue_Left,
				out paramName_Left,
				out units,
				ValueLeft,
				"comparevalue");
			if (!res)
			{
				ErrorMessage = errorMessage + ErrorMessage;
				IsPass = false;
				return;
			}

			ErrorMessage = errorHeader + "Failed to get the right value parameter for compare range";

			_stepsCounter++;

			double paramValue_Right = 0;
			string paramName_Right = "";
			
			res = GetValueAndName(
				out paramValue_Right,
				out paramName_Right,
				out units,
				ValueRight,
				"rightvalue");
			if (!res)
			{
				ErrorMessage = errorMessage + ErrorMessage;
				IsPass = false;
				return;
			}

			_stepsCounter++;

			string comparisonMethod;

			if (IsBetween2Values)
			{
				comparisonMethod = ComparationTypesEnum.BetweenRange.ToString();

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
                comparisonMethod = ComparationTypesEnum.Tolerance.ToString();
                Compare_ValueWithTolerance(
					paramValue,
					paramName,
					paramValue_Left,
					paramName_Left,
					paramValue_Right,
					paramName_Right,
					errorHeader);
			}



            string stepDescription = Description;
            if (!string.IsNullOrEmpty(UserTitle))
                stepDescription = UserTitle;
           
            EOLStepSummeryData eolStepSummeryData = new EOLStepSummeryData(
				"",
				stepDescription,
				this);

            eolStepSummeryData.TestValue = paramValue;
            eolStepSummeryData.ComparisonValue = paramValue_Right;
            eolStepSummeryData.MinVal = paramValue_Left;
            eolStepSummeryData.MaxVal = paramValue_Right;
            eolStepSummeryData.Method = comparisonMethod;
            eolStepSummeryData.IsPass = IsPass;
            eolStepSummeryData.ErrorDescription = ErrorMessage;
			eolStepSummeryData.Units = mainUnits;
			EOLStepSummerysList.Add(eolStepSummeryData);
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

			_stepsCounter++;

			Compare(
				false,
				paramValue,
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
			out string units,
			object value,
			string paramDesc)
		{
			paramValue = 0;
			paramName = "";
			units = "";

			if (value is DeviceParameterData param)
			{
				units = param.Units;
				object val = GetCompareParaValue(param, paramDesc);
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
			DeviceParameterData parameter,
			string paramDesc)
		{
			//Parameter = parameter;

			if (parameter != null)
			{
				if (parameter is DBC_ParamData)
				{
					DeviceFullData deviceFullData =
						DevicesContainer.TypeToDevicesFullData[DeviceTypesEnum.MCU];
					Communicator = deviceFullData.DeviceCommunicator;
				}
				else
				{
					DeviceFullData deviceFullData =
						DevicesContainer.DevicesFullDataList.ToList().Find((d) => d.Device.DeviceType == parameter.DeviceType);
					Communicator = deviceFullData.DeviceCommunicator;
				}
			}

			if (paramDesc == "parameter")
			{
				Parameter_ExtraData.Parameter = parameter;
				Parameter_ExtraData.SetToParameter(parameter);
			}
			else if (paramDesc == "comparevalue")
			{
				CompareValue_ExtraData.Parameter = parameter;
				CompareValue_ExtraData.SetToParameter(parameter);
			}
			else if (paramDesc == "rightvalue")
			{
				RightValue_ExtraData.Parameter = parameter;
				RightValue_ExtraData.SetToParameter(parameter);
			}

			string description = Description;
            if (string.IsNullOrEmpty(UserTitle) == false)
				description = UserTitle;

            EOLStepSummeryData eolStepSummeryData;
			bool isOK = SendAndReceive(parameter, out eolStepSummeryData, description);
			EOLStepSummerysList.Add(eolStepSummeryData);
			if (!isOK)
			{
                IsPass = false;

				string paramName = "Unknown";
				DeviceTypesEnum paramDeviceType = DeviceTypesEnum.None;
				CommSendResLog paramComSendResLog = null;
				if (parameter != null)
				{
					paramName = parameter.Name;
					paramDeviceType = parameter.DeviceType;
					paramComSendResLog = parameter.CommSendResLog;
				}

				PopulateSendResponseLog(
					UserTitle,
					this.GetType().Name,
					paramName,
					paramDeviceType,
					paramComSendResLog);
                return 0;
			}

			if (parameter == null)
				return null;
            PopulateSendResponseLog(UserTitle, this.GetType().Name, parameter.Name, parameter.DeviceType, parameter.CommSendResLog);
            return parameter.Value;
		}

		public override bool IsNotSet(
			DevicesContainer devicesContainer,
			ObservableCollection<InvalidScriptItemData> errorsList)
		{
			if (Value == null)
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
			Value = (sourceNode as ScriptNodeCompareRange).Parameter;
			ValueLeft = (sourceNode as ScriptNodeCompareRange).CompareValue as DeviceParameterData;
			ValueRight = (sourceNode as ScriptNodeCompareRange).ValueRight;
			Comparation1 = (sourceNode as ScriptNodeCompareRange).Comparation1;
			Comparation2 = (sourceNode as ScriptNodeCompareRange).Comparation2;
			IsBetween2Values = (sourceNode as ScriptNodeCompareRange).IsBetween2Values;
			IsValueWithTolerance = (sourceNode as ScriptNodeCompareRange).IsValueWithTolerance;
			IsValueTolerance = (sourceNode as ScriptNodeCompareRange).IsValueTolerance;
			IsPercentageTolerance = (sourceNode as ScriptNodeCompareRange).IsPercentageTolerance;




			Parameter_ExtraData = new ExtraDataForParameter((sourceNode as ScriptNodeCompareRange).Parameter_ExtraData);
			CompareValue_ExtraData = new ExtraDataForParameter((sourceNode as ScriptNodeCompareRange).CompareValue_ExtraData);
			RightValue_ExtraData = new ExtraDataForParameter((sourceNode as ScriptNodeCompareRange).RightValue_ExtraData);
		}

		public override void GetRealParamAfterLoad(
			DevicesContainer devicesContainer)
		{
			base.GetRealParamAfterLoad(devicesContainer);

			if (Value is DeviceParameterData)
			{
				if (Value is ICalculatedParamete)
					return;

				Value = GetRealParam(
					Value as DeviceParameterData,
					devicesContainer);
			}
		}

        public override List<DeviceTypesEnum> GetUsedDevices()
        {
            List<DeviceTypesEnum> UsedDevices = new List<DeviceTypesEnum>();

            if (ValueLeft is DeviceParameterData deviceLeft)
            {
                UsedDevices.Add(deviceLeft.DeviceType);
            }
            if (ValueRight is DeviceParameterData deviceRight)
            {
                UsedDevices.Add(deviceRight.DeviceType);
            }
            return UsedDevices;
        }

		public override List<string> GetReportHeaders()
		{
			List<string> headers = base.GetReportHeaders();

			string stepDescription = headers[0].Trim('\"');

			if (Value is DeviceParameterData valueParam)
			{
				string description =
						$"{stepDescription}\r\nGet {valueParam.Name}";

				headers.Add($"\"{description}\"");
			}

			if (ValueLeft is DeviceParameterData valueLeft)
			{
				string description =
						$"{stepDescription}\r\nGet {valueLeft.Name}";

				headers.Add($"\"{description}\"");
			}

			if (ValueRight is DeviceParameterData valueRight)
			{
				string description =
						$"{stepDescription}\r\nGet {valueRight.Name}";

				headers.Add($"\"{description}\"");
			}

			return headers;
		}

		public override List<string> GetReportValues()
		{
			List<string> values = base.GetReportValues();

			if(Value != null || ValueLeft != null || ValueRight != null)
			{
                if (Value is DeviceParameterData valueParam)
                {
                    EOLStepSummeryData stepSummeryData =
                        EOLStepSummerysList.Find((e) =>
                            !string.IsNullOrEmpty(e.Description) && e.Description.Contains(valueParam.Name));

                    if (stepSummeryData != null)
                        values.Add(stepSummeryData.TestValue.ToString());
                    else
                        values.Add("");

                }

                if (ValueLeft is DeviceParameterData valueLeft)
                {
                    EOLStepSummeryData stepSummeryData =
                        EOLStepSummerysList.Find((e) =>
                            !string.IsNullOrEmpty(e.Description) && e.Description.Contains(valueLeft.Name));

                    if (stepSummeryData != null)
                        values.Add(stepSummeryData.TestValue.ToString());
                    else
                        values.Add("");

                }

                if (ValueRight is DeviceParameterData valueRight)
                {
                    EOLStepSummeryData stepSummeryData =
                        EOLStepSummerysList.Find((e) =>
                            !string.IsNullOrEmpty(e.Description) && e.Description.Contains(valueRight.Name));

                    if (stepSummeryData != null)
                        values.Add(stepSummeryData.TestValue.ToString());
                    else
                        values.Add("");

                }
            }

			IsExecuted = false;

			return values;
		}

		#endregion Methodes
	}
}
