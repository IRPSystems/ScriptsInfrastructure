
using DeviceCommunicators.General;
using DeviceCommunicators.MCU;
using DeviceCommunicators.Models;
using DeviceHandler.Interfaces;
using DeviceHandler.Models;
using DeviceHandler.Models.DeviceFullDataModels;
using Entities.Enums;
using Entities.Models;
using Newtonsoft.Json;
using ScriptHandler.Enums;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Services;
using Services.Services;
using Syncfusion.Windows.Tools.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
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

		private double MeasuredTolerance;

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
			try
			{
				IsPass = false;
				IsExecuted = true;
				string errorHeader = "Compare range:\r\n";
				string errorMessage = errorHeader + "Failed to get the compared parameter for compare range\r\n\r\n";
				_getParamValue.EOLReportsSelectionData = EOLReportsSelectionData;
				OperatorErrorDescription = string.Empty;
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
					OperatorErrorDescription = $"Failed to Get {Parameter.Name} Value";
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
					if(CompareValue is DeviceParameterData paramdata)
						OperatorErrorDescription = $"Failed to Get {paramdata.Name} Value";
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
					stepDescription = UserTitle;


				double minVal;
				double maxVal;

				if (IsPercentageTolerance)
				{
					minVal = paramValue_Right - (paramValue_Right * Tolerance / 100);
					maxVal = paramValue_Right + (paramValue_Right * Tolerance / 100);
				}
				else
				{
					minVal = paramValue_Right - Tolerance;
					maxVal = paramValue_Right + Tolerance;
				}


				EOLStepSummeryData eolStepSummeryData = new EOLStepSummeryData(
					"",
					stepDescription,
					this);

				eolStepSummeryData.MeasuredTolerance = MeasuredTolerance;
				eolStepSummeryData.TestValue = paramValue_Left;
				eolStepSummeryData.ComparisonValue = paramValue_Right;
				eolStepSummeryData.MinVal = minVal;
				eolStepSummeryData.MaxVal = maxVal;
				eolStepSummeryData.Method = ComparationTypesEnum.Tolerance.ToString();
				eolStepSummeryData.Reference = reference;
				eolStepSummeryData.IsPass = IsPass;
				eolStepSummeryData.ErrorDescription = ErrorMessage;
				eolStepSummeryData.Units = Parameter.Units;
				EOLStepSummerysList.Add(eolStepSummeryData);
			}
			catch (Exception ex)
			{
				LoggerService.Error(this, "Failed to compare with tolerance", "Error", ex);
			}
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
                MeasuredTolerance = Math.Abs((paramValue_Left - paramValue_Right) * 100 * 2) /
								 ((paramValue_Left + paramValue_Right));
				
				if (MeasuredTolerance < tolerance)
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
					OperatorErrorDescription = "Value not in range: " + paramValue_Left +
						"\r\nComparison Value: " + paramValue_Right +
						"\r\nAllowed tolerance: " + tolerance;
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
				{
					IsPass = false;
					OperatorErrorDescription = "Value not in range: " + paramValue_Left +
						"\r\nMax limit: " + heighest +
						"\r\nLower Limit: " + lowest;				
                }

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
					"(" + param + " = " + paramValue + ")";

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
            else if (Parameter is MCU_ParamData mcuparam)
            {
                if (mcuparam.DropDown != null)
                {
                    int tvalue = Convert.ToInt32(value);
                    paramName = mcuparam.DropDown.FirstOrDefault(item => item.Value == tvalue.ToString())?.Name;
                    paramValue = Convert.ToDouble(tvalue);
                }
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

				
				if (deviceFullData != null)
					Communicator = deviceFullData.DeviceCommunicator;
			}


			if (!isUseAverage)
				averageOfNRead = 1;
			if(!isUseFactor) 
				factor = 1;

			double avgSum = 0;
			bool NegativeReads = false;
			EOLStepSummeryData eolStepSummeryData = null;
			for (int i = 0; i < averageOfNRead; i++)
			{			

				string description = Description;
				if (!string.IsNullOrEmpty(UserTitle))
					description = UserTitle;

				_getParamValue.Parameter = parameter;
				_getParamValue.Communicator = Communicator;
				bool isOK = _getParamValue.SendAndReceive(
					parameter, 
					out eolStepSummeryData,
					description);
				Thread.Sleep(100);
				if (!isOK)
				{
					ErrorMessage = "Compare Error \r\n"
					+ parameter.ErrorDescription;
					IsPass = false;
					return null;
				}

				double dValue = 0;
				if(parameter.Value is string str)
				{
					if(parameter is IParamWithDropDown dropDown &&
						dropDown.DropDown != null && dropDown.DropDown.Count > 0)
					{
						DropDownParamData dd = dropDown.DropDown.Find((d) => d.Name == str);
						if(dd != null)
							str = dd.Value;

						bool res = double.TryParse(str, out dValue);
					}
				}
				else
					dValue = Convert.ToDouble(parameter.Value);

				if(dValue < 0)
				{
					NegativeReads = true;
				}

				avgSum += Math.Abs(dValue);

				System.Threading.Thread.Sleep(1);
            }

			if (parameter == null)
					return null;
			avgSum = (avgSum / averageOfNRead) * factor;
            
			if(NegativeReads)
			{
				avgSum = -avgSum;
			}

			eolStepSummeryData.TestValue = avgSum;
			EOLStepSummerysList.Add(eolStepSummeryData);

			return avgSum;
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

        public override List<DeviceTypesEnum> GetUsedDevices()
        {
            List<DeviceTypesEnum> UsedDevices = new List<DeviceTypesEnum>();

            if (Parameter is DeviceParameterData deviceParameter)
            {
                UsedDevices.Add(deviceParameter.DeviceType);
            }
            return UsedDevices;
        }

		public override List<string> GetReportHeaders()
		{
			List<string> headers = base.GetReportHeaders();

			if(Parameter != null)
			{
                string stepDescription = headers[0].Trim('\"');

                string description =
                        $"{stepDescription}\r\nGet {Parameter?.Name}";

                headers.Add($"\"{description}\"");

				if (CompareValue is DeviceParameterData compareValue)
				{
					description =
							$"{stepDescription}\r\nGet {compareValue.Name}";

					headers.Add($"\"{description}\"");
				}

            }


            return headers;
		}

		public override List<string> GetReportValues()
		{
			List<string> values = base.GetReportValues();

			EOLStepSummeryData stepSummeryData =
					EOLStepSummerysList.Find((e) =>
						!string.IsNullOrEmpty(e.Description) && e.Description.Contains(Parameter.Name));

			if (stepSummeryData != null)
				values.Add(stepSummeryData.TestValue.ToString());
			else
				values.Add("");

			if (CompareValue is DeviceParameterData compareValue)
			{
				stepSummeryData =
					EOLStepSummerysList.Find((e) =>
						!string.IsNullOrEmpty(e.Description) && e.Description.Contains(compareValue.Name));

				if (stepSummeryData != null)
					values.Add(stepSummeryData.TestValue.ToString());
				else
					values.Add("");

			}


			return values;
		}


		#endregion Methods
	}

}
