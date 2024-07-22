
using DeviceCommunicators.General;
using DeviceHandler.Models;
using FlashingToolLib.FlashingTools;
using Newtonsoft.Json;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Services;
using System.Collections.Generic;
using System.IO;

namespace ScriptHandler.Models.ScriptSteps
{
	public class ScriptStepEOLFlash: ScriptStepBase
	{
		#region Properties and Fields

		public string FilePath { get; set; }
		public string RXId { get; set; }
		public string TXId { get; set; }
		public UdsSequence UdsSequence { get; set; }

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

		#region Methods

		public override void Execute()
		{
			FlashingHandler.OnUploadProccesEvent += FlashingHandler_OnUploadProccesEvent;
			FlashingHandler.OnWriteToTerminalEvent += FlashingHandler_OnWriteToTerminalEvent;
			FlashingHandler.UploadEndedEvent += FlashingHandler_UploadEndedEvent;

			string extension = Path.GetExtension(FilePath);

			FlashingHandler.Flash(FilePath, RXId, TXId, UdsSequence.ToString());

			FlashingHandler.OnUploadProccesEvent -= FlashingHandler_OnUploadProccesEvent;
			FlashingHandler.OnWriteToTerminalEvent -= FlashingHandler_OnWriteToTerminalEvent;
			FlashingHandler.UploadEndedEvent -= FlashingHandler_UploadEndedEvent;
		}

		private void FlashingHandler_UploadEndedEvent()
		{
			
		}

		private void FlashingHandler_OnWriteToTerminalEvent(string message)
		{
			throw new System.NotImplementedException();
		}

		private void FlashingHandler_OnUploadProccesEvent(string uploadStatus, int uploadPrecent, string remainingTime)
		{
			UploadStatus = uploadStatus;
			UploadPrecent = uploadPrecent;
			RemainingTime = remainingTime;
		}

		protected override void Stop()
		{
			FlashingHandler.Stop();
		}

		public override void Generate(
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
		}

		#endregion Methods
	}
}
