
using DBCFileParser.Model;
using DeviceCommunicators.General;
using DeviceHandler.Models;
using FlashingToolLib.FlashingTools;
using Newtonsoft.Json;
using ScriptHandler.Interfaces;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace ScriptHandler.Models.ScriptSteps
{
	public class ScriptStepEOLFlash: ScriptStepBase
	{
		#region Properties and Fields

		public string FilePath { get; set; }
		public string RXId { get; set; }
		public string TXId { get; set; }
		public UdsSequence UdsSequence { get; set; }


		public int NumOfFlashFile { get; set; }

		public bool IsEolSource { get; set; }
		public bool IsToolSource { get; set; }

		[JsonIgnore]
		public FlashingHandler FlashingHandler { get; set; }
		


		[JsonIgnore]
		public string UploadStatus { get; set; }
		[JsonIgnore]
		public int UploadPrecent {  get; set; }
		[JsonIgnore]
		public string RemainingTime { get; set; }
		[JsonIgnore]
		public string ProgressMessage { get; set; }

		#endregion Properties and Fields

		#region Constructor

		public ScriptStepEOLFlash()
		{
			Template = Application.Current.MainWindow.FindResource("EOLFlashTemplate") as DataTemplate;
		}

		#endregion Constructor

		#region Methods

		public override void Execute()
		{
			FlashingHandler.OnUploadProccesEvent += FlashingHandler_OnUploadProccesEvent;
			FlashingHandler.OnWriteToTerminalEvent += FlashingHandler_OnWriteToTerminalEvent;
			FlashingHandler.UploadEndedEvent += FlashingHandler_UploadEndedEvent;

			
			UploadStatus = "";
			UploadPrecent = 0;
			RemainingTime = "";
			ProgressMessage = "";

			Application.Current.Dispatcher.Invoke(() =>
			{
				Mouse.OverrideCursor = Cursors.AppStarting;
			});
			

			IsPass = FlashingHandler.Flash(FilePath, RXId, TXId, UdsSequence.ToString());
			ErrorMessage = FlashingHandler.ErrorMessage;

			Application.Current.Dispatcher.Invoke(() =>
			{
				Mouse.OverrideCursor = null;
			});

			FlashingHandler.OnUploadProccesEvent -= FlashingHandler_OnUploadProccesEvent;
			FlashingHandler.OnWriteToTerminalEvent -= FlashingHandler_OnWriteToTerminalEvent;
			FlashingHandler.UploadEndedEvent -= FlashingHandler_UploadEndedEvent;

			AddToEOLSummary();
		}

		private void FlashingHandler_UploadEndedEvent()
		{
			
		}

		private void FlashingHandler_OnWriteToTerminalEvent(string message)
		{
			if (Application.Current == null)
				return;

			Application.Current.Dispatcher.Invoke(() =>
			{
				ProgressMessage = message;
			});
		}

		private void FlashingHandler_OnUploadProccesEvent(string uploadStatus, int uploadPrecent, string remainingTime)
		{
			if (Application.Current == null)
				return;

			Application.Current.Dispatcher.Invoke(() =>
			{
				UploadStatus = uploadStatus;
				UploadPrecent = uploadPrecent;
				RemainingTime = remainingTime;
			});
		}

		protected override void Stop()
		{
			FlashingHandler.Stop();
		}

		protected override void Generate(
			ScriptNodeBase sourceNode,
			Dictionary<int, ScriptStepBase> stepNameToObject,
			ref List<DeviceCommunicator> usedCommunicatorsList,
			GenerateProjectService generateService,
			DevicesContainer devicesContainer)
		{
			FilePath = (sourceNode as ScriptNodeEOLFlash).FlashFilePath;
			RXId = (sourceNode as ScriptNodeEOLFlash).RXId;
			TXId = (sourceNode as ScriptNodeEOLFlash).TXId;
			UdsSequence = (sourceNode as ScriptNodeEOLFlash).UdsSequence;

			NumOfFlashFile = (sourceNode as ScriptNodeEOLFlash).NumOfFlashFile;

			IsEolSource = (sourceNode as ScriptNodeEOLFlash).IsEolSource;
			IsToolSource = (sourceNode as ScriptNodeEOLFlash).IsToolSource;
		}

		public override bool IsNotSet(
			DevicesContainer devicesContainer,
			ObservableCollection<InvalidScriptItemData> errorsList)
		{
			if (string.IsNullOrEmpty(FilePath))
				return true;

			return false;
		}

		#endregion Methods
	}
}
