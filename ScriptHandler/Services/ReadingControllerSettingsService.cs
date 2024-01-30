
using CommunityToolkit.Mvvm.ComponentModel;
using Entities.Models;
using ExcelDataReader;
using ScriptHandler.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace ScriptHandler.Services
{
	public class ReadingControllerSettingsService : ObservableObject
	{
		public enum ColumnTypesEnum
		{
			Device,
			Name,
		}

		public List<ControllerSettingsData> GetMotorSettings(
			string pathCommand, 
			string pathSettings)
		{
			if (File.Exists(pathCommand) == false)
				return null;

			List<ControllerSettingsData> motorSettingsList = ReadCommands(pathCommand);

			List<ParameterValueData> statusParams = ReadSettings(pathSettings); 
			foreach (ControllerSettingsData data in motorSettingsList)
			{
				data.StatusParameterValueList = new List<ParameterValueData>();
				foreach (ParameterValueData paramData in statusParams) 
				{ 
					data.StatusParameterValueList.Add(paramData);
				}
			}


			return motorSettingsList;
		}

		private List<ParameterValueData> ReadSettings(string path)
		{
			List<ParameterValueData> statusParams = new List<ParameterValueData>();

			IExcelDataReader reader;
			var stream = File.Open(path, FileMode.Open, FileAccess.Read);
			reader = ExcelReaderFactory.CreateReader(stream);

			//// reader.IsFirstRowAsColumnNames
			var conf = new ExcelDataSetConfiguration
			{
				ConfigureDataTable = _ => new ExcelDataTableConfiguration
				{
					//	UseHeaderRow = true
				}
			};

			var dataSet = reader.AsDataSet(conf);

			// Now you can get data from each sheet by its index or its "name"
			var dataTable = dataSet.Tables[0];

			int row = 0;
			
			for (int col = 0; col < dataTable.Columns.Count; col++)
			{
				var item = dataTable.Rows[row][col];
				string colStr = item.ToString();
				colStr = colStr.Replace("\"", string.Empty);

				ParameterValueData paramData = new ParameterValueData();
				paramData.ParameterName = colStr.Trim();
				statusParams.Add(paramData);
			}

			reader.Close();

			return statusParams;
		}

		private List<ControllerSettingsData> ReadCommands(string path)
		{
			List<ControllerSettingsData> motorSettingsList = new List<ControllerSettingsData>();

			IExcelDataReader reader;
			var stream = File.Open(path, FileMode.Open, FileAccess.Read);
			reader = ExcelReaderFactory.CreateReader(stream);

			//// reader.IsFirstRowAsColumnNames
			var conf = new ExcelDataSetConfiguration
			{
				ConfigureDataTable = _ => new ExcelDataTableConfiguration
				{
				//	UseHeaderRow = true
				}
			};

			var dataSet = reader.AsDataSet(conf);

			// Now you can get data from each sheet by its index or its "name"
			var dataTable = dataSet.Tables[0];

			int row = 0;
			List<string> columnsList = new List<string>();
			for (int col = 0; col < dataTable.Columns.Count; col++)
			{
				var item = dataTable.Rows[row][col];
				string colStr = item.ToString();
				colStr = colStr.Replace("\"", string.Empty);
				columnsList.Add(colStr.Trim());
			}

			row++;
			for (; row < dataTable.Rows.Count; row++)
			{
				var item = dataTable.Rows[row][0];
				if (item is DBNull)
					continue;


				ControllerSettingsData data = new ControllerSettingsData();
				data.CommandParameterValueList = new List<ParameterValueData>();

				int val = 0;
				bool res = false;
				for(int col = 0; col < dataTable.Columns.Count; col++)
				{
					item = dataTable.Rows[row][col];

					switch ((ColumnTypesEnum)col)
					{
						case ColumnTypesEnum.Device: data.Device = item.ToString(); break;
						case ColumnTypesEnum.Name: data.Name = item.ToString(); break;
						default:
							if (col > (int)ColumnTypesEnum.Name &&
								col < columnsList.Count)
							{
								string itemStr = item.ToString();
								itemStr = itemStr.Replace("°c", string.Empty);
								res = int.TryParse(itemStr.ToString(), out val);

								ParameterValueData pvd = new ParameterValueData()
								{
									ParameterName = columnsList[col],
									Value = val,
								};

								data.CommandParameterValueList.Add(pvd);
							}

							break;
					}
				}

				motorSettingsList.Add(data);
			}


			reader.Close();

			return motorSettingsList;
		}
	}
}
