
using DeviceCommunicators.EvvaDevice;
using DeviceCommunicators.General;
using DeviceCommunicators.MCU;
using DeviceCommunicators.Models;
using DeviceHandler.Interfaces;
using DeviceHandler.Models;
using DeviceHandler.Models.DeviceFullDataModels;
using Entities.Enums;
using Microsoft.Win32;
using Newtonsoft.Json;
using ScriptHandler.Interfaces;
using ScriptHandler.Models;
using ScriptHandler.Models.ScriptSteps;
using ScriptHandler.Services;
using ScriptHandler.ViewModels;
using Services.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Windows;
using System.Windows.Media;

namespace ScriptRunner.Services
{
	public class OpenProjectForRunService
	{
		public GeneratedProjectData Open(
			ScriptUserData scriptUserData,
			DevicesContainer devicesContainer,
			FlashingHandler flashingHandler,
			StopScriptStepService stopScriptStep)
		{
			string scriptsPath = null;

			try
			{
				string initDir = scriptUserData.LastRunScriptPath;
				if (string.IsNullOrEmpty(initDir))
					initDir = "";
				if (Directory.Exists(initDir) == false)
					initDir = "";

				OpenFileDialog openFileDialog = new OpenFileDialog();
				openFileDialog.Filter = "Generated Project Files|*.gprj|Test and Scripts Files|*.tst;*.scr";
				openFileDialog.InitialDirectory = initDir;
				bool? result = openFileDialog.ShowDialog();
				if (result != true)
					return null;

				scriptsPath = openFileDialog.FileName;
				scriptUserData.LastRunScriptPath =
					Path.GetDirectoryName(scriptsPath);

				GeneratedProjectData project = Open(scriptsPath, devicesContainer, flashingHandler, stopScriptStep);
				return project;
			}
			catch (Exception ex)
			{
				LoggerService.Error(this, "Failed to open project \"" + scriptsPath + "\"", "Open Project Error", ex);
				return null;
			}
		}

		public GeneratedProjectData Open(
			string path,
			DevicesContainer devicesContainer,
			FlashingHandler flashingHandler,
			StopScriptStepService stopScriptStep)
		{
			if (string.IsNullOrEmpty(path))
				return null;


			string extension = Path.GetExtension(path);

			GeneratedProjectData currentProject = null;
			if (extension == ".gprj")
			{
				FixOldScriptsAndProjectsService fixGeneratedProjectService = new FixOldScriptsAndProjectsService();
				fixGeneratedProjectService.Fix(path);


				string jsonString = System.IO.File.ReadAllText(path);

				JsonSerializerSettings settings = new JsonSerializerSettings();
				settings.Formatting = Formatting.Indented;
				settings.TypeNameHandling = TypeNameHandling.All;
				currentProject = JsonConvert.DeserializeObject(jsonString, settings) as
					GeneratedProjectData;
			}
			else if (extension == ".scr" || extension == ".tst")
			{
				GeneratedScriptData script = GetSingleScript(path, devicesContainer, flashingHandler);
				currentProject = new GeneratedProjectData()
				{
					TestsList = new ObservableCollection<GeneratedScriptData>() { script },
				};
			}

			if (currentProject == null)
				return null;

			currentProject.ProjectPath = path;


			IsTestValidService isTestValidService = new IsTestValidService();
			


			foreach (GeneratedScriptData scriptData in currentProject.TestsList)
			{
				

				if (!(scriptData is GeneratedTestData testData))
					continue;

				int totlsRunSteps = 0;
				GetTotlsRunSteps(
					testData,
					ref totlsRunSteps);
				testData.TotalRunSteps = totlsRunSteps;

				IsScriptContainsSO(testData);

				GetRealScriptParameters(
					scriptData,
					devicesContainer);

				GetTestCanMessages(
					testData,
					scriptData);

				GetUpdateStopCanMessages(
					testData,
					scriptData);


				SetParentSweepToReset(scriptData); 
				
				SetConvergeTargetValueCommunicator(
					testData,
					devicesContainer);
			}

			foreach (GeneratedScriptData scriptData in currentProject.TestsList)
			{
				MatchPassFailNext(scriptData, devicesContainer, flashingHandler, stopScriptStep);
				SetScriptStop(scriptData, devicesContainer, stopScriptStep);

				InvalidScriptData invalidScriptData = new InvalidScriptData() { Script = scriptData };
				isTestValidService.IsValid(scriptData, invalidScriptData, devicesContainer);
				if (invalidScriptData.ErrorsList.Count > 0)
				{
					scriptData.Background = Brushes.Red;
					scriptData.Foreground = Brushes.White;
					scriptData.ErrorsList = invalidScriptData.ErrorsList;
				}
				else
				{
					scriptData.Background = Brushes.Transparent;
					if (Application.Current != null)
						scriptData.Foreground = Application.Current.MainWindow.Foreground;
					scriptData.ErrorsList = null;
				}
			}

			GetRealRecordingParameters(
				devicesContainer,
				currentProject);



			LoggerService.Inforamtion(this, "Loaded a list of scripts");

			return currentProject;
		}

		private void IsScriptContainsSO(
			GeneratedScriptData scriptData)
		{
			foreach (IScriptItem item in scriptData.ScriptItemsList)
			{
				if (item is ISubScript subScript)
				{
					IsScriptContainsSO(
						subScript.Script as GeneratedScriptData);

					if (((GeneratedScriptData)subScript.Script).IsContainsSO)
					{
						scriptData.IsContainsSO = true;
						return;
					}
					continue;
				}

				if (item is ScriptStepStartStopSaftyOfficer)
				{
					scriptData.IsContainsSO = true;
					return;
				}
			}
		}

		private void GetTotlsRunSteps(
			GeneratedScriptData scriptData,
			ref int totlsRunSteps)
		{
			foreach (IScriptItem item in scriptData.ScriptItemsList)
			{
				if(item is ISubScript subScript)
				{
					GetTotlsRunSteps(
						subScript.Script as GeneratedScriptData,
						ref totlsRunSteps);
					continue;
				}

				totlsRunSteps++;
			}
		}

		private void GetRealRecordingParameters(
			DevicesContainer devicesContainer,
			GeneratedProjectData currentProject)
		{
			if (currentProject.RecordingParametersList == null || currentProject.RecordingParametersList.Count == 0)
				return;

			ObservableCollection<DeviceParameterData> newList = 
				new ObservableCollection<DeviceParameterData>();

			InvalidScriptData invalidScriptData = new InvalidScriptData();
			foreach (DeviceParameterData param in currentProject.RecordingParametersList)
			{
				if (devicesContainer.TypeToDevicesFullData.ContainsKey(param.DeviceType) == false)
				{
					InvalidScriptItemData_DeviceNotFound error = new InvalidScriptItemData_DeviceNotFound()
					{
						Name = "Project \"" + currentProject.Name + "\"",
						ErrorString = "The device \"" + param.DeviceType + "\" was not found",
						DeviceType = param.DeviceType
					};
					invalidScriptData.ErrorsList.Add(error);
					continue;
				}

				DeviceFullData deviceFullData =
					devicesContainer.TypeToDevicesFullData[param.DeviceType];

				DeviceParameterData actualParam = null;
				if(param is MCU_ParamData mcuParam)
					actualParam = deviceFullData.Device.ParemetersList.ToList().Find((p) => ((MCU_ParamData)p).Cmd == mcuParam.Cmd);
				else
					actualParam = deviceFullData.Device.ParemetersList.ToList().Find((p) => p.Name == param.Name);
				if (actualParam == null)
				{
					InvalidScriptItemData_ParamDontExist error = new InvalidScriptItemData_ParamDontExist()
					{
						Name = "Parameter \"" + param.Name + "\"",
						ErrorString = "The recording parameter \"" + param.Name + "\" was not found in device \"" + param.DeviceType + "\"",
						Parameter = param
					};
					invalidScriptData.ErrorsList.Add(error);
					continue;
				}

				newList.Add(actualParam);
			}

			currentProject.RecordingParametersList = newList;

			if(invalidScriptData.ErrorsList.Count > 0) 
				currentProject.ErrorsList = invalidScriptData.ErrorsList;

			return;
		}

		public GeneratedScriptData GetSingleScript(
			string scriptPath,
			DevicesContainer devicesContainer,
			FlashingHandler flashingHandler)
		{
			DesignScriptViewModel sdvm = new DesignScriptViewModel(null, devicesContainer, false);
			sdvm.Open(path: scriptPath);

			ScriptData scriptData = sdvm.CurrentScript;

			GenerateProjectService generator = new GenerateProjectService();
			List<DeviceCommunicator> usedCommunicatorsList = new List<DeviceCommunicator>();
			GeneratedScriptData script = generator.GenerateScript(
				scriptPath,
				scriptData,
				devicesContainer,
				flashingHandler,
				ref usedCommunicatorsList);

			return script;
		}

		private void GetRealScriptParameters(
			IScript scriptData,
			DevicesContainer devicesContainer)
		{			
			foreach (IScriptItem scriptItem in scriptData.ScriptItemsList)
			{			
				if(scriptItem is ScriptStepBase step)
					step.GetRealParamAfterLoad(devicesContainer);
			}
		}

		

		private void SetParentSweepToReset(IScript script)
		{
			if (script == null)
				return;

			foreach (IScriptItem item in script.ScriptItemsList)
			{
				if (item is ScriptStepSweep sweep)
					SetParentSweepToReset_Sweep(sweep);

				if (item is ScriptStepSubScript subScript)
				{
					SetParentSweepToReset(subScript.Script);
				}
			}
		}

		private void SetParentSweepToReset_Sweep(ScriptStepSweep sweep)
		{
			foreach (SweepItemData item in sweep.SweepItemsList)
			{
				if (item.SubScript == null)
					continue;

				SetParentSweepToReset_Script(
					sweep,
					item.SubScript);
			}
		}

		private void SetParentSweepToReset_Script(
			ScriptStepSweep sweep,
			IScript script)
		{
			foreach (IScriptItem item in script.ScriptItemsList)
			{
				if (item is ScriptStepResetParentSweep resetSweep)
					resetSweep.ParentSweep = sweep;

				if (item is ScriptStepSubScript subScript)
				{
					SetParentSweepToReset_Script(
						sweep,
						subScript.Script);
				}
			}
		}

		private void SetConvergeTargetValueCommunicator(
			IScript script,
			DevicesContainer devicesContainer)
		{
			if (script == null)
				return;

			foreach (IScriptItem item in script.ScriptItemsList)
			{
				if (item is ScriptStepConverge converge)
					converge.SetTargetValueCommunicator(devicesContainer);

				if (item is ScriptStepSubScript subScript)
				{
					SetConvergeTargetValueCommunicator(subScript.Script, devicesContainer);
				}
			}
		}

		private void GetTestCanMessages(
			GeneratedTestData testData,
			GeneratedScriptData scriptData)
		{

			if (scriptData == null || testData == null)
				return;

			foreach (ScriptStepBase node in scriptData.ScriptItemsList)
			{
				if (node is ScriptStepCANMessage canMessage)
					testData.CanMessagesList.Add(canMessage);

				if (!(node is ScriptStepSubScript subScript))
					continue;

				GetTestCanMessages(
					testData,
					subScript.Script as GeneratedScriptData);
			}
		}

		private void GetUpdateStopCanMessages(
			GeneratedTestData testData,
			GeneratedScriptData scriptData)
		{

			if (scriptData == null)
				return;

			foreach (ScriptStepBase node in scriptData.ScriptItemsList)
			{
				if (node is ScriptStepCANMessageUpdate update)
					update.ParentProject = testData;
				else if (node is ScriptStepStopContinuous stopContinuous)
					stopContinuous.ParentProject = testData;


				if (!(node is ScriptStepSubScript subScript))
					continue;

				GetUpdateStopCanMessages(
					testData,
					subScript.Script as GeneratedScriptData);
			}
		}


		private void MatchPassFailNext(
			IScript scriptData,
			DevicesContainer devicesContainer,
			FlashingHandler flashingHandler,
			StopScriptStepService stopScriptStep)
		{
			if (scriptData == null)
				return;

			foreach (IScriptItem scriptItem in scriptData.ScriptItemsList)
			{
				if (!(scriptItem is ScriptStepBase scriptStep))
					continue;

				if (string.IsNullOrEmpty(scriptStep.PassNextDescription) == false)
				{
					foreach (IScriptItem scriptItem1 in scriptData.ScriptItemsList)
					{
						if (scriptItem1.Description == scriptStep.PassNextDescription)
						{
							scriptStep.PassNext = scriptItem1;
							break;
						}
					}
				}

				if (string.IsNullOrEmpty(scriptStep.FailNextDescription) == false)
				{
					foreach (IScriptItem scriptItem1 in scriptData.ScriptItemsList)
					{
						if (scriptItem1.Description == scriptStep.FailNextDescription)
						{
							scriptStep.FailNext = scriptItem1;
						}
					}
				}



				if (scriptStep is ScriptStepSubScript subScript)
				{
				//	SetStepToCanMessageUpdateStop(subScript.Script);
					MatchPassFailNext(subScript.Script, devicesContainer, flashingHandler, stopScriptStep);
					SetScriptStop(subScript.Script, devicesContainer, stopScriptStep);
				}

				if (scriptStep is ScriptStepSweep sweep)
				{
					foreach (SweepItemData sweepItem in sweep.SweepItemsList)
					{
						if (sweepItem.SubScript == null)
							continue;

					//	SetStepToCanMessageUpdateStop(sweepItem.SubScript);
						MatchPassFailNext(sweepItem.SubScript, devicesContainer, flashingHandler, stopScriptStep);
						SetScriptStop(sweepItem.SubScript, devicesContainer, stopScriptStep);
						SetConvergeTargetValueCommunicator(sweepItem.SubScript, devicesContainer);
					}
				}

				if (scriptStep is ScriptStepEOLFlash flash)
				{
					flash.FlashingHandler = flashingHandler;
				}
				else if (scriptStep is ScriptStepEOLCalibrate calibrate)
				{
					if (devicesContainer.TypeToDevicesFullData.ContainsKey(DeviceTypesEnum.MCU))
					{
						DeviceFullData deviceFullData = devicesContainer.TypeToDevicesFullData[DeviceTypesEnum.MCU];
						calibrate.MCU_Communicator = deviceFullData.DeviceCommunicator;
					}

					if (calibrate.RefSensorParam != null &&
						devicesContainer.TypeToDevicesFullData.ContainsKey(calibrate.RefSensorParam.DeviceType))
					{
						DeviceFullData deviceFullData = devicesContainer.TypeToDevicesFullData[
							calibrate.RefSensorParam.DeviceType];
						calibrate.RefSensorCommunicator = deviceFullData.DeviceCommunicator;
					}
				}

				if(scriptStep is ScriptStepEOLPrint print)
				{
                    if (devicesContainer.TypeToDevicesFullData.ContainsKey(DeviceTypesEnum.Printer_TSC))
                    {
                        DeviceFullData deviceFullData = devicesContainer.TypeToDevicesFullData[DeviceTypesEnum.Printer_TSC];
                        print.TscCommunicator = deviceFullData.DeviceCommunicator;
                    }
                }

				if (scriptStep is ScriptStepDynamicControl dynamicControl)
				{
					foreach (DynamicControlColumnData columnData in dynamicControl.ColumnDatasList)
					{
						if (string.IsNullOrEmpty(columnData.ParameterNameAndDevice))
							continue;

						string[] paramData = columnData.ParameterNameAndDevice.Split(" ;; ");
						DeviceTypesEnum dt;
						bool res = Enum.TryParse(paramData[1].Trim(), false, out dt);
						if (res == false)
							continue;

						if (devicesContainer.TypeToDevicesFullData.ContainsKey(dt) == false)
							continue;

						DeviceFullData deviceFullData = devicesContainer.TypeToDevicesFullData[dt];
						DeviceParameterData param = null;
						if(deviceFullData.Device.DeviceType == DeviceTypesEnum.MCU) 
						{
							param = deviceFullData.Device.ParemetersList.ToList().Find((p) => ((MCU_ParamData)p).Cmd == paramData[0].Trim());
						}
						else
							param = deviceFullData.Device.ParemetersList.ToList().Find((p) => p.Name == paramData[0].Trim());
						if (param != null)
						{
							columnData.Parameter = param;
							columnData.Communicator = deviceFullData.DeviceCommunicator;
						}
					}
				}

				if (scriptStep is IScriptStepWithParameter withParam)
				{
					if (withParam.Parameter != null)
					{
						if (devicesContainer.TypeToDevicesFullData.ContainsKey(withParam.Parameter.DeviceType))
						{
							DeviceFullData device =
								devicesContainer.TypeToDevicesFullData[withParam.Parameter.DeviceType];
							if (device != null)
							{
								withParam.Parameter.Device = device.Device;
								if (scriptStep is IScriptStepWithCommunicator withCommunicator)
								{
									withCommunicator.Communicator = device.DeviceCommunicator;
								}
							}
						}
					}
				}
				else if (scriptStep is IScriptStepWithCommunicator withCommunicator)
				{
					if (devicesContainer.TypeToDevicesFullData.ContainsKey(DeviceTypesEnum.MCU))
					{
						DeviceFullData device =
									devicesContainer.TypeToDevicesFullData[DeviceTypesEnum.MCU];
						withCommunicator.Communicator = device.DeviceCommunicator;
					}
				}

				if(scriptStep is ScriptStepSetParameter setParameter) 
				{
					if (setParameter.ValueParameter != null)
					{
						if (devicesContainer.TypeToDevicesFullData.ContainsKey(setParameter.ValueParameter.DeviceType))
						{
							DeviceFullData device =
								devicesContainer.TypeToDevicesFullData[setParameter.ValueParameter.DeviceType];
							if (device != null)
							{
								setParameter.ValueParameter.Device = device.Device;
								setParameter.GetParamValue.Communicator = device.DeviceCommunicator;
								
							}
						}
					}
				}
			}
		}

		private void SetScriptStop(
			IScript scriptData,
			DevicesContainer devicesContainer,
			StopScriptStepService stopScriptStep)
		{
			if (scriptData == null)
				return;

			foreach (IScriptItem scriptItem in scriptData.ScriptItemsList)
			{
				if (!(scriptItem is ScriptStepBase step))
					continue;

				step.StopScriptStep = stopScriptStep;
				if (step is ScriptStepGetParamValue getParamValue)
				{
					getParamValue.DevicesContainer = devicesContainer;
				}
				if (step is ScriptStepSweep sweep)
				{
					sweep.DevicesList = devicesContainer.DevicesFullDataList;
				}
			}
		}
	}
}
