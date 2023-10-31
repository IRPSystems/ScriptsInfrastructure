using Controls.Views;
using ScriptHandler.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace ScriptHandler.Views
{
	/// <summary>
	/// Interaction logic for ErrorsListView.xaml
	/// </summary>
	public partial class ErrorsListView : UserControl
	{
		public ErrorsListView()
		{
			InitializeComponent();
		}

		#region ErrorsList

		public static readonly DependencyProperty ErrorsListProperty = DependencyProperty.Register(
			"ErrorsList", typeof(ObservableCollection<InvalidScriptItemData>), typeof(ErrorsListView));

		public ObservableCollection<InvalidScriptItemData> ErrorsList
		{
			get => (ObservableCollection<InvalidScriptItemData>)GetValue(ErrorsListProperty);
			set => SetValue(ErrorsListProperty, value);
		}

		#endregion ErrorsList
	}
}
