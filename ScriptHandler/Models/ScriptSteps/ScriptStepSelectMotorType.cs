using DeviceCommunicators.Enums;
using DeviceCommunicators.General;
using DeviceCommunicators.MCU;
using Entities.Models;
using Newtonsoft.Json;
using ScriptHandler.Interfaces;
using System.Linq;
using System.Threading;
using System.Windows;

namespace ScriptHandler.Models
{
	public class ScriptStepSelectMotorType : ScriptStepBase, IScriptStepWithCommunicator
	{
		#region Properties

		[JsonIgnore]
		public MCU_DeviceData MCU_Device { get; set; }

		[JsonIgnore]
		public MotorSettingsData SelectedMotor { get; set; }
		[JsonIgnore]
		public ControllerSettingsData SelectedController { get; set; }
		[JsonIgnore]
		public DeviceCommunicator Communicator { get; set; }

		// For debug
		[JsonIgnore]
		public bool IsDoAbort { get; set; }

		#endregion Properties

		#region Fields

		private bool _isStopped;
		private ManualResetEvent _waitGetCallback;

		#endregion Fields

		#region Constructor

		public ScriptStepSelectMotorType()
		{
			Template = Application.Current.MainWindow.FindResource("AutoRunTemplate") as DataTemplate;


			_isStopped = false;

			_waitGetCallback = new ManualResetEvent(false);
		}

		#endregion Constructor

		#region Methods

		public override void Execute()
		{
		
			if(SelectedMotor == null)
			{
				ErrorMessage = "No motor is selected.";
				IsPass = false;
				return;
			}

			if (SelectedController == null)
			{
				ErrorMessage = "No controller is selected.";
				IsPass = false;
				return;
			}

			ErrorMessage = "Failed to set the seurity values.\r\n";

			_isStopped = false;
			IsPass = true;
			foreach (ParameterValueData data in SelectedMotor.CommandParameterValueList)
			{
				if (_isStopped)
					break;

				if (data.ParameterName.ToLower().StartsWith("motorbike") && SelectedMotor.Name == "Mahindra")
					continue;

				SetParameter(data.ParameterName, data.Value);
				if (!IsPass)
					break;
			}

			if(!IsPass)
			{
				
			//	ExecutionEndedEvent?.Invoke();
				return;
			}

			if (_isStopped)
				return;

			foreach (ParameterValueData data in SelectedController.CommandParameterValueList)
			{
				if (_isStopped)
					break;

				SetParameter(data.ParameterName, data.Value);
				if (!IsPass)
					break;
			}

			if (_isStopped)
				return;
		}

		private void SetParameter(
			string parameterName,
			int value)
		{
			if (MCU_Device == null)
				return;

			parameterName = parameterName.Trim();
			DeviceParameterData data = MCU_Device.MCU_FullList.ToList().Find((p) => p.Name == parameterName);
			if (data == null)
				return;

			

			ErrorMessage = "Failed to set the security value.\r\n" +
					"\tParameter: \"" + data.Name + "\"\r\n" +
					"\tValue: " + value + "\r\n\r\n";

			Communicator.SetParamValue(data, value, GetCallback);
			bool isNotTimeout =_waitGetCallback.WaitOne(5000);
			if (!isNotTimeout)
			{
				ErrorMessage += " Communication timeout.";
				IsPass = isNotTimeout;
			}

			_waitGetCallback.Reset();

			if (!IsDoAbort)
				IsPass = true;
		}

		protected override void Stop()
		{
			_isStopped = true;
			_waitGetCallback.Set();
		}

		private void GetCallback(DeviceParameterData param, CommunicatorResultEnum result, string resultDescription)
		{
			_waitGetCallback.Set();

			if (_isStopped || !IsPass)
				return;

			

			if (!IsDoAbort)
				return;

			IsPass = result == CommunicatorResultEnum.OK;
			if(!IsPass) { }

			switch (result)
			{
				case CommunicatorResultEnum.NoResponse:
					ErrorMessage +=
						"No response was received from the device.\r\n";
					break;

				case CommunicatorResultEnum.ValueNotSet:
					ErrorMessage +=
						"Failed to set the value.";
					break;

				case CommunicatorResultEnum.Error:
					ErrorMessage +=
						"The device returned an error:\r\n" +
						resultDescription;
					break;

				case CommunicatorResultEnum.InvalidUniqueId:
					ErrorMessage +=
						"Invalud Unique ID was received from the Dyno.";
					break;
			}

			

			
		}

		#endregion Methods

		
	}
}
