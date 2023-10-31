using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace ScriptHandler.Views
{
	/// <summary>
	/// Interaction logic for ScriptNameView.xaml
	/// </summary>
	public partial class ScriptNameView : Window
	{
		#region Script name

		public static readonly DependencyProperty ScriptNameProperty = DependencyProperty.Register(
			"ScriptName", typeof(string),
			typeof(ScriptNameView));

		public string ScriptName
		{
			get => (string)GetValue(ScriptNameProperty);
			set => SetValue(ScriptNameProperty, value);
		}

		#endregion Script name


		#region SubTitle

		public static readonly DependencyProperty SubTitleProperty = DependencyProperty.Register(
			"SubTitle", typeof(string),
			typeof(ScriptNameView));

		public string SubTitle
		{
			get => (string)GetValue(SubTitleProperty);
			set => SetValue(SubTitleProperty, value);
		}

		#endregion SubTitle


		#region ButtonTitle

		public static readonly DependencyProperty ButtonTitleProperty = DependencyProperty.Register(
			"ButtonTitle", typeof(string),
			typeof(ScriptNameView));

		public string ButtonTitle
		{
			get => (string)GetValue(ButtonTitleProperty);
			set => SetValue(ButtonTitleProperty, value);
		}

		#endregion ButtonTitle




		public ScriptNameView()
		{
			InitializeComponent();
			tb.Focus();
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			if(string.IsNullOrEmpty(tb.Text))
			{
				MessageBox.Show("Empty name is not valid", "Error");
				return;
			}

			DialogResult = true;
			Close();
		}
	}
}
