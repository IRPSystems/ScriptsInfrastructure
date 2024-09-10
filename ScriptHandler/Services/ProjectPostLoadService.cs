
using ControlzEx.Theming;
using DeviceHandler.Models;
using DeviceHandler.Models.DeviceFullDataModels;
using ScriptHandler.Interfaces;
using ScriptHandler.Models;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.ViewModels;
using Services.Services;
using System;
using System.Linq;

namespace ScriptHandler.Services
{
	public class ProjectPostLoadService
	{
		public void PostLoad(
			ProjectData project,
			EventHandler scriptSavedEventHandler,
			DevicesContainer devicesContainer) 
		{
			project.CanMessagesList.Clear();
			foreach (DesignScriptViewModel vm in project.ScriptsList)
			{
				vm.CurrentScript.Parent = project;
				SetEvvaDeviceToSaftyOfficer(vm.CurrentScript);
				HandleSubScriptInScript(vm.CurrentScript, project);
				HandleSweepSubScriptInScript(vm.CurrentScript, project);

				HandleDynamicControlInScript(
					vm.CurrentScript,
					devicesContainer);

				if (!vm.IsScriptIsSavedEvent)
				{
					vm.ScriptIsSavedEvent += scriptSavedEventHandler;
					vm.IsScriptIsSavedEvent = true;
				}

			
			}

		}




		private void SetEvvaDeviceToSaftyOfficer(ScriptData scriptData)
		{
			if (scriptData == null)
				return;

			foreach (ScriptNodeBase node in scriptData.ScriptItemsList)
			{
				if (node is IScriptStepWithParameter withParameter && withParameter.Parameter != null)
				{
					if (withParameter.Parameter.Name == "Safety officer on/off")
					{
						withParameter.Parameter.DeviceType = Entities.Enums.DeviceTypesEnum.EVVA;
					}
				}
			}
		}

		public void HandleSubScriptInScript(
			ScriptData scriptData, 
			ProjectData project)
		{
			if (scriptData == null)
				return;

			try
			{

				foreach (ScriptNodeBase node in scriptData.ScriptItemsList)
				{
					if (!(node is ScriptNodeSubScript subScript))
						continue;

					subScript.Parent = project;

					if (string.IsNullOrEmpty(subScript.SelectedScriptName))
						continue;



					foreach (DesignScriptViewModel vm in project.ScriptsList)
					{
						if (!(vm.CurrentScript is ScriptData testSubScriptData))
							continue;

						if (testSubScriptData.Name == subScript.SelectedScriptName)
						{
							subScript.Script = testSubScriptData;
						}
					}

					HandleSubScriptInScript(
						subScript.Script as ScriptData, project);
				}
			}
			catch (Exception ex)
			{
				LoggerService.Error(this, "Failed HandleSubScriptInScript", ex);
			}
		}

		private void HandleSweepSubScriptInScript(
			ScriptData scriptData,
			ProjectData project)
		{
			if (scriptData == null)
				return;

			foreach (ScriptNodeBase node in scriptData.ScriptItemsList)
			{
				if (!(node is ScriptNodeSweep sweep))
					continue;

				sweep.Parent = project;


				foreach (SweepItemData sweepItem in sweep.SweepItemsList)
				{

					if (sweepItem.SubScriptName == null)
						continue;


					ScriptData subScript = project.ScriptsOnlyList.ToList().Find((s) => s.Name == sweepItem.SubScriptName);
					if (subScript == null)
						continue;

					sweepItem.SubScript = subScript;

				}
			}
		}

		private void HandleDynamicControlInScript(
			ScriptData scriptData,
			DevicesContainer devicesContainer)
		{
			if (scriptData == null)
				return;

			foreach (ScriptNodeBase node in scriptData.ScriptItemsList)
			{
				if (!(node is ScriptNodeDynamicControl dynamicControl))
					continue;

				foreach(DynamicControlColumnData column in dynamicControl.ColumnDatasList)
				{
					if(column.Parameter == null) 
						continue;

					if (devicesContainer.TypeToDevicesFullData.ContainsKey(column.Parameter.DeviceType) == false)
						continue;

					DeviceFullData deviceFullData = devicesContainer.TypeToDevicesFullData[column.Parameter.DeviceType];
					column.Parameter.Device = deviceFullData.Device;
				}
			}
		}



		//private void GetProjectCanMessages(
		//	ScriptData scriptData,
		//	ProjectData project)
		//{
		//	foreach (ScriptNodeBase node in scriptData.ScriptItemsList)
		//	{
		//		if (node is ScriptNodeCANMessage canMessage)
		//		{
		//			if (node is ScriptNodeCANMessageUpdate update)
		//				update.ParentProject = project;
		//			else
		//				project.CanMessagesList.Add(canMessage);

		//		}
		//		else if (node is ScriptNodeStopContinuous stopContinuous)
		//		{
		//			stopContinuous.ParentProject = project;
		//		}
		//	}

		//}

		//private void SetTestsCanMessagesUpdate(
		//	ScriptData scriptData,
		//	ProjectData project)
		//{

		//	foreach (ScriptNodeBase node in scriptData.ScriptItemsList)
		//	{
		//		if (node is ScriptNodeCANMessageUpdate update)
		//		{
		//			if (update.StepToUpdateID != 0)
		//			{
		//				foreach (ScriptNodeCANMessage canMessage in project.CanMessagesList)
		//				{
		//					if (canMessage.IDInProject == update.StepToUpdateID)
		//					{
		//						update.StepToUpdate = canMessage;
		//						break;
		//					}
		//				}
		//			}
		//		}
		//	}

		//}

		//private void SetTestsStopContinuous(
		//	ScriptData scriptData,
		//	ProjectData project)
		//{

		//	foreach (ScriptNodeBase node in scriptData.ScriptItemsList)
		//	{
		//		if (node is ScriptNodeStopContinuous stop)
		//		{
		//			if (stop.StepToStopID != 0)
		//			{
		//				foreach (ScriptNodeCANMessage canMessage in project.CanMessagesList)
		//				{
		//					if (canMessage.IDInProject == stop.StepToStopID)
		//					{
		//						stop.StepToStop = canMessage;
		//						break;
		//					}
		//				}
		//			}
		//		}
		//	}

		//}
	}
}
