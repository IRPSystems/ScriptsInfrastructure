
using DeviceCommunicators.General;
using DeviceHandler.Models;
using Newtonsoft.Json;
using ScriptHandler.Enums;
using ScriptHandler.Interfaces;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Timers;
using System.Windows;

namespace ScriptHandler.Models
{
	public class ScriptStepSubScript : ScriptStepBase, ISubScript
	{
		public IScript Script { get; set; }
		public string ScriptPath { get; set; }
		public string SelectedScriptName { get; set; }

		public SubScriptContinueUntilTypeEnum ContinueUntilType { get; set; }

		public int Repeats { get; set; }

		private int _timeout;
		public int Timeout 
		{
			get => _timeout;
			set
			{
				_timeout = value;
				switch (TimeoutUnite)
				{
					case TimeUnitsEnum.ms: TimeoutSpan = new TimeSpan(0, 0, 0, 0, Timeout); break;
					case TimeUnitsEnum.sec: TimeoutSpan = new TimeSpan(0, 0, 0, Timeout, 0); break;
					case TimeUnitsEnum.min: TimeoutSpan = new TimeSpan(0, 0, Timeout, 0, 0); break;
					case TimeUnitsEnum.hour: TimeoutSpan = new TimeSpan(0, Timeout, 0, 0, 0); break;
				}
			}
		}

		private TimeUnitsEnum _timeoutUnite;
		public TimeUnitsEnum TimeoutUnite 
		{
			get => _timeoutUnite;
			set
			{
				_timeoutUnite = value;
				switch (TimeoutUnite)
				{
					case TimeUnitsEnum.ms: TimeoutSpan = new TimeSpan(0, 0, 0, 0, Timeout); break;
					case TimeUnitsEnum.sec: TimeoutSpan = new TimeSpan(0, 0, 0, Timeout, 0); break;
					case TimeUnitsEnum.min: TimeoutSpan = new TimeSpan(0, 0, Timeout, 0, 0); break;
					case TimeUnitsEnum.hour: TimeoutSpan = new TimeSpan(0, Timeout, 0, 0, 0); break;
				}
			}
		}

		public TimeSpan TimeoutSpan { get; set; }

		public bool IsStopOnFail { get; set; }
		public bool IsStopOnPass { get; set; }


		[JsonIgnore]
		public DateTime StartTime { get; set; }
		[JsonIgnore]
		public int RunIndex { get; set; }
		[JsonIgnore]
		public TimeSpan TimeInSubScript { get; set; }


		public override string ErrorMessage
		{ 
			get => "Failed to run the sub script \"" + Description + "\"";
		}




		private System.Timers.Timer _timerRunTime;


		public ScriptStepSubScript()
		{
			Template = Application.Current.MainWindow.FindResource("AutoRunTemplate") as DataTemplate;

			_timerRunTime = new System.Timers.Timer(300);
			_timerRunTime.Elapsed += RunTimeElapsedEventHandler;
		}

		public void SetTimeoutSpen()
		{
			switch (TimeoutUnite)
			{
				case TimeUnitsEnum.ms: TimeoutSpan = new TimeSpan(0, 0, 0, 0, Timeout); break;
				case TimeUnitsEnum.sec: TimeoutSpan = new TimeSpan(0, 0, 0, Timeout, 0); break;
				case TimeUnitsEnum.min: TimeoutSpan = new TimeSpan(0, 0, Timeout, 0, 0); break;
				case TimeUnitsEnum.hour: TimeoutSpan = new TimeSpan(0, Timeout, 0, 0, 0); break;
			}

			_timerRunTime.Start();
		}

		public void Dispose()
		{
			_timerRunTime.Stop();
		}

		protected override void Stop()
		{

		}

		private void RunTimeElapsedEventHandler(object sender, ElapsedEventArgs e)
		{
			TimeInSubScript = DateTime.Now - StartTime;
		}

		public override bool IsNotSet(
			DevicesContainer devicesContainer,
			ObservableCollection<InvalidScriptItemData> errorsList)
		{
			if (Script == null)
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
			ContinueUntilType = (sourceNode as ScriptNodeSubScript).ContinueUntilType;
			Repeats = (sourceNode as ScriptNodeSubScript).Repeats;
			Timeout = (sourceNode as ScriptNodeSubScript).Timeout;
			TimeoutUnite = (sourceNode as ScriptNodeSubScript).TimeoutUnite;
			IsStopOnFail = (sourceNode as ScriptNodeSubScript).IsStopOnFail;
			IsStopOnPass = (sourceNode as ScriptNodeSubScript).IsStopOnPass;
		}
	}
}
