
using CommunityToolkit.Mvvm.ComponentModel;
using DeviceCommunicators.MCU;
using DeviceCommunicators.Models;
using DeviceHandler.Models;
using Entities.Enums;
using Newtonsoft.Json;
using ScriptHandler.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace ScriptHandler.Models
{
	public class ScriptNodeBase: ObservableObject, IScriptItem, ICloneable
	{
		#region Properties and Fields

		public string UserTitle { get; set; }

		public string Name { get; set; }
		[JsonIgnore]
		public virtual string Description { get; set; }

		public bool IsPass { get; set; }

		private IScriptItem _passNext;
		[JsonIgnore]
		public IScriptItem PassNext 
		{
			get => _passNext;
			set
			{
				_passNext = value;
				if (_passNext != null)
					PassNextId = _passNext.ID;
				else
					PassNextId = -1;

				OnPropertyChanged(nameof(PassNextId));
			}
		}

		private int _passNextId;
		public int PassNextId 
		{
			get
			{
				if (PassNext == null)
					return _passNextId;
				else
				{
					_passNextId = PassNext.ID;
					return PassNext.ID;
				}

			}
			set
			{
				_passNextId = value;
				OnPropertyChanged(nameof(PassNextId));
			}
		}


		private IScriptItem _failNext;
		[JsonIgnore]
		public IScriptItem FailNext
		{
			get => _failNext;
			set
			{
				_failNext = value;
				if (_failNext != null)
					FailNextId = _failNext.ID;
				else
					FailNextId = -1;

				OnPropertyChanged(nameof(FailNextId));
			}
		}

		private int _failNextId;
		public int FailNextId
		{
			get
			{

				if (FailNext == null)
					return _failNextId;
				else
				{
					_failNextId = FailNext.ID;
					return FailNext.ID;
				}

			}
			set
			{
				_failNextId = value;
				OnPropertyChanged(nameof(FailNextId));
			}
		}


		[JsonIgnore]
		public bool IsSelected { get; set; }
		[JsonIgnore]
		public bool IsExpanded { get; set; }

		private int _ID;
		public int ID 
		{
			get => _ID;
			set
			{
				_ID = value;
				OnPropertyChanged("ID");
				OnPropertyChanged("Description");
			}
		}

		public EOLReportsSelectionData EOLReportsSelectionData { get; set; }

		#endregion Properties and Fields


		#region Constructor

		public ScriptNodeBase()
		{
			EOLReportsSelectionData = new EOLReportsSelectionData();
		}

		#endregion Constructor

		#region Methods

		public virtual object Clone()
		{
			return MemberwiseClone();
		}

		
		public override string ToString()
		{
			return Description;
		}

		public virtual void PostLoad(
			DevicesContainer devicesContainer,
			IScript currentScript)
		{

		}

		

		protected DeviceParameterData GetParameter(
			DeviceTypesEnum deviceType,
			DeviceParameterData param,
			DevicesContainer devicesContainer)
		{
			//if (devicesContainer.TypeToDevicesFullData.ContainsKey(deviceType) == false )
			//	return null;

			DeviceData deviceData =
				devicesContainer.GetDeviceData(param);

			string name = param.Name;
			if(param is MCU_ParamData mcuParam)
				name = mcuParam.Cmd;

			DeviceParameterData data = null;
			if(deviceType == DeviceTypesEnum.MCU) 
			{
				data = deviceData.ParemetersList.ToList().Find((p) => ((MCU_ParamData)p).Cmd == name);
			}
			else
				data = deviceData.ParemetersList.ToList().Find((p) => p.Name == name);

			return data;
		}

		public virtual bool IsNotSet(
			DevicesContainer devicesContainer,
			ObservableCollection<InvalidScriptItemData> errorsList)
		{
			return false;
		}

		#endregion Methods

		#region Events

		//public event PropertyChangedEventHandler PropertyChanged;
		//public event Action<ScriptNodeBase, string> NodePropertyChangeEvent;

		#endregion Events

	}
}
