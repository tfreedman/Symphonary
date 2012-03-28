using System;
using System.Collections;
using System.ComponentModel;
using Sanford.Collections;
using Sanford.Multimedia;
using Sanford.Multimedia.Midi;
using Sanford.Multimedia.Timers;
using Sanford.Threading;

namespace Symphonary
{
    public class MidiPlayer
    {
        private bool b_FinishedLoading = false;
        //private bool b_MuteOtherTracks = false;
        private bool b_Playing = false;
        private bool b_ProgramClosing = false;
        
        private ChannelStopper channelStopper = new ChannelStopper();
        private OutputDevice outputDevice = new OutputDevice(0);
        
        private Sequence sequence = new Sequence();
        public Sequence Sequence
        {
            get { return sequence; }
        }

        private Sequencer sequencer = new Sequencer();
        public Sequencer Sequencer
        {
            get { return sequencer; }
        }

        private int i_PersistentChannel = -1;
        private int i_NumChannelNotesPlayed = 0;
        //public List<int> l_CurrentPlayingChannelNotes = new List<int>();
        
        // lame hack is lame
        public ArrayList al_CurrentPlayingChannelNotes = new ArrayList();

        private EventHandler<ChannelMessageEventArgs> extHandleChannelMessagePlayed;
        private EventHandler extHandlePlayingCompleted;

        public bool IsInPreviewMode { get; private set; }

        /// <summary>
        /// Constructor for the class. Several additional event handlers are hooked up, these are for driving changes in the UI
        /// </summary>
        /// <param name="s_Filename">directory of the MIDI file to play</param>
        /// <param name="extHandleLoadProgressChanged">external event handler for load progress changed</param>
        /// <param name="extHandleLoadCompleted">external event handler for load completed</param>
        /// <param name="extHandleChannelMessagePlayed">external event handler for channel message played</param>
        /// <param name="extHandlePlayingCompleted">external event handler for loading completed</param>
        public MidiPlayer(string s_Filename,
            ProgressChangedEventHandler extHandleLoadProgressChanged,
            EventHandler<AsyncCompletedEventArgs> extHandleLoadCompleted,
            EventHandler<ChannelMessageEventArgs> extHandleChannelMessagePlayed,
            EventHandler extHandlePlayingCompleted)
        {
            sequence.Format = 1;
            sequence.LoadProgressChanged += extHandleLoadProgressChanged;
            sequence.LoadCompleted += HandleLoadCompleted;
            sequence.LoadCompleted += extHandleLoadCompleted;
            sequence.LoadAsync(s_Filename);
            sequencer.Position = 0;
            sequencer.Sequence = sequence;
            sequencer.ChannelMessagePlayed += HandleChannelMessagePlayed;
            sequencer.ChannelMessagePlayed += extHandleChannelMessagePlayed;
            sequencer.Chased += HandleChased;
            sequencer.Stopped += HandleStopped;
            sequencer.PlayingCompleted += HandlePlayingCompleted;
            sequencer.PlayingCompleted += extHandlePlayingCompleted;

            this.extHandleChannelMessagePlayed = extHandleChannelMessagePlayed;
            this.extHandlePlayingCompleted = extHandlePlayingCompleted;

            PlayPersistentChannel = true;
            PlayOtherChannels = true;
        }


        /// <summary>
        /// Gets or sets whether to play the persistent channel
        /// </summary>
        public bool PlayPersistentChannel { get; set; }

        private bool playOtherChannels;
        /// <summary>
        /// Gets or sets whether to play the other non-persistent channels
        /// </summary>
        public bool PlayOtherChannels
        {
            get { return playOtherChannels; }
            set
            {
                playOtherChannels = value;
                if (!playOtherChannels)
                {
                    MuteOtherChannels();
                }
            }
        }

        /// <summary>
        /// Property to indicate whether file has finished loading
        /// </summary>
        public bool IsFinishedLoading
        {
            get { return b_FinishedLoading; }
        }

        /// <summary>
        /// Property to indicate whether MIDI is playing
        /// </summary>
        public bool IsPlaying
        {
            get { return b_Playing; }
        }

        /// <summary>
        /// Sets or gets the persistent channel - the channel that the user is playing
        /// </summary>
        public int PersistentChannel
        {
            get { return i_PersistentChannel; }
            set
            {
                i_PersistentChannel = value;
                i_NumChannelNotesPlayed = 0;
            }
        }

        /// <summary>
        /// Gets the number of channel notes played
        /// </summary>
        public int NumChannelNotesPlayed
        {
            get { return i_NumChannelNotesPlayed; }
        }

        /// <summary>
        /// Gets the offset selected by the transposer 
        /// </summary>
        public int NoteOffset { get; set; }


        private bool playPersistentChannelStashed, playOtherChannelsStashed; 

        

        /// <summary>
        /// Configure player for preview mode, stashes settings for which channels are played,
        /// detaches event handlers related to gameplay, attaches preview completion event handler
        /// </summary>
        public void EnterPreviewMode(EventHandler extHandlePreviewPlayingCompleted)
        {
            IsInPreviewMode = true;
            StashChannelPlaySettings();
            UnHookExternalPlaybackEventHandles();
            PlayPersistentChannel = true;
            PlayOtherChannels = false;
            Sequencer.PlayingCompleted += extHandlePreviewPlayingCompleted;
        }

        /// <summary>
        /// Resets player configuration from preview mode, restores settings for which channels are played,
        /// re-attaches event handlers related to gameplay, detaches preview completion event handler
        /// </summary>
        public void ExitPreviewMode(EventHandler extHandlePreviewPlayingCompleted)
        {
            RecoverChannelPlaySettings();
            ReattachExternalPlaybackEventHandles();
            Sequencer.PlayingCompleted -= extHandlePreviewPlayingCompleted;
            IsInPreviewMode = false;
        }

        /// <summary>
        /// Used by preview mode
        /// </summary>
        private void StashChannelPlaySettings()
        {
            playPersistentChannelStashed = PlayPersistentChannel;
            playOtherChannelsStashed = PlayOtherChannels;
        }

        /// <summary>
        /// Used by preview mode
        /// </summary>
        private void RecoverChannelPlaySettings()
        {
            PlayPersistentChannel = playPersistentChannelStashed;
            PlayOtherChannels = playOtherChannelsStashed;
        }

        /// <summary>
        /// Used by preview mode
        /// </summary>
        private void UnHookExternalPlaybackEventHandles()
        {
            sequencer.ChannelMessagePlayed -= extHandleChannelMessagePlayed;
            sequencer.PlayingCompleted -= extHandlePlayingCompleted;
        }

        /// <summary>
        /// Used by preview mode
        /// </summary>
        private void ReattachExternalPlaybackEventHandles()
        {
            sequencer.ChannelMessagePlayed += extHandleChannelMessagePlayed;
            sequencer.PlayingCompleted += extHandlePlayingCompleted;
        }

        /// <summary>
        /// Mutes all channels in the MIDI file. This is used to get rid of "hanging" notes when the MIDI 
        /// file stops playing
        /// </summary>
        private void MuteAllChannels()
        {
            for (int i = 0; i < 16; i++)
            {
                outputDevice.Send(new ChannelMessage(ChannelCommand.Controller, i, (int)ControllerType.AllSoundOff, 0));
            }
        }

        /// <summary>
        /// Mutes all channels except the persistent channel
        /// </summary>
        private void MuteOtherChannels()
        {
            for (int i = 0; i < 16; i++)
            {
                if (i != i_PersistentChannel)
                {
                    // sending all-sounds-off command to channel 
                    outputDevice.Send(new ChannelMessage(ChannelCommand.Controller, i, (int)ControllerType.AllSoundOff, 0));
                }
            }
        }

        /// <summary>
        /// Unmute all channels other than the persistent channel. The persistent channel is not muted in the first place so
        /// this effectively unmutes all channels
        /// </summary>
        public void UnmuteOtherChannels()
        {
            PlayOtherChannels = true;
        }

        /// <summary>
        /// Sets the program closing flag value to true. This stops teh player from performing some opertions that may cause
        /// an exception when the program is closing
        /// </summary>
        public void OnClosingOperations()
        {
            b_ProgramClosing = true;
        }

        /// <summary>
        /// Disposes some resources when program has closed
        /// </summary>
        public void OnClosedOperations()
        {
            sequencer.Dispose();
            if (outputDevice != null)
                outputDevice.Dispose();
        }

        /// <summary>
        /// Play the MIDI file
        /// </summary>
        public void StartPlaying()
        {
            if (b_FinishedLoading)
            {
                sequencer.Start();
                b_Playing = true;
                //MessageBox.Show("playing");
            }
        }

        /// <summary>
        /// Stop playing the MIDI file
        /// </summary>
        public void StopPlaying()
        {
            b_Playing = false;
            MuteAllChannels();
            sequencer.Stop();
        }

        /// <summary>
        /// Pause playing the MIDI file
        /// </summary>
        public void ResumePlaying()
        {
            b_Playing = true;
            sequencer.Continue();
        }

        /// <summary>
        /// Internal event handler for channel message played
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleChannelMessagePlayed(object sender, ChannelMessageEventArgs e)
        {
            if (b_ProgramClosing) // don't try playing anything if program is closing
                return;

            // always play NoteOff messages
            if (e.Message.Command == ChannelCommand.NoteOn && e.Message.Data2 > 0)
            {
                if (!PlayOtherChannels && e.Message.MidiChannel != i_PersistentChannel)
                    return;

                if (!PlayPersistentChannel && e.Message.MidiChannel == i_PersistentChannel)
                    return;
            }

            if (e.Message.MidiChannel == i_PersistentChannel)
            {
                if (e.Message.Command == ChannelCommand.NoteOn && e.Message.Data2 > 0)
                {
                    i_NumChannelNotesPlayed++;
                    al_CurrentPlayingChannelNotes.Add(e.Message.Data1 + NoteOffset);
                    e = new ChannelMessageEventArgs(new ChannelMessage(e.Message.Command, e.Message.MidiChannel, e.Message.Data1 + NoteOffset, e.Message.Data2));
                }
                else if (e.Message.Command == ChannelCommand.NoteOn || e.Message.Command == ChannelCommand.NoteOff)
                {
                    al_CurrentPlayingChannelNotes.Remove(e.Message.Data1 + NoteOffset);
                    e = new ChannelMessageEventArgs(new ChannelMessage(e.Message.Command, e.Message.MidiChannel, e.Message.Data1 + NoteOffset, e.Message.Data2));
                }
            }

            outputDevice.Send(e.Message); 
        }

        /// <summary>
        /// Internal event handler for channel message chased. Not sure what this does
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleChased(object sender, ChasedEventArgs e)
        {
            // I don't know what exactly this handles
            foreach (ChannelMessage message in e.Messages)
            {
                outputDevice.Send(message);
            }
        }

        /// <summary>
        /// Internal event handler for load completed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleLoadCompleted(object sender, AsyncCompletedEventArgs e)
        {
            b_FinishedLoading = true;
        }

        /// <summary>
        /// Internal event handler for playing stopped
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleStopped(object sender, StoppedEventArgs e)
        {
            foreach (ChannelMessage message in e.Messages)
            {
                outputDevice.Send(message);
            }

            b_Playing = false;
        }

        /// <summary>
        /// Internal event handler for playing completed. Don't have anything that needed to be done
        /// by this yet
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandlePlayingCompleted(object sender, EventArgs e)
        {
            // left blank intentionally
        }
    } // end MidiPlayer
}