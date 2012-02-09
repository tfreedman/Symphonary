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

namespace Symphonary {
    public class SerialPortEntry {
        public string s_PortName { get; set; }
        public string s_Status { get; set; }
    }

    /// <summary>
    /// Interaction logic for ChannelPortSelector.xaml
    /// </summary>
    public partial class SerialPortSelector : Window {
        ObservableCollection<SerialPortEntry> _SerialPorts = new ObservableCollection<SerialPortEntry>();

        private string s_PrevSelectedSerialPort = string.Empty;
        private string[] a_SerialPorts;

        public SerialPortSelector(string s_PrevSelectedSerialPort, RoutedEventHandler extOkButtonClickedEvent, NWGUI Browser) {
            Browser.openButton.Click += extOkButtonClickedEvent;

            this.s_PrevSelectedSerialPort = s_PrevSelectedSerialPort;
            getPortList(Browser);
        }

        public ObservableCollection<SerialPortEntry> SerialPorts {
            get { return _SerialPorts; }
        }

        public void getPortList(NWGUI Browser) {
            _SerialPorts.Clear();
            a_SerialPorts = SerialPort.GetPortNames();

            SerialPort testPort;
            foreach (string port in a_SerialPorts) {
                try {
                    testPort = new SerialPort(port);
                    testPort.Open();
                    _SerialPorts.Add(new SerialPortEntry {
                        s_PortName = port,
                        s_Status = "ON"
                    });
                    testPort.Close();
                } catch (IOException e) {
                    _SerialPorts.Add(new SerialPortEntry {
                        s_PortName = port,
                        s_Status = "OFF"
                    });
                }
            }

            for (int i = 0; i < Browser.listView.Items.Count; i++) {
                if (s_PrevSelectedSerialPort == ((SerialPortEntry)Browser.listView.Items[i]).s_PortName) {
                    Browser.listView.SelectedIndex = i;
                    break;
                }
            }
        }

        public string getSelectedSerialPort(NWGUI Browser) {
            if (Browser.listView.SelectedIndex >= 0) {
                return ((SerialPortEntry)Browser.listView.Items[Browser.listView.SelectedIndex]).s_PortName;
            }

            return string.Empty;
        }
    }
}
