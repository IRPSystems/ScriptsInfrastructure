
using ScriptHandler.Models;
using System;
using System.IO;

namespace ScriptHandler.Services
{
	public class FixOldScriptsAndProjectsService
	{
		public void Fix(string path)
		{
			string fileData = null;
			using (StreamReader stream = new StreamReader(path))
			{
				fileData = stream.ReadToEnd();
			}

			FixOldScriptsAndProjectsService fixOldScriptsAndProjectsService = new FixOldScriptsAndProjectsService();
			fixOldScriptsAndProjectsService.FixDynamicControl(ref fileData);

			using (StreamWriter sw = new StreamWriter(path))
			{
				sw.Write(fileData);
			}
		}

		public void FixDynamicControl(ref string fileData)
		{

			int index = fileData.IndexOf("DynamicControlData");
			if (index >= 0)
				return;

			index = fileData.IndexOf("DynamicControlFileLine");
			while (index >= 0)
			{
				index = fileData.IndexOf("DynamicControlFileLine", index);
				FixSingleDynamicControl(ref fileData, ref index);
			}

			fileData = fileData.Replace(
				"\"$type\": \"System.Collections.Generic.List`1[[System.Double, System.Private.CoreLib]], System.Private.CoreLib\",",
				"\"$type\": \"System.Collections.ObjectModel.ObservableCollection`1[[ScriptHandler.Models.DynamicControlFileLine+DynamicControlData, ScriptHandler]], System.ObjectModel\",");

			fileData = fileData.Replace(
				"\"$type\": \"System.Collections.Generic.List`1[[System.Double, System.Private.CoreLib]], System.Private.CoreLib\",",
				"\"$type\": \"System.Collections.ObjectModel.ObservableCollection`1[[ScriptHandler.Models.DynamicControlFileLine+DynamicControlData, ScriptHandler]], System.ObjectModel\",");
		}

		private void FixSingleDynamicControl(
			ref string fileData,
			ref int index)
		{
			string valuesStr = null;
			string[] valuesList = null;
			try
			{
				if (index < 0)
					return;

				index = fileData.IndexOf("ValuesList", index);
				if (index < 0)
					return;

				string valuesStartStr = "\"$values\": [\r\n";
				int indexStart = fileData.IndexOf(valuesStartStr, index);
				int indexEnd = fileData.IndexOf("]", indexStart);

				valuesStr = fileData.Substring(indexStart + valuesStartStr.Length, indexEnd - (indexStart + valuesStartStr.Length));
				//fileData = fileData.Remove(indexStart + valuesStartStr.Length, indexEnd - (indexStart + valuesStartStr.Length));

				valuesList = valuesStr.Split("\r\n");
				string newString = "";
				for (int i = 0; i < valuesList.Length; i++)
				{
					string value = valuesList[i];
					value = value.Trim();
					if (string.IsNullOrEmpty(value))
						continue;

					if (value.EndsWith(',') == false)
						value += ',';


					newString += "\t\t\t\t\t\t\t\t\t\t\t\t{\r\n";
					newString += "\t\t\t\t\t\t\t\t\t\t\t\t\t\"$type\": \"ScriptHandler.Models.DynamicControlFileLine+DynamicControlData, ScriptHandler\",\r\n";
					newString += "\t\t\t\t\t\t\t\t\t\t\t\t\t\"Value\": " + value + "\r\n";
					newString += "\t\t\t\t\t\t\t\t\t\t\t\t\t\"IsCurrent\": false\r\n";
					newString += "\t\t\t\t\t\t\t\t\t\t\t\t},\r\n";

				}

				int indexOfLastComma = newString.LastIndexOf(",");
				newString = newString.Remove(indexOfLastComma, 1);
				fileData = fileData.Replace(valuesStr, newString);
				//fileData = fileData.Insert(indexStart + valuesStartStr.Length, newString);

				index = indexStart + valuesStartStr.Length + newString.Length;

			}
			catch (Exception)
			{
				index = -1;
			}
		}

	}
}
