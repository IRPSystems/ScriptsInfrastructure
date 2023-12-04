
using CommunityToolkit.Mvvm.Input;
using DeviceCommunicators.General;
using DeviceHandler.Models;
using Newtonsoft.Json;
using ScriptHandler.Interfaces;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Services;
using Services.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ScriptHandler.Models
{
	public class ScriptStepDynamicControl : ScriptStepBase
	{
		#region Properties

		public ObservableCollection<DynamicControlColumnData> ColumnDatasList { get; set; }
		public ObservableCollection<DynamicControlFileLine> ExecuteLinesList { get; set; }


		public int PercentageOfLines { get; set; }

		public DynamicControlFileLine CurrentLine { get; set; }

		#endregion Properties

		#region Fields

		private CancellationTokenSource _cancellationTokenSource;
		private CancellationToken _cancellationToken;

		private ScriptStepSetParameter _setParam;

		private int _linesCounter;
		private DateTime _startTime;

		#endregion Fields

		#region Constructor

		public ScriptStepDynamicControl()
		{
			Template = Application.Current.MainWindow.FindResource("DynamicControlTemplate") as DataTemplate;

			_setParam = new ScriptStepSetParameter();
		}

		#endregion Constructor

		#region Methods

		public override void Execute()
		{
			IsPass = false;
			_linesCounter = 0;
			ErrorMessage = "Dynamic Control failed\r\n\r\n";
			PercentageOfLines = 0;

			_cancellationTokenSource = new CancellationTokenSource();
			_cancellationToken = _cancellationTokenSource.Token;

			_linesCounter = 1;
			_startTime = DateTime.Now;
			Task executeTesk = Execute_Do();

			try
			{
				executeTesk.Wait(_cancellationToken);
			}
			catch (OperationCanceledException) { }
		}

		private  Task Execute_Do()
		{
			return Task.Run(() =>
			{
				//List<(TimeSpan, double, double, double)> list = new List<(TimeSpan, double, double, double)>();
				while (!_cancellationToken.IsCancellationRequested && _linesCounter <= ExecuteLinesList.Count)
				{
					DateTime startSend = DateTime.Now;
					DynamicControlFileLine line = ExecuteLinesList[_linesCounter - 1];
					DynamicControlFileLine lineNext = null;
					if (_linesCounter < ExecuteLinesList.Count)
						lineNext = ExecuteLinesList[_linesCounter];
					_linesCounter++;

					CurrentLine = line;

					for (int i = 0; i < line.ValuesList.Count && i < ColumnDatasList.Count; i++)
					{
						_setParam.Parameter = ColumnDatasList[i].Parameter;
						_setParam.Communicator = ColumnDatasList[i].Communicator;
						_setParam.Value = line.ValuesList[i].Value;
						line.ValuesList[i].IsCurrent = true;
						_setParam.Execute();
						line.ValuesList[i].IsCurrent = false;
						if (_setParam.IsPass == false)
							break;
					}

					if (_setParam.IsPass == false)
					{
						ErrorMessage += _setParam.ErrorMessage;
						break;
					}

					if (lineNext != null)
					{
						TimeSpan sendDiff = DateTime.Now - startSend;
						TimeSpan lineDiff = lineNext.Time - line.Time;
						double timeToWait = lineDiff.TotalMilliseconds - sendDiff.TotalMilliseconds;
						if((int)timeToWait > 0)
							Task.Delay((int)timeToWait).Wait(_cancellationToken);
					}

					//list.Add((DateTime.Now - startSend, line.ValuesList[0], line.ValuesList[1], line.ValuesList[2]));

					PercentageOfLines = (int)(((double)_linesCounter / (double)ExecuteLinesList.Count) * 100.0);
					OnPropertyChanged(nameof(PercentageOfLines));
				}

				if(_linesCounter >= ExecuteLinesList.Count)
					IsPass = true;

				//using(StreamWriter sw = new StreamWriter(@"C:\Users\smadar\Documents\Stam\DynamicControl.csv"))
				//{
				//	foreach((TimeSpan, double, double, double) line in list)
				//	{
				//		string str = line.Item1 + "," + line.Item2 + "," + line.Item3 + "," + line.Item4;
				//		sw.WriteLine(str);
				//	}
				//}
			});
		}

		public override void Generate(
			ScriptNodeBase sourceNode,
			Dictionary<int, ScriptStepBase> stepNameToObject,
			ref List<DeviceCommunicator> usedCommunicatorsList,
			GenerateProjectService generateService,
			DevicesContainer devicesContainer)
		{
			ExecuteLinesList = new ObservableCollection<DynamicControlFileLine>();
			foreach (DynamicControlFileLine line in (sourceNode as ScriptNodeDynamicControl).FileLinesList)
			{
				ExecuteLinesList.Add(line.Clone() as DynamicControlFileLine);
			}

					ColumnDatasList = new ObservableCollection<DynamicControlColumnData>();
			foreach (DynamicControlColumnData column in (sourceNode as ScriptNodeDynamicControl).ColumnDatasList)
			{
				ColumnDatasList.Add(column.Clone() as DynamicControlColumnData);
			}
		}

		protected override void Stop()
		{
			if (_cancellationTokenSource == null)
				return;

			_cancellationTokenSource.Cancel();
			LoggerService.Debug(this, "Dynamic Control stopped");
		}

		public override void Resume()
		{
			_cancellationTokenSource = new CancellationTokenSource();
			_cancellationToken = _cancellationTokenSource.Token;

			Task executeTesk = Execute_Do();
			executeTesk.Wait(_cancellationToken);
		}

		public override bool IsNotSet(
			DevicesContainer devicesContainer,
			ObservableCollection<InvalidScriptItemData> errorsList)
		{		

			if (ColumnDatasList == null || ColumnDatasList.Count == 0 ||
				ExecuteLinesList == null || ExecuteLinesList.Count == 0)
			{
				return true;
			}

			return false;
		}

		private void DynamicControlDataGrid_SelectionChanged(SelectionChangedEventArgs e)
		{
			if (!(e.Source is DataGrid dataGrid))
				return;

			dataGrid.ScrollIntoView(CurrentLine);
		}

		#endregion Methods


		#region Commands

		private RelayCommand<SelectionChangedEventArgs> _DynamicControlDataGrid_SelectionChangedCommand;
		public RelayCommand<SelectionChangedEventArgs> DynamicControlDataGrid_SelectionChangedCommand
		{
			get
			{
				return _DynamicControlDataGrid_SelectionChangedCommand ?? (_DynamicControlDataGrid_SelectionChangedCommand =
					new RelayCommand<SelectionChangedEventArgs>(DynamicControlDataGrid_SelectionChanged));
			}
		}

		#endregion Commands

	}
}
