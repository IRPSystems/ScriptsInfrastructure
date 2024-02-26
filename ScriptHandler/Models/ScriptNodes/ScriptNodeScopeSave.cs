using DeviceCommunicators.Models;
using DeviceCommunicators.Scope_KeySight;
using DeviceHandler.Models;
using DeviceHandler.Models.DeviceFullDataModels;
using Entities.Enums;
using Entities.Models;
using Newtonsoft.Json;
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
			string jsonStr =
				" {\r\n        \"$type\": \"DeviceCommunicators.Scope_KeySight.Scope_KeySight_ParamData, DeviceCommunicators\",\r\n        \"User_command\": null,\r\n        \"Command\": null,\r\n        \"Status_paramter\": null,\r\n        \"data\": null,\r\n        \"Channel\": 0,\r\n        \"Name\": \"Save\",\r\n        \"Units\": null,\r\n        \"Value\": null,\r\n        \"DropDown\": {\r\n          \"$type\": \"System.Collections.Generic.List`1[[Entities.Models.DropDownParamData, Entities]], System.Private.CoreLib\",\r\n          \"$values\": [\r\n            {\r\n              \"$type\": \"Entities.Models.DropDownParamData, Entities\",\r\n              \"Name\": \"PNG\",\r\n              \"Value\": \"0\"\r\n            },\r\n            {\r\n              \"$type\": \"Entities.Models.DropDownParamData, Entities\",\r\n              \"Name\": \"CSV\",\r\n              \"Value\": \"1\"\r\n            }\r\n          ]\r\n        },\r\n        \"DeviceType\": 4\r\n      }";

			JsonSerializerSettings settings = new JsonSerializerSettings();
			settings.Formatting = Formatting.Indented;
			settings.TypeNameHandling = TypeNameHandling.All;
			Scope_KeySight_ParamData param = JsonConvert.DeserializeObject(jsonStr, settings) as Scope_KeySight_ParamData;
			param.Device = devicesContainer.DevicesList.ToList().Find((d) => d.DeviceType == DeviceTypesEnum.KeySight);

			Parameter = param;
		}


		public override bool IsNotSet(
			DevicesContainer devicesContainer,
			ObservableCollection<InvalidScriptItemData> errorsList)
		{
			if (Parameter == null)
				return true;

			if(string.IsNullOrEmpty(FilePath)) 
				return true;

			return false;
		}


	}
}
