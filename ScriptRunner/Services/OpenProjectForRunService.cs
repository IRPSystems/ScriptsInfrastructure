
using DeviceCommunicators.General;
using DeviceCommunicators.Models;
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
using System.Windows;
using System.Windows.Media;

namespace ScriptRunner.Services
{
	public class OpenProjectForRunService
	{
		public GeneratedProjectData Open(
			ScriptUserData scriptUserData,
			DevicesContainer devicesContainer,
			RunScriptService runScript)
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

				GeneratedProjectData project = Open(scriptsPath, devicesContainer, runScript);
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
			RunScriptService runScript)
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
				GeneratedScriptData script = GetSingleScript(path, devicesContainer);
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
				MatchPassFailNext(scriptData, devicesContainer, runScript);
				SetScriptStop(scriptData, devicesContainer, runScript);

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

				DeviceParameterData actualParam =
					deviceFullData.Device.ParemetersList.ToList().Find((p) => p.Name == param.Name);
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

		private GeneratedScriptData GetSingleScript(
			string scriptPath,
			DevicesContainer devicesContainer)
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
				ref usedCommunicatorsList);

			return script;
		}

		private void GetRealScriptParameters(
			GeneratedScriptData scriptData,
			DevicesContainer devicesContainer)
		{
			
			foreach (IScriptItem scriptItem in scriptData.ScriptItemsList)
			{
				if (scriptItem is IScriptStepCompare compare)
				{
					if (compare.ValueLeft is DeviceParameterData)
					{
						compare.ValueLeft = GetRealParam(
							compare.ValueLeft as DeviceParameterData,
							devicesContainer);
					}

					if (compare.ValueRight is DeviceParameterData)
					{
						compare.ValueRight = GetRealParam(
							compare.ValueRight as DeviceParameterData,
							devicesContainer);
					}

					if (scriptItem is ScriptStepCompareRange compareRange)
					{
						if (compareRange.Value is DeviceParameterData)
						{
							compareRange.Value = GetRealParam(
								compareRange.Value as DeviceParameterData,
								devicesContainer);
						}
					}
				}
				else if (scriptItem is IScriptStepWithParameter withParameter)
				{
					withParameter.Parameter = GetRealParam(
						withParameter.Parameter,
						devicesContainer);
				}
				
				

			}

			
		}

		private DeviceParameterData GetRealParam(
			DeviceParameterData originalParam,
			DevicesContainer devicesContainer)
		{
			if (originalParam == null)
				return null;

			DeviceFullData deviceFullData =
				devicesContainer.TypeToDevicesFullData[originalParam.DeviceType];
			if (deviceFullData == null)
				return null;

			DeviceParameterData actualParam =
				deviceFullData.Device.ParemetersList.ToList().Find((p) =>
					p.Name == originalParam.Name);
			return actualParam;
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
			RunScriptService runScript)
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
					MatchPassFailNext(subScript.Script, devicesContainer, runScript);
					SetScriptStop(subScript.Script, devicesContainer, runScript);
				}

				if (scriptStep is ScriptStepSweep sweep)
				{
					foreach (SweepItemData sweepItem in sweep.SweepItemsList)
					{
						if (sweepItem.SubScript == null)
							continue;

					//	SetStepToCanMessageUpdateStop(sweepItem.SubScript);
						MatchPassFailNext(sweepItem.SubScript, devicesContainer, runScript);
						SetScriptStop(sweepItem.SubScript, devicesContainer, runScript);
						SetConvergeTargetValueCommunicator(sweepItem.SubScript, devicesContainer);
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
						DeviceParameterData param = 
							deviceFullData.Device.ParemetersList.ToList().Find((p) => p.Name == paramData[0].Trim());
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
			}
		}

		private void SetScriptStop(
			IScript scriptData,
			DevicesContainer devicesContainer,
			RunScriptService runScript)
		{
			if (scriptData == null)
				return;

			foreach (IScriptItem scriptItem in scriptData.ScriptItemsList)
			{
				if (!(scriptItem is ScriptStepBase step))
					continue;

				step.StopScriptStep = runScript.StopScriptStep;
				if (step is ScriptStepGetParamValue getParamValue)
				{
					getParamValue.DevicesList = devicesContainer.DevicesFullDataList;
				}
				if (step is ScriptStepSweep sweep)
				{
					sweep.DevicesList = devicesContainer.DevicesFullDataList;
				}
			}
		}
	}
}
