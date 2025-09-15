
using Controls.ViewModels;
using ScriptHandler.DesignDiagram.Views;
using DeviceHandler.Models;
using DeviceHandler.ViewModel;
using DeviceHandler.Views;
using Syncfusion.Windows.Tools.Controls;
using System.Windows.Controls;

namespace ScriptHandler.DesignDiagram.ViewModels
{
    public class DesignDiagramDockingViewModel: DocumantsDokcingViewModel
	{
		#region Fields

		private ContentControl _parametrs;
		private ContentControl _stencil;
		private ContentControl _nodeProperties;

		#endregion Fields

		public DesignDiagramDockingViewModel(
		DevicesContainer devicesContainer,
			StencilViewModel stencil,
			NodePropertiesViewModel nodeDataView) :
			base("DesignDiagram", "Evva")
		{
			CreateWindows(devicesContainer, stencil, nodeDataView);
		}

		private void CreateWindows(
			DevicesContainer devicesContainer,
			StencilViewModel stencil,
			NodePropertiesViewModel nodeDataView)
		{
			ParametersView parametersView = new ParametersView()
			{
				DataContext = new ParametersViewModel(
					new Entities.Models.DragDropData(),
					devicesContainer,
					false)
			};
			CreateWindow(
				parametersView,
				"Parameters",
				"Parameters",
				DockSide.Right,
				out _parametrs);
			SetCanClose(_parametrs, false);
			SetDesiredWidthInDockedMode(_parametrs, 600);

			NodePropertiesView nodePropertiesView = new NodePropertiesView()
			{ DataContext = nodeDataView };
			CreateWindow(
				nodePropertiesView,
				"Properties",
				"Properties",
				DockSide.Bottom,
				out _nodeProperties);
			SetCanClose(_nodeProperties, false);
			SetTargetName(_nodeProperties, "Parameters", DockState.Dock);

			StencilView stencilView = new StencilView()
			{ DataContext = stencil };
			CreateWindow(
			stencilView,
				"Tools",
				"Tools",
				DockSide.Left,
				out _stencil);
			SetCanClose(_stencil, false);
			SetDesiredWidthInDockedMode(_stencil, 300);
		}
	}
}
