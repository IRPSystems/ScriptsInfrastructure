using Controls.Interfaces;
using ScriptHandler.DesignDiagram.ViewModels;
using Syncfusion.UI.Xaml.Diagram;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
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

namespace ScriptHandler.DesignDiagram.Views
{
	/// <summary>
	/// Interaction logic for DesignDiagramView.xaml
	/// </summary>
	public partial class DesignDiagramView : UserControl, IDocumentV
	{
		public DesignDiagramView()
		{
			InitializeComponent();
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			SelectorViewModel svm = (diagram.SelectedItems as SelectorViewModel);
			if((svm.Commands as QuickCommandCollection).Count > 2)
				(svm.Commands as QuickCommandCollection).RemoveAt(1);
			svm.SelectorConstraints =
				svm.SelectorConstraints & ~SelectorConstraints.Rotator;

			if(DataContext is DesignDiagramViewModel vm)
				vm.SubNodeDeletedEvent += Vm_SubNodeDeletedEvent;
		}

		private void Vm_SubNodeDeletedEvent(NodeViewModel node)
		{
			(diagram.Info as IGraphInfo).Commands.Delete.Execute(node);
		}
	}
}
