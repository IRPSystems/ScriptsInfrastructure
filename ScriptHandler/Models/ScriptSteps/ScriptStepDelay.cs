
using DeviceCommunicators.General;
using DeviceHandler.Models;
using ScriptHandler.Enums;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Services;
using Services.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ScriptHandler.Models
{
	public class ScriptStepDelay: ScriptStepBase
	{
		#region Properties

		public TimeUnitsEnum IntervalUnite { get; set; }
		public int Interval { get; set; }


		public TimeSpan Diff_Timeout { get; set; }
		public TimeSpan TsTimeout { get; set; }
		public int TimeoutPercentage { get; set; }

		#endregion Properties

		#region Fields

		private CancellationTokenSource _cancellationTokenSource;
		private CancellationToken _cancellationToken;

		private DateTime _timeout;
		private TimeSpan _pauseTime;
		private DateTime _startTimeout;

		#endregion Fields

		#region Constructor

		public ScriptStepDelay()
		{
			if (Application.Current != null)
			{
				Application.Current.Dispatcher.Invoke(() =>
				{
					Template = Application.Current.MainWindow.FindResource("DelayTemplate") as DataTemplate;
				});
			}
		}

		#endregion Constructor


		#region Methods

		public override void Execute()
		{
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _cancellationToken = _cancellationTokenSource.Token;

                _pauseTime = new TimeSpan();
                _startTimeout = DateTime.Now;
                IsExecuted = true;

                switch (IntervalUnite)
                {
                    case TimeUnitsEnum.ms: TsTimeout = new TimeSpan(0, 0, 0, 0, Interval); break;
                    case TimeUnitsEnum.sec: TsTimeout = new TimeSpan(0, 0, 0, Interval, 0); break;
                    case TimeUnitsEnum.min: TsTimeout = new TimeSpan(0, 0, Interval, 0, 0); break;
                    case TimeUnitsEnum.hour: TsTimeout = new TimeSpan(0, Interval, 0, 0, 0); break;
                }

                var myTask = Task.Factory
                        .StartNew(() => Execute_Do(), _cancellationTokenSource.Token);


                myTask.Wait();

                LoggerService.Debug(this, "Delay ended");
                IsPass = true;
            }
            finally
            {
                //finished derived class execute method
                stopwatch.Stop();
                ExecutionTime = stopwatch.Elapsed;
            }

		}

		private void Execute_Do()
		{
			while (!_cancellationToken.IsCancellationRequested)
			{
				if (Application.Current == null)
					break;

				Application.Current.Dispatcher.Invoke(() =>
				{
					_timeout = DateTime.Now;
					Diff_Timeout = (_timeout - _startTimeout) - _pauseTime;

					TimeoutPercentage = (int)((Diff_Timeout.TotalMilliseconds / TsTimeout.TotalMilliseconds) * 100);

					if (Diff_Timeout.TotalMilliseconds >= TsTimeout.TotalMilliseconds)
					{
						_cancellationTokenSource.Cancel();
					}
				});
				
				

				System.Threading.Thread.Sleep(10);
			}

			
		}

		protected override void Stop()
		{
			if (_cancellationTokenSource == null)
				return;

			_cancellationTokenSource.Cancel();
			LoggerService.Debug(this, "Delay stopped");
		}

		public override void Resume()
		{
			_cancellationTokenSource = new CancellationTokenSource();
			_cancellationToken = _cancellationTokenSource.Token;

			var myTask = Task.Factory
					.StartNew(() => Execute_Do(), _cancellationTokenSource.Token);


			myTask.Wait();

			LoggerService.Debug(this, "Delay ended");
			IsPass = true;
		}

		public override bool IsNotSet(
			DevicesContainer devicesContainer,
			ObservableCollection<InvalidScriptItemData> errorsList)
		{
			if (Interval <= 0)
				return true;

			return false;
		}

		protected override void Generate(
			ScriptNodeBase sourceNode,
			Dictionary<int, ScriptStepBase> stepNameToObject,
			ref List<DeviceCommunicator> usedCommunicatorsList,
			GenerateProjectService generateService,
			DevicesContainer devicesContainer)
		{
			Interval = (sourceNode as ScriptNodeDelay).Interval;
			IntervalUnite = (sourceNode as ScriptNodeDelay).IntervalUnite;
		}

		#endregion Methods
	}
}
