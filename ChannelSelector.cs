﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Symphonary {
    public class Channel {
        public int i_ChannelNumber { get; set; }
        public string s_Instrument { get; set; }
    }

    public partial class ChannelSelector {
        ObservableCollection<Channel> _Channels = new ObservableCollection<Channel>();

        public ChannelSelector(ref MidiInfo midiInfo, int i_Channel, RoutedEventHandler extOkButtonClickedEvent, NWGUI Browser) {
            Browser.okButton.Click += extOkButtonClickedEvent;

            for (int i = 0; i < midiInfo.a_UsedChannels.Length; i++) {
                if (midiInfo.a_UsedChannels[i]) {
                    _Channels.Add(new Channel {
                        i_ChannelNumber = i,
                        s_Instrument = midiInfo.a_ChannelInstrumentNames[i]
                    });
                }
            }

            if (i_Channel >= 0) {
                for (int i = 0; i < Browser.listView.Items.Count; i++) {
                    if (i_Channel == ((Channel)Browser.listView.Items[i]).i_ChannelNumber) {
                        Browser.listView.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        public ObservableCollection<Channel> Channels {
            get { return _Channels; }
        }

        public int getSelectedChannel(NWGUI Browser) {
            if (Browser.listView.SelectedIndex >= 0)
                return ((Channel)Browser.listView.Items[Browser.listView.SelectedIndex]).i_ChannelNumber;
            return -1;
        }
    }
}