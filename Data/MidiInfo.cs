using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;

using NAudio;
using NAudio.CoreAudioApi;
using NAudio.Midi;

namespace Symphonary
{
    public class Note
    {
        public int i_NoteNumber;
        public long li_BeginTime;
        public long li_EndTime;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="i_NoteNumber"></param>
        /// <param name="li_BeginTime"></param>
        /// <param name="li_EndTime"></param>
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

        private string s_Title;
        public string Title
        {
            get { return s_Title; }
        }

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


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="s_Filename"></param>
        /// <param name="i_Channel"></param>
        public MidiInfo(string s_Filename, int i_Channel)
        {
            s_Title = s_Filename.Substring(Math.Max(0, s_Filename.LastIndexOf('\\') + 1));
            int i_lastDotPos = s_Title.LastIndexOf('.');
            if (i_lastDotPos >= 0)
            {
                s_Title = s_Title.Substring(0, i_lastDotPos);
            }

            midiEventCollection = new MidiFile(s_Filename).Events;

            i_DeltaTicksPerQuarterNote = midiEventCollection.DeltaTicksPerQuarterNote;
            i_NumMusicChannels = midiEventCollection.Tracks - 1; // one of the tracks is used for metadata

            GetTempo();
            d_MilisecondsPerQuarterNote = i_MicrosecondsPerQuarterNote / 1000.0;
            d_MilisecondsPerTick = d_MilisecondsPerQuarterNote / i_DeltaTicksPerQuarterNote;

            GetTimeSignature();
            GetUsedChannels();
            GetChannelInstruments();

            for (int i = 0; i < midiEventCollection[0].Count; i++)
            {
                l_Metadata.Add(midiEventCollection[0][i]);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="i_Channel"></param>
        /// <returns></returns>
        public bool LoadChannelNotes(int i_Channel)
        {
            if (i_Channel < 0 || i_Channel > 15 || !a_UsedChannels[i_Channel])
            {
                return false;
            }

            l_Notes.Clear();

            Dictionary<int, long> d_NoteOnTimes = new Dictionary<int, long>();

            for (int i = 0; i < midiEventCollection[a_ExistingChannelOrder[i_Channel]].Count; i++)
            {
                NAudio.Midi.MidiEvent midiEvent = midiEventCollection[a_ExistingChannelOrder[i_Channel]][i];

                if (midiEvent.CommandCode == MidiCommandCode.NoteOff ||
                    midiEvent.CommandCode == MidiCommandCode.NoteOn && ((NoteOnEvent)midiEvent).Velocity == 0)
                {
                    NoteEvent noteOff = (NoteEvent)midiEvent;
                    long noteOnTime;
                    if (d_NoteOnTimes.TryGetValue(noteOff.NoteNumber, out noteOnTime))
                    {
                        l_Notes.Add(new Note(noteOff.NoteNumber, noteOnTime, ActualTime(noteOff.AbsoluteTime)));
                        d_NoteOnTimes.Remove(noteOff.NoteNumber);
                        //noteOff.
                    }
                    else
                    {
                        MessageBox.Show("Error: the NoteOff command at " + noteOff.AbsoluteTime + " does not match a previous NoteOn command");
                    }
                }
                else if (midiEvent.CommandCode == MidiCommandCode.NoteOn)
                {
                    NoteOnEvent noteOn = (NoteOnEvent)midiEvent;
                    try
                    {
                        d_NoteOnTimes.Add(noteOn.NoteNumber, ActualTime(noteOn.AbsoluteTime));
                    }
                    catch (ArgumentException e)
                    {
                        MessageBox.Show("Error: an event with NoteNumber " + noteOn.NoteNumber + " already exists");
                    }
                }
                else if (midiEvent.CommandCode == MidiCommandCode.MetaEvent && ((MetaEvent)midiEvent).MetaEventType == MetaEventType.EndTrack)
                {
                    i_EndTime = ActualTime(midiEvent.AbsoluteTime);
                    break;
                }
            }

            if (d_NoteOnTimes.Count != 0)
            {
                MessageBox.Show("Error: there are still " + d_NoteOnTimes.Count + " NoteOn events for which there were no NoteOff event");
            }

            return true;
        }

        /// <summary>
        /// Calcuates the actual clock time given the time unit found in MIDI file
        /// </summary>
        /// <param name="i_TimeInMIDIFile"></param>
        /// <returns></returns>
        private long ActualTime(long i_TimeInMIDIFile)
        {
            long i_ActualTime = (long)(i_TimeInMIDIFile * d_MilisecondsPerTick);

            return i_ActualTime;
            //return i_TimeInMIDIFile;
        }


        /// <summary>
        /// Calculated the tempo
        /// </summary>
        private void GetTempo()
        {
            for (int i = 0; i < midiEventCollection[0].Count; i++)
            {
                try
                {
                    i_TempoInBPM = (int)((TempoEvent)midiEventCollection[0][i]).Tempo;
                    i_TempoInNanoseconds = (int)(1000000 * (60 / (double)i_TempoInBPM));
                    i_MicrosecondsPerQuarterNote = ((TempoEvent)midiEventCollection[0][i]).MicrosecondsPerQuarterNote;
                    return;
                }
                catch (InvalidCastException ex) { }
            }
        }

        /// <summary>
        /// Gets the time signature
        /// </summary>
        private void GetTimeSignature()
        {
            for (int i = 0; i < midiEventCollection[0].Count; i++)
            {
                try
                {
                    i_TimeSignatureNumerator = ((TimeSignatureEvent)midiEventCollection[0][i]).Numerator;
                    i_TimeSignatureDenominator = (int)Math.Pow(2, ((TimeSignatureEvent)midiEventCollection[0][i]).Denominator);
                    //MessageBox.Show(((TimeSignatureEvent)midiEventCollection[0][i]).Denominator.ToString());
                    s_TimeSignature = ((TimeSignatureEvent)midiEventCollection[0][i]).TimeSignature;
                    return;
                }
                catch (InvalidCastException ex) { }
            }
        }

        /// <summary>
        /// Gets the channels being used
        /// </summary>
        private void GetUsedChannels()
        {
            for (int i = 0; i < a_UsedChannels.Length; i++)
            {
                a_UsedChannels[i] = false;
            }

            for (int i = 0; i < i_NumMusicChannels; i++)
            {
                for (int j = 0; j < midiEventCollection[i + 1].Count; j++)
                {
                    if (midiEventCollection[i + 1][j].CommandCode == MidiCommandCode.NoteOn || midiEventCollection[i + 1][j].CommandCode == MidiCommandCode.NoteOff)
                    {
                        a_UsedChannels[midiEventCollection[i + 1][j].Channel - 1] = true;
                        a_ExistingChannelOrder[midiEventCollection[i + 1][j].Channel - 1] = i + 1;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the channel instruments
        /// </summary>
        private void GetChannelInstruments()
        {
            for (int i = 0; i < a_ChannelInstrumentNumbers.Length; i++)
            {
                a_ChannelInstrumentNumbers[i] = 1; // default is 1 (acoustic grand piano)
            }


            for (int i = 0; i < a_ChannelInstrumentNumbers.Length; i++)
            {
                a_ChannelInstrumentNames[i] = PatchChangeEvent.GetPatchName(0);
            }

            for (int i = 0; i < i_NumMusicChannels; i++)
            {
                for (int j = 0; j < midiEventCollection[i + 1].Count; j++)
                {
                    if (midiEventCollection[i + 1][j].CommandCode == MidiCommandCode.PatchChange)
                    {
                        a_ChannelInstrumentNumbers[midiEventCollection[i + 1][j].Channel - 1] = ((PatchChangeEvent)midiEventCollection[i + 1][j]).Patch + 1;
                        a_ChannelInstrumentNames[midiEventCollection[i + 1][j].Channel - 1] = PatchChangeEvent.GetPatchName(a_ChannelInstrumentNumbers[midiEventCollection[i + 1][j].Channel - 1] - 1);
                        break;
                    }
                }
            }
        }
    }
}