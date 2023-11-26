
using Communication;
using DeviceCommunicators.General;
using Newtonsoft.Json;
using ScriptHandler.Interfaces;
using ScriptHandler.Models;
using ScriptHandler.Services;
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

		
		private NamedPipeSenderSevice _pipeSender;

		#endregion Fiedls

		#region Constructor

		public CANMessagesService()
		{
			WaitForConnection();
		}

		#endregion Constructor


		#region Methods

		private void WaitForConnection()
		{
			Task.Run(() =>
			{
				_pipeSender = new NamedPipeSenderSevice();
				_pipeSender.Init("CANMessage");
			});
		}

		public void OpenCANMessageSender()
		{
			Mouse.OverrideCursor = Cursors.Wait;

			Process[] processList = Process.GetProcessesByName("EvvaCANMessageSender");
			if(processList != null && processList.Length > 0) 
			{
				Process process = processList[0];

				IntPtr s = process.MainWindowHandle;
				SetForegroundWindow(s);

				Mouse.OverrideCursor = null;
				return;
			}


			ProcessStartInfo start = new ProcessStartInfo();
			start.FileName = "EvvaCANMessageSender\\EvvaCANMessageSender.exe";
			Process evvaCANMessageSender = Process.Start(start);
			Mouse.OverrideCursor = null;

			Task.Run(() =>
			{
				evvaCANMessageSender.WaitForExit();
				WaitForConnection();
			});
		}

		public void CloseCANMessageSender()
		{
			if (_pipeSender == null || !_pipeSender.IsConnected)
				return;

			_pipeSender.Send("Clear");
			_pipeSender.Dispose();
		}

		public void SendCANMessageScript(string scriptPath)
		{
			if (_pipeSender == null || !_pipeSender.IsConnected)
				return;

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
					ref usedCommunicatorsList);

				foreach (IScriptItem item in genScript.ScriptItemsList)
				{
					if (!(item is ScriptStepCANMessage canMessage))
						continue;

					string sz = JsonConvert.SerializeObject(canMessage, settings);

					if (_pipeSender.IsConnected)
						_pipeSender.Send(sz);
				}
			}
			catch (Exception ex) 
			{
				LoggerService.Error(this, "Failed to send script", ex);
			}
		}

		public void StopSendCANMessageScript()
		{
			if (_pipeSender == null || !_pipeSender.IsConnected)
				return;

			if (_pipeSender.IsConnected)
				_pipeSender.Send("Clear");
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

			if (_pipeSender.IsConnected)
				_pipeSender.Send(sz);
		}


		#endregion Methods
	}
}
