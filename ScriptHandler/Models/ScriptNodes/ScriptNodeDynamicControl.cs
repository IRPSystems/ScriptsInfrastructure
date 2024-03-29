﻿
using CommunityToolkit.Mvvm.Input;
using DeviceHandler.Models;
using ExcelDataReader;
using Microsoft.Win32;
using Services.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace ScriptHandler.Models.ScriptNodes
{
	public class ScriptNodeDynamicControl : ScriptNodeBase
	{
		#region Properties

		private string _filePath;
		public string FilePath
		{
			get => _filePath;
			set
			{
				_filePath = value;
				OnPropertyChanged(nameof(FilePath));
			}
		}

		public ObservableCollection<DynamicControlColumnData> ColumnDatasList { get; set; }		
		public List<DynamicControlFileLine> FileLinesList { get; set; }

		public override string Description
		{
			get
			{
				return "Dynamic Control - ID:" + ID;
			}
		}

		#endregion Properties

		#region Constructor

		public ScriptNodeDynamicControl()
		{
			Name = "Dynamic Control";

			Init();
		}

		#endregion Constructor

		#region Methods

		private void Init()
		{
			BrowseFilePathCommand = new RelayCommand(BrowseFilePath);
			FileLinesList = new List<DynamicControlFileLine>();
			ColumnDatasList = new ObservableCollection<DynamicControlColumnData>();
		}

		private void BrowseFilePath()
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Excel Files|*.xls;*.xlsx;*.xlsm;*.csv";
			bool? result = openFileDialog.ShowDialog();
			if (result != true)
				return;

			FilePath = openFileDialog.FileName;
			OnPropertyChanged(nameof(FilePath));
			ReadFile();



			if (FileLinesList == null || FileLinesList.Count == 0)
			{
				//LoggerService.Error(this, "Failed to read the file \"" + FilePath + "\"", "Read File Error");
				return;
			}

			OnPropertyChanged(nameof(FilePath));
			OnPropertyChanged(nameof(ColumnDatasList));
		}

		public void ReadFile()
		{
			if (string.IsNullOrEmpty(FilePath))
				return;

			string extension = Path.GetExtension(FilePath);
			if (extension.ToLower().Contains("csv"))
				ReadFile_CSV();
			else
				ReadFile_Excel();
		}

		private void ReadFile_CSV() 
		{ 
			if(File.Exists(FilePath) == false)
			{
				LoggerService.Error(this, "The file \"" + FilePath + "\" was not found", "Read File Error");
				return;
			}

			try
			{

				string fileData = "";
				using (StreamReader sr = new StreamReader(FilePath))
				{
					fileData = sr.ReadToEnd();
				}

				string[] fileLines = fileData.Split("\r\n");
				FileLinesList.Clear();
				bool isFirst = true;


				ObservableCollection<DynamicControlColumnData> columnsData = 
					new ObservableCollection<DynamicControlColumnData>();
				foreach (string line in fileLines)
				{


					DynamicControlFileLine lineData = new DynamicControlFileLine();

					string[] lineCols = line.Split(',');


					#region Init the columns data
					if (isFirst)
					{
						for (int col = 1; col < lineCols.Length; col++)
						{
							DynamicControlColumnData newCol = new DynamicControlColumnData()
							{
								FileIndex = col,
								ColHeader = lineCols[col],
							};

							DynamicControlColumnData column =
								ColumnDatasList.ToList().Find((c) => c.ColHeader == lineCols[col]);
							if(column != null)
								newCol.Parameter = column.Parameter;

							columnsData.Add(newCol);
						}

						isFirst = false;
						ColumnDatasList = columnsData;
						continue;
					}
					#endregion Init the columns data



					double numberOfSecs;
					bool res = double.TryParse(lineCols[0], out numberOfSecs); 
					if (res == false)
					{
						LoggerService.Error(this, "The value \"" + lineCols[0] + "\" is invalid number");
						continue;
					}

					lineData.Time = TimeSpan.FromSeconds(numberOfSecs);

					lineData.ValuesList = new ObservableCollection<DynamicControlData>();
					for (int i = 1; i < lineCols.Length; i++)
					{
						double vald;
						res = double.TryParse(lineCols[i], out vald);
						if (res == false)
						{
							LoggerService.Error(this, "The value \"" + lineCols[i] + "\" is invalid number");
							continue;
						}

						lineData.ValuesList.Add(
							new DynamicControlData() { Value = vald, IsCurrent = false });
					}

					FileLinesList.Add(lineData);
				}
			}
			catch (IOException)
			{
				LoggerService.Error(this, "The file \"" + FilePath + "\" is being used", "Read File Error");
			}
			catch (Exception ex) 
			{
				LoggerService.Error(this, "Failed to open the file  \"" + FilePath + "\"", "Read File Error", ex);
			}
		}


		private void ReadFile_Excel() 
		{
			if (File.Exists(FilePath) == false)
			{
				LoggerService.Error(this, "The file \"" + FilePath + "\" was not found");
				return;
			}

			try
			{
				IExcelDataReader reader;
				System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
				var stream = File.Open(FilePath, FileMode.Open, FileAccess.Read);
				reader = ExcelReaderFactory.CreateReader(stream);

				var dataSet = reader.AsDataSet();

				// Now you can get data from each sheet by its index or its "name"
				var dataTable = dataSet.Tables[0];


				ObservableCollection<DynamicControlColumnData> columnsData =
					new ObservableCollection<DynamicControlColumnData>();
				for (int col = 1; col < dataTable.Columns.Count; col++)
				{

					var v = dataTable.Rows[0][col];
					if (v == null)
						continue;

					DynamicControlColumnData newCol = new DynamicControlColumnData()
					{
						FileIndex = col,
						ColHeader = v.ToString(),
					};

					DynamicControlColumnData column =
								ColumnDatasList.ToList().Find((c) => c.ColHeader == v.ToString());
					if (column != null)
						newCol.Parameter = column.Parameter;

					columnsData.Add(newCol);
				}

				ColumnDatasList = columnsData;


				FileLinesList.Clear();
				for (int row = 1; row < dataTable.Rows.Count; row++)
				{
					DynamicControlFileLine lineData = new DynamicControlFileLine();

					var v = dataTable.Rows[row][0];
					if (v == null)
						continue;

					double numberOfSecs;
					bool res = double.TryParse(v.ToString(), out numberOfSecs);
					if (res == false)
					{
						LoggerService.Error(this, "The value \"" + v + "\" is invalid number");
						continue;
					}

					lineData.Time = TimeSpan.FromSeconds(numberOfSecs);

					lineData.ValuesList = new ObservableCollection<DynamicControlData>();
					for (int col = 1; col < dataTable.Columns.Count; col++)
					{
						v = dataTable.Rows[row][col];
						if (v == null)
							continue;

						string s = v.ToString();

						double vald;
						res = double.TryParse(v.ToString(), out vald);
						if (res == false)
						{
							LoggerService.Error(this, "The value \"" + v.ToString() + "\" is invalid number");
							continue;
						}

						lineData.ValuesList.Add(new DynamicControlData() { Value = vald, IsCurrent = false });
					}

					FileLinesList.Add(lineData);
				}
			}

			catch (IOException)
			{
				LoggerService.Error(this, "The file \"" + FilePath + "\" is being used", "Read File Error");
			}
			catch (Exception ex)
			{
				LoggerService.Error(this, "Failed to open the file  \"" + FilePath + "\"", "Read File Error", ex);
			}
		}



		public override object Clone()
		{
			ScriptNodeDynamicControl dynamicControl = MemberwiseClone() as ScriptNodeDynamicControl;
			dynamicControl.Init();
			return dynamicControl;
		}


		public override bool IsNotSet(
			DevicesContainer devicesContainer,
			ObservableCollection<InvalidScriptItemData> errorsList)
		{
			if (string.IsNullOrEmpty(FilePath))
				return true;

			if (ColumnDatasList.Count == 0 ||
				FileLinesList.Count == 0)
			{
				return true;
			}

			//InvalidScriptItemData invalidScriptItemData = new InvalidScriptItemData()
			//{
			//	Name = Description
			//};

			//if (ColumnDatasList.Count > 0) 
			//{ 
			//	foreach (DynamicControlColumnData item in ColumnDatasList) 
			//	{
			//		if(item.Parameter == null)
			//		{
			//			invalidScriptItemData.ErrorString = "No parameter set for \"" + item.ColHeader + "\"";
			//			errorsList.Add(invalidScriptItemData);
			//			continue;
			//		}

			//		DeviceData deviceData =
			//		   devicesContainer.TypeToDevicesFullData[item.Parameter.DeviceType].Device;

			//		DeviceParameterData data = deviceData.ParemetersList.ToList().Find((p) => p.Name == item.Parameter.Name);
			//		if (data == null)
			//		{
			//			if (item.Parameter == null)
			//			{
			//				string err = "The parameter \"" + item.Parameter.Name + "\" dosn't exist in the current " + deviceData.Name + " parameter file";
			//				errorsList.Add(invalidScriptItemData);
			//				continue;
			//			}
			//		}
			//	}
			//}

			return false;
		}

		#endregion Methods

		#region Commands

		public RelayCommand BrowseFilePathCommand { get; private set; }

		#endregion Commands
	}
}
