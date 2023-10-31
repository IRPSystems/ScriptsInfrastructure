
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DBCFileParser.Model;
using DBCFileParser.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static System.Net.Mime.MediaTypeNames;

namespace ScriptHandler.ViewModel
{
	public class DBCFileViewModel: ObservableObject
	{
		#region Properties

		public ObservableCollection<Message> MessagesList { get; set; }

		public string WindowHeader { get; set; }

		#endregion Properties

		#region Fields

		public Message SelectedMessage;

		#endregion Fields

		#region Constructor

		public DBCFileViewModel(
			DbcData dbcData, 
			string windowHeader)
		{			
			WindowHeader = windowHeader;

			BuildMessagesList(dbcData);

			CloseOKCommand = new RelayCommand(CloseOK);
			CloseCancelCommand = new RelayCommand(CloseCancel);
		}

		#endregion Constructor

		#region Methods

		private void BuildMessagesList(DbcData dbcData)
		{
			MessagesList = new ObservableCollection<Message>();
			foreach (Message message in dbcData.Messages)
			{
				message.Visibility = System.Windows.Visibility.Visible;
				message.IsSelected = false;
				MessagesList.Add(message);

				foreach(Signal signal in message.Signals) 
				{
					if (signal.Unit == "�C")
						signal.Unit = "˚C";
					else if (signal.Unit == "�")
						signal.Unit = "˚";
				}
			}
		}

		#region Search parameter

		private void DeviceParamSearch_Text(TextChangedEventArgs e)
		{
			if (MessagesList == null)
				return;

			if (!(e.Source is TextBox tb))
				return;

			foreach (Message message in MessagesList)
			{
				message.IsExpanded = false;
				message.Visibility = Visibility.Collapsed;

				foreach (Signal signal in message.Signals)
				{
					signal.Visibility = Visibility.Collapsed;
				}
			}

			SearchMessage(tb.Text);
			SearchSignal(tb.Text);
			SearchId(tb.Text);
		}

		private void SearchMessage(string text)
		{
			foreach (Message message in MessagesList)
			{
				if (message.Name.ToLower().Contains(text.ToLower()))
					message.Visibility = System.Windows.Visibility.Visible;
				else
					message.Visibility = System.Windows.Visibility.Collapsed;
				
			}
		}

		private void SearchSignal(string text)
		{
			foreach (Message message in MessagesList)
			{
				foreach (Signal signal in message.Signals)
				{
					if (signal.Name.ToLower().Contains(text.ToLower()))
						signal.Visibility = System.Windows.Visibility.Visible;
				}
			}


			foreach (Message message in MessagesList)
			{
				bool isVisibleSignal = false;
				foreach (Signal signal in message.Signals)
				{
					if (signal.Visibility == System.Windows.Visibility.Visible)
					{
						isVisibleSignal = true;
						break;
					}
				}

				if (isVisibleSignal)
				{
					message.Visibility = System.Windows.Visibility.Visible;
					message.IsExpanded = true;
				}
			}
		}

		private void SearchId(string text)
		{
			foreach (Message message in MessagesList)
			{
				string hexID = message.ID.ToString("X");
				if (hexID.ToLower().Contains(text.ToLower()))
					message.Visibility = System.Windows.Visibility.Visible;
			}
		}


		#endregion Search parameter

		private void CloseOK()
		{
			foreach (Message msg in MessagesList)
			{
				if(msg.IsSelected) 
				{
					SelectedMessage = msg;
					return;
				}
			}
		}

		private void CloseCancel()
		{
		}

		private void MessageSelectionChanged(RoutedPropertyChangedEventArgs<object> e)
		{
			if (e.NewValue == null)
				return;

			if (e.NewValue is Message message)
			{
				message.IsSelected = true;
				return;
			}

			if (e.NewValue is Signal signal)
			{
				foreach (Message msg in MessagesList)
				{
					foreach (Signal sgnl in msg.Signals)
					{
						if (sgnl.Name == signal.Name)
						{
							msg.IsSelected = true;
							return;
						}
					}
				}
			}
		}

		#endregion Methods

		#region Commands


		public RelayCommand CloseOKCommand { get; private set; }
		public RelayCommand CloseCancelCommand { get; private set; }


		private RelayCommand<TextChangedEventArgs> _DeviceParamSearch_TextChanged;
		public RelayCommand<TextChangedEventArgs> DeviceParamSearch_TextChanged
		{
			get
			{
				return _DeviceParamSearch_TextChanged ?? (_DeviceParamSearch_TextChanged =
					new RelayCommand<TextChangedEventArgs>(DeviceParamSearch_Text));
			}
		}


		private RelayCommand<RoutedPropertyChangedEventArgs<object>> _MessageSelectionChangedCommand;
		public RelayCommand<RoutedPropertyChangedEventArgs<object>> MessageSelectionChangedCommand
		{
			get
			{
				return _MessageSelectionChangedCommand ?? (_MessageSelectionChangedCommand =
					new RelayCommand<RoutedPropertyChangedEventArgs<object>>(MessageSelectionChanged));
			}
		}

		#endregion Commands
	}
}
