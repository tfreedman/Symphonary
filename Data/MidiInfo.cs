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
        public int NoteNumber;
        public long BeginTime;
        public long EndTime;
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="noteNumber"></param>
        /// <param name="beginTime"></param>
        /// <param name="endTime"></param>
        public Note(int noteNumber, long beginTime, long endTime)
        {
            this.NoteNumber = noteNumber;
            this.BeginTime = beginTime;
            this.EndTime = endTime;
        }
    } // end Note


    public class MidiInfo
    {
        public DebugWindow debugConsole;
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

        public List<MidiEvent> l_Metadata = new List<MidiEvent>();
        public MidiEventCollection midiEventCollection;

        public List<Note>[] notesForAllChannels = new List<Note>[16]; 

        // these are for just one channel of choice
        private long i_EndTime;
        //public List<Note> l_Notes = new List<Note>();


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="s_Filename"></param>
        /// <param name="i_Channel"></param>
        public MidiInfo(DebugWindow console)
        {
            debugConsole = console;
            for (int channel = 0; channel < 16; channel++)
            {
                notesForAllChannels[channel] = new List<Note>();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="selectedChannel"></param>
        public void Refresh(string filename, int selectedChannel)
        {
            GetTitle(filename);

            midiEventCollection = new MidiFile(filename).Events;
 
            i_NumMusicChannels = midiEventCollection.Tracks - 1; // one of the tracks is used for metadata
            
            GetAllTimingInformation();

            GetUsedChannels();

            GetChannelInstruments();

            GetNotesForAllChannels();

            l_Metadata.Clear();
            for (int i = 0; i < midiEventCollection[0].Count; i++) {
                l_Metadata.Add(midiEventCollection[0][i]);
            }
        }


        /// <summary>
        /// Calcuates the actual clock time given the time unit found in MIDI file
        /// </summary>
        /// <param name="timeInMIDIFile"></param>
        /// <returns></returns>
        private long ActualTime(long timeInMIDIFile)
        {
            long actualTime = (long)(timeInMIDIFile * d_MilisecondsPerTick);

            return actualTime;
        }


        private void GetTitle(string filename)
        {
            s_Title = filename.Substring(Math.Max(0, filename.LastIndexOf('\\') + 1));
            int i_lastDotPos = s_Title.LastIndexOf('.');
            if (i_lastDotPos >= 0) {
                s_Title = s_Title.Substring(0, i_lastDotPos);
            }
        }


        /// <summary>
        /// Gets all the timing information:
        /// delta ticks per quarter note,
        /// tempo (in BPM and nanoseconds),
        /// microseconds per quarter note,
        /// miliseconds per quarter note,
        /// miliseconds per tick,
        /// time signature (numerator and denominator)
        /// </summary>
        private void GetAllTimingInformation()
        {
            i_DeltaTicksPerQuarterNote = midiEventCollection.DeltaTicksPerQuarterNote;
            GetAllTimingInformationHelper_GetTempo();
            d_MilisecondsPerQuarterNote = i_MicrosecondsPerQuarterNote / 1000.0;
            d_MilisecondsPerTick = d_MilisecondsPerQuarterNote / i_DeltaTicksPerQuarterNote;
            GetAllTimingInformationHelper_GetTimeSignature();
        }


        /// <summary>
        /// Calculate the tempo. This should only be called from GetAllTimingInformation.
        /// </summary>
        private void GetAllTimingInformationHelper_GetTempo()
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
                catch (InvalidCastException) { }
            }
        }

        /// <summary>
        /// Gets the time signature. This should only be called from GetAllTimingInformation.
        /// </summary>
        private void GetAllTimingInformationHelper_GetTimeSignature()
        {
            for (int i = 0; i < midiEventCollection[0].Count; i++)
            {
                try
                {
                    i_TimeSignatureNumerator = ((TimeSignatureEvent)midiEventCollection[0][i]).Numerator;
                    i_TimeSignatureDenominator = (int)Math.Pow(2, ((TimeSignatureEvent)midiEventCollection[0][i]).Denominator);
                    s_TimeSignature = ((TimeSignatureEvent)midiEventCollection[0][i]).TimeSignature;
                    return;
                }
                catch (InvalidCastException) { }
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

        
        /// <summary>
        /// Gets all the notes for every used channel in the midi file  
        /// </summary>
        private void GetNotesForAllChannels()
        {
            for (int channel = 0; channel < 16; channel++)
            {
                notesForAllChannels[channel].Clear();
                
                if (!a_UsedChannels[channel]) 
                    continue;

                Dictionary<int, long> d_NoteOnTimes = new Dictionary<int, long>();

                for (int i = 0; i < midiEventCollection[a_ExistingChannelOrder[channel]].Count; i++) 
                {
                    MidiEvent midiEvent = midiEventCollection[a_ExistingChannelOrder[channel]][i];

                    if (midiEvent.CommandCode == MidiCommandCode.NoteOff ||
                        midiEvent.CommandCode == MidiCommandCode.NoteOn && ((NoteOnEvent)midiEvent).Velocity == 0) {
                        NoteEvent noteOff = (NoteEvent)midiEvent;
                        long noteOnTime;
                        if (d_NoteOnTimes.TryGetValue(noteOff.NoteNumber, out noteOnTime)) {
                            notesForAllChannels[channel].Add(new Note(noteOff.NoteNumber, noteOnTime, ActualTime(noteOff.AbsoluteTime)));
                            d_NoteOnTimes.Remove(noteOff.NoteNumber);
                        }
                        else {
                            debugConsole.AddText("Error: the NoteOff command at " + noteOff.AbsoluteTime + " does not match a previous NoteOn command");
                        }
                    }
                    else if (midiEvent.CommandCode == MidiCommandCode.NoteOn) {
                        NoteOnEvent noteOn = (NoteOnEvent)midiEvent;
                        try {
                            d_NoteOnTimes.Add(noteOn.NoteNumber, ActualTime(noteOn.AbsoluteTime));
                        }
                        catch (ArgumentException e) {
                            debugConsole.AddText("Error: an event with NoteNumber " + noteOn.NoteNumber + " already exists");
                        }
                    }
                    else if (midiEvent.CommandCode == MidiCommandCode.MetaEvent && ((MetaEvent)midiEvent).MetaEventType == MetaEventType.EndTrack) {
                        i_EndTime = ActualTime(midiEvent.AbsoluteTime);
                        break;
                    }
                }

                if (d_NoteOnTimes.Count != 0) {
                    debugConsole.AddText("Error: there are still " + d_NoteOnTimes.Count + " NoteOn events for which there were no NoteOff event");
                }
            }
        }
    }
}