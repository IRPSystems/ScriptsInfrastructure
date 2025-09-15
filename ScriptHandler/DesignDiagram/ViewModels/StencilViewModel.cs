
using CommunityToolkit.Mvvm.ComponentModel;
using ScriptHandler.Models;
using Syncfusion.UI.Xaml.Diagram;
using Syncfusion.UI.Xaml.Diagram.Stencil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;

namespace ScriptHandler.DesignDiagram.ViewModels
{
	public class StencilViewModel: ObservableObject
	{
		#region Properties

		public SymbolCollection SymbolSource { get; set; }

		public Brush Background { get; set; }
		public Brush Foreround { get; set; }

		#endregion Properties

		#region Constructor

		public StencilViewModel()
		{
			SymbolSource = new SymbolCollection();


			AddSymbols();
		}

		#endregion Constructor

		#region Mesthods

		private void AddSymbols()
		{
			var assemblyList = AppDomain.CurrentDomain.GetAssemblies();
			Assembly assembly = assemblyList.
				SingleOrDefault(assembly => assembly.GetName().Name == "ScriptHandler");

			List<Type> typesList = assembly.GetTypes().ToList();
			string name = "ScriptHandler.Models.ScriptNodes";
			typesList = typesList.Where((t) => t.Namespace == name).ToList();

			foreach (Type type in typesList)
			{
				if (!IsNodeBase(type))
					continue;

				if (type.Name == "ScriptNodeScopeSave" ||
					type.Name == "ScriptNodeStopContinuous")
					continue;

				var c = Activator.CreateInstance(type);
				SymbolViewModel symbol = new SymbolViewModel()
				{
					Symbol = (c as ScriptNodeBase).Name,
					Name = (c as ScriptNodeBase).Name,
					SymbolTemplate = Application.Current.Resources["SymbolTemplate"] as DataTemplate,
				};
				SymbolSource.Add(symbol);
			}
		}

		public void ChangeDarkLight()
		{
			Background =
				Application.Current.Resources["MahApps.Brushes.ThemeBackground"] as SolidColorBrush;
			Foreround =
				Application.Current.Resources["MahApps.Brushes.ThemeForeground"] as SolidColorBrush;
		}

		public static bool IsNodeBase(Type type)
		{
			while (type.BaseType.Name != "ScriptNodeBase")
			{
				if (type.BaseType.Name == "Object")
					return false;

				type = type.BaseType;
			}

			return true;
		}

		#endregion Mesthods
	}
}
