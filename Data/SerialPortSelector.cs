using System.Collections.ObjectModel;
using System.IO;
using System.IO.Ports;
using System.Windows;
using System.Windows.Controls;

namespace Symphonary
{
    public class SerialPortEntry
    {
        public string s_PortName { get; set; }
        public string s_Status { get; set; }
    }

    public class SerialPortSelector
    {
        ObservableCollection<SerialPortEntry> _SerialPorts = new ObservableCollection<SerialPortEntry>();
        private string[] a_SerialPorts;
        private ListView serialPortsListView;

        public SerialPortSelector(ListView serialPortsListView)
        {
            this.serialPortsListView = serialPortsListView;
        }

        public ObservableCollection<SerialPortEntry> SerialPorts
        {
            get { return _SerialPorts; }
        }

        public void Refresh(string s_PrevSelectedSerialPort)
        {
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
                }
                catch (IOException e) {
                    _SerialPorts.Add(new SerialPortEntry {
                        s_PortName = port,
                        s_Status = "OFF"
                    });
                }
            }

            for (int i = 0; i < serialPortsListView.Items.Count; i++) {
                if (s_PrevSelectedSerialPort == ((SerialPortEntry)serialPortsListView.Items[i]).s_PortName) {
                    serialPortsListView.SelectedIndex = i;
                    break;
                }
            }
        }


        public string SelectedSerialPort
        {
            get
            {
                if (serialPortsListView.SelectedIndex >= 0)
                {
                    return ((SerialPortEntry) serialPortsListView.Items[serialPortsListView.SelectedIndex]).s_PortName;
                }

                return string.Empty;
            }
        }
    }
}