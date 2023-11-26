using CommunityToolkit.Mvvm.ComponentModel;
using DeviceCommunicators.General;
using Entities.Models;
using ScriptHandler.Enums;
using ScriptHandler.Interfaces;
using System;
using System.Threading;

namespace ScriptHandler.Models
{
	public class SweepItemForRunData : ObservableObject
	{
		public enum SubScriptStateEnum { Success, Failure, None }

		public DeviceParameterData Parameter { get; set; }

		public double StartValue { get; set; }
		public double EndValue { get; set; }
		public double StepValue { get; set; }

		public int StepInterval { get; set; }
		public TimeUnitsEnum StepIntervalTimeUnite { get; set; }

		public double CurrentValue { get; set; }

		public TimeSpan ActualInterval { get; set; }

		public ScriptStepSetParameter SetParameter { get; set; }
		public ScriptStepDelay Delay { get; set; }

		public IScriptRunner SubScriptRunner { get; set; }

		public SubScriptStateEnum IsSubScriptPass { get; set; }

		//public ManualResetEvent ScriptEndedEventHandler { get; set; }



	}
}
