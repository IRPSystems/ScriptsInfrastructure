
using ScriptHandler.Models;
using System.Collections.Generic;
using System.Windows;

namespace ScriptHandler.Services
{
	public class ScriptValidationService
	{
		public bool Validate(ScriptData script)
		{

			return true;
			//string errorDescription = "";

			//List<string> checkedNames = new List<string>();
			//foreach(ScriptNodeBase node in script.ScriptItemsList)
			//{
			//	if (checkedNames.Contains(node.Name))
			//		continue;

			//	checkedNames.Add(node.Name);

			//	int counter = 0;
			//	foreach (ScriptNodeBase node1 in script.ScriptItemsList)
			//	{
			//		if (node == node1)
			//			continue;

			//		if (node1.Description == node.Description)
			//			counter++;
			//	}

			//	if(counter > 0)
			//	{
			//		errorDescription += "-- The node name \"" + node.Description + "\" exist " + (counter + 1) + ". The name must be unique.\r\n";
			//	}
			//}

			//if (string.IsNullOrEmpty(errorDescription))
			//{
			//	return true;
			//}

			//MessageBox.Show(errorDescription, "Script Validation Errors");
			//return false;
		}
	}
}
