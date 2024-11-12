using ScriptRunner.Services;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ScriptRunner.Views
{
	/// <summary>
	/// Interaction logic for ScriptLoggerView.xaml
	/// </summary>
	public partial class ScriptLoggerView : UserControl
	{
		public ScriptLoggerView()
		{
			InitializeComponent();
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is ScriptLoggerService logger)
			{
				logger.LogLinesList.CollectionChanged += LogLinesList_CollectionChanged;
			}
		}

		private void LogLinesList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			dataGrid.ScrollIntoView(dataGrid.Items[dataGrid.Items.Count - 1]);
			
		}
	}
}
