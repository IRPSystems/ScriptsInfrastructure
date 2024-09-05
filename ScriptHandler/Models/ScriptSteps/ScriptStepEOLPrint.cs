
using DeviceCommunicators.General;
using DeviceHandler.Models;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace ScriptHandler.Models
{
	public class ScriptStepEOLPrint : ScriptStepBase
	{
		#region Properties

		public string PartNumber { get; set; }
		public string CustomerPartNumber { get; set; }
		public string Spec { get; set; }
		public string HW_Version { get; set; }
		public string MCU_Version { get; set; }


		public string SerialNumber { get; set; }

		#endregion Properties


		#region Constructor

		public ScriptStepEOLPrint()
		{
			if (Application.Current != null)
			{
				Application.Current.Dispatcher.Invoke(() =>
				{
					Template = Application.Current.MainWindow.FindResource("AutoRunTemplate") as DataTemplate;
				});
			}
		}

		#endregion Constructor


		#region Methods

		public override void Execute()
		{
			
			IsPass = true;
		}

		

		public override bool IsNotSet(
			DevicesContainer devicesContainer,
			ObservableCollection<InvalidScriptItemData> errorsList)
		{
			

			return false;
		}

		protected override void Generate(
			ScriptNodeBase sourceNode,
			Dictionary<int, ScriptStepBase> stepNameToObject,
			ref List<DeviceCommunicator> usedCommunicatorsList,
			GenerateProjectService generateService,
			DevicesContainer devicesContainer)
		{
			PartNumber = (sourceNode as ScriptNodeEOLPrint).PartNumber;
			CustomerPartNumber = (sourceNode as ScriptNodeEOLPrint).CustomerPartNumber;
			Spec = (sourceNode as ScriptNodeEOLPrint).Spec;
			HW_Version = (sourceNode as ScriptNodeEOLPrint).HW_Version;
			MCU_Version = (sourceNode as ScriptNodeEOLPrint).MCU_Version;
		}

		#endregion Methods
	}
}
