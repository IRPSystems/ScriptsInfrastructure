
using DeviceCommunicators.DBC;
using DeviceCommunicators.General;
using DeviceCommunicators.MCU;
using DeviceCommunicators.Models;
using DeviceHandler.Interfaces;
using DeviceHandler.Models;
using DeviceHandler.Models.DeviceFullDataModels;
using Entities.Enums;
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
using System.IO;
using System.Linq;
using System.Windows;

namespace ScriptHandler.Models
{
	public class ScriptStepCompare: ScriptStepGetParamValue
	{
		private DeviceParameterData _valueLeft;
		public DeviceParameterData ValueLeft
		{
			get => _valueLeft;
			set
			{
				_valueLeft = value;
				if (Parameter_ExtraData != null)
					Parameter_ExtraData.Parameter = value;
				OnPropertyChanged(nameof(Parameter));
			}
		}

		private object _valueRight;
		public object ValueRight
		{
			get => _valueRight;
			set
			{
				_valueRight = value;
				if (_valueRight is DeviceParameterData && CompareValue_ExtraData != null)
					CompareValue_ExtraData.Parameter = _valueRight as DeviceParameterData;
				OnPropertyChanged(nameof(ValueRight));
			}
		}

		public ComparationTypesEnum Comparation { get; set; }

		public bool IsUseAverage { get; set; }
		public int AverageOfNRead { get; set; }

		public ExtraDataForParameter Parameter_ExtraData { get; set; }
		public ExtraDataForParameter CompareValue_ExtraData { get; set; }

		public ScriptStepCompare()
		{
			if (Application.Current != null)
				Template = Application.Current.MainWindow.FindResource("AutoRunTemplate") as DataTemplate;
			_totalNumOfSteps = 5;
		}

		public override void Execute()
		{
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

			try
			{
				IsPass = false;
				IsExecuted = true;

				if (!IsUseAverage)
					AverageOfNRead = 1;

				double leftVal = 0;
				string leftParamName = "";
				_stepsCounter = 1;


				string units = "";
				if (ValueLeft is DeviceParameterData paramLeft)
				{
					int intValue = 0;

					double sum = 0;
					for (int i = 0; i < AverageOfNRead; i++)
					{
						object val = GetCompareParaValue(true, paramLeft);

						if (val == null || IsPass == false)
						{
							IsError = true;
							return;
						}

						if (val is string strval && strval.StartsWith("0x"))
						{
							string hexSubstring = strval.Substring(2);
							bool isSuccess = int.TryParse(hexSubstring, System.Globalization.NumberStyles.HexNumber, null, out intValue);
							val = intValue;
							if (!isSuccess)
							{ return; }

							val = intValue;
						}
						if (val is string)
						{
							if (paramLeft is MCU_ParamData mCU_ParamData)
							{
								foreach (DropDownParamData dropDown in mCU_ParamData.DropDown)
								{
									if (dropDown.Name == (string)val)
									{
										sum = Convert.ToDouble(dropDown.Value);
										break;
									}
								}
							}
						}
						else
						{
							sum += Convert.ToDouble(val);
						}




						System.Threading.Thread.Sleep(1);
					}

					leftVal = sum / AverageOfNRead;
					leftParamName = paramLeft.Name;

					units = paramLeft.Units;
				}

				_stepsCounter++;
				double? rightVal = 0;
				string rightParamName = "";
				string compareReference = "Fixed Value";
				if (ValueRight is DeviceParameterData paramRight)
				{
					compareReference = paramRight.DeviceType.ToString();

					object val = GetCompareParaValue(false, paramRight);
					if (val == null || IsPass == false)
					{
						IsError = null;
						return;
					}
					rightVal = Convert.ToDouble(val);
					rightParamName = paramRight.Name;
				}
				else
                    rightVal = Convert.ToDouble(ValueRight);
                if (rightVal == null)
				{
					IsError = true;
					return;
				}


				ErrorMessage = leftParamName + " = " + leftVal + "; ";
				if (!string.IsNullOrEmpty(rightParamName))
				{
					ErrorMessage += rightParamName + " = " + rightVal + "; ";
				}
				else
					ErrorMessage += "The value = " + rightVal + "; ";

				_stepsCounter++;



				Compare(leftVal, (double)rightVal);

				_stepsCounter++;

				string stepDescription = Description;
				if (!string.IsNullOrEmpty(UserTitle))
					stepDescription = UserTitle;


				EOLStepSummeryData eolStepSummeryData = new EOLStepSummeryData(
					"",
					stepDescription,
					this);

				eolStepSummeryData.TestValue = leftVal;
				eolStepSummeryData.ComparisonValue = rightVal;
				eolStepSummeryData.Reference = compareReference;
				eolStepSummeryData.Method = Comparation.ToString();
				eolStepSummeryData.IsPass = IsPass;
				eolStepSummeryData.ErrorDescription = ErrorMessage;
				eolStepSummeryData.Units = units;

				EOLStepSummerysList.Add(eolStepSummeryData);


				#region Log comparation
				string str = leftParamName + " = " + leftVal + "; ";
				if (!string.IsNullOrEmpty(rightParamName))
				{
					str += rightParamName + " = " + rightVal + "; ";
				}
				else
					str += "The value = " + rightVal + "; ";

				str += "\r\n" + "IsPass = " + IsPass;

				LoggerService.Inforamtion(this, str);
			}
			finally 
			{
                //finished derived class execute method
                stopwatch.Stop();
                ExecutionTime = stopwatch.Elapsed;
            }

            #endregion Log comparation
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
			bool isParameter,
			DeviceParameterData parameter)
		{
			Parameter = parameter;

			if (parameter != null)
			{
				DeviceFullData deviceFullData =
						DevicesContainer.GetDeviceFullData(parameter);
				if (parameter is DBC_ParamData)
				{
					deviceFullData =
						DevicesContainer.TypeToDevicesFullData[DeviceTypesEnum.MCU];
				}

				Communicator = deviceFullData.DeviceCommunicator;
			}

			if (isParameter && Parameter_ExtraData != null)
			{
				Parameter_ExtraData.Parameter = parameter;
				Parameter_ExtraData.SetToParameter(parameter);
			}
			else if (CompareValue_ExtraData != null)
			{
				CompareValue_ExtraData.Parameter = parameter;
				CompareValue_ExtraData.SetToParameter(parameter);
			}

			string description = Description;
			if(string.IsNullOrEmpty(UserTitle) == false)
				description = UserTitle;

			EOLStepSummeryData eolStepSummeryData;
			bool isOK = SendAndReceive(
				parameter, 
				out eolStepSummeryData,
				description);


			EOLStepSummerysList.Add(eolStepSummeryData);
			if (!isOK)
			{
				IsPass = false;
                PopulateSendResponseLog(UserTitle, this.GetType().Name, parameter.Name, parameter.DeviceType, parameter.CommSendResLog);
                return null;
			}
			LoggerService.Error(this, " value "+parameter.Value.ToString());
            PopulateSendResponseLog(UserTitle, this.GetType().Name, parameter.Name, parameter.DeviceType, parameter.CommSendResLog);
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

		protected override void Generate(
			ScriptNodeBase sourceNode,
			Dictionary<int, ScriptStepBase> stepNameToObject,
			ref List<DeviceCommunicator> usedCommunicatorsList,
			GenerateProjectService generateService,
			DevicesContainer devicesContainer)
		{
			ValueLeft = (sourceNode as ScriptNodeCompare).Parameter;
			ValueRight = (sourceNode as ScriptNodeCompare).CompareValue;
			Comparation = (sourceNode as ScriptNodeCompare).Comparation;


			Parameter_ExtraData = new ExtraDataForParameter((sourceNode as ScriptNodeCompare).Parameter_ExtraData);
			CompareValue_ExtraData = new ExtraDataForParameter((sourceNode as ScriptNodeCompare).CompareValue_ExtraData);



			IsUseAverage = (sourceNode as ScriptNodeCompare).IsUseAverage;
			AverageOfNRead = (sourceNode as ScriptNodeCompare).AverageOfNRead;
		}

		public override void GetRealParamAfterLoad(
			DevicesContainer devicesContainer)
		{
			if (ValueLeft is DeviceParameterData)
			{
				if (ValueLeft is ICalculatedParamete ||
					ValueLeft is DBC_ParamData)
					return;

				ValueLeft = GetRealParam(
					ValueLeft as DeviceParameterData,
					devicesContainer);
			}

			if (ValueRight is DeviceParameterData)
			{
				if (ValueRight is ICalculatedParamete ||
					ValueLeft is DBC_ParamData)
					return;

				ValueRight = GetRealParam(
					ValueRight as DeviceParameterData,
					devicesContainer);
			}
			DevicesContainer = devicesContainer;
		}

        public override List<DeviceTypesEnum> GetUsedDevices()
		{
			List<DeviceTypesEnum> UsedDevices = new List<DeviceTypesEnum>();
            
			if(ValueLeft is DeviceParameterData deviceLeft)
			{
				UsedDevices.Add(deviceLeft.DeviceType);
            }
			if(ValueRight is DeviceParameterData deviceRight)
			{
                UsedDevices.Add(deviceRight.DeviceType);
            }
			return UsedDevices;
        }

		public override List<string> GetReportHeaders()
		{
			List<string> headers = base.GetReportHeaders();

			string stepDescription = headers[0].Trim('\"');

			if(ValueLeft != null || ValueRight != null)
			{
                if (ValueLeft is DeviceParameterData paramLeft)
                {
                    string description =
                        $"{stepDescription}\r\nGet {paramLeft.Name}";

                    headers.Add($"\"{description}\"");
                }

                if (ValueRight is DeviceParameterData paramRight)
                {
                    string description =
                        $"{stepDescription}\r\nGet {paramRight.Name}";

                    headers.Add($"\"{description}\"");
                }
            }

			return headers;
		}

		public override List<string> GetReportValues()
		{
			List<string> values = base.GetReportValues();

			if (ValueLeft is DeviceParameterData paramLeft)
			{
				EOLStepSummeryData stepSummeryData =
					EOLStepSummerysList.Find((e) =>
						!string.IsNullOrEmpty(e.Description) && e.Description.Contains(paramLeft.Name));

				if (stepSummeryData != null)
					values.Add(stepSummeryData.TestValue.ToString());
				else
					values.Add("");

			}

			if (ValueRight is DeviceParameterData paramRight)
			{
				EOLStepSummeryData stepSummeryData =
					EOLStepSummerysList.Find((e) =>
						!string.IsNullOrEmpty(e.Description) && e.Description.Contains(paramRight.Name));

				if (stepSummeryData != null)
					values.Add(stepSummeryData.TestValue.ToString());
				else
					values.Add("");

			}

			IsExecuted = false;

			return values;
		}
	}
}
