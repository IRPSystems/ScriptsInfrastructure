
using DeviceCommunicators.General;
using DeviceHandler.Models;
using DeviceHandler.Models.DeviceFullDataModels;
using Newtonsoft.Json;
using ScriptHandler.Enums;
using ScriptHandler.Interfaces;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Services;
using ScriptHandler.ViewModels;
using Services.Services;
using Syncfusion.Windows.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows;

namespace ScriptHandler.Models
{
	public class ScriptStepSweep : ScriptStepBase
	{
		#region Properties

		public ObservableCollection<SweepItemData> SweepItemsList { get; set; }

		[JsonIgnore]
		public ObservableCollection<SweepItemForRunData> ForRunList { get; set; }

		private ObservableCollection<DeviceFullData> _devicesList;
		[JsonIgnore]
		public ObservableCollection<DeviceFullData> DevicesList 
		{
			get => _devicesList;
			set
			{
				_devicesList = value;
				SetCommunicators();
			}
		}

		[JsonIgnore]
		public IScriptRunner CurrentScript { get; set; }
		[JsonIgnore]
		public ScriptDiagramViewModel CurrentScriptDiagram { get; set; }


		#endregion Properties

		#region Fields

		private bool _isStopped;

		private Dictionary<SweepItemData, SweepItemForRunData> _sweepItemToForRunItem;


		private CancellationTokenSource _cancellationTokenSource;
		private CancellationToken _cancellationToken;

		private ManualResetEvent _scriptEndedEventHandler;

		private SweepItemForRunData _itemForRun;


		#endregion Fields

		#region Constructor

		public ScriptStepSweep()
		{
			Template = Application.Current.MainWindow.FindResource("SweepTemplate") as DataTemplate;
		}

		#endregion Constructor

		#region Methods

		public override void Execute()
		{
			if (SweepItemsList == null || SweepItemsList.Count == 0)
				return;

			_isStopped = false;
			IsPass = true;

			_cancellationTokenSource = new CancellationTokenSource();
			_cancellationToken = _cancellationTokenSource.Token;

			System.Threading.Thread.Sleep(1000);

			LoggerService.Inforamtion(this, "Start Execute");

			ExecuteItem(SweepItemsList[0]);

			LoggerService.Inforamtion(this, "End Execute");
		}

		
		private void ExecuteItem(SweepItemData item)
		{
			LoggerService.Inforamtion(this, "Start item: " + item.Parameter.Name);

			for (double i = item.StartValue; IsContinueLoop(item.EndValue, item.StepValue, i) && !_cancellationToken.IsCancellationRequested; i += item.StepValue)
			{
				if (_isStopped)
					break;

				if (_sweepItemToForRunItem.ContainsKey(item) == false)
					continue;

				_itemForRun = _sweepItemToForRunItem[item];
				if(_itemForRun.SetParameter.Communicator == null)
				{
					ErrorMessage = "Communicator not initiated";
					IsPass = false;
						return;
				}

				_scriptEndedEventHandler = new ManualResetEvent(false);

				ErrorMessage = "Failed to set the parameter \"" + item.Parameter.Name +
					"\" to the value " + i + "\r\n\r\n";

				LoggerService.Inforamtion(this, "Set value item: " + item.Parameter.Name + "; value: " + i);
				bool isContinue = SetValue(
					i,
					_itemForRun);
				if (!isContinue)
					return;



				_itemForRun.CurrentValue = i;

				if (_itemForRun.SubScriptRunner != null)
				{
					LoggerService.Inforamtion(this, "Start sub-script for item: " + item.Parameter.Name);

					CurrentScriptDiagram = _itemForRun.CurrentScriptDiagram;
					

					CurrentScript = _itemForRun.SubScriptRunner;
					CurrentScript.ScriptEndedEvent += SubScriptEndedEventHandler;
					CurrentScript.Start();

					WaitHandle.WaitAny(
						new WaitHandle[] {
							_scriptEndedEventHandler,
							_cancellationToken.WaitHandle });

					CurrentScript.ScriptEndedEvent -= SubScriptEndedEventHandler;
					if (CurrentScript.CurrentScript.IsPass == false)
					{
						LoggerService.Inforamtion(this, "Faild sub-script for item: " + item.Parameter.Name);
						ErrorMessage = "Sub script \"" + CurrentScript.CurrentScript.Name + "\" failed.\r\n\r\n" +
							CurrentScript.ScriptErrorMessage;
						IsPass = false;
						CurrentScript = null;
						_itemForRun.SubScriptState = SweepItemForRunData.SubScriptStateEnum.Failure;
						return;
					}

					LoggerService.Inforamtion(this, "Pass sub-script for item: " + item.Parameter.Name);
					CurrentScript = null;
					_itemForRun.SubScriptState = SweepItemForRunData.SubScriptStateEnum.Success;
				}

				

				if (item.Next != null && item.Next.Parameter != null)
					ExecuteItem(item.Next);
				else
				{
					if (_itemForRun.SubScriptRunner == null)
					{
						_itemForRun.Delay.Execute();
						//WaitHandle.WaitAny(
						//	new WaitHandle[] { _cancellationToken.WaitHandle },
						//		forRun.ActualInterval);
					}
				}


				if (IsPass == false && _itemForRun.SubScriptState != SweepItemForRunData.SubScriptStateEnum.Failure)
					return;
				
			}
		}


		private bool SetValue(
			double val,
			SweepItemForRunData itemForRun)
		{
			itemForRun.SetParameter.Value = val;
			itemForRun.SetParameter.Execute();
			IsPass = itemForRun.SetParameter.IsPass;
			if (_isStopped)
				return false;

			if (IsPass == false)
			{
				ErrorMessage = itemForRun.SetParameter.ErrorMessage;
				return false;
			}

			return true;
		}

		private bool IsContinueLoop(
			double endValue,
			double stepValue,
			double i)
		{
			double prevValue = i - stepValue;
			if (prevValue != endValue)
				return true;

			return false;
		}

		protected override void Stop()
		{
			if (_isStopped)
				return;

			_isStopped = true;


			if (CurrentScript != null)
				CurrentScript.Abort();


			if (_cancellationTokenSource != null)
				_cancellationTokenSource.Cancel();

		}

		#region Init data for run

		public void SetDataCollections()
		{
			if (SweepItemsList == null || SweepItemsList.Count == 0)
				return;

			ForRunList = new ObservableCollection<SweepItemForRunData>();
			_sweepItemToForRunItem = new Dictionary<SweepItemData, SweepItemForRunData>();

			for (int i = 1; i < SweepItemsList.Count; i++)
			{
				SweepItemsList[i - 1].Next = SweepItemsList[i];
			}

			for (int i = 0; i < SweepItemsList.Count; i++)
			{
				SweepItemForRunData forRun = new SweepItemForRunData()
				{
					Parameter = SweepItemsList[i].Parameter,
					StartValue = SweepItemsList[i].StartValue,
					EndValue = SweepItemsList[i].EndValue,
					StepValue = SweepItemsList[i].StepValue,
					StepInterval = SweepItemsList[i].StepInterval,
					StepIntervalTimeUnite = SweepItemsList[i].StepIntervalTimeUnite,
					CurrentValue = 0,
					ActualInterval = new TimeSpan(),
					SetParameter = new ScriptStepSetParameter() { Parameter = SweepItemsList[i].Parameter, },
					Delay = new ScriptStepDelay()
					{
						Interval = SweepItemsList[i].StepInterval,
						IntervalUnite = SweepItemsList[i].StepIntervalTimeUnite,
						StopScriptStep = this.StopScriptStep,
					},
					SubScriptState = SweepItemForRunData.SubScriptStateEnum.None,				
				
				};
				
				switch (SweepItemsList[i].StepIntervalTimeUnite)
				{
					case TimeUnitsEnum.ms: forRun.ActualInterval = new TimeSpan(0, 0, 0, 0, SweepItemsList[i].StepInterval); break;
					case TimeUnitsEnum.sec: forRun.ActualInterval = new TimeSpan(0, 0, 0, SweepItemsList[i].StepInterval, 0); break;
					case TimeUnitsEnum.min: forRun.ActualInterval = new TimeSpan(0, 0, SweepItemsList[i].StepInterval, 0, 0); break;
					case TimeUnitsEnum.hour: forRun.ActualInterval = new TimeSpan(0, SweepItemsList[i].StepInterval, 0, 0, 0); break;
				}


				ForRunList.Add(forRun);
				_sweepItemToForRunItem.Add(SweepItemsList[i], forRun);


			}
		}

		public void SetCommunicators()
		{
			if (DevicesList == null ||
				ForRunList == null || ForRunList.Count == 0)
			{
				return;
			}


			foreach(SweepItemForRunData data in ForRunList) 
			{ 
				if(data.Parameter == null) 
					continue;

				DeviceFullData deviceData = DevicesList.ToList().Find((d) => d.Device.DeviceType == data.Parameter.DeviceType);
				if(deviceData == null)
					continue;

				data.SetParameter.Communicator = deviceData.DeviceCommunicator;
			}
		}

		#endregion Init data for run

		private void SubScriptEndedEventHandler(bool isAborted)
		{
			_scriptEndedEventHandler.Set();
		}

		public bool Reset()
		{
			foreach(SweepItemForRunData itemForRun in _sweepItemToForRunItem.Values)
			{
				if (itemForRun == _itemForRun)
					break;


				bool isContinue = SetValue(
					itemForRun.CurrentValue,
					itemForRun);
				if (!isContinue)
					return false;

			}

			return true;
		}

		public override bool IsNotSet(
			DevicesContainer devicesContainer,
			ObservableCollection<InvalidScriptItemData> errorsList)
		{
			if (SweepItemsList == null || SweepItemsList.Count == 0)
				return true;

			IsTestValidService isTestValid = new IsTestValidService();

			foreach(SweepItemData item in SweepItemsList)
			{

				if (item.SubScript != null)
				{
					InvalidScriptData invalidScriptData = new InvalidScriptData()
					{
						Name = item.SubScript.Name,
						Script = item.SubScript,
						ScirptItem = this,
					};


					bool isValid = isTestValid.IsValid(
						item.SubScript,
						invalidScriptData,
						devicesContainer);

					if(invalidScriptData.ErrorsList.Count > 0)
						errorsList.Add(invalidScriptData);
				}
			}

			return false;
		}

		public override void Generate(
			ScriptNodeBase sourceNode,
			Dictionary<int, ScriptStepBase> stepNameToObject,
			ref List<DeviceCommunicator> usedCommunicatorsList,
			GenerateProjectService generateService,
			DevicesContainer devicesContainer)
		{
			SweepItemsList = new ObservableCollection<SweepItemData>();

			for (int i = 0; i < (sourceNode as ScriptNodeSweep).SweepItemsList.Count; i++)
			{
				SweepItemData sourceItem = (sourceNode as ScriptNodeSweep).SweepItemsList[i];
				SweepItemData destItem = sourceItem.Clone() as SweepItemData;

				if (sourceItem.SubScript != null)
				{
					destItem.SubScript = generateService.GenerateScript(
						null,
						sourceItem.SubScript,
						devicesContainer,
						null,
						ref usedCommunicatorsList);
				}

				SweepItemsList.Add(destItem);
			}
		}

		#endregion Methods


	}
}
