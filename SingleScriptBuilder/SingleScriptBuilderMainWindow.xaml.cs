using MahApps.Metro.Controls;

namespace SingleScriptBuilder
{
	/// <summary>
	/// Interaction logic for SingleScriptBuilderMainWindow.xaml
	/// </summary>
	public partial class SingleScriptBuilderMainWindow : MetroWindow
	{
		public SingleScriptBuilderMainWindow()
		{
			InitializeComponent();
			DataContext = new SingleScriptBuilderMainWindowViewModel();
		}
	}
}
