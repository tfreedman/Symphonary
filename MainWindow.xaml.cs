using NAudio;
using NAudio.CoreAudioApi;
using NAudio.Midi;
using Sanford.Collections;
using Sanford.Multimedia;
using Sanford.Multimedia.Midi;
using Sanford.Multimedia.Timers;
using Sanford.Threading;
using System;
using System.Collections;
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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace NiceWindow
{
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
    } // end Note
    
    public class MidiInfo
    {
        // length of 1 tick = microseconds per beat / 60 

        public double d_MilisecondsPerQuarterNote;
        public double d_MilisecondsPerTick;
        
        public int i_DeltaTicksPerQuarterNote;
        public int i_MicrosecondsPerQuarterNote;
        public int i_NumMusicChannels;
        public int i_TempoInBPM;
        public int i_TempoInNanoseconds;        
        public int i_TimeSignatureNumerator;
        public int i_TimeSignatureDenominator;
        
        public string s_Title;
        public string s_TimeSignature;
        

        // 16 channels from 0 to 15
        public bool[] a_UsedChannels = new bool[16];

        public int[] a_ChannelInstrumentNumbers = new int[16];
        public string[] a_ChannelInstrumentNames = new string[16];

        public int[] a_ExistingChannelOrder = new int[16]; // maps channels from their numbers (0 to 15) to indices in MidiEventCollection

        public List<NAudio.Midi.MidiEvent> l_Metadata = new List<NAudio.Midi.MidiEvent>();
        public MidiEventCollection midiEventCollection;

        // these are for just one channel of choice
        public long i_EndTime;
        public List<Note> l_Notes = new List<Note>();
       

        
        public MidiInfo(string s_Filename, int i_Channel)
        {
            s_Title = s_Filename.Substring(Math.Max(0, s_Filename.LastIndexOf('\\') + 1));
            int i_lastDotPos = s_Title.LastIndexOf('.');
            if (i_lastDotPos >= 0) {
                s_Title = s_Title.Substring(0, i_lastDotPos);
            }
            
            midiEventCollection = new MidiFile(s_Filename).Events;
            
            i_DeltaTicksPerQuarterNote = midiEventCollection.DeltaTicksPerQuarterNote;
            i_NumMusicChannels = midiEventCollection.Tracks - 1; // one of the tracks is used for metadata

            getTempo();
            d_MilisecondsPerQuarterNote = i_MicrosecondsPerQuarterNote / 1000.0;
            d_MilisecondsPerTick = d_MilisecondsPerQuarterNote / i_DeltaTicksPerQuarterNote;

            getTimeSignature();
            getUsedChannels();
            getChannelInstruments();

            for (int i = 0; i < midiEventCollection[0].Count; i++) {
                l_Metadata.Add(midiEventCollection[0][i]);
            }
        }

        public bool loadChannelNotes(int i_Channel)
        {
            if (i_Channel < 0 || i_Channel > 15 || !a_UsedChannels[i_Channel]) {
                return false;
            }

            l_Notes.Clear();

            Dictionary<int, long> d_NoteOnTimes = new Dictionary<int, long>();

            for (int i = 0; i < midiEventCollection[a_ExistingChannelOrder[i_Channel]].Count; i++) {
                NAudio.Midi.MidiEvent midiEvent = midiEventCollection[a_ExistingChannelOrder[i_Channel]][i];

                if (midiEvent.CommandCode == MidiCommandCode.NoteOff ||
                    midiEvent.CommandCode == MidiCommandCode.NoteOn && ((NoteOnEvent)midiEvent).Velocity == 0) {
                    NoteEvent noteOff = (NoteEvent)midiEvent;
                    long noteOnTime;
                    if (d_NoteOnTimes.TryGetValue(noteOff.NoteNumber, out noteOnTime)) {
                        l_Notes.Add(new Note(noteOff.NoteNumber, noteOnTime, actualTime(noteOff.AbsoluteTime)));
                        d_NoteOnTimes.Remove(noteOff.NoteNumber);
                        //noteOff.
                    }
                    else {
                        MessageBox.Show("Error: the NoteOff command at " + noteOff.AbsoluteTime + " does not match a previous NoteOn command");
                    }
                }
                else if (midiEvent.CommandCode == MidiCommandCode.NoteOn) {
                    NoteOnEvent noteOn = (NoteOnEvent)midiEvent;
                    try {
                        d_NoteOnTimes.Add(noteOn.NoteNumber, actualTime(noteOn.AbsoluteTime));
                    }
                    catch (ArgumentException e) {
                        MessageBox.Show("Error: an event with NoteNumber " + noteOn.NoteNumber + " already exists");
                    }
                }
                else if (midiEvent.CommandCode == MidiCommandCode.MetaEvent && ((MetaEvent)midiEvent).MetaEventType == MetaEventType.EndTrack) {
                    i_EndTime = actualTime(midiEvent.AbsoluteTime);
                    break;
                }
            }

            if (d_NoteOnTimes.Count != 0) {
                MessageBox.Show("Error: there are still " + d_NoteOnTimes.Count + " NoteOn events for which there were no NoteOff event");
            }

            return true;
        }

        private long actualTime(long i_TimeInMIDIFile)
        {
            long i_ActualTime = (long)(i_TimeInMIDIFile * d_MilisecondsPerTick);

            return i_ActualTime;
            //return i_TimeInMIDIFile;
        }


        private void getTempo()
        {
            for (int i = 0; i < midiEventCollection[0].Count; i++) {
                try {
                    i_TempoInBPM = (int)((TempoEvent)midiEventCollection[0][i]).Tempo;
                    i_TempoInNanoseconds = (int)(1000000 * (60 / (double)i_TempoInBPM));
                    i_MicrosecondsPerQuarterNote = ((TempoEvent)midiEventCollection[0][i]).MicrosecondsPerQuarterNote;                    
                    return;
                }
                catch (InvalidCastException ex) { }
            }
        }

        private void getTimeSignature()
        {
            for (int i = 0; i < midiEventCollection[0].Count; i++) {
                try {
                    i_TimeSignatureNumerator = ((TimeSignatureEvent)midiEventCollection[0][i]).Numerator;
                    i_TimeSignatureDenominator = (int)Math.Pow(2, ((TimeSignatureEvent)midiEventCollection[0][i]).Denominator);
                    //MessageBox.Show(((TimeSignatureEvent)midiEventCollection[0][i]).Denominator.ToString());
                    s_TimeSignature = ((TimeSignatureEvent)midiEventCollection[0][i]).TimeSignature;
                    return;
                }
                catch (InvalidCastException ex) { }
            }
        }

        private void getUsedChannels()
        {
            for (int i = 0; i < a_UsedChannels.Length; i++) {
                a_UsedChannels[i] = false;
            }

            for (int i = 0; i < i_NumMusicChannels; i++) {
                for (int j = 0; j < midiEventCollection[i + 1].Count; j++) {
                    if (midiEventCollection[i + 1][j].CommandCode == MidiCommandCode.NoteOn || midiEventCollection[i + 1][j].CommandCode == MidiCommandCode.NoteOff) {
                        a_UsedChannels[midiEventCollection[i + 1][j].Channel - 1] = true;
                        a_ExistingChannelOrder[midiEventCollection[i + 1][j].Channel - 1] = i + 1;
                        break;
                    }
                }
            }
        }

        private void getChannelInstruments()
        {
            for (int i = 0; i < a_ChannelInstrumentNumbers.Length; i++) {
                a_ChannelInstrumentNumbers[i] = 1; // default is 1 (acoustic grand piano)
            }

            
            for (int i = 0; i < a_ChannelInstrumentNumbers.Length; i++) {
                a_ChannelInstrumentNames[i] = PatchChangeEvent.GetPatchName(0);
            }

            for (int i = 0; i < i_NumMusicChannels; i++) {
                for (int j = 0; j < midiEventCollection[i + 1].Count; j++) {
                    if (midiEventCollection[i + 1][j].CommandCode == MidiCommandCode.PatchChange) {
                        a_ChannelInstrumentNumbers[midiEventCollection[i + 1][j].Channel - 1] = ((PatchChangeEvent)midiEventCollection[i + 1][j]).Patch + 1;
                        a_ChannelInstrumentNames[midiEventCollection[i + 1][j].Channel - 1] = PatchChangeEvent.GetPatchName(a_ChannelInstrumentNumbers[midiEventCollection[i + 1][j].Channel - 1] - 1);
                        break;
                    }
                }
            }
        }
    }


    public class MidiPlayer
    {
        bool b_FinishedLoading = false;
        bool b_MuteOtherTracks = false;
        bool b_Playing = false;
        bool b_ProgramClosing = false;

        
        ChannelStopper channelStopper = new ChannelStopper();
        OutputDevice outputDevice = new OutputDevice(0);
        Sequence sequence = new Sequence();
        Sequencer sequencer = new Sequencer();

        int i_PersistentChannel = -1;
        public int i_NumChannelNotesPlayed = 0;
        //public List<int> l_CurrentPlayingChannelNotes = new List<int>();
        public ArrayList al_CurrentPlayingChannelNotes = new ArrayList();

        public MidiPlayer(string s_Filename, 
            System.ComponentModel.ProgressChangedEventHandler extHandleLoadProgressChanged,
            System.EventHandler<System.ComponentModel.AsyncCompletedEventArgs> extHandleLoadCompleted,
            System.EventHandler<ChannelMessageEventArgs> extHandleChannelMessagePlayed)
        {
            sequence.Format = 1;
            sequence.LoadProgressChanged += extHandleLoadProgressChanged;
            sequence.LoadCompleted += handleLoadCompleted;
            sequence.LoadCompleted += extHandleLoadCompleted;
            sequence.LoadAsync(s_Filename);
            sequencer.Position = 0;
            sequencer.Sequence = sequence;
            sequencer.ChannelMessagePlayed += handleChannelMessagePlayed;
            sequencer.ChannelMessagePlayed += extHandleChannelMessagePlayed;
            sequencer.Chased += handleChased;
            sequencer.Stopped += handleStopped;
        }

        public bool isFinishedLoading()
        {
            return b_FinishedLoading;
        }
        
        public bool isPlaying()
        {
            return b_Playing;
        }


        public void setPersistentChannel(int i_PersistentChannel)
        {
            this.i_PersistentChannel = i_PersistentChannel;
            i_NumChannelNotesPlayed = 0;
        }


        private void muteAllChannels()
        {
            for (int i = 0; i < 16; i++) {
                outputDevice.Send(new ChannelMessage(ChannelCommand.Controller, i, (int)ControllerType.AllSoundOff, 0));
            }
        }

        public void muteOtherChannels()
        {
            b_MuteOtherTracks = true;
            for (int i = 0; i < 16; i++) {
                if (i != i_PersistentChannel) {
                    // sending all-sounds-off command to channel 
                    outputDevice.Send(new ChannelMessage(ChannelCommand.Controller, i, (int)ControllerType.AllSoundOff, 0));
                }
            }
        }


        public void unmuteOtherChannels()
        {
            b_MuteOtherTracks = false;
        }

        public void OnClosingOperations()
        {
            b_ProgramClosing = true;
        }

        public void OnClosedOperations()
        {
            sequencer.Dispose();
            if (outputDevice != null)
                outputDevice.Dispose();
        }

        public void startPlaying()
        {
            if (b_FinishedLoading) {
                sequencer.Start();
                b_Playing = true;
                //MessageBox.Show("playing");
            }
        }

        public void stopPlaying()
        {
            b_Playing = false;
            muteAllChannels();
            sequencer.Stop();
        }

        private void handleChannelMessagePlayed(object sender, ChannelMessageEventArgs e)
        {
            if (b_ProgramClosing) // don't try playing anything if program is closing
                return;

            if (b_MuteOtherTracks && e.Message.MidiChannel != i_PersistentChannel)
                return;

            if (e.Message.MidiChannel == i_PersistentChannel) {
                if (e.Message.Command == ChannelCommand.NoteOn && e.Message.Data2 > 0) {
                    i_NumChannelNotesPlayed++;
                    al_CurrentPlayingChannelNotes.Add(e.Message.Data1);
                }
                else if (e.Message.Command == ChannelCommand.NoteOn || e.Message.Command == ChannelCommand.NoteOff) {
                    al_CurrentPlayingChannelNotes.Remove(e.Message.Data1);
                }
            }

            outputDevice.Send(e.Message);
        }


        private void handleChased(object sender, ChasedEventArgs e)
        {
            // I don't know what exactly this handles
            foreach (ChannelMessage message in e.Messages) {
                outputDevice.Send(message);
            }
        }

        private void handleLoadCompleted(object sender, AsyncCompletedEventArgs e)
        {
            b_FinishedLoading = true;
        }

        private void handleStopped(object sender, StoppedEventArgs e)
        {
            foreach (ChannelMessage message in e.Messages) {
                outputDevice.Send(message);
            }

            b_Playing = false;
        }
    } // end MidiPlayer



    public class NoteMatcher
    {
        // This does matching for the violin. We will need to switch to wildcards though to handle chords.
        /*
        public bool noteMatches(string serialData, int noteNumber)
        {
            switch (serialData)
            {
                case "0000": // GN3*** || DN4*** || AN4*** || EN5***
                    return noteNumber == 55 || noteNumber == 62 || noteNumber == 69 || noteNumber == 76;

                case "1000": // GS3
                    return noteNumber == 56;
                case "0100": // DS4
                    return noteNumber == 63;
                case "0010": // AS4
                    return noteNumber == 70;
                case "0001": // F5
                    return noteNumber == 77;

                case "2000": // A3
                    return noteNumber == 57;
                case "0200": // E4
                    return noteNumber == 64;
                case "0020": // B4
                    return noteNumber == 71;
                case "0002": // F5
                    return noteNumber == 78;

                case "3000": // AS3
                    return noteNumber == 58;
                case "0300": // F4
                    return noteNumber == 65;
                case "0030": // C5
                    return noteNumber == 72;
                case "0003": // G5
                    return noteNumber == 79;

                case "4000": // B4
                    return noteNumber == 59;
                case "0400": // FS4
                    return noteNumber == 66;
                case "0040": // CS5
                    return noteNumber == 73;
                case "0004": // GS5
                    return noteNumber == 80;

                case "5000": // C4
                    return noteNumber == 60;
                case "0500": // G4
                    return noteNumber == 67;
                case "0050": // D5
                    return noteNumber == 74;
                case "0005": // A5
                    return noteNumber == 81;

                case "6000": // CS4
                    return noteNumber == 61;
                case "0600": // GS4
                    return noteNumber == 68;
                case "0060": // DS5
                    return noteNumber == 75;
                case "0006": // AS5
                    return noteNumber == 82;

                case "7000": // D4
                    return noteNumber == 62;
                case "0700": // A4
                    return noteNumber == 69;
                case "0070": // E5
                    return noteNumber == 76;
                case "0007": // B5
                    return noteNumber == 83;

                default: // does not match the note
                    return false;
            }
        }
         */

        // this does note-checking for the flute, just ignore this for now as it is not being used
        
        public bool noteMatches(string serialData, int noteNumber)
        {
            switch (serialData) {
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
        
    } // end NoteMatcher


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public delegate void ChangeTextDelegate(string text);
        
        public MainWindow()
        {
            InitializeComponent();
        }


        public void changeText(string text)
        {
            textbox1.Text = text;
            textbox1.ScrollToEnd();
        }

    }
}
