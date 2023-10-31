

using ControlzEx.Standard;
using DeviceHandler.Models;
using Newtonsoft.Json;
using ScriptHandler.Interfaces;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace ScriptHandler.Models.ScriptNodes
{
	public class ScriptNodeSweep : ScriptNodeBase
	{
		#region Properties and Fields

		[JsonIgnore]
		public ProjectData Parent { get; set; }

		public bool IsLoaded { get; set; }

		public ObservableCollection<SweepItemData> SweepItemsList { get; set; }

		private int _numberOfDimensions;
		public int NumberOfDimensions 
		{
			get => _numberOfDimensions;
			set
			{
				if(IsLoaded && value == 0 && SweepItemsList.Count > 0)
				{
					_numberOfDimensions = SweepItemsList.Count;
					return;
				}

				if(value < SweepItemsList.Count)
				{
					MessageBoxResult result = MessageBox.Show(
						"Are you sure you wish to delete rows from the dimentions list?",
						"Sweep Warning",
						MessageBoxButton.YesNo);
					if(result == MessageBoxResult.No) 
					{
						OnPropertyChanged(nameof(NumberOfDimensions));
						return;
					}
				}

				_numberOfDimensions = value;
				if(_numberOfDimensions < SweepItemsList.Count) 
				{ 
					while(_numberOfDimensions < SweepItemsList.Count)
						SweepItemsList.RemoveAt(SweepItemsList.Count - 1);
				}
				else if (_numberOfDimensions > SweepItemsList.Count)
				{
					while (_numberOfDimensions > SweepItemsList.Count)
						SweepItemsList.Add(new SweepItemData());
				}
			}
		}

		public override string Description
		{
			get
			{
				return "Sweep " + " - ID:" + ID;
			}
		}

		#endregion Properties and Fields

		#region Constructor

		public ScriptNodeSweep()
		{
			IsLoaded = false;

			Name = "Sweep";

			SweepItemsList = new ObservableCollection<SweepItemData>();
			SweepItemsList.CollectionChanged += SweepItemsList_CollectionChangedEventHandler;
		}

		#endregion Constructor

		#region Methods

		private void SweepItemsList_CollectionChangedEventHandler(object sender, NotifyCollectionChangedEventArgs e)
		{
			OnPropertyChanged(nameof(SweepItemsList));

			if(e.NewItems != null && e.NewItems.Count > 0) 
			{
				SweepItemsList[SweepItemsList.Count - 1].PropertyChanged += SweepItemData_PropertyChangedEventHandler;
			}
		}

		private void SweepItemData_PropertyChangedEventHandler(object sender, PropertyChangedEventArgs e)
		{
			OnPropertyChanged(nameof(SweepItemsList));
		}

		public override object Clone()
		{
			ScriptNodeSweep sweep = MemberwiseClone() as ScriptNodeSweep;
			sweep.SweepItemsList = new ObservableCollection<SweepItemData>();
			return sweep;
		}

		public void CleanList()
		{
			List<SweepItemData> list = SweepItemsList.ToList();
			list.RemoveAll((si) => si.Parameter == null);
			SweepItemsList = new ObservableCollection<SweepItemData>(list);
			_numberOfDimensions = SweepItemsList.Count;
		}

		public override void PostLoad(
			DevicesContainer devicesContainer,
			IScript currentScript)
		{
			IsLoaded = true;
			CleanList();
		}

		public override bool IsNotSet(
			DevicesContainer devicesContainer,
			ObservableCollection<InvalidScriptItemData> errorsList)
		{
			if (SweepItemsList == null || SweepItemsList.Count == 0)
				return true;

			return false;
		}

		#endregion Methods
	}
}
