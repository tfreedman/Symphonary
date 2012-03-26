using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Symphonary
{
    public partial class NWGUI : Window
    {
        private bool isFullScreen = false;
        /// <summary>
        /// Event handler for clicking the "Full Screen" menu item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Fullscreen_Clicked(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized) {
                WindowStyle = WindowStyle.SingleBorderWindow;
                Topmost = false;
                WindowState = WindowState.Normal;
                ResizeMode = ResizeMode.CanResizeWithGrip;
                FullScreen.Header = "FULL SCREEN";
            }

            if (isFullScreen) {
                WindowStyle = WindowStyle.SingleBorderWindow;
                Topmost = false;
                WindowState = WindowState.Normal;
                ResizeMode = ResizeMode.CanResizeWithGrip;
                FullScreen.Header = "FULL SCREEN";
            }
            else {
                WindowStyle = WindowStyle.None;
                Topmost = true;
                WindowState = WindowState.Maximized;
                ResizeMode = ResizeMode.NoResize;
                FullScreen.Header = "UNDO FULL SCREEN";
            }
            isFullScreen = !isFullScreen;
            Size_Changed(this, e);
        }

        /// <summary>
        /// Event handler for clicking the "Pause" menu item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Pause_Clicked(object sender, RoutedEventArgs e)
        {
            if (midiPlayer == null)
                return;
            
            if (midiPlayer.IsPlaying)
            {
                midiPlayer.StopPlaying();
                pause.Header = "RESUME";
                playDuration.Stop();
                CompositionTarget.Rendering -= MoveCanvas;
                CompositionTarget.Rendering -= CanvasNotesScheduledAdder;
            }
            else
            {
                midiPlayer.ResumePlaying();
                pause.Header = "PAUSE";
                playDuration.Start();
                CompositionTarget.Rendering += MoveCanvas;
                CompositionTarget.Rendering += CanvasNotesScheduledAdder;
            }
        }


        /// <summary>
        /// Event handler for clicking the "Start" menu item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Start_Clicked(object sender, RoutedEventArgs e)
        {
            Instrument_Clicked(instrument);
            try {
                if (midiPlayer.IsPlaying) {
                    MessageBox.Show("The file is currently being played, please have it finish first.");
                    return;
                }

                if (i_Channel < 0) {
                    MessageBox.Show("Please select a channel to play first.");
                    return;
                }

                if (!midiPlayer.IsFinishedLoading) {
                    MessageBox.Show("Please wait for the MIDI file to finish loading");
                    return;
                }

                Stop.IsEnabled = true;
                Stop.Width = Double.NaN;
                Instruments.IsEnabled = false;
                Instruments.Width = 0;

                midiPlayer.PersistentChannel = i_Channel;

                score.ResetScore();
                
                InitializeCanvas();
                Background.Visibility = Visibility.Visible;
                ShowSubCanvas();
                pause.Visibility = Visibility.Visible;
                
                playDuration.Restart();
                CompositionTarget.Rendering += MoveCanvas;
                CompositionTarget.Rendering += CanvasNotesScheduledAdder;
                b_AnimationStarted = true;
                midiPlayer.StartPlaying();
                startTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

            } catch (NullReferenceException ex) {
                MessageBox.Show(
                    "Please load a MIDI file first! (or some other weird error occured, so read the proceeding message)");
                MessageBox.Show(ex.ToString());
            }
        }

        /// <summary>
        /// Event handler for clicking the "Back" button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Back_Clicked(object sender, RoutedEventArgs e)
        {
            muteSelectedChannel.Width = 0;
            muteSelectedChannel.Header = "";
            Instruments.Width = 0;
            Instruments.Header = "";
            listViewGrid.Visibility = Visibility.Hidden;
            MidiPlayerExitPreviewMode();
            midiPlayer.StopPlaying();
            normal.Visibility = Visibility.Visible;
        }


        /// <summary>
        /// Event handler for clicking the "Stop" menu item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Stop_Clicked(object sender, RoutedEventArgs e)
        {
            FPS.Header = "";
            debugConsole.ChangeText("");
            Stop.IsEnabled = false;
            Stop.Width = 0;
            ScoreDisplay.Width = 0;
            Instruments.IsEnabled = true;
            Instruments.Width = Double.NaN;
            muteSelectedChannel.Width = Double.NaN;
            muteSelectedChannel.Header = "MUTE";
            Background.Visibility = Visibility.Hidden;
            pause.Visibility = Visibility.Collapsed;
            HideCanvasChildren();

            Instrument_Clicked(instrument);

            try {
                //midiPlayer.OnClosingOperations();
                //midiPlayer.OnClosedOperations();
               
                listViewGrid.Visibility = Visibility.Visible;
                midiPlayer.StopPlaying();
                
                HideSubCanvas();
                ResetSubCanvas(false);

                b_AnimationStarted = false;
                CompositionTarget.Rendering -= MoveCanvas;
                CompositionTarget.Rendering -= CanvasNotesScheduledAdder;

            } catch (NullReferenceException) { }
        }

        /// <summary>
        /// Event handler for "Open" menu item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Open_Clicked(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "MIDI Files (*.mid)|*.mid|All Files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;

            if (openFileDialog.ShowDialog().Value) {
                try {
                    midiPlayer.OnClosingOperations();
                    midiPlayer.OnClosedOperations();
                } catch (NullReferenceException ex) { }

                HideSubCanvas();
                ResetSubCanvas(true);

                HideCanvasChildren();

                b_AnimationStarted = false;
                CompositionTarget.Rendering -= MoveCanvas;
                CompositionTarget.Rendering -= CanvasNotesScheduledAdder;
                normal.Visibility = Visibility.Hidden;
                try {
                    loadingScreen.Visibility = Visibility.Hidden;
                } catch (NullReferenceException ex) { }

                listViewGrid.Visibility = Visibility.Hidden;
                loadingScreen.Visibility = Visibility.Visible;
                i_Channel = -1;

                midiPlayer = new MidiPlayer(
                    openFileDialog.FileName, 
                    HandleMIDILoadProgressChanged, 
                    HandleMIDILoadCompleted,
                    HandleMIDIChannelMessagePlayed, 
                    HandleMIDIPlayingCompleted);
                //midiPlayerForPreview = new MidiPlayer(midiPlayer.Sequence, midiPlayer.Sequencer);

                midiPlayer.PlayOtherChannels = true;

                midiInfo.Refresh(openFileDialog.FileName, i_Channel);
                tb_listViewTitle.Text = midiInfo.Title;
            }
        }



        /// <summary>
        /// Event handler for "Debug" menu item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Debug_Clicked(object sender, RoutedEventArgs e)
        {
            //debugConsole = new DebugWindow();
            debugConsole.Show();
            //serialPortReadTimer.Start();

            try {
                
                /*
                debugConsole.textbox1.Text += midiInfo.s_Title + Environment.NewLine;

                debugConsole.textbox1.Text += midiInfo.i_DeltaTicksPerQuarterNote + Environment.NewLine;
                debugConsole.textbox1.Text += midiInfo.i_MicrosecondsPerQuarterNote + " " + midiInfo.d_MilisecondsPerQuarterNote + Environment.NewLine;
                debugConsole.textbox1.Text += midiInfo.d_MilisecondsPerTick + Environment.NewLine;
                */


                //foreach (NAudio.Midi.MidiEvent metadata in midiInfo.l_Metadata) {
                //    debugConsole.textbox1.Text += metadata.ToString();
                //}

                /*
                debugConsole.textbox1.Text += "-----" + Environment.NewLine;
                                 
                foreach (Note note in midiInfo.l_Notes) {
                    debugConsole.textbox1.Text += note.li_BeginTime + " " + note.li_EndTime + Environment.NewLine;
                }

                debugConsole.textbox1.Text += ">> " + midiInfo.i_EndTime + Environment.NewLine;
                 */

                //debugConsole.textbox1.Text += midiInfo.l_Notes.Count + Environment.NewLine;
                //debugConsole.textbox1.Text += s_SelectedSerialPort + Environment.NewLine;

                /*
                for (int i = 0; i < midiInfo.midiEventCollection[midiInfo.a_ExistingChannelOrder[i_Channel]].Count; i++) {
                    debugConsole.textbox1.Text += midiInfo.midiEventCollection[midiInfo.a_ExistingChannelOrder[i_Channel]][i].ToString() + Environment.NewLine;
                }*/

            } catch (NullReferenceException ex) { }
        }

        /// <summary>
        /// Event handler for "Select Channel" menu item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectChannel_Clicked(object sender, RoutedEventArgs e)
        {
            if (midiInfo == null) {
                MessageBox.Show("Please load a MIDI file first!");
                return;
            }

            if (midiPlayer.IsPlaying) {
                MessageBox.Show("The file is currently being played, please have it finish first.");
                return;
            }

            //channelSelector = new ChannelSelector(channelsListView);
            //channelSelector.Show();
        }

        /// <summary>
        /// Event handler for the "Instruments" menu item selectors
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Instrument_Clicked(object sender, RoutedEventArgs e)
        {
            int num = Convert.ToInt32(((MenuItem)sender).Tag);
            canv.Children.Remove(keyLine);
            WriteSettingsToFile(num);
            instrument = num;

            //MessageBox.Show(num.ToString());

            if (num == 30 || num == 35) {
                hInst = 1;
                //keyLine.Height = 400;
                //keyLine.Width = 53;
            }
            else {
                hInst = 0;
                keyLine.SetValue(Canvas.LeftProperty, 0.0);
                keyLine.SetValue(Canvas.BottomProperty, 20.0);
                Canvas.SetZIndex(keyLine, (int)90);
                //keyLine.Height = 3;
                //keyLine.Width = 1280;
            }

            // remove the fingering rectangles from the canvas right now, because after they are re-initialized we'll
            // lose track of the ones already on the canvas
            try {
                for (int i = 0; i < r_instrument.Length; i++) {
                    canv.Children.Remove(r_instrument[i]);
                    canv.Children.Remove(tb_instrument[i]);
                    canv.Children.Remove(r_separators[i]);
                }
                canv.Children.Remove(r_separators[r_separators.Length - 1]);
            } catch (NullReferenceException nr_e) { }

            if (num == 35 || num == 41) {
                r_instrument = new Border[4];
                tb_instrument = new TextBlock[4];
                r_separators = new Rectangle[5];
            }
            else if (num == 74) {
                r_instrument = new Border[11];
                tb_instrument = new TextBlock[11];
                r_separators = new Rectangle[12];
            }
            else if (num == 30) {
                r_instrument = new Border[6];
                tb_instrument = new TextBlock[6];
                r_separators = new Rectangle[7];
            }


            for (int i = 0; i < r_instrument.Length; i++) {
                r_instrument[i] = new Border{ };
                r_separators[i] = new Rectangle { };
                tb_instrument[i] = new TextBlock { };
                r_instrument[i].Visibility = Visibility.Hidden;
                r_separators[i].Visibility = Visibility.Hidden;
                tb_instrument[i].Visibility = Visibility.Hidden;
            }
            r_separators[r_separators.Length - 1] = new Rectangle { };
            r_separators[r_separators.Length - 1].Visibility = Visibility.Hidden;

            keyLine.Fill = new SolidColorBrush(Color.FromRgb(51, 51, 51));
            canv.Children.Add(keyLine);


            // this has been moved from updateFingeringDisplay --------------------

            // if violin
            if (num == 41) {
                int margin = 300;
                int padding = 30;
                for (int i = 0; i < r_instrument.Length; i++) {
                    tb_instrument[i].Height = 50;
                    tb_instrument[i].Width = 50;
                    r_instrument[i].Height = 50;
                    r_instrument[i].Width = 46;
                    tb_instrument[i].SetValue(Canvas.LeftProperty, (margin + ((i + 1) * (r_instrument[i].Width + (padding)))));
                    r_instrument[i].SetValue(Canvas.LeftProperty, (margin + ((i + 1) * (r_instrument[i].Width + (padding)))));

                    tb_instrument[i].SetValue(Canvas.TopProperty, (double)630);
                    r_instrument[i].SetValue(Canvas.TopProperty, (double)630);

                    switch (i) {
                        case 0:
                            r_instrument[i].Background = new SolidColorBrush(tint[6]);
                            r_instrument[i].BorderBrush = new SolidColorBrush(border[6]);
                            break;
                        case 1:
                            r_instrument[i].Background = new SolidColorBrush(tint[2]);
                            r_instrument[i].BorderBrush = new SolidColorBrush(border[2]);
                            break;
                        case 2:
                            r_instrument[i].Background = new SolidColorBrush(tint[0]);
                            r_instrument[i].BorderBrush = new SolidColorBrush(border[0]);
                            break;
                        case 3:
                            r_instrument[i].Background = new SolidColorBrush(tint[10]);
                            r_instrument[i].BorderBrush = new SolidColorBrush(border[10]);
                            break;
                    }

                }
            } // end if (num == 41)

            // if guitar
            else if (num >= 25 && num <= 32) {
                int margin = 170;
                int padding = 10;
                for (int i = 0; i < r_instrument.Length; i++) {
                    tb_instrument[i].Height = 50;
                    tb_instrument[i].Width = 50;
                    r_instrument[i].Height = 46;
                    r_instrument[i].Width = 60;
                    
                    tb_instrument[i].SetValue(Canvas.TopProperty, (double)(margin + (i * (r_instrument[i].Height + padding)) - 7));
                    tb_instrument[i].SetValue(Canvas.LeftProperty, (double)5);
                    r_instrument[i].SetValue(Canvas.TopProperty, (double)(margin + (i * (r_instrument[i].Height + padding))));

                    switch (i) {
                        case 5:
                            r_instrument[i].Background = new SolidColorBrush(tint[6]);
                            r_instrument[i].BorderBrush = new SolidColorBrush(border[6]);
                            tb_instrument[i].Foreground = new SolidColorBrush(border[6]);
                            break;
                        case 4:
                            r_instrument[i].Background = new SolidColorBrush(tint[2]);
                            r_instrument[i].BorderBrush = new SolidColorBrush(border[2]);
                            tb_instrument[i].Foreground = new SolidColorBrush(border[2]);
                            break;
                        case 3:
                            r_instrument[i].Background = new SolidColorBrush(tint[0]);
                            r_instrument[i].BorderBrush = new SolidColorBrush(border[0]);
                            tb_instrument[i].Foreground = new SolidColorBrush(border[0]);
                            break;
                        case 2:
                            r_instrument[i].Background = new SolidColorBrush(tint[10]);
                            r_instrument[i].BorderBrush = new SolidColorBrush(border[10]);
                            tb_instrument[i].Foreground = new SolidColorBrush(border[10]);
                            break;
                        case 1:
                            r_instrument[i].Background = new SolidColorBrush(tint[9]);
                            r_instrument[i].BorderBrush = new SolidColorBrush(border[9]);
                            tb_instrument[i].Foreground = new SolidColorBrush(border[9]);
                            break;
                        case 0:
                            r_instrument[i].Background = new SolidColorBrush(tint[8]);
                            r_instrument[i].BorderBrush = new SolidColorBrush(border[8]);
                            tb_instrument[i].Foreground = new SolidColorBrush(border[8]);
                            break;
                    }
                }
                for (int i = 0; i < r_separators.Length; i++) {
                    r_separators[i].Height = 8;
                    r_separators[i].Width = 8;
                    r_separators[i].Fill = new SolidColorBrush(Color.FromRgb(250, 250, 250));
                    r_separators[i].SetValue(Canvas.TopProperty, (double)(margin + (i * (r_instrument[1].Height + padding))) - 9);
                }
            } // end else if (num >= 25 && num <= 32) 
            // -------------------------------------------------------

            for (int i = 0; i < r_instrument.Length; i++) {
                canv.Children.Add(tb_instrument[i]);
                canv.Children.Add(r_instrument[i]);
                canv.Children.Add(r_separators[i]);
                Canvas.SetZIndex(tb_instrument[i], (int)99);
                Canvas.SetZIndex(r_instrument[i], (int)98);
            }
            canv.Children.Add(r_separators[r_separators.Length - 1]);
        }

        /// <summary>
        /// Event handler for "Mute Selected Channel" menu item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MuteSelectedChannel_Clicked(object sender, RoutedEventArgs e)
        {
            try {
                midiPlayer.PlayPersistentChannel = !(muteSelectedChannel.IsChecked);
            } catch (NullReferenceException ex) { }
        }

        /// <summary>
        /// Event handler for the "About" menu item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void About_Clicked(object sender, RoutedEventArgs e)
        {
            if (aboutScreen.Visibility == Visibility.Visible) {
                aboutScreen.Visibility = Visibility.Hidden;
                normal.Visibility = Visibility.Visible;
            }
            else {
                aboutScreen.Visibility = Visibility.Visible;
                normal.Visibility = Visibility.Hidden;
            }
        }

    }
}