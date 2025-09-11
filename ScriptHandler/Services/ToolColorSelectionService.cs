
using Newtonsoft.Json.Linq;
using ScriptHandler.Interfaces;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Models.ScriptSteps;
using ScriptHandler.Models;
using System.Windows.Media;

namespace ScriptHandler.Services
{
	public class ToolColorSelectionService
	{
		public static Brush SelectColor(IScriptItem item)
		{
			if (item is ScriptNodeSetParameter || item is ScriptStepSetParameter)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#A02231");
            if (item is ScriptNodeSaveParameter || item is ScriptStepSaveParameter)
                return (SolidColorBrush)new BrushConverter().ConvertFrom("#CE1825");
            if (item is ScriptStepStartStopSaftyOfficer)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#BA112F");
			if (item is ScriptStepStartStopRecording)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#F6403F");
			if (item is ScriptNodeSetSaveParameter || item is ScriptStepSetSaveParameter)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#B8303C");
			if (item is ScriptNodeDelay || item is ScriptStepDelay)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#AF1D34");
			if (item is ScriptNodeResetParentSweep || item is ScriptStepResetParentSweep)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#FB9F6B");
			if (item is ScriptNodeDynamicControl || item is ScriptStepDynamicControl)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#F6663A");
			if (item is ScriptNodeCANMessageUpdate || item is ScriptStepCANMessageUpdate)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#FD4E1A");
			if (item is ScriptNodeCANMessage || item is ScriptStepCANMessage)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#FB8045");
			if (item is ScriptNodeSweep || item is ScriptStepSweep)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#FD6E2B");
			if (item is ScriptNodeCompareRange || item is ScriptStepCompareRange)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#FD8845");
			if(item is ScriptNodeCompareWithTolerance || item is ScriptStepCompareWithTolerance)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#F3E880");
			if (item is ScriptNodeCompare || item is ScriptStepCompare)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#F5B52E");
			if (item is ScriptNodeNotification || item is ScriptStepNotification)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#F3DD57");
			if (item is ScriptNodeScopeSave || item is ScriptStepScopeSave)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#FEE82D");
			if (item is ISubScript subScript)
			{
				if (subScript.Script == null)
					return Brushes.Red;
				else
					return (SolidColorBrush)new BrushConverter().ConvertFrom("#EDEDB8");
			}
			if (item is ScriptNodeIncrementValue || item is ScriptStepIncrementValue)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#FED32A");
            if (item is ScriptNodeLoopIncrement || item is ScriptStepLoopIncrement)
                return (SolidColorBrush)new BrushConverter().ConvertFrom("#0A733E");
            if (item is ScriptNodeConverge || item is ScriptStepConverge)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#7C8343");
			if (item is ScriptNodeCANMessageStop || item is ScriptStepCANMessageStop)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#7C8343");
			if (item is ScriptNodeStopContinuous || item is ScriptStepStopContinuous)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#BADE41");
			if (item is ScriptNodeEOLFlash || item is ScriptStepEOLFlash)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#87A971");
			if (item is ScriptNodeEOLCalibrate || item is ScriptStepEOLCalibrate)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#486B32");
			if (item is ScriptNodeEOLSendSN || item is ScriptStepEOLSendSN)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#486B32");
			if (item is ScriptNodeEOLPrint || item is ScriptStepEOLPrint)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#CCD88C");
			if (item is ScriptNodeCompareBit || item is ScriptStepCompareBit)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#8AB8D3");
            if (item is ScriptNodeGetRegisterValues || item is ScriptStepGetRegisterValues)
                return (SolidColorBrush)new BrushConverter().ConvertFrom("#1492BA");

            return Brushes.Transparent;
		}
	}
}
