using ControlzEx.Theming;
using System.Windows;

namespace ScriptHandler
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
		public static void ChangeDarkLight(bool isLightTheme)
		{
			if (isLightTheme)
				ThemeManager.Current.ChangeTheme(Current, "Light.Blue");
			else
				ThemeManager.Current.ChangeTheme(Current, "Dark.Blue");
		}
	}
}
