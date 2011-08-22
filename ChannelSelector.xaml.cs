using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public class Channel
    {
        public int i_ChannelNumber { get; set; }
        public string s_Instrument { get; set; }
    }
    
    /// <summary>
    /// Interaction logic for ChannelSelector.xaml
    /// </summary>
    public partial class ChannelSelector : Window
    {
        //int i_Channel;

        ObservableCollection<Channel> _Channels = new ObservableCollection<Channel>();
        
        public ChannelSelector(ref MidiInfo midiInfo, int i_Channel, RoutedEventHandler extOkButtonClickedEvent)
        {
            InitializeComponent();

            okButton.Click += extOkButtonClickedEvent;

            for (int i = 0; i < midiInfo.a_UsedChannels.Length; i++) {
                if (midiInfo.a_UsedChannels[i]) {
                    _Channels.Add(new Channel
                    {
                        i_ChannelNumber = i,
                        s_Instrument = "(not yet implemented)"
                    });
                    //MessageBox.Show("aha");
                }
            }
        }

        public ObservableCollection<Channel> Channels
        {
            get { return _Channels; }
        }

        public int getSelectedChannel()
        {
            if (listView.SelectedIndex >= 0) {
                //MessageBox.Show(((Channel)listView.Items[listView.SelectedIndex]).i_ChannelNumber.ToString());
                return (((Channel)listView.Items[listView.SelectedIndex]).i_ChannelNumber);
            }

            return -1;
        }
    }
}
