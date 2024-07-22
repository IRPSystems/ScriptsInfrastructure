using FlashingToolLib.FlashingTools;
using System;
using Services.Services;
using System.IO;
using DeviceCommunicators.MCU;
using DeviceCommunicators.Models;
using DeviceCommunicators.Enums;
using FlashingToolLib;
using System.Threading;
using Communication.Services;
using DeviceCommunicators.General;

namespace ScriptHandler.Services
{
    public class FlashingHandler
    {

		public event Action<string, int, string> OnUploadProccesEvent;
		public event Action<string> OnWriteToTerminalEvent;
        public event Action UploadEndedEvent;

		#region Fields

		private FlasherService _flasherService;
		
        private AutoResetEvent _bootCommandEvent;

        private string _udsLogPath;

        private bool _isWaitFor_run_btlr;
		private bool _isStopped;

        private MCU_Communicator _mcuCommunicator;

		#endregion Fields

		#region Constructor

		public FlashingHandler(DeviceCommunicator mcuCommunicator)
        {
            try
            {  
                _mcuCommunicator = mcuCommunicator as MCU_Communicator;

                _flasherService = new FlasherService();
				_flasherService.OnUploadProccesEvent += _flasherService_OnUploadProcces;
				_flasherService.OnWriteToTerminalEvent += _flasherService_OnWriteToTerminal;

				
				_bootCommandEvent = new AutoResetEvent(false);

                _udsLogPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                _udsLogPath = Path.Combine(_udsLogPath, "Logs");
                if(Directory.Exists(_udsLogPath) == false)
                    Directory.CreateDirectory(_udsLogPath);
				_udsLogPath = Path.Combine(_udsLogPath, "UDS Logs");
				if (Directory.Exists(_udsLogPath) == false)
					Directory.CreateDirectory(_udsLogPath);

                _isWaitFor_run_btlr = false;
				_isStopped = false;


			}
            catch(Exception ex) 
            {
                LoggerService.Error(this, "Failed to construct", "Error", ex);
            }

		}

		private void _flasherService_OnWriteToTerminal(string message)
		{
			OnWriteToTerminalEvent?.Invoke(message);
		}

		private void _flasherService_OnUploadProcces(string uploadStatus, int uploadPrecent, string remainingTime)
		{
            OnUploadProccesEvent?.Invoke(uploadStatus, uploadPrecent, remainingTime);
		}

		#endregion Constructor

		#region Methods

		public bool Flash(
            string filePath,
			string rxMsgIdStr = "1CFFF9FE",
			string txMsgIdStr = "1CFFFEF9",
            string udsSequenceStr = "generic",
            string securityKey = "")
        {

            try
            {
                // Check if no firmware file was selected
                if (string.IsNullOrEmpty(filePath))
                {
                    LoggerService.Error(this, "Please Select FW file", "Error");
                    return false;
                }

                if (_flasherService == null)
                {
                    LoggerService.Error(this, "The flashing service was not initialized", "Error");
                    return false;
                }


                if (!_flasherService.SelectFlashTool(filePath))
                {
                    return false;
                }


				//Check if PCAN connection is required
				if (_flasherService.flashingTool != FlasherService.eFlashingTool.PSoC)
                {

                    if (!_mcuCommunicator.IsInitialized)
                    {
                        LoggerService.Error(this, "Please connect PCAN device");
                        return false;
                    }
                }

				_isStopped = false;


				_flasherService.FileHandler(filePath);



                

                bool flashStatus = false;

                string errorMsg = "No Error";

                switch (_flasherService.flashingTool)
                {
                    case FlasherService.eFlashingTool.UDS:

                        uint rx;
                        bool result = uint.TryParse(rxMsgIdStr, System.Globalization.NumberStyles.HexNumber, null, out rx);
                        if (!result)
                        {
                            errorMsg = $"Invalid RX: {rxMsgIdStr}";
                            break;
                        }

                        uint tx;
                        result = uint.TryParse(txMsgIdStr, System.Globalization.NumberStyles.HexNumber, null, out tx);
                        if (!result)
                        {
                            errorMsg = $"Invalid TX: {rxMsgIdStr}";
                            break;
                        }

                        UdsSequence udsSequence;
                        result = Enum.TryParse(udsSequenceStr, out udsSequence);
                        if (!result)
                        {
                            errorMsg = $"Invalid UDS sequence: {udsSequence}";
                            break;
                        }

                        //Disconnect from peak to allow cyflash to use peak port
                        if (_mcuCommunicator.IsInitialized)
							_mcuCommunicator.Dispose();

                        System.Threading.Thread.Sleep(100);



                        _flasherService.SetFlashingParamsUDS(
                            udsSequence,
                            rx,
                            tx,
							_mcuCommunicator.CanService.GetHwId(),
							_mcuCommunicator.CanService.Baudrate,
							_udsLogPath);
                        flashStatus = _flasherService.Flash(ref errorMsg);

                        //Reopen can port
                        if (!_mcuCommunicator.IsInitialized)
                            ConnectRequiredEvent?.Invoke();


						break;

                    case FlasherService.eFlashingTool.Cyflash:

						//Boot Command
						_mcuCommunicator.GetParamValue(
                            new MCU_ParamData() { Cmd = "run_btlr", Name = "Run boot" },
                            Callback);

						_isWaitFor_run_btlr = true;

						_bootCommandEvent.WaitOne(5000);

						_isWaitFor_run_btlr = false;

                        if (_isStopped)
                        {
                            break;
                        }

						if (_mcuCommunicator.IsInitialized)
							_mcuCommunicator.Dispose();

						System.Threading.Thread.Sleep(500);

						ushort hwId = _mcuCommunicator.CanService.GetHwId();
						_flasherService.SetFlashingParamsCyFlash(
								CanPCanService.FormatPortName(hwId),
								_mcuCommunicator.CanService.Baudrate);
						flashStatus = _flasherService.Flash(ref errorMsg);

						//Reopen can port
						if (!_mcuCommunicator.IsInitialized)
							ConnectRequiredEvent?.Invoke();


						break;

                    case FlasherService.eFlashingTool.PSoC:

                         _flasherService.SetFlashingParamsPSoC(false); // For TDM don't use chip protection
						flashStatus = _flasherService.Flash(ref errorMsg);
                        break;

                }

				if (flashStatus)
				{
					OnWriteToTerminalEvent?.Invoke("Flashing Finished Successfully");
					OnUploadProccesEvent?.Invoke("100%", 100, "");
				}
				else
				{
					OnWriteToTerminalEvent?.Invoke("Flashing Error: " + errorMsg);
				}

				//_flashingRemainingTime.Reset();
				UploadEndedEvent?.Invoke();


				return flashStatus;
            }
            catch (Exception ex)
            {
                LoggerService.Error(this, "Failed to flash", "Error", ex);
                return false;
            }
        }

        private void Callback(DeviceParameterData param, CommunicatorResultEnum result, string errDescription)
        {
            if (param.Name == "Run boot")
            {
				_bootCommandEvent.Set();
			}
        }

        
				

        public void Stop()
        {
            _isStopped = true;

			// If flashing with CyFlash, we need to wait for run_btlr to end
			if (_isWaitFor_run_btlr)
                return;

            Stop_Do();

		}

		private void Stop_Do()
		{
            if (_flasherService != null) 
			    _flasherService.Stop();
		}




        #endregion Methods

        #region Events

        public event Action ConnectRequiredEvent;

		#endregion Events
	}
}
