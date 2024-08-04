
using DeviceCommunicators.Enums;
using DeviceCommunicators.General;
using DeviceCommunicators.Models;
using DeviceHandler.Models;
using Entities.Models;
using Newtonsoft.Json;
using ScriptHandler.Interfaces;
using ScriptHandler.Models;
using ScriptHandler.Services;
using ScriptRunner.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace ScriptRunner.Models
{
	public class ScriptStepAbort : ScriptStepSubScript
	{
		public ScriptStepAbort(
			string scriptFile,
			DevicesContainer devicesContainer)
		{
			Template = Application.Current.MainWindow.FindResource("AbortAutoRunTemplate") as DataTemplate;

			Description = "Abort script";

			GenerateProjectService generateProject = new GenerateProjectService();

			Script = new GeneratedScriptData() 
			{ 
				Name = "Abort",
				IsPass = null,
				State = ScriptHandler.Enums.SciptStateEnum.None,
				ScriptPath = null,
			};

			Script.ScriptItemsList = new ObservableCollection<IScriptItem>();

			
			string jsonString = File.ReadAllText(scriptFile);
			if (jsonString == null)
				return;

			JsonSerializerSettings settings = new JsonSerializerSettings();
			settings.Formatting = Formatting.Indented;
			settings.TypeNameHandling = TypeNameHandling.All;
			ScriptData sd = JsonConvert.DeserializeObject(jsonString, settings) as ScriptData;

			foreach (ScriptNodeBase scriptNode in sd.ScriptItemsList)
			{
				if (scriptNode is IScriptStepWithParameter withParam &&
					withParam.Parameter != null)
				{
					if (devicesContainer.TypeToDevicesFullData.ContainsKey(withParam.Parameter.DeviceType) == false)
						continue;

					DeviceData deviceData =
						devicesContainer.TypeToDevicesFullData[withParam.Parameter.DeviceType].Device;

					DeviceParameterData data = deviceData.ParemetersList.ToList().Find((p) => p.Name == withParam.Parameter.Name);
					if (data != null)
						withParam.Parameter = data;
				}
			}

			List<DeviceCommunicator> usedCommunicatorsList = new List<DeviceCommunicator>();
			Script = generateProject.GenerateScript(
				scriptFile,
				sd,
				devicesContainer,
				null,
				ref usedCommunicatorsList);

			foreach (IScriptItem item in Script.ScriptItemsList)
			{
				if (!(item is ScriptStepBase step))
					continue;

				step.Template = Application.Current.MainWindow.FindResource("AbortAutoRunTemplate") as DataTemplate;
			}
			
		}

		public override void Execute()
		{
			
		}

		

		private void GetCallback(DeviceParameterData param, CommunicatorResultEnum result, string resultDescription)
		{
			
		}
	}
}
