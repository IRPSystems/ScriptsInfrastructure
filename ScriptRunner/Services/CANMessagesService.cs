
using Communication;
using DeviceCommunicators.General;
using Newtonsoft.Json;
using ScriptHandler.Interfaces;
using ScriptHandler.Models;
using ScriptHandler.Services;
using ScriptRunner.ViewModels;
using Services.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ScriptRunner.Services
{
	public class CANMessagesService
	{
		[DllImport("user32.dll")]
		public static extern bool SetForegroundWindow(IntPtr hWnd);

		#region Fiedls

		
		private CANMessageSenderViewModel _canMessageSender;

		#endregion Fiedls

		#region Constructor

		public CANMessagesService(CANMessageSenderViewModel canMessageSender)
		{
			_canMessageSender = canMessageSender;
		}

		#endregion Constructor


		#region Methods


		

		public void CloseCANMessageSender()
		{
			_canMessageSender.StopAllCANMessages();
		}

		public void SendCANMessageScript(string scriptPath)
		{
			

			try
			{

				string jsonString = System.IO.File.ReadAllText(scriptPath);

				JsonSerializerSettings settings = new JsonSerializerSettings();
				settings.Formatting = Formatting.Indented;
				settings.TypeNameHandling = TypeNameHandling.All;

				ScriptData script = JsonConvert.DeserializeObject(jsonString, settings) as
					ScriptData;

				GenerateProjectService generator = new GenerateProjectService();
				List<DeviceCommunicator> usedCommunicatorsList = new List<DeviceCommunicator>();
				GeneratedScriptData genScript = generator.GenerateScript(
					scriptPath,
					script,
					null,
					null,
					ref usedCommunicatorsList);

				foreach (IScriptItem item in genScript.ScriptItemsList)
				{
					if (!(item is ScriptStepCANMessage canMessage))
						continue;

					string sz = JsonConvert.SerializeObject(canMessage, settings);

					_canMessageSender.CANMessageRequest(sz);
				}
			}
			catch (Exception ex) 
			{
				LoggerService.Error(this, "Failed to send script", ex);
			}
		}

		public void StopSendCANMessageScript()
		{
			_canMessageSender.StopAllCANMessages();
		}


		public void SendUpdateMessage(ScriptStepCANMessageUpdate updateMessage)
		{
			try
			{
				SendStep(updateMessage);
			}
			catch(Exception ex) 
			{
				LoggerService.Error(this, "Failed to update message", ex);
			}
		}

		public void SendStopMessage(ScriptStepCANMessageStop stopMessage)
		{
			try
			{
				SendStep(stopMessage);
			}
			catch (Exception ex)
			{
				LoggerService.Error(this, "Failed to stop message", ex);
			}
		}





		private void SendStep(ScriptStepBase step)
		{
			JsonSerializerSettings settings = new JsonSerializerSettings();
			settings.Formatting = Formatting.Indented;
			settings.TypeNameHandling = TypeNameHandling.All;
			string sz = JsonConvert.SerializeObject(step, settings);

			_canMessageSender.CANMessageRequest(sz);
		}


		#endregion Methods
	}
}
