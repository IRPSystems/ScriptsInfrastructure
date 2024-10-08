﻿
using DeviceCommunicators.General;
using DeviceCommunicators.MCU;
using DeviceCommunicators.Models;
using DeviceCommunicators.NI_6002;
using DeviceCommunicators.ZimmerPowerMeter;
using DeviceHandler.Interfaces;
using DeviceHandler.Models;
using DeviceHandler.Models.DeviceFullDataModels;
using Entities.Models;
using Newtonsoft.Json;
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
using System.Reflection.Metadata;
using System.Windows;

namespace ScriptHandler.Models
{
	public class ScriptStepCompareWithTolerance : ScriptStepBase
	{
		#region Properties

		private DeviceParameterData _parameter;
		public DeviceParameterData Parameter
		{
			get => _parameter;
			set
			{
				_parameter = value;
				if(Parameter_ExtraData != null) 
					Parameter_ExtraData.Parameter = value;
				OnPropertyChanged(nameof(Parameter));
			}
		}

		private object _compareValue;
		public object CompareValue
		{
			get => _compareValue;
			set
			{
				_compareValue = value;
				if (_compareValue is DeviceParameterData && CompareValue_ExtraData != null)
					CompareValue_ExtraData.Parameter = _compareValue as DeviceParameterData;
				OnPropertyChanged(nameof(CompareValue));
			}
		}

		

		public double Tolerance { get; set; }

		public ComparationTypesEnum Comparation { get; set; }

		public bool IsValueTolerance { get; set; }
		public bool IsPercentageTolerance { get; set; }

		public bool IsUseParamAverage { get; set; }
		public int AverageOfNRead_Param { get; set; }

		public bool IsUseParamFactor { get; set; }
		public double ParamFactor { get; set; }


		public bool IsUseCompareValueAverage { get; set; }
		public int AverageOfNRead_CompareValue { get; set; }

		public bool IsUseCompareValueFactor { get; set; }
		public double CompareValueFactor { get; set; }


		public ExtraDataForParameter Parameter_ExtraData { get; set; }
		public ExtraDataForParameter CompareValue_ExtraData { get; set; }

		[JsonIgnore]
		public DevicesContainer DevicesContainer { get; set; }

		[JsonIgnore]
		public DeviceCommunicator Communicator { get; set; }

		private ScriptStepGetParamValue _getParamValue;

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

			_getParamValue = new ScriptStepGetParamValue();
		}

		#endregion Constructor

		#region Methods

		public override void Execute()
		{
			IsPass = false;
			string errorHeader = "Compare range:\r\n";
			string errorMessage = errorHeader + "Failed to get the compared parameter for compare range\r\n\r\n";

			_stepsCounter = 1;

			double paramValue_Left = 0;
			string paramName_Left = "";

			bool res = GetValueAndName(
				true,
				IsUseParamAverage,
				AverageOfNRead_Param,
				IsUseParamFactor,
				ParamFactor,
				out paramValue_Left,
				out paramName_Left,
				Parameter);
			if (!res)
			{
				ErrorMessage = errorMessage + ErrorMessage;
				IsPass = false;
				return;
			}

			ErrorMessage = errorHeader + "Failed to get the left value parameter for compare range";

			_stepsCounter++;

			double paramValue_Right = 0;
			string paramName_Right = "";
			res = GetValueAndName(
				false,
				IsUseCompareValueAverage,
				AverageOfNRead_CompareValue,
				IsUseCompareValueFactor,
				CompareValueFactor,
				out paramValue_Right,
				out paramName_Right,
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
					paramValue_Left,
					paramName_Left,
					paramValue_Right,
					paramName_Right,
					Tolerance,
					errorHeader);

			string reference = "Fixed Value";
            if (CompareValue is DeviceParameterData param)
			{
				reference = param.DeviceType.ToString();
            }

                string stepDescription = Description;
            if (!string.IsNullOrEmpty(UserTitle))
            {
                stepDescription = UserTitle + " - Result";
            }

			double minVal;
			double maxVal;

			if(IsPercentageTolerance)
			{
                minVal = paramValue_Right - (paramValue_Right * Tolerance / 100);
                maxVal = paramValue_Right - (paramValue_Right * Tolerance / 100);
            }
			else
			{
				minVal = paramValue_Right - Tolerance;
				maxVal = paramValue_Right + Tolerance;
			}


            EOLStepSummeryData eolStepSummeryData = new EOLStepSummeryData(
				stepDescription,
				Description);

            eolStepSummeryData.TestValue = paramValue_Left;
            eolStepSummeryData.ComparisonValue = paramValue_Right;
            eolStepSummeryData.MinVal = minVal;
            eolStepSummeryData.MaxVal = maxVal;
            eolStepSummeryData.Method = ComparationTypesEnum.Tolerance.ToString();
			eolStepSummeryData.Reference = reference;
            eolStepSummeryData.IsPass = IsPass;
            eolStepSummeryData.ErrorDescription = ErrorMessage;
            EOLStepSummerysList.Add(eolStepSummeryData);
        }

		private void Compare_ValueWithTolerance(
			double paramValue_Left,
			string paramName_Left,
			double paramValue_Right,
			string paramName_Right,
			double tolerance,
			string errorHeader)
		{
			ErrorMessage =
				errorHeader +
				paramName_Left + " = " +
				paramName_Right + " ± " +
				tolerance;

			if (IsPercentageTolerance)
			{
				double deviationPrecentage = Math.Abs((paramValue_Left - paramValue_Right) * 100 * 2) /
								 ((paramValue_Left + paramValue_Right));
				if (deviationPrecentage < tolerance)
				{
					IsPass = true;
				}
				else
				{
                    ErrorMessage =
						errorHeader + "Tolerance deviation exceeds limits" +
						paramName_Left + " = " +
						paramName_Right + " ± " +
						tolerance;
                    IsPass = false;
				}
			}
			else
			{
                double lowest = paramValue_Right - tolerance;
                double heighest = paramValue_Right + tolerance;

                if (lowest <= paramValue_Left && paramValue_Left <= heighest)
                    IsPass = true;
                else
                    IsPass = false;
            }
		}


		private bool GetValueAndName(
			bool isParameter,
			bool isUseAverage,
			int averageOfNRead,
			bool isUseFactor,
			double factor,
			out double paramValue,
			out string paramName,
			object value)
		{
			paramValue = 0;
			paramName = "";

			if (value is DeviceParameterData param)
			{
				if (isParameter)
				{
					Parameter_ExtraData.Parameter = param;
					Parameter_ExtraData.SetToParameter(param);
				}
				else
				{
					CompareValue_ExtraData.Parameter = param;
					CompareValue_ExtraData.SetToParameter(param);
				}

				object val = GetCompareParaValue(
					isUseAverage,
					averageOfNRead,
					isUseFactor,
					factor, 
					param);
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
			bool isUseAverage,
			int averageOfNRead,
			bool isUseFactor,
			double factor,
			DeviceParameterData parameter)
		{
			//Parameter = parameter;

			if (parameter != null && DevicesContainer != null)
			{
				DeviceFullData deviceFullData =
					DevicesContainer.DevicesFullDataList.ToList().Find((d) => d.Device.DeviceType == parameter.DeviceType);
				Communicator = deviceFullData.DeviceCommunicator;
			}


			if (!isUseAverage)
				averageOfNRead = 1;
			if(!isUseFactor) 
				factor = 1;

			double avgSum = 0;
			for (int i = 0; i < averageOfNRead; i++)
			{
				EOLStepSummeryData eolStepSummeryData;

				_getParamValue.Parameter = parameter;
				_getParamValue.Communicator = Communicator;

				bool isOK = _getParamValue.SendAndReceive(parameter, out eolStepSummeryData);
				EOLStepSummerysList.Add(eolStepSummeryData);
				if (!isOK)
				{
					ErrorMessage = "Compare Error \r\n"
					+ parameter.ErrorDescription;
					IsPass = false;
					return 0;
				}
				avgSum += Convert.ToDouble(parameter.Value);
            }

			if (parameter == null)
					return null;
			
			return (avgSum / averageOfNRead) * factor;
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

			IsUseParamAverage = (sourceNode as ScriptNodeCompareWithTolerance).IsUseParamAverage;
			AverageOfNRead_Param = (sourceNode as ScriptNodeCompareWithTolerance).AverageOfNRead_Param;
			IsUseParamFactor = (sourceNode as ScriptNodeCompareWithTolerance).IsUseParamFactor;
			ParamFactor = (sourceNode as ScriptNodeCompareWithTolerance).ParamFactor;

			IsUseCompareValueAverage = (sourceNode as ScriptNodeCompareWithTolerance).IsUseCompareValueAverage;
			AverageOfNRead_CompareValue = (sourceNode as ScriptNodeCompareWithTolerance).AverageOfNRead_CompareValue;
			IsUseCompareValueFactor = (sourceNode as ScriptNodeCompareWithTolerance).IsUseCompareValueFactor;
			CompareValueFactor = (sourceNode as ScriptNodeCompareWithTolerance).CompareValueFactor;


			Parameter_ExtraData = new ExtraDataForParameter((sourceNode as ScriptNodeCompareWithTolerance).Parameter_ExtraData);
			CompareValue_ExtraData = new ExtraDataForParameter((sourceNode as ScriptNodeCompareWithTolerance).CompareValue_ExtraData);

			DevicesContainer = devicesContainer;

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

			DevicesContainer = devicesContainer;
		}

		#endregion Methods
	}
}
