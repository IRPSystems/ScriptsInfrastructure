﻿using CommunityToolkit.Mvvm.Input;
using DBCFileParser.Model;
using DBCFileParser.Services;
using DeviceHandler.Models;
using Entities.Models;
using Microsoft.Win32;
using Newtonsoft.Json;
using ScriptHandler.Enums;
using ScriptHandler.Interfaces;
using ScriptHandler.ViewModel;
using ScriptHandler.Views;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace ScriptHandler.Models.ScriptNodes
{
	public class ScriptNodeCANMessage : ScriptNodeBase
	{
		#region Properties

		

		public uint CANID { get; set; }

		[Obsolete]
		[JsonIgnore]
		public IScriptItem ReplacedCanMessage { get; set; }

		[Obsolete]
		[JsonIgnore]
		public int ReplacedCanMessageId { get; set; }


		#region Repeat

		public bool IsOneTime { get; set; }
		public bool IsCyclic { get; set; }

		public int Interval { get; set; }
		public TimeUnitsEnum IntervalUnite { get; set; }

		public bool IsStopByTime { get; set; }
		public bool IsStopByInterations { get; set; }
		public bool IsStopNever { get; set; }

		//public int RepeatTimeLength { get; set; }
		public int Iterations { get; set; }


		public int RepeateLengthTime { get; set; }
		public TimeUnitsEnum RepeateLengthTimeUnite { get; set; }

		public int RepeateLengthIterations { get; set; }

		#endregion Repeat



		public bool IsDBCFile { get; set; }
		public bool IsFreeStyle { get; set; }

		private string _dbcFilePath;
		public string DBCFilePath 
		{
			get => _dbcFilePath;
			set
			{
				_dbcFilePath = value;
				OnPropertyChanged(nameof(DBCFilePath));
			}
		}


		public BitwiseNumberDisplayData Payload { get; set; }

		public Message Message { get; set; }

		public bool IsHex { get; set; }
		public bool IsBinary { get; set; }

		public string FreeStyleIntputGroupName
		{
			get => "FreeStyleIntput " + Description;
		}

		public string IsCyclicGroupName
		{
			get => "IsCyclic " + Description;
		}

		public string PayloadTypeGroupName
		{
			get => "PayloadType " + Description;
		}

		public string RepeatLengthTypeGroupName
		{
			get => "RepeatLengthType " + Description;
		}



		public int IDInProject { get; set; }


		public override string Description
		{
			get
			{
				return "CAN Message - " + "0x" + CANID.ToString("X") + "-0x" + Payload.NumericValue.ToString("X") + " - ID:" + ID;
			}
		}

		#endregion Properties

		#region Fields

		
		#endregion Fields


		#region Constructor

		public ScriptNodeCANMessage()
		{
			Name = "CAN Message";

			SetCommands();

			Payload = new BitwiseNumberDisplayData(is64Bit: true);
			Payload.PropertyChanged += Payload_PropertyChangedEventHandler;

			IsHex = true;
			IsOneTime = true;
			IsDBCFile = true;
			IsStopByTime = true;

			IntervalUnite = TimeUnitsEnum.ms;
			RepeateLengthTimeUnite = TimeUnitsEnum.sec;


		}

		#endregion Constructor

		#region Methods

		

		#region DBC file

		private void DBCFilePathOpen()
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "DBC Files | *.dbc";
			bool? result = openFileDialog.ShowDialog();
			if (result != true)
				return;

			DBCFilePath = openFileDialog.FileName;
		}

		private void DBCFileLoad()
		{
			var dbc = Parser.ParseFromPath(DBCFilePath);
			if(dbc == null) 
				return;

			DBCFileViewModel vm = new DBCFileViewModel(dbc, "DBC - " + DBCFilePath);
			DBCFileView view = new DBCFileView() { DataContext = vm };
			bool? result = view.ShowDialog();

			if (result != true || vm.SelectedMessage == null) 
				return;

			Message selectedMessage = vm.SelectedMessage;
			ulong message = 0;

			foreach(Signal signal in selectedMessage.Signals)
			{
				double dVal = signal.Value;
				dVal += signal.Offset;
				dVal /= signal.Factor;

				message += (ulong)dVal << signal.StartBit;
			}

			Payload.NumericValue = message;
			Message = selectedMessage;

			CANID = Message.ID;
		}

		#endregion DBC file



		private void DeleteReplacedCanMessage()
		{
		}

		public override object Clone()
		{
			ScriptNodeCANMessage canMessage = MemberwiseClone() as ScriptNodeCANMessage;
			if (Payload != null)
			{
				canMessage.Payload = new BitwiseNumberDisplayData(is64Bit: true);
				canMessage.Payload.NumericValue = Payload.NumericValue;
			}

			canMessage.SetCommands();


			return canMessage;
		}

		private void SetCommands()
		{
			DBCFilePathOpenCommand = new RelayCommand(DBCFilePathOpen);
			DBCFileLoadCommand = new RelayCommand(DBCFileLoad);
			DeleteReplacedCanMessageCommand = new RelayCommand(DeleteReplacedCanMessage);
		}

		protected virtual void Payload_PropertyChangedEventHandler(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "BinaryValue" || e.PropertyName == "HexValue")
			{
				OnPropertyChanged(nameof(Payload));
				OnPropertyChanged(nameof(Description));
			}
		}




		public override bool IsNotSet(
			DevicesContainer devicesContainer,
			ObservableCollection<InvalidScriptItemData> errorsList)
		{
			if (CANID == 0)
				return true;

			if(IsCyclic)
			{
				if (Interval <= 0)
					return true;
			}

			return false;
		}

		#endregion Methods

		#region Commands

		public RelayCommand DBCFilePathOpenCommand { get; private set; }
		public RelayCommand DBCFileLoadCommand { get; private set; }


		public RelayCommand DeleteReplacedCanMessageCommand { get; private set; }

		#endregion Commands
	}
}
