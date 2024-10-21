
using System.Windows;

namespace ScriptHandler.Models
{
	public class ScriptStepPause : ScriptStepBase
	{

		public ScriptStepPause()
		{
			if (Application.Current != null)
				Template = Application.Current.MainWindow.FindResource("PausedTemplate") as DataTemplate;
		}
	}
}
