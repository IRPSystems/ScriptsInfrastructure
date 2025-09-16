
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
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#F4EEBE");
            if (item is ScriptNodeSaveParameter || item is ScriptStepSaveParameter)
                return (SolidColorBrush)new BrushConverter().ConvertFrom("#F1CBAC");
            if (item is ScriptStepStartStopSaftyOfficer)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#EDB5AB");
			if (item is ScriptStepStartStopRecording)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#ECB8C8");
			if (item is ScriptNodeSetSaveParameter || item is ScriptStepSetSaveParameter)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#D0AFCF");
			if (item is ScriptNodeDelay || item is ScriptStepDelay)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#AAA5CC");
			if (item is ScriptNodeResetParentSweep || item is ScriptStepResetParentSweep)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#A8C0DE");
			if (item is ScriptNodeDynamicControl || item is ScriptStepDynamicControl)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#A7D5E4");
			if (item is ScriptNodeCANMessageUpdate || item is ScriptStepCANMessageUpdate)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#ACD4C2");
			if (item is ScriptNodeCANMessage || item is ScriptStepCANMessage)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#CADEBA");
			if (item is ScriptNodeSweep || item is ScriptStepSweep)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#F4EAB9");
			if (item is ScriptNodeCompareRange || item is ScriptStepCompareRange)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#F0C5A9");
			if(item is ScriptNodeCompareWithTolerance || item is ScriptStepCompareWithTolerance)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#ECB7AE");
			if (item is ScriptNodeCompare || item is ScriptStepCompare)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#ECB8CC");
			if (item is ScriptNodeNotification || item is ScriptStepNotification)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#CAAFCD");
			if (item is ScriptNodeScopeSave || item is ScriptStepScopeSave)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#A8AACF");
			if (item is ISubScript subScript)
			{
				if (subScript.Script == null)
					return Brushes.Red;
				else
					return (SolidColorBrush)new BrushConverter().ConvertFrom("#A8C6E2");
			}
			if (item is ScriptNodeIncrementValue || item is ScriptStepIncrementValue)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#A8D6DD");
            if (item is ScriptNodeLoopIncrement || item is ScriptStepLoopIncrement)
                return (SolidColorBrush)new BrushConverter().ConvertFrom("#ABD4BE");
            if (item is ScriptNodeConverge || item is ScriptStepConverge)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#D1E1BB");
			if (item is ScriptNodeCANMessageStop || item is ScriptStepCANMessageStop)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#F2E4B7");
			if (item is ScriptNodeStopContinuous || item is ScriptStepStopContinuous)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#EFC1A6");
			if (item is ScriptNodeEOLFlash || item is ScriptStepEOLFlash)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#EDB7B5");
			if (item is ScriptNodeEOLCalibrate || item is ScriptStepEOLCalibrate)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#ECB7CF");
			if (item is ScriptNodeEOLSendSN || item is ScriptStepEOLSendSN)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#C5ADCE");
			if (item is ScriptNodeEOLPrint || item is ScriptStepEOLPrint)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#A9AFD2");
			if (item is ScriptNodeCompareBit || item is ScriptStepCompareBit)
				return (SolidColorBrush)new BrushConverter().ConvertFrom("#A6CDE6");
            if (item is ScriptNodeGetRegisterValues || item is ScriptStepGetRegisterValues)
                return (SolidColorBrush)new BrushConverter().ConvertFrom("#A9D6D6");

            return Brushes.Transparent;
		}
	}
}
