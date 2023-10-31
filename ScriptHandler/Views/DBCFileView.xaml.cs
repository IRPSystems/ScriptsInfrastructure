using MahApps.Metro.Controls;

namespace ScriptHandler.Views
{
	/// <summary>
	/// Interaction logic for DBCFileView.xaml
	/// </summary>
	public partial class DBCFileView : MetroWindow
	{
		public DBCFileView()
		{
			InitializeComponent();
		}

		private void OK_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			DialogResult = true;
			Close();
		}

		private void Cancel_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			DialogResult = null;
			Close();
		}

		private void TreeView_SelectedItemChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<object> e)
		{

        }
    }
}
