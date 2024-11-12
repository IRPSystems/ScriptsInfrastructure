﻿
using CommunityToolkit.Mvvm.ComponentModel;
using CsvHelper;
using Entities.Models;
using ScriptRunner.Enums;
using Services.Services;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace ScriptRunner.Services
{
    public class ScriptLoggerService: ObservableObject, IDisposable
    {
		#region Properties

		public ObservableCollection<LogLineData> LogLinesList { get; set; }

		#endregion Properties

		#region Fields



		#endregion Fields

		#region Constructor

		public ScriptLoggerService()
        {
			
			LogLinesList = new ObservableCollection<LogLineData>();
		}

		#endregion Constructor

		#region Methods


		public void AddLine(
			LogLineData lineData, 
			LogTypeEnum logType)
        {
			LogLinesList.Add(lineData);

			SetLineCollors(lineData, logType);

			LoggerService.Inforamtion(this, $"***[{lineData.Time}] [{lineData.Data}] [{logType}]");
		}

		public void AddLine( 
			TimeSpan time,
			string data,
			LogTypeEnum logType)
		{
			LogLineData lineData = new LogLineData()
			{
				Time = time,
				Data = data,
			};

			

			if (Application.Current != null)
			{
				Application.Current.Dispatcher.Invoke(() =>
				{
					SetLineCollors(lineData, logType);
					LogLinesList.Add(lineData);
				});
			}

			LoggerService.Inforamtion(this, $"***[{lineData.Time}] [{lineData.Data}] [{logType}]");
		}

		private void SetLineCollors(
			LogLineData lineData,
			LogTypeEnum logType)
		{
			switch (logType)
			{

				case LogTypeEnum.ScriptData: lineData.Background = Brushes.Magenta; break;
				case LogTypeEnum.StepData: lineData.Background = Brushes.Transparent; break;
				case LogTypeEnum.Pass: lineData.Background = Brushes.Green; break;
				case LogTypeEnum.Fail: lineData.Background = Brushes.Red; break;
				case LogTypeEnum.None: lineData.Background = Brushes.Transparent; break;
			}


			switch (logType)
			{

				case LogTypeEnum.ScriptData: lineData.Foreground = Brushes.White; break;
				case LogTypeEnum.StepData:
					if (Application.Current != null)
						lineData.Foreground = Application.Current.MainWindow.Foreground;
					break;
				case LogTypeEnum.Pass: lineData.Foreground = Brushes.White; break;
				case LogTypeEnum.Fail: lineData.Foreground = Brushes.White; break;
				case LogTypeEnum.None:
					if (Application.Current != null)
						lineData.Foreground = Application.Current.MainWindow.Foreground;
					break;
			}
		}


		public void Dispose()
		{
		}
		public void Clear()
		{
			try
			{
				if (LogLinesList != null && LogLinesList.Count != 0)
				{
					if (Application.Current != null)
					{
						Application.Current.Dispatcher.Invoke(() =>
						{
							LogLinesList.Clear();
						});
					}
				}
					
			}
			catch(Exception ex)
			{
				LoggerService.Error(this, "Failed to clear the log", "Error", ex);
			}
		}

		public void Save(string scriptName)
		{
			try
			{
				string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
				string path = System.IO.Path.Combine(documentsPath, "Logs");
				if (!Directory.Exists(path))
					Directory.CreateDirectory(path);
				path = System.IO.Path.Combine(path, "SciptsLogs");
				if (!Directory.Exists(path))
					Directory.CreateDirectory(path);

				path = System.IO.Path.Combine(path, "Log - " + scriptName + " - " +
						DateTime.Now.ToString("dd-MMM-yyyy HH-mm-ss") + ".csv");

				using (TextWriter textWriter = new StreamWriter(path, false, System.Text.Encoding.UTF8))
				{
					using (CsvWriter csvWriter = new CsvWriter(textWriter, CultureInfo.CurrentCulture))
					{
						csvWriter.WriteField("Time");
						csvWriter.WriteField("Data");
						csvWriter.WriteField("Log type");
						csvWriter.NextRecord();


						foreach (LogLineData line in LogLinesList)
						{
							if(line == null)
								continue;
							string time = line.Time.ToString(@"hh\:mm\:ss\.fffff");
							csvWriter.WriteField(time);
							csvWriter.WriteField(line.Data);
							//csvWriter.WriteField(line.LogType);
							csvWriter.NextRecord();
						}
					}
				}
			}
			catch(Exception ex) 
			{
				LoggerService.Error(this, "Failed to save the log data", ex);
			}
			
		}

		#endregion Methods
	}
}
