
using CommunityToolkit.Mvvm.ComponentModel;
using DeviceCommunicators.Models;
using Entities.Enums;
using ScriptHandler.Interfaces;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace ScriptHandler.Models
{


	public class InvalidScriptData: InvalidScriptItemData
	{
		public ObservableCollection<InvalidScriptItemData> ErrorsList { get; set; }
		public IScript Script { get; set; }

		public InvalidScriptData() 
		{
			ErrorsList = new ObservableCollection<InvalidScriptItemData>();
			ErrorsList.CollectionChanged += ErrorsList_CollectionChanged;
		}

		private void ErrorsList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if(e.Action == NotifyCollectionChangedAction.Add)
			{
				ErrorsList[ErrorsList.Count - 1].Parent = this;
			}
		}
	}

	public class InvalidScriptItemData : ObservableObject
	{
		public string ErrorString { get; set; }
		public string Name { get; set; }
		public IScriptItem ScirptItem { get; set; }
		public InvalidScriptData Parent { get; set; }
	}

	public class InvalidScriptItemData_DeviceNotFound: InvalidScriptItemData
	{
		public DeviceTypesEnum DeviceType { get; set; }
	}

	public class InvalidScriptItemData_DataIsNotSet : InvalidScriptItemData
	{
	}

	public class InvalidScriptItemData_ParamDontExist : InvalidScriptItemData
	{
		public DeviceParameterData Parameter { get; set; }
	}
}
