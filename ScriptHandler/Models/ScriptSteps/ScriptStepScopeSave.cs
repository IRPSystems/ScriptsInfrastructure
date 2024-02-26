
using DeviceCommunicators.Enums;
using DeviceCommunicators.General;
using DeviceCommunicators.Models;
using DeviceCommunicators.Scope_KeySight;
using DeviceHandler.Models;
using Newtonsoft.Json;
using ScriptHandler.Interfaces;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;

namespace ScriptHandler.Models
{
	public class ScriptStepScopeSave : ScriptStepBase, IScriptStepWithCommunicator, IScriptStepWithParameter
	{
		#region Properties

		public DeviceParameterData Parameter { get; set; }
		[JsonIgnore]
		public DeviceCommunicator Communicator { get; set; }

		public double Value { get; set; }		

		public string FilePath { get; set; }

		#endregion Properties

		#region Fields

		private ManualResetEvent _waitGetCallback;
		private bool _isStopped;

		#endregion Fields

		#region Constructor

		public ScriptStepScopeSave()
		{
			if (Application.Current != null)
			{
				Application.Current.Dispatcher.Invoke(() =>
				{
					Template = Application.Current.MainWindow.FindResource("AutoRunTemplate") as DataTemplate;
				});
			}


			_isStopped = false;
		}

		#endregion Constructor


		#region Methods

		public override void Execute()
		{
			if (Communicator == null || Parameter == null ||
				string.IsNullOrEmpty(FilePath))
			{
				ErrorMessage = "Communicator or Parameter is null";
				return;
			}

			if(!(Parameter is Scope_KeySight_ParamData ks_Param))
			{
				ErrorMessage = "The parameter is not valid";
				return;
			}

			_waitGetCallback = new ManualResetEvent(false);
			IsPass = true;
			ErrorMessage = "Scope Save\r\n";

			ks_Param.data = FilePath;
			ks_Param.Value = Value;

			Communicator.SetParamValue(Parameter, 0, Callback);

			bool isNotTimeout = _waitGetCallback.WaitOne(1000);
			if (!isNotTimeout)
			{
				ErrorMessage += "Communication timeout.";
				IsPass = false;
			}
		}

		protected override void Stop()
		{
			_isStopped = true;
			if (_waitGetCallback != null)
				_waitGetCallback.Set();
		}

		private void Callback(DeviceParameterData param, CommunicatorResultEnum result, string resultDescription)
		{
			if (_isStopped)
				return;




			switch (result)
			{
				case CommunicatorResultEnum.NoResponse:
					ErrorMessage +=
						"No response was received from the device.";
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

			IsPass = result == CommunicatorResultEnum.OK;
			if (!IsPass) { }

			_waitGetCallback.Set();
		}

		public override bool IsNotSet(
			DevicesContainer devicesContainer,
			ObservableCollection<InvalidScriptItemData> errorsList)
		{
			if (Parameter == null)
				return true;

			if(string.IsNullOrEmpty(FilePath)) 
				return true;

			return false;
		}

		public override void Generate(
			ScriptNodeBase sourceNode,
			Dictionary<int, ScriptStepBase> stepNameToObject,
			ref List<DeviceCommunicator> usedCommunicatorsList,
			GenerateProjectService generateService,
			DevicesContainer devicesContainer)
		{
			Parameter = (sourceNode as ScriptNodeScopeSave).Parameter;
			Value = (sourceNode as ScriptNodeScopeSave).Value;
			FilePath = (sourceNode as ScriptNodeScopeSave).FilePath;

			string ext = System.IO.Path.GetExtension(FilePath);
			if(Value == 0) // PNG
			{
				if (string.IsNullOrEmpty(ext))
					FilePath += ".png";

				else if (ext.ToLower() != ".png")
					FilePath = FilePath.Replace(ext, ".png");
			}
			else // CSV or other
			{
				if (string.IsNullOrEmpty(ext))
					FilePath += ".csv";

				if (ext.ToLower() != ".csv")
					FilePath = FilePath.Replace(ext, ".csv");
			}
		}

		#endregion Methods
	}
}
