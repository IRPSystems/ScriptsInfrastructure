
using CommunityToolkit.Mvvm.ComponentModel;
using Entities.Models;
using ExcelDataReader;
using ScriptHandler.Models;
using Services.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Shapes;

namespace ScriptRunner.Services
{
	public class ReadingMotorSettingsService: ObservableObject
	{
		public enum ColumnTypesEnum
		{
			Customer,
			Name,
			//MotorModel,
		}

		public List<MotorSettingsData> GetMotorSettings(
			string pathCommands,
			string pathStatus)
		{
			try
			{

				List<ParameterValueData> stateParamsList = GetStatusParameters(pathStatus);


				List<MotorSettingsData> motorSettingsList = new List<MotorSettingsData>();

				IExcelDataReader reader;
				var stream = File.Open(pathCommands, FileMode.Open, FileAccess.Read);
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
					columnsList.Add(colStr);
				}

				row++;
				for (; row < dataTable.Rows.Count; row++)
				{
					var item = dataTable.Rows[row][0];
					if (item is DBNull)
						continue;


					MotorSettingsData data = new MotorSettingsData()
					{
						StatusParameterValueList = stateParamsList,
					};
					data.CommandParameterValueList = new List<ParameterValueData>();

					int val = 0;
					bool res = false;
					for (int col = 0; col < dataTable.Columns.Count; col++)
					{
						item = dataTable.Rows[row][col];
						res = int.TryParse(item.ToString(), out val);

						switch ((ColumnTypesEnum)col)
						{
							case ColumnTypesEnum.Customer: data.Customer = item.ToString(); break;
							case ColumnTypesEnum.Name: data.Name = item.ToString(); break;
							default:
								if (col > (int)ColumnTypesEnum.Name &&
									col < columnsList.Count)
								{
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

					data.StatusParameterValueList = stateParamsList;
					motorSettingsList.Add(data);
				}


				reader.Close();

				return motorSettingsList;

			}
			catch(Exception ex) 
			{
				LoggerService.Error(this, "Failed to read the file security parameters", ex);
				return null;
			}
		}

		private List<ParameterValueData> GetStatusParameters(string pathStatus)
		{
			IExcelDataReader reader;
			var stream = File.Open(pathStatus, FileMode.Open, FileAccess.Read);
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

			List<ParameterValueData> statusParameters = new List<ParameterValueData>();

			for (int col = 0; col < dataTable.Columns.Count; col++)
			{
				var item = dataTable.Rows[0][col];
				string colStr = item.ToString();
				statusParameters.Add(new ParameterValueData() 
					{ ParameterName = colStr });
			}

			return statusParameters;
		}
	}
}
