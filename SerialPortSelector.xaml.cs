using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace NiceWindow
{
    public class SerialPortEntry
    {
        public string s_PortName { get; set; }
        public string s_Status { get; set; } 
    }
    
    /// <summary>
    /// Interaction logic for ChannelPortSelector.xaml
    /// </summary>
    public partial class SerialPortSelector : Window
    {
        ObservableCollection<SerialPortEntry> _SerialPorts = new ObservableCollection<SerialPortEntry>();

        private string s_PrevSelectedSerialPort = string.Empty;
        private string[] a_SerialPorts; 
        
        public SerialPortSelector(string s_PrevSelectedSerialPort, RoutedEventHandler extOkButtonClickedEvent)
        {
            InitializeComponent();

            openButton.Click += extOkButtonClickedEvent;

            this.s_PrevSelectedSerialPort = s_PrevSelectedSerialPort;
            getPortList();
        }

        public ObservableCollection<SerialPortEntry> SerialPorts
        {
            get { return _SerialPorts; }
        }

        private void getPortList()
        {
            _SerialPorts.Clear();
            a_SerialPorts = SerialPort.GetPortNames();

            SerialPort testPort;
            foreach (string port in a_SerialPorts) {
                try {
                    testPort = new SerialPort(port);
                    testPort.Open();
                    _SerialPorts.Add(new SerialPortEntry
                    {
                        s_PortName = port,
                        s_Status = "ON"
                    });
                    testPort.Close();
                }
                catch (IOException e) {
                    _SerialPorts.Add(new SerialPortEntry
                    {
                        s_PortName = port,
                        s_Status = "OFF"
                    });
                }
            }

            for (int i = 0; i < listView.Items.Count; i++) {
                if (s_PrevSelectedSerialPort == ((SerialPortEntry)listView.Items[i]).s_PortName) {
                    listView.SelectedIndex = i;
                    break;
                }
            }
        }

        public string getSelectedSerialPort()
        {
            if (listView.SelectedIndex >= 0) {
                return ((SerialPortEntry)listView.Items[listView.SelectedIndex]).s_PortName;
            }

            return string.Empty;
        }

        private void refreshButton_Click(object sender, RoutedEventArgs e)
        {
            getPortList();
        }
    }   
}
