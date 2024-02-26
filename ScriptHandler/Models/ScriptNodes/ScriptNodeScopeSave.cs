using DeviceCommunicators.Models;
using DeviceHandler.Models;
using DeviceHandler.Models.DeviceFullDataModels;
using Entities.Enums;
using Entities.Models;
using ScriptHandler.Interfaces;
using System.Collections.ObjectModel;
using System.Linq;

namespace ScriptHandler.Models.ScriptNodes
{
	public class ScriptNodeScopeSave : ScriptNodeBase, IScriptNodeWithParam
	{
		public DeviceParameterData Parameter { get; set; }

		private double _value;
		public double Value
		{
			get => _value;
			set
			{
				_value = value;
				OnPropertyChanged("Value");
				OnPropertyChanged("Description");
			}
		}

		private int _valueDropDwonIndex;
		public int ValueDropDwonIndex
		{
			get => _valueDropDwonIndex;
			set
			{
				if (!(Parameter is IParamWithDropDown dropDown))
					return;

				if (dropDown.DropDown == null)
					return;

				_valueDropDwonIndex = value;

				if (_valueDropDwonIndex < 0 || _valueDropDwonIndex >= dropDown.DropDown.Count)
					return;

				int iVal;
				bool res = int.TryParse(dropDown.DropDown[_valueDropDwonIndex].Value, out iVal);
				if (res)
					Value = iVal;

				OnPropertyChanged("ValueDropDwonIndex");
				OnPropertyChanged("Description");
			}
		}

		public string FilePath { get; set; }

		public override string Description
		{
			get
			{
				return "Scope Save - ID:" + ID;
			}
		}


		public ScriptNodeScopeSave()
		{
			Name = "Scope Save";

		}

		

		
		public override void PostLoad(
			DevicesContainer devicesContainer,
			IScript currentScript)
		{
			if (devicesContainer.TypeToDevicesFullData.ContainsKey(DeviceTypesEnum.KeySight) == false)
				return;

			DeviceFullData deviceFullData = 
				devicesContainer.TypeToDevicesFullData[DeviceTypesEnum.KeySight];

			Parameter = deviceFullData.Device.ParemetersList.ToList().Find((p) => p.Name == "Save");
		}


		public override bool IsNotSet(
			DevicesContainer devicesContainer,
			ObservableCollection<InvalidScriptItemData> errorsList)
		{
			

			return false;
		}


	}
}
