
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using ScriptHandler.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace ScriptHandler.Models
{
	public class ScriptData: ObservableObject, IScript
	{
		public string Name { get; set; }
		public ObservableCollection<IScriptItem> ScriptItemsList { get; set; }

		public bool? IsPass { get; set; }

		[JsonIgnore]
		public object Parent { get; set; }
		[JsonIgnore]
		public string ScriptPath { get; set; }

		public ScriptData()
		{
			ScriptItemsList = new ObservableCollection<IScriptItem>();
			ScriptItemsList.CollectionChanged += NotifyCollectionChangedEventHandler;
		}

		private void NotifyCollectionChangedEventHandler(object sender, NotifyCollectionChangedEventArgs e)
		{
			NodesListChanged?.Invoke();
		}

		public event Action NodesListChanged;
	}
}
