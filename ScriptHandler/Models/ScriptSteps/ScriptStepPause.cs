
using System.Windows;

namespace ScriptHandler.Models
{
	public class ScriptStepPause : ScriptStepBase
	{

		public ScriptStepPause()
		{
			Template = Application.Current.MainWindow.FindResource("PausedTemplate") as DataTemplate;
		}
	}
}
