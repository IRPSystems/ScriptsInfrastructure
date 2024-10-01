
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
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#CA59FF");
            if (item is ScriptNodeSaveParameter || item is ScriptStepSaveParameter)
                return (SolidColorBrush)new BrushConverter().ConvertFrom("#CA45FF");
            if (item is ScriptStepStartStopSaftyOfficer)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#8E66FF");
			if (item is ScriptNodeSetSaveParameter || item is ScriptStepSetSaveParameter)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#FF6A00");
			if (item is ScriptNodeDelay || item is ScriptStepDelay)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#B6FF00");
			if (item is ScriptNodeResetParentSweep || item is ScriptStepResetParentSweep)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#FFE97F");
			if (item is ScriptNodeDynamicControl || item is ScriptStepDynamicControl)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#7FFFFF");
			if (item is ScriptNodeCANMessageUpdate || item is ScriptStepCANMessageUpdate)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#FF7F7F");
			if (item is ScriptNodeCANMessage || item is ScriptStepCANMessage)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#FF006E");
			if (item is ScriptNodeSweep || item is ScriptStepSweep)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#0000FF");
			if (item is ScriptNodeCompareRange || item is ScriptStepCompareRange ||
				item is ScriptNodeCompareWithTolerance || item is ScriptStepCompareWithTolerance)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#FF00DC");
			if (item is ScriptNodeCompare || item is ScriptStepCompare)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#A5FF7F");
			if (item is ScriptNodeNotification || item is ScriptStepNotification)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#4CFF00");
			if (item is ScriptNodeScopeSave || item is ScriptStepScopeSave)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#FF7FB6");
			if (item is ISubScript subScript)
			{
				if (subScript.Script == null)
					return Brushes.Red;
				else
					return (SolidColorBrush)new BrushConverter().ConvertFrom("#00FF90");
			}
			if (item is ScriptNodeIncrementValue || item is ScriptStepIncrementValue)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#0094FF");
            if (item is ScriptNodeLoopIncrement || item is ScriptStepLoopIncrement)
                return (SolidColorBrush)new BrushConverter().ConvertFrom("#2B00FF");
            if (item is ScriptNodeConverge || item is ScriptStepConverge)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#0026FF");
			if (item is ScriptNodeCANMessageStop || item is ScriptStepCANMessageStop)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#FF7FED");
			if (item is ScriptNodeStopContinuous || item is ScriptStepStopContinuous)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#FFB27F");
			if (item is ScriptNodeEOLFlash || item is ScriptStepEOLFlash)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#7FFFC5");
			if (item is ScriptNodeEOLCalibrate || item is ScriptStepEOLCalibrate)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#7FC9FF");
			if (item is ScriptNodeEOLSendSN || item is ScriptStepEOLSendSN)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#6BFFED");
			if (item is ScriptNodeEOLPrint || item is ScriptStepEOLPrint)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#D67FFF");
			if (item is ScriptNodeCompareBit || item is ScriptStepCompareBit)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#BBA3FF");

			return Brushes.Transparent;
		}
	}
}
