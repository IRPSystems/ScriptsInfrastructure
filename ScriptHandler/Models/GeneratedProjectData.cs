
using CommunityToolkit.Mvvm.ComponentModel;
using DeviceCommunicators.Models;
using Newtonsoft.Json;
using ScriptHandler.Enums;
using System.Collections.ObjectModel;

namespace ScriptHandler.Models
{
	public class GeneratedProjectData: ObservableObject
	{
		public string Name { get; set; }
		public ObservableCollection<GeneratedScriptData> TestsList { get; set; }

		public ObservableCollection<DeviceParameterData> RecordingParametersList { get; set; }


		[JsonIgnore]
		public ObservableCollection<InvalidScriptItemData> ErrorsList { get; set; }

		[JsonIgnore]
		public string ProjectPath { get; set; }

		[JsonIgnore]
		public bool IsDoRun { get; set; }
		[JsonIgnore]
		public bool IsSelected { get; set; }

		[JsonIgnore]
		public SciptStateEnum State { get; set; }

		[JsonIgnore]
		public string RecordingPath { get; set; }


		public GeneratedProjectData()
		{
			IsDoRun = true;
		}

	}
}
