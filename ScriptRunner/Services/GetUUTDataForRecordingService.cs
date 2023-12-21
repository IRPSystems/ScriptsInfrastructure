
using DeviceCommunicators.General;
using DeviceCommunicators.MCU;
using DeviceCommunicators.Models;
using DeviceHandler.Enums;
using DeviceHandler.Models;
using DeviceHandler.Models.DeviceFullDataModels;
using Entities.Enums;
using Entities.Models;
using ScriptHandler.Models;
using Services.Services;
using System.Linq;

namespace ScriptRunner.Services
{
	public class GetUUTDataForRecordingService
	{
		public string SerialNumber { get; set; }
		public string FirmwareVersion { get; set; }
		public string CoreVersion { get; set; }

		public bool GetUUTData(DevicesContainer devicesContainer)
		{
			SerialNumber = "--";
			FirmwareVersion = "--";
			CoreVersion = "--";

			if (devicesContainer.TypeToDevicesFullData.ContainsKey(DeviceTypesEnum.MCU) == false)
			{
				LoggerService.Error(this, "No UUT found");
				return false;
			}

			

			DeviceFullData deviceFullData = 
				devicesContainer.TypeToDevicesFullData[DeviceTypesEnum.MCU];
			if (deviceFullData == null)
			{
				LoggerService.Error(this, "No UUT found");
				return false;
			}

			if(deviceFullData.CheckCommunication != null &&
				deviceFullData.CheckCommunication.Status != CommunicationStateEnum.Connected)
			{
				LoggerService.Error(this, "The MCU is not connected");
				return false;
			}

			if (!(deviceFullData.Device is MCU_DeviceData mcu_Device))
			{
				LoggerService.Error(this, "No MCU device was found");
				return false;
			}

			bool isOK = GetParameterValue_SerialNumber(
				deviceFullData.DeviceCommunicator,
				mcu_Device);
			if(!isOK)
				return false;

			isOK = GetParameterValue_FWVersion(
				deviceFullData.DeviceCommunicator,
				mcu_Device);
			if (!isOK)
				return false;

			isOK = GetParameterValue_COREVersion(
				deviceFullData.DeviceCommunicator,
				mcu_Device);
			if (!isOK)
				return false;

			return true;
		}

		private bool GetParameterValue_SerialNumber(
			DeviceCommunicator deviceCommunicator,
			MCU_DeviceData mcu_Device)
		{
			ScriptStepGetParamValue getParam = new ScriptStepGetParamValue()
			{
				Communicator = deviceCommunicator
			};

			ParamGroup group = mcu_Device.MCU_GroupList.ToList().Find((g) => g.Name == "HW Version");
			if (group == null)
			{
				LoggerService.Error(this, "Failed to find the HW Version group");
				return false;
			}

			DeviceParameterData param =
				group.ParamList.ToList().Find((p) => p.Name == "Serial Number");
			if (param == null)
			{
				LoggerService.Error(this, "Failed to find the Serial Number parameter");
				return false;
			}

			getParam.Parameter = param;
			getParam.IsPass = true;
			getParam.SendAndReceive();
			if (!getParam.IsPass)
			{
				LoggerService.Error(this, "Failed to get the Serial Number from the UUT");
				return false;
			}

			SerialNumber = param.Value.ToString();

			return true;
		}

		private bool GetParameterValue_FWVersion(
			DeviceCommunicator deviceCommunicator,
			MCU_DeviceData mcu_Device)
		{
			ScriptStepGetParamValue getParam = new ScriptStepGetParamValue()
			{
				Communicator = deviceCommunicator
			};

			ParamGroup group = mcu_Device.MCU_GroupList.ToList().Find((g) => g.Name == "FW Version");
			if (group == null)
			{
				LoggerService.Error(this, "Failed to find the HW Version group");
				return false;
			}

			#region FW Major

			DeviceParameterData param =
				group.ParamList.ToList().Find((p) => p.Name == "Version Major (FW Version)");
			if (param == null)
			{
				LoggerService.Error(this, "Failed to find the FW Version Major parameter");
				return false;
			}

			getParam.Parameter = param;
			getParam.IsPass = true;
			getParam.SendAndReceive();
			if (!getParam.IsPass)
			{
				LoggerService.Error(this, "Failed to get the FW Version Major from the UUT");
				return false;
			}

			FirmwareVersion = param.Value.ToString();

			#endregion FW Major

			#region FW Middle
			param =
				group.ParamList.ToList().Find((p) => p.Name == "Version Middle (FW Version)");
			if (param == null)
			{
				LoggerService.Error(this, "Failed to find the FW Version Middle parameter");
				return false;
			}

			getParam.Parameter = param;
			getParam.IsPass = true;
			getParam.SendAndReceive();
			if (!getParam.IsPass)
			{
				LoggerService.Error(this, "Failed to get the FW Version Middle from the UUT");
				return false;
			}

			FirmwareVersion += "." + param.Value.ToString();

			#endregion FW Middle

			#region FW Minor
			param =
				group.ParamList.ToList().Find((p) => p.Name == "Version Minor (FW Version)");
			if (param == null)
			{
				LoggerService.Error(this, "Failed to find the FW Version Minor parameter");
				return false;
			}

			getParam.Parameter = param;
			getParam.IsPass = true;
			getParam.SendAndReceive();
			if (!getParam.IsPass)
			{
				LoggerService.Error(this, "Failed to get the FW Version Minor from the UUT");
				return false;
			}

			FirmwareVersion += "." + param.Value.ToString();

			#endregion FW Minor

			return true;
		}

		private bool GetParameterValue_COREVersion(
			DeviceCommunicator deviceCommunicator,
			MCU_DeviceData mcu_Device)
		{
			ScriptStepGetParamValue getParam = new ScriptStepGetParamValue()
			{
				Communicator = deviceCommunicator
			};

			ParamGroup group = mcu_Device.MCU_GroupList.ToList().Find((g) => g.Name == "Core Version");
			if (group == null)
			{
				LoggerService.Error(this, "Failed to find the HW Version group");
				return false;
			}

			#region CORE Major

			DeviceParameterData param =
				group.ParamList.ToList().Find((p) => p.Name == "Version Major (Core Version)");
			if (param == null)
			{
				LoggerService.Error(this, "Failed to find the CORE Version Major parameter");
				return false;
			}

			getParam.Parameter = param;
			getParam.IsPass = true;
			getParam.SendAndReceive();
			if (!getParam.IsPass)
			{
				LoggerService.Error(this, "Failed to get the CORE Version Major from the UUT");
				return false;
			}

			CoreVersion = param.Value.ToString();

			#endregion CORE Major

			#region CORE Middle
			param =
				group.ParamList.ToList().Find((p) => p.Name == "Version Middle (Core Version)");
			if (param == null)
			{
				LoggerService.Error(this, "Failed to find the Core Version Middle parameter");
				return false;
			}

			getParam.Parameter = param;
			getParam.IsPass = true;
			getParam.SendAndReceive();
			if (!getParam.IsPass)
			{
				LoggerService.Error(this, "Failed to get the Core Version Middle from the UUT");
				return false;
			}

			CoreVersion += "." + param.Value.ToString();

			#endregion CORE Middle

			#region Core Minor
			param =
				group.ParamList.ToList().Find((p) => p.Name == "Version Minor (Core Version)");
			if (param == null)
			{
				LoggerService.Error(this, "Failed to find the Core Version Minor parameter");
				return false;
			}

			getParam.Parameter = param;
			getParam.IsPass = true;
			getParam.SendAndReceive();
			if (!getParam.IsPass)
			{
				LoggerService.Error(this, "Failed to get the Core Version Minor from the UUT");
				return false;
			}

			CoreVersion += "." + param.Value.ToString();

			#endregion Core Minor

			return true;
		}
	}
}
