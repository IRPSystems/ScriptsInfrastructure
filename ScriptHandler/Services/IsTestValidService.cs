
using DeviceCommunicators.MCU;
using DeviceCommunicators.Models;
using DeviceHandler.Models;
using Entities.Enums;
using Entities.Models;
using ScriptHandler.Interfaces;
using ScriptHandler.Models;
using Services.Services;
using System.Collections.ObjectModel;
using System.Linq;

namespace ScriptHandler.Services
{
	public class IsTestValidService
	{
		public bool IsValid(
			IScript script,
			InvalidScriptData invalidScriptData,
			DevicesContainer devicesContainer) 
		{
			if (script == null)
				return false;

			foreach (IScriptItem item in script.ScriptItemsList)
			{
				bool isNodeSet = item.IsNotSet(
					devicesContainer,
					invalidScriptData.ErrorsList);
				if (isNodeSet)
				{
					InvalidScriptItemData invalidItem = new InvalidScriptItemData()
					{
						ErrorString = "data is not set",
						Name = item.Description,
					};

					invalidScriptData.ErrorsList.Add(invalidItem);
				}

				IsValidJson(
					item,
					invalidScriptData.ErrorsList,
					devicesContainer);

				if (item is ISubScript subScript)
				{
					if (subScript.IsNotSet(devicesContainer, invalidScriptData.ErrorsList))
						continue;

					InvalidScriptData invalidScriptData_Sub = new InvalidScriptData()
					{
						Name = subScript.Script.Name,
						ErrorString = null,
						ErrorsList = new ObservableCollection<InvalidScriptItemData>(),
					};


					IsValid(
						subScript.Script,
						invalidScriptData_Sub,
						devicesContainer);

					if(invalidScriptData_Sub.ErrorsList.Count > 0)
						invalidScriptData.ErrorsList.Add(invalidScriptData_Sub);
				}
			}

			return true;
		}

		private bool IsValidJson(
			IScriptItem scriptNode,
			ObservableCollection<InvalidScriptItemData> errorsList,
			DevicesContainer devicesContainer)
		{

			if (!(scriptNode is IScriptStepWithParameter withParam))
				return true;

			if (withParam.Parameter == null)
				return true;

			if (withParam.Parameter.DeviceType == DeviceTypesEnum.EVVA)
			{
				//if (errorString == string.Empty)
				//	errorString = null;
				return true;
			}

			if (devicesContainer.TypeToDevicesFullData.ContainsKey(withParam.Parameter.DeviceType) == false)
			{
				string err = "The device " + withParam.Parameter.DeviceType +
					" of the parameter \"" + withParam.Parameter.Name + "\" doesn't exist in the setup";
				LoggerService.Error(this, err);
				
				InvalidScriptItemData invalidItem = new InvalidScriptItemData()
				{
					ErrorString = err,
					Name = scriptNode.Description,
				};
				errorsList.Add(invalidItem);
				return false;
			}

			DeviceData device = devicesContainer.TypeToDevicesFullData[withParam.Parameter.DeviceType].Device;

			DeviceParameterData deviceParam = null;
			if (withParam.Parameter.Device != null && withParam.Parameter.Device.DeviceType == DeviceTypesEnum.MCU)
				deviceParam = (device as MCU_DeviceData).MCU_FullList.ToList().Find((p) => p.Name == withParam.Parameter.Name);
			else
				deviceParam = device.ParemetersList.ToList().Find((p) => p.Name == withParam.Parameter.Name);

			if (deviceParam == null)
			{
				string err = "The parameter \"" + withParam.Parameter.Name + "\" dosn't exist in the current " + device.Name + " parameter file";
				LoggerService.Error(this, err);
				
				InvalidScriptItemData invalidItem = new InvalidScriptItemData()
				{
					ErrorString = err,
					Name = scriptNode.Description,
				};
				errorsList.Add(invalidItem);

				return false;
			}


			return true;
		}
	}
}
