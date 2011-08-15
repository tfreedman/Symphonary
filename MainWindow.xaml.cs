using NAudio;
using NAudio.Midi;
using NAudio.CoreAudioApi;
using Sanford.Collections;
using Sanford.Multimedia;
using Sanford.Multimedia.Midi;
using Sanford.Multimedia.Timers;
using Sanford.Threading;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace NiceWindow
{
    public class MidiInfo
    {
        public int i_BPM;
        public int i_DeltaTicksPerQuarterNote;
        public int i_NumMusicTracks;
        public List<NAudio.Midi.MidiEvent> l_Metadata = new List<NAudio.Midi.MidiEvent>();
        public List<Note> l_Notes = new List<Note>();
        public MidiEventCollection midiEventCollection;

        public MidiInfo(MidiEventCollection midiEventCollection, int i_Channel)
        {
            this.i_BPM = 120; // this will be changed later
            this.i_DeltaTicksPerQuarterNote = midiEventCollection.DeltaTicksPerQuarterNote;
            this.i_NumMusicTracks = midiEventCollection.Tracks - 1; // one of the tracks is used for metadata
            
            for (int i = 0; i < midiEventCollection[0].Count; i++)
            {
                this.l_Metadata.Add(midiEventCollection[0][i]);
            }

            this.midiEventCollection = midiEventCollection;

            Dictionary<int, long> d_NoteOnTimes = new Dictionary<int, long>();
            for (int i = 0; i < midiEventCollection[i_Channel].Count; i++)
            {
                if (midiEventCollection[i_Channel][i].CommandCode == MidiCommandCode.NoteOn)
                {
                    NoteOnEvent noteOn = (NoteOnEvent)midiEventCollection[i_Channel][i];
                    d_NoteOnTimes.Add(noteOn.NoteNumber, noteOn.AbsoluteTime);
                }
                else if (midiEventCollection[i_Channel][i].CommandCode == MidiCommandCode.NoteOff)
                {
                    NoteEvent noteOff = (NoteEvent)midiEventCollection[i_Channel][i];
                    long noteOnTime;
                    if (d_NoteOnTimes.TryGetValue(noteOff.NoteNumber, out noteOnTime))
                    {
                        l_Notes.Add(new Note(noteOff.NoteNumber, noteOnTime, noteOff.AbsoluteTime));
                        d_NoteOnTimes.Remove(noteOff.NoteNumber);
                    }
                    else
                    {
                        MessageBox.Show("Error: the NoteOff command at " + noteOff.AbsoluteTime + " does not match a previous NoteOn command");
                    }
                }
                else
                {
                    
                }
            }
            if (d_NoteOnTimes.Count != 0)
            {
                MessageBox.Show("Error: there are still " + d_NoteOnTimes.Count + " NoteOn events for which there were no NoteOff event");
            }
        }
    }


    public class Note
    {
        public int i_NoteNumber;
        public long li_BeginTime;
        public long li_EndTime;

        public Note(int i_NoteNumber, long li_BeginTime, long li_EndTime)
        {
            this.i_NoteNumber = i_NoteNumber;
            this.li_BeginTime = li_BeginTime;
            this.li_EndTime = li_EndTime;
        }
    }


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public delegate void ChangeTextDelegate(string text);

        int i_PersistentTrack = 0; // select the track to always play (begins at 0)
        string s_Filename = "Test1.mid";

        
        // variables used strictly for playback
        bool b_FinishedLoading = false;
        bool b_MuteOtherTracks = false;
        bool b_Playing = false;
        bool b_ProgramClosing = false;
        ChannelStopper channelStopper = new ChannelStopper();
        OutputDevice outputDevice = new OutputDevice(0);
        Sequence sequence = new Sequence();
        Sequencer sequencer = new Sequencer();
        
        // variables used strictly for storing data
        MidiEventCollection midiEventCollection;
        MidiFile midiFile;
        MidiInfo midiInfo;
        

        public MainWindow()
        {
            InitializeComponent();
            
            sequence.Format = 1;
            sequence.LoadCompleted += handleLoadCompleted;
            sequence.LoadAsync(s_Filename);
            sequencer.Position = 0;
            sequencer.Sequence = sequence;
            sequencer.ChannelMessagePlayed += handleChannelMessagePlayed;
            sequencer.Chased += handleChased;
            sequencer.Stopped += handleStopped;
            
            midiFile = new MidiFile(s_Filename);
            midiEventCollection = midiFile.Events;

            midiInfo = new MidiInfo(midiEventCollection, i_PersistentTrack + 1);

            foreach (NAudio.Midi.MidiEvent metadata in midiInfo.l_Metadata)
            {
                textbox1.Text += metadata.ToString() + Environment.NewLine;
            }

            textbox1.Text += Environment.NewLine;

            foreach (Note note in midiInfo.l_Notes)
            {
                textbox1.Text += note.i_NoteNumber + ", " + note.li_BeginTime + ", " + note.li_EndTime + Environment.NewLine;
            }

            textbox1.Text += Environment.NewLine;

            textbox1.Text += midiInfo.i_DeltaTicksPerQuarterNote;
        }


        public void changeText(string text)
        {
            textbox1.Text = text;
            textbox1.ScrollToEnd();
        }



        public void muteOtherTracks()
        {
            for (int i = 0; i < 16; i++)
            {
                if (i != i_PersistentTrack) 
                {
                    // sending all-sounds-off command to channel 
                    outputDevice.Send(new ChannelMessage(ChannelCommand.Controller, i, (int)ControllerType.AllSoundOff, 0));
                }
            }
        }


        // this does note-checking for the flute, just ignore this for now as it is not being used
        private bool noteMatches(ref string serialData, int noteNumber)
        {
            switch (serialData)
            {
                case "01111001110": // DN4***
                    return noteNumber == 50;
                case "01111001111": // DS4EF4
                    return noteNumber == 51;
                case "01111001101": // EN4FF4, EN5FF5
                    return noteNumber == 52 || noteNumber == 64;
                case "01111001001": // ES4FN4
                    return noteNumber == 53;
                case "01111000011": // FS4GF4, FS5GF5
                    return noteNumber == 54 || noteNumber == 66;
                case "01111000001": // GN4***, GN5***
                    return noteNumber == 55 || noteNumber == 67;
                case "01111010001": // GS4AF4, GS5AF5
                    return noteNumber == 56 || noteNumber == 68;
                case "01110000001": // AN4***, AN5***
                    return noteNumber == 57 || noteNumber == 69;
                case "01100001001": // AS4BF4
                    return noteNumber == 58;
                case "01100000001": // BN4CF5, BN5CF6
                    return noteNumber == 59 || noteNumber == 71;
                case "00100000001": // BS4CN5, BS5CN6
                    return noteNumber == 60 || noteNumber == 72;
                case "00000000001": // CS5DF5, CS6DF6
                    return noteNumber == 61 || noteNumber == 73;
                case "01011001110": // DN5***
                    return noteNumber == 62;
                case "01011001111": // DS5EF5
                    return noteNumber == 63;
                case "10100000001": // AS5BF5
                    return noteNumber == 70;
                case "01011000001": // DN6***
                    return noteNumber == 74;
                case "01111011111": // DS6EF6
                    return noteNumber == 75;
                case "01110001101": // EN6FF6
                    return noteNumber == 76;
                case "01101001001": // ES6FN6
                    return noteNumber == 77;
                case "01101000011": // FS6GF6
                    return noteNumber == 78;
                case "00111000001": // GN6***
                    return noteNumber == 79;
                case "00011010001": // GS6AF6
                    return noteNumber == 80;
                case "01010001001": // AN6***
                    return noteNumber == 81;
                default: // does not match the note
                    return false;
            }
        }


        // override some program event handlers to ensure extra things are loaded/closed properly on start/close

        protected override void OnClosing(CancelEventArgs e)
        {
            b_ProgramClosing = true;

            base.OnClosing(e);
        }


        protected override void OnClosed(EventArgs e)
        {
            sequencer.Dispose();
            if (outputDevice != null)
                outputDevice.Dispose();
            
            base.OnClosed(e);
        }


        // handlers for the sequence and sequencer (playback components)

        private void handleChannelMessagePlayed(object sender, ChannelMessageEventArgs e)
        {
            if (b_ProgramClosing) // don't try playing anything if program is closing
                return;


            if (b_MuteOtherTracks && e.Message.MidiChannel != i_PersistentTrack)
                return;

            outputDevice.Send(e.Message);
            
            
            // textbox1.Text = "hello"; // can't do something like this because textbox1 is owned by another thread
            // do something like this instead
            // startButton.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ChangeTextDelegate(changeText), e.Message.Data1.ToString());

            
        }

        
        private void handleChased(object sender, ChasedEventArgs e) 
        {
            // I don't know what exactly this handles
            foreach (ChannelMessage message in e.Messages)
            {
                outputDevice.Send(message);
            }
        }

        private void handleLoadCompleted(object sender, AsyncCompletedEventArgs e)
        {
            b_FinishedLoading = true;
        }

        private void handleStopped(object sender, StoppedEventArgs e)
        {
            foreach (ChannelMessage message in e.Messages)
            {
                outputDevice.Send(message);
            }

            startButton.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                new ChangeTextDelegate(changeText), string.Empty); // clear the contents of the textbox
            b_Playing = false;
        }


        // button events 

        private void startButton_Clicked(object sender, EventArgs e)
        {
            if (!b_FinishedLoading)
            {
                MessageBox.Show("Please wait for the MIDI file to finish loading");
                return;
            }

            sequencer.Start();
            b_Playing = true;
        }

        // keyboard events

        private void window_KeyUp(object sender, KeyEventArgs e)
        {
            if (!b_Playing)
                return;

            if (e.Key.ToString() == "M")
            {
                b_MuteOtherTracks = false;
                //changeText("All tracks are playing...");
            }
        }

        private void window_KeyDown(object sender, KeyEventArgs e)
        {
            if (!b_Playing)
                return;

            if (e.Key.ToString() == "M")
            {
                b_MuteOtherTracks = true;
                muteOtherTracks();
                //changeText("Tracks are muted, only track #" + i_PersistentTrack + " is playing...");
            }

            
        }

    }
}
