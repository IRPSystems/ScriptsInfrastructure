using Controls.Views;
using ScriptHandler.Models;
using ScriptHandler.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

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


		#region ScriptName

		public static readonly DependencyProperty ScriptNameProperty = DependencyProperty.Register(
			"ScriptName", typeof(string), typeof(ErrorsListView));

		public string ScriptName
		{
			get => (string)GetValue(ScriptNameProperty);
			set => SetValue(ScriptNameProperty, value);
		}

		#endregion ScriptName
	}
}
