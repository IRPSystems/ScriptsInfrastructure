
using DeviceCommunicators.General;
using DeviceCommunicators.Models;
using DeviceHandler.Models;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace ScriptHandler.Models
{
	public class ScriptStepScopeSave : ScriptStepBase
	{
		#region Properties

		public DeviceParameterData Parameter { get; set; }

		public double Value { get; set; }		

		public string FilePath { get; set; }

		#endregion Properties



		#region Constructor

		public ScriptStepScopeSave()
		{
			if (Application.Current != null)
			{
				Application.Current.Dispatcher.Invoke(() =>
				{
					Template = Application.Current.MainWindow.FindResource("DelayTemplate") as DataTemplate;
				});
			}
		}

		#endregion Constructor


		#region Methods

		public override void Execute()
		{
			
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
		}

		#endregion Methods
	}
}
