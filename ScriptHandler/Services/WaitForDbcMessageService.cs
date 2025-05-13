
using Communication.Services;
using DeviceCommunicators.DBC;
using DeviceCommunicators.MCU;
using DeviceCommunicators.Models;
using System;
using System.Threading;

namespace ScriptHandler.Services
{
	public class WaitForDbcMessageService
	{
		#region Properties and Fields

		private CancellationTokenSource _cancellationTokenSource;
		private CancellationToken _cancellationToken;

		private DBC_ParamData _DBCParamData;

		private AutoResetEvent _waitForMessage;

		private byte[] _messageBuffer;

		#endregion Properties and Fields

		#region Constructor

		public WaitForDbcMessageService() 
		{
			_cancellationTokenSource = new CancellationTokenSource();
			_cancellationToken = _cancellationTokenSource.Token;
		}

		#endregion Constructor

		#region Methods

		public void Stop()
		{
			_cancellationTokenSource.Cancel();
		}

		public bool? Run(
			DeviceParameterData parameter,
			MCU_Communicator communicator)
		{
			if (!(parameter is DBC_ParamData paramData))
				return null;

			if (!(communicator.CommService is CanService canService))
				return false;

			_DBCParamData = paramData;
			_waitForMessage = new AutoResetEvent(false);
			_messageBuffer = null;

			canService.CanMessageReceivedEvent += CanService_CanMessageReceivedEvent;

			int waitResult =
					WaitHandle.WaitAny(
						new WaitHandle[] { _cancellationToken.WaitHandle, _waitForMessage },
						paramData.Interval);

			canService.CanMessageReceivedEvent -= CanService_CanMessageReceivedEvent;
			_cancellationTokenSource.Cancel();

			if (waitResult == WaitHandle.WaitTimeout || _messageBuffer == null)
				return false;

			double value = paramData.GetValue(_messageBuffer);
			paramData.Value = value;

			return true;
		}

		private void CanService_CanMessageReceivedEvent(uint canId, byte[] buffer)
		{
			if (canId != _DBCParamData.ParentMessage.ID)
				return;

			_messageBuffer = buffer;
			_waitForMessage.Set();
		}

		#endregion Methods
	}
}
