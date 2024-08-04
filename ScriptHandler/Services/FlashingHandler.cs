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
using DeviceHandler.Models;
using DeviceHandler.Models.DeviceFullDataModels;
using Entities.Enums;
using DeviceHandler.Enums;

namespace ScriptHandler.Services
{
    public class FlashingHandler
    {

		

		#region Properties

        public string ErrorMessage { get; set; }

		#endregion Properties

		#region Fields

		private FlasherService _flasherService;
		
        private AutoResetEvent _bootCommandEvent;

        private string _udsLogPath;

        private bool _isWaitFor_run_btlr;
		private bool _isStopped;

        private DevicesContainer _devicesContainer;

		#endregion Fields

		#region Constructor

		public FlashingHandler(DevicesContainer devicesContainer)
        {
            try
            {  
                _devicesContainer = devicesContainer;

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
                    ErrorMessage = "Please Select FW file";
					LoggerService.Error(this, ErrorMessage);
                    return false;
                }

                if (_flasherService == null)
                {
                    ErrorMessage = "The flashing service was not initialized";
					LoggerService.Error(this, ErrorMessage);
                    return false;
                }


                if (!_flasherService.SelectFlashTool(filePath))
                {
                    return false;
                }

                DeviceFullData mcuDevice = _devicesContainer.TypeToDevicesFullData[DeviceTypesEnum.MCU];
				MCU_Communicator mcuCommunicator = mcuDevice.DeviceCommunicator as MCU_Communicator;
				

				//Check if PCAN connection is required
				if (_flasherService.flashingTool != FlasherService.eFlashingTool.PSoC)
                {

                    if (!mcuCommunicator.IsInitialized)
                    {
                        ErrorMessage = "No communication";
						LoggerService.Error(this, ErrorMessage);
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
                        if (mcuCommunicator.IsInitialized)
							mcuDevice.Disconnect();

                        System.Threading.Thread.Sleep(100);



                        _flasherService.SetFlashingParamsUDS(
                            udsSequence,
                            rx,
                            tx,
							mcuCommunicator.CanService.GetHwId(),
							mcuCommunicator.CanService.Baudrate,
							_udsLogPath);
                        flashStatus = _flasherService.Flash(ref errorMsg);

                        //Reopen can port
                        if (!mcuCommunicator.IsInitialized)
							mcuDevice.Connect();


						break;

                    case FlasherService.eFlashingTool.Cyflash:

						//Boot Command
						mcuCommunicator.GetParamValue(
                            new MCU_ParamData() { Cmd = "run_btlr", Name = "Run boot" },
                            Callback);

						_isWaitFor_run_btlr = true;

						_bootCommandEvent.WaitOne(5000);

						_isWaitFor_run_btlr = false;

                        if (_isStopped)
                        {
                            break;
                        }

						if (mcuCommunicator.IsInitialized)
							mcuDevice.Disconnect();

						System.Threading.Thread.Sleep(500);

						ushort hwId = mcuCommunicator.CanService.GetHwId();
						_flasherService.SetFlashingParamsCyFlash(
								CanPCanService.FormatPortName(hwId),
								mcuCommunicator.CanService.Baudrate);
						flashStatus = _flasherService.Flash(ref errorMsg);

						//Reopen can port
						if (!mcuCommunicator.IsInitialized)
							mcuDevice.Connect();


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
                    ErrorMessage = "Flashing Error: " + errorMsg;
					OnWriteToTerminalEvent?.Invoke(ErrorMessage);
				}

				//_flashingRemainingTime.Reset();
				UploadEndedEvent?.Invoke();

				DateTime start = DateTime.Now;

				while ((DateTime.Now - start).TotalMilliseconds < 10000)
				{
					if (mcuDevice.CheckCommunication.Status == CommunicationStateEnum.Connected)
						break;
				}

				return flashStatus;
            }
            catch (Exception ex)
            {
                ErrorMessage = "Failed to flash";
				LoggerService.Error(this, ErrorMessage, ex);
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

		public event Action<string, int, string> OnUploadProccesEvent;
		public event Action<string> OnWriteToTerminalEvent;
		public event Action UploadEndedEvent;

		#endregion Events
	}
}
