
using ScriptHandler.Models;
using System.Windows;
using System.Windows.Controls;

namespace ScriptHandler.Selectors
{
	public class ExplorerItemTemplateSelector : DataTemplateSelector
	{
		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			FrameworkElement element = container as FrameworkElement;

			if(item is ScriptData script)
			{
				if (script.Name.EndsWith(" - Unloaded"))
					return element.FindResource("UnloadedScriptTemplate") as DataTemplate;
				
			}

			if(item is TestData)
				return element.FindResource("TestTemplate") as DataTemplate;
			if (item is ScriptData)
				return element.FindResource("ScriptTemplate") as DataTemplate;

			return null;
		}
	}
}
