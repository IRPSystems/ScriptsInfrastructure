﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SingleScriptBuilder
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public App()
		{
			Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(
				"MTAwODQzOUAzMjMwMmUzNDJlMzBsQlRMeUl0THVueXVMcWhEMnlCeVJLTnZZdFhLRUh2aEZGKytIdUVIRTRBPQ==");

		}
	}
}
