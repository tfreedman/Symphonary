using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace Symphonary
{
    public class Channel
    {
        public int i_ChannelNumber { get; set; }
        public string s_Instrument { get; set; }
        public int i_NoteCount { get; set; }
    }
   

    /// <summary>
    /// Interaction logic for ChannelSelector.xaml
    /// </summary>
    public class ChannelSelector
    {
        //int i_Channel;
        private ObservableCollection<Channel> _Channels = new ObservableCollection<Channel>();
        private ListView channelsListView;

        public ChannelSelector(ListView channelsListView)
        {
            this.channelsListView = channelsListView;
            
            //_Channels.Add(new Channel
            //                  {
            //                      i_ChannelNumber = 0,
            //                      s_Instrument = "THIS IS A TEST",
            //                      i_NoteCount = 42
            //                  });

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="midiInfo"></param>
        /// <param name="i_Channel"></param>
        public void Refresh(ref MidiInfo midiInfo, int i_Channel)
        {
            _Channels.Clear();
            
            for (int i = 0; i < midiInfo.a_UsedChannels.Length; i++)
            {
                if (midiInfo.a_UsedChannels[i])
                {
                    _Channels.Add(new Channel
                                      {
                                          i_ChannelNumber = i,
                                          s_Instrument = midiInfo.a_ChannelInstrumentNames[i],
                                          i_NoteCount = midiInfo.notesForAllChannels[i].Count
                                          //i_NoteCount = -1
                                      });
                }
            }

            
            if (i_Channel >= 0)
            {
                for (int i = 0; i < channelsListView.Items.Count; i++)
                {
                    if (i_Channel == ((Channel)channelsListView.Items[i]).i_ChannelNumber)
                    {
                        channelsListView.SelectedIndex = i;
                        break;
                    }
                }
            }
        }
        

        public ObservableCollection<Channel> Channels
        {
            get { return _Channels; }
        }

        public int SelectedChannel
        {
            get
            {
                if (channelsListView.SelectedIndex >= 0)
                {
                    return ((Channel)channelsListView.Items[channelsListView.SelectedIndex]).i_ChannelNumber;
                }

                return -1;
            }
        }
    }

}