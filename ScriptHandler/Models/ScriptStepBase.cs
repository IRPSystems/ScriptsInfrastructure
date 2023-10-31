
using CommunityToolkit.Mvvm.ComponentModel;
using DeviceCommunicators.General;
using DeviceHandler.Models;
using Newtonsoft.Json;
using ScriptHandler.Enums;
using ScriptHandler.Interfaces;
using ScriptHandler.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;

namespace ScriptHandler.Models
{
	public class ScriptStepBase : ObservableObject, IScriptItem
	{
		public string Name { get; set; }
		public virtual string Description { get; set; }

		public bool IsPass { get; set; }

		[JsonIgnore]
		public IScriptItem PassNext { get; set; }
		[JsonIgnore]
		public IScriptItem FailNext { get; set; }

		public string PassNextDescription { get; set; }
		public string FailNextDescription { get; set; }

		[JsonIgnore]
		public DataTemplate Template { get; set; }

		public virtual string ErrorMessage { get; set; }

		private StopScriptStepService _stopScriptStep;
		public StopScriptStepService StopScriptStep
		{
			get => _stopScriptStep;
			set
			{
				_stopScriptStep = value;
				if(_stopScriptStep != null) 
				{
					_stopScriptStep.StopEvent += Stop;
				}
			}
		}

		public Brush BorderBrush { get; set; }

		private SciptStateEnum _stepState;
		public SciptStateEnum StepState 
		{
			get => _stepState;
			set
			{
				_stepState = value;
				OnPropertyChanged(nameof(StepState));
			}
		}

		public int ID { get; set; }

		public ScriptStepBase()
		{
			
		}


		public virtual void Execute()
		{

		}

		protected virtual void Stop()
		{

		}

		public virtual void Resume()
		{
			Execute();
		}

		public virtual bool IsNotSet(
			DevicesContainer devicesContainer,
			ObservableCollection<InvalidScriptItemData> errorsList)
		{
			return false;
		}

		public virtual void Generate(
			ScriptNodeBase sourceNode,
			Dictionary<int, ScriptStepBase> stepNameToObject,
			ref List<DeviceCommunicator> usedCommunicatorsList,
			GenerateProjectService generateService,
			DevicesContainer devicesContainer)
		{
		}


	}
}
