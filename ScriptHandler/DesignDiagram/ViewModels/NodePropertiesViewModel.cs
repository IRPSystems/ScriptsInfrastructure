
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeviceCommunicators.Models;
using DeviceHandler.ViewModel;
using ScriptHandler.Interfaces;
using ScriptHandler.Models;
using ScriptHandler.Models.ScriptNodes;
using Services.Services;
using Syncfusion.UI.Xaml.Diagram;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace ScriptHandler.DesignDiagram.ViewModels
{
    public class NodePropertiesViewModel: ObservableObject
    {
		#region Properties

		public ScriptNodeBase Node { get; set; }

		#endregion Properties


		#region Constructor

		public NodePropertiesViewModel()
		{

		}

		#endregion Constructor


		#region Methods

		#region Drop

		private void ListScript_Drop(DragEventArgs e)
		{
			LoggerService.Inforamtion(this, "Object is dropped");

			try
			{

				if (Node == null)
					return;

				if (e.Data.GetDataPresent(ParametersViewModel.DragDropFormat))
				{
					DropOfParameter(e);
				}

			}
			catch (Exception ex)
			{
				LoggerService.Error(this, "Failed to drop", "Design Error", ex);
			}

		}

		private void DropOfParameter(DragEventArgs e)
		{
			string tbName = "";
			TextBlock textBlock = null;
			TextBox textBox = null;
			if (e.OriginalSource is TextBlock)
			{
				textBlock = e.OriginalSource as TextBlock;
				tbName = textBlock.Name;
				if (!tbName.StartsWith("tbParam"))
					return;
			}
			else
			{
				textBox =
					FindAncestorService.FindAncestor<TextBox>((DependencyObject)e.OriginalSource);
				if (textBox != null)
				{
					tbName = textBox.Name;
					if (!tbName.StartsWith("tbParam"))
						return;
				}
			}

			//ContentControl contentControl =
			//	FindAncestorService.FindAncestor<ContentControl>((DependencyObject)e.OriginalSource);
			//if (contentControl == null)
			//	return;

			var data = e.Data.GetData(ParametersViewModel.DragDropFormat);
			DeviceParameterData param = null;
			if (data is DeviceParameterData)
				param = data as DeviceParameterData;
			else if (data is System.Collections.IList list)
			{
				foreach (object obj in list)
				{
					param = obj as DeviceParameterData;

					if (Node is ScriptNodeCompare compare)
					{
						if (tbName.EndsWith("CompareValue"))
							compare.CompareValue = param;
						else
							compare.Parameter = param;
					}
					else if (Node is ScriptNodeCompareRange compareRange)
					{
						if (tbName.EndsWith("CompareValue"))
							compareRange.CompareValue = param;
						else if (tbName.EndsWith("RightValue"))
							compareRange.ValueRight = param;
						else
							compareRange.Parameter = param;
					}
					else if (Node is ScriptNodeCompareWithTolerance compareWithTolerance)
					{
						if (tbName.EndsWith("CompareValue"))
							compareWithTolerance.CompareValue = param;
						else
							compareWithTolerance.Parameter = param;
					}
					else if (Node is ScriptNodeConverge converge)
					{
						if (tbName.EndsWith("TargetValue"))
							converge.TargetValue = param;
						else
							converge.Parameter = param;
					}
					else if (Node is ScriptNodeDynamicControl dynamicControl)
					{
						if (textBlock != null && textBlock.DataContext is DynamicControlColumnData columnData)
						{
							columnData.Parameter = param;
						}
					}
					else if (Node is IScriptStepWithParameter withParam)
					{
						if (Node is ScriptNodeSetParameter setParameter &&
							tbName == "tbParamValue")
						{
							setParameter.ValueParameter = param;
						}
						else
							withParam.Parameter = param;
					}
					else if (Node is ScriptNodeEOLCalibrate calibrate)
					{
						if (tbName == "tbParamGain")
						{
							calibrate.GainParam = param;
						}
						else if (tbName == "tbParamMCU")
						{
							calibrate.McuParam = param;
						}
						else if (tbName == "tbParamRefSensor")
						{
							calibrate.RefSensorParam = param;
						}
					}
				}
			}
		}

		#endregion Drop


		private void SweepItemsList_PreviewDrop(DragEventArgs e)
		{
			if (e.Data.GetDataPresent(ParametersViewModel.DragDropFormat) == false)
				return;

			e.Effects = DragDropEffects.None;
			e.Handled = true;

			FrameworkElement frameworkElement = null;
			if (e.OriginalSource is TextBlock textBlock)
				frameworkElement = textBlock;
			else
			{
				TextBox textBox =
					FindAncestorService.FindAncestor<TextBox>((DependencyObject)e.OriginalSource);
				frameworkElement = textBox;
			}

			if (frameworkElement == null)
				return;

			if (!(frameworkElement.DataContext is SweepItemData sweepItem))
				return;

			var data = e.Data.GetData(ParametersViewModel.DragDropFormat);
			DeviceParameterData param = null;
			if (data is DeviceParameterData)
				param = data as DeviceParameterData;
			else if (data is System.Collections.IList list)
				param = list[0] as DeviceParameterData;

			if (frameworkElement.Name == "tbParam")
				sweepItem.Parameter = param;
			else if (frameworkElement.Name == "tbParamStart")
				sweepItem.StartValue = param;
			else if (frameworkElement.Name == "tbParamEnd")
				sweepItem.EndValue = param;
			else if (frameworkElement.Name == "tbParamStep")
				sweepItem.StepValue = param;

		}

		private void TextBox_PreviewDragOver(DragEventArgs e)
		{
			e.Handled = true;
		}

		#endregion Methods

		#region Commands

		#region Drop

		private RelayCommand<DragEventArgs> _ListScript_DropCommand;
		public RelayCommand<DragEventArgs> ListScript_DropCommand
		{
			get
			{
				return _ListScript_DropCommand ?? (_ListScript_DropCommand =
					new RelayCommand<DragEventArgs>(ListScript_Drop));
			}
		}

		#endregion Drop

		private RelayCommand<DragEventArgs> _SweepItemsList_PreviewDropCommand;
		public RelayCommand<DragEventArgs> SweepItemsList_PreviewDropCommand
		{
			get
			{
				return _SweepItemsList_PreviewDropCommand ?? (_SweepItemsList_PreviewDropCommand =
					new RelayCommand<DragEventArgs>(SweepItemsList_PreviewDrop));
			}
		}

		private RelayCommand<DragEventArgs> _TextBox_PreviewDragOverCommand;
		public RelayCommand<DragEventArgs> TextBox_PreviewDragOverCommand
		{
			get
			{
				return _TextBox_PreviewDragOverCommand ?? (_TextBox_PreviewDragOverCommand =
					new RelayCommand<DragEventArgs>(TextBox_PreviewDragOver));
			}
		}

		#endregion Commands
	}
}
