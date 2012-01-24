﻿using Microsoft.Win32;
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
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
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
using System.Threading;
using System.Windows.Threading;

namespace Symphonary {
    public class Score {
        public int i_NumNotesScored = 0;
        public string s_CurrentFingering = string.Empty;

        NoteMatcher noteMatcher = new NoteMatcher();

        public void resetScore() {
            i_NumNotesScored = 0;
        }

        public void updateScore(ref MidiPlayer midiPlayer) {
            if (midiPlayer == null)
                return;

            // this is a hack, we shouldn't be modifying this list in the MidiPlayer, it should be used for
            // informational purposes
            for (int i = 0; i < midiPlayer.al_CurrentPlayingChannelNotes.Count; i++) {
                if (noteMatcher.noteMatches(s_CurrentFingering, (int)midiPlayer.al_CurrentPlayingChannelNotes[i])) {
                    i_NumNotesScored++;
                    midiPlayer.al_CurrentPlayingChannelNotes.RemoveAt(i);
                    break;
                }
            }
        }

        public string scoreGrade(int i_NumChannelNotesPlayed) {
            try {
                if (i_NumChannelNotesPlayed == 0) {
                    return "...";
                }
            } catch (NullReferenceException e) {
                return "...";
            }

            double percentage = ((double)i_NumNotesScored / (double)(i_NumChannelNotesPlayed)) * 100;

            if (percentage >= 90)
                return "A+";
            else if (percentage >= 85)
                return "A";
            else if (percentage >= 80)
                return "A-";
            else if (percentage >= 76)
                return "B+";
            else if (percentage >= 73)
                return "B";
            else if (percentage >= 70)
                return "B-";
            else if (percentage >= 67)
                return "C+";
            else if (percentage >= 63)
                return "C";
            else if (percentage >= 60)
                return "C-";
            else if (percentage >= 57)
                return "D+";
            else if (percentage >= 53)
                return "D";
            else if (percentage >= 50)
                return "D-";
            else
                return "F";
        }
    }

    public partial class NWGUI : Window {

        bool b_AnimationStarted = false;
        int i_CanvasMoveDirection = 1;

        double i_InitialCanvasPosY;

        MainWindow debugConsole;
        ChannelSelector channelSelector;
        SerialPortSelector serialPortSelector;
        LoadingScreen loadingScreen;
        long starterTime = 0;
        int i_Channel = -1;
        double scrollSpeed = 2.00000;
        double multiplier = 1;
        MidiPlayer midiPlayer;
        MidiInfo midiInfo;

        Rectangle[] r_instrument;
        TextBlock[] tb_instrument;
        
        //int i_NumNotesPlayed;
        int i_NumNotesScored;
        string s_ScoreGrade;

        Color[] color = new Color[11] { 
                Color.FromRgb(220, 42, 62), 
                Color.FromRgb(67, 66, 64), 
                Color.FromRgb(250, 181, 65), 
                Color.FromRgb(0, 100, 100), 
                Color.FromRgb(253, 251, 230), 
                Color.FromRgb(254, 135, 33), 
                Color.FromRgb(144, 187, 69), 
                Color.FromRgb(231, 107, 117), 
                Color.FromRgb(120, 132, 161), 
                Color.FromRgb(82, 44, 95), 
                Color.FromRgb(26, 98, 179) };
        Color[] border = new Color[11] { 
                Color.FromRgb(189, 36, 54), 
                Color.FromRgb(31, 30, 29), 
                Color.FromRgb(227, 165, 59), 
                Color.FromRgb(0, 43, 43), 
                Color.FromRgb(191, 190, 174), 
                Color.FromRgb(220, 123, 37), 
                Color.FromRgb(114, 148, 55), 
                Color.FromRgb(194, 89, 98), 
                Color.FromRgb(94, 102, 125), 
                Color.FromRgb(26, 14, 31), 
                Color.FromRgb(24, 82, 148) };

        string s_SelectedSerialPort = string.Empty;
        SerialPort serialPort = new SerialPort();
        Thread serialPortReadThread;
        int hInst = 1;
        int instrument = 0;

        Score score = new Score();
        public NWGUI() {
            InitializeComponent();
            Stop.IsEnabled = false;
            Start.IsEnabled = false;

            instrument = readSettingsFromFile();
            if (instrument != 0) {
                instrument_Clicked(instrument);
                foreach (MenuItem item in Instruments.Items)
                    if (instrument == Convert.ToInt32(item.Tag))
                        item.IsChecked = true;
            }

                        i_InitialCanvasPosY = (double)(subcanv.GetValue(Canvas.TopProperty));

            r_HeaderBackground.Background = new SolidColorBrush(Color.FromRgb(51, 51, 51));

            tb_SongTitle.Height = 50;
            tb_SongTitle.Foreground = new SolidColorBrush(Colors.White);
            tb_SongTitle.FontStyle = FontStyles.Italic;
            tb_SongTitle.FontSize = 30;
            tb_SongTitle.TextAlignment = TextAlignment.Left;

            tb_ScoreDisplay.Height = 50;
            tb_ScoreDisplay.Foreground = new SolidColorBrush(Colors.White);
            tb_ScoreDisplay.FontSize = 30;
            tb_ScoreDisplay.TextAlignment = TextAlignment.Right;
            hideCanvasChildren();

            serialPort.ReadTimeout = 5;
            serialPortReadThread = new Thread(new ThreadStart(getSerialData));
            serialPortReadThread.Start();
        }

        bool isFullScreen = false;
        private void fullscreen_Clicked(object sender, RoutedEventArgs e) {

            if (isFullScreen) {
                WindowStyle = WindowStyle.SingleBorderWindow;
                Topmost = false;
                WindowState = WindowState.Normal;
            }
            else {
                WindowStyle = WindowStyle.None;
                Topmost = true;
                WindowState = WindowState.Maximized;
            }
            size_Changed(this, e);
            isFullScreen = !isFullScreen;
        }

        private void start_Clicked(object sender, RoutedEventArgs e) {
            instrument_Clicked(instrument);
            try {
                if (midiPlayer.isPlaying()) {
                    MessageBox.Show("The file is currently being played, please have it finish first.");
                    return;
                }

                if (i_Channel < 0) {
                    MessageBox.Show("Please select a channel to play first.");
                    return;
                }

                if (!midiPlayer.isFinishedLoading()) {
                    MessageBox.Show("Please wait for the MIDI file to finish loading");
                    return;
                }

                Start.IsEnabled = false;
                Stop.IsEnabled = true;
                Instruments.IsEnabled = false;

                midiPlayer.setPersistentChannel(i_Channel);

                score.resetScore();
                initializeCanvas();

                showSubCanvas();
                CompositionTarget.Rendering += new EventHandler(moveCanvas);
                b_AnimationStarted = true;
                midiPlayer.startPlaying();
                starterTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

            } catch (NullReferenceException ex) {
                MessageBox.Show("Please load a MIDI file first! (or some other weird error occured, so read the proceeding message)");
                MessageBox.Show(ex.ToString());
            }

        }


        private void stop_Clicked(object sender, RoutedEventArgs e) {
            Start.IsEnabled = true;
            Stop.IsEnabled = false;
            Instruments.IsEnabled = true;
            instrument_Clicked(instrument);
            hideCanvasChildren();

            try {
                //midiPlayer.OnClosingOperations();
                //midiPlayer.OnClosedOperations();
                midiPlayer.stopPlaying();

                hideSubCanvas();
                resetSubCanvas(false);

                b_AnimationStarted = false;
                CompositionTarget.Rendering -= new EventHandler(moveCanvas);

            } catch (NullReferenceException ex) { }
        }

        private void drawGridLines(long endTime, int bpm, int count) {
            int runner = 0;
            for (double i = 0; i < (endTime + 1000); i = i + (1.775 * multiplier) + (bpm / count)) {
                Rectangle r = new Rectangle();
                if (hInst == 0) {
                    r.Width = 1280;
                    if (runner % count == 0) {
                        r.Fill = new SolidColorBrush(Color.FromRgb(221, 221, 221));
                        r.SetValue(Canvas.TopProperty, (double)-i * scrollSpeed);
                        r.Height = 3;
                    }
                    else {
                        r.Fill = new SolidColorBrush(Color.FromRgb(255, 222, 222));
                        r.SetValue(Canvas.TopProperty, (double)-i * scrollSpeed);
                        r.Height = 1;
                    }
                }
                else {
                    r.Height = 700;
                    if (runner % count == 0) {
                        r.Fill = new SolidColorBrush(Color.FromRgb(221, 221, 221));
                        r.SetValue(Canvas.LeftProperty, (double)i * scrollSpeed);
                        r.Width = 3;
                    }
                    else {
                        r.Fill = new SolidColorBrush(Color.FromRgb(255, 222, 222));
                        r.SetValue(Canvas.LeftProperty, (double)i * scrollSpeed);
                        r.Width = 1;
                    }
                }
                gridlines.Children.Add(r);
                runner++;
            }
        }

        private void fingering(int note, int instrument, long startTime, long endTime) {
            string noteString = "";
            TextBlock textBlock = new TextBlock();
            textBlock.FontSize = 26;
            textBlock.FontWeight = FontWeights.Bold;
            textBlock.TextAlignment = TextAlignment.Center;
            textBlock.Foreground = new SolidColorBrush(Colors.White);
            RoutedEventArgs e = new RoutedEventArgs();
            size_Changed(this, e);
            Canvas.SetZIndex(textBlock, (int)99);
            Rectangle r = new Rectangle();
            r.StrokeThickness = 2;

            if (instrument == 41) { //VIOLIN
                int margin = 300;
                int padding = 30;
                textBlock.Height = 50;
                textBlock.Width = 50;
                r.Width = 46;

                int[,] violin = new int[4, 2] { { 55, 61 }, { 62, 68 }, { 69, 75 }, { 76, 83 } };
                int[] ctrl_violin = { 0, 6, 2, 0, 10 };
                for (int i = violin.GetLength(0); i > 0; i--) {
                    if (note >= violin[i - 1, 0] && note <= violin[i - 1, 1]) {
                        textBlock.Text = Convert.ToString(note - violin[i - 1, 0]);
                        r.Fill = new SolidColorBrush(color[ctrl_violin[i]]);
                        r.Stroke = new SolidColorBrush(border[ctrl_violin[i]]);
                        textBlock.SetValue(Canvas.LeftProperty, (margin + (i * (r.Width + padding))));
                        r.SetValue(Canvas.LeftProperty, (double)(margin + (i * (r.Width + padding))));
                    }
                }
                r.Height = (endTime - startTime) * multiplier;
                textBlock.SetValue(Canvas.TopProperty, (double)((-1 * startTime * multiplier) - 35));
                r.SetValue(Canvas.BottomProperty, (double)(startTime) * multiplier);
                subcanv.Children.Add(r);
                subcanv.Children.Add(textBlock);
            }
            else if (instrument >= 25 && instrument <= 32) { //GUITAR
                int margin = 220;
                int padding = 20;
                textBlock.Height = 50;
                textBlock.Width = 50;
                r.Height = 46;
                int[,] guitar = new int[6, 2] { { 64, 68 }, { 59, 63 }, { 55, 58 }, { 50, 54 }, { 45, 49 }, { 40, 44 } };
                int[] ctrl_guitar = {1,8,9,10,0,2,6};
                for (int i = guitar.GetLength(0); i > 0; i--) {
                    if (note >= guitar[i-1,0] && note <= guitar[i-1,1]) {
                        textBlock.Text = Convert.ToString(note - guitar[i - 1, 0]);
                        r.Fill = new SolidColorBrush(color[ctrl_guitar[i]]);
                        r.Stroke = new SolidColorBrush(border[ctrl_guitar[i]]);
                        textBlock.SetValue(Canvas.TopProperty, (5 + margin + ((i-1) * (r.Height + padding))));
                        r.SetValue(Canvas.TopProperty, (double)(margin + ((i-1)* (r.Height + padding))));
                    }
                }
                r.Width = (endTime - startTime) * multiplier;
                textBlock.SetValue(Canvas.LeftProperty, (double)((startTime * multiplier) - 14));
                r.SetValue(Canvas.LeftProperty, (double)(startTime) * multiplier);
                subcanv.Children.Add(r);
                subcanv.Children.Add(textBlock);
            }
            else if (instrument >= 33 && instrument <= 40) { //BASS
                int margin = 200;
                int padding = 20;
                textBlock.Height = 50;
                textBlock.Width = 50;
                r.Height = 46;

                int[,] bass = new int[4, 2] { { 28, 32 }, { 33, 37 }, { 38, 42 }, { 43, 47 }};
                int[] ctrl_bass = { 1, 6, 2, 0, 10};
                for (int i = bass.GetLength(0); i > 0; i--) {
                    if (note >= bass[i - 1, 0] && note <= bass[i - 1, 1]) {
                        textBlock.Text = Convert.ToString(note - bass[i - 1, 0]);
                        r.Fill = new SolidColorBrush(color[ctrl_bass[i]]);
                        r.Stroke = new SolidColorBrush(border[ctrl_bass[i]]);
                        textBlock.SetValue(Canvas.TopProperty, (5 + margin + ((i - 1) * (r.Height + padding))));
                        r.SetValue(Canvas.TopProperty, (double)(margin + ((i - 1) * (r.Height + padding))));
                    }
                }
                r.Width = (endTime - startTime) * multiplier;
                textBlock.SetValue(Canvas.LeftProperty, (double)((startTime * multiplier) - 14));
                r.SetValue(Canvas.LeftProperty, (double)(startTime) * multiplier);
                subcanv.Children.Add(r);
                subcanv.Children.Add(textBlock);
            }
            else if (instrument == 74) { //FLUTE
                int margin = 40;
                int padding = 30;
                if (note == 50)
                    noteString = "01111001110";
                else if (note == 51)
                    noteString = "01111001111";
                else if (note == 52 || note == 64)
                    noteString = "01111001101";
                else if (note == 53)
                    noteString = "01111001001";
                else if (note == 54 || note == 66)
                    noteString = "01111000011";
                else if (note == 55 || note == 67)
                    noteString = "01111000001";
                else if (note == 56 || note == 68)
                    noteString = "01111010001";
                else if (note == 57 || note == 69)
                    noteString = "01110000001";
                else if (note == 58)
                    noteString = "01100001001";
                else if (note == 59 || note == 71)
                    noteString = "01100000001";
                else if (note == 60 || note == 72)
                    noteString = "00100000001";
                else if (note == 61 || note == 73)
                    noteString = "00000000001";
                else if (note == 62)
                    noteString = "01011001110";
                else if (note == 63)
                    noteString = "01011001111";
                else if (note == 70)
                    noteString = "10100000001";
                else if (note == 74)
                    noteString = "01011000001";
                else if (note == 75)
                    noteString = "01111011111";
                else if (note == 76)
                    noteString = "01110001101";
                else if (note == 77)
                    noteString = "01101001001";
                else if (note == 78)
                    noteString = "01101000011";
                else if (note == 79)
                    noteString = "00111000001";
                else if (note == 80)
                    noteString = "00011010001";
                else if (note == 81)
                    noteString = "01010001001";

                Rectangle[] rect = new Rectangle[noteString.Length];
                for (int i = 0; i < noteString.Length; i++) {
                    rect[i] = new Rectangle();
                }

                int rWidth = 46;
                for (int i = 0; i < noteString.Length; i++) {
                    if (i == 0)
                        rect[i].SetValue(Canvas.LeftProperty, (double)(margin + rWidth));
                    else
                        rect[i].SetValue(Canvas.LeftProperty, (double)(margin + ((i + 1) * (rWidth + padding))));
                    rect[i].Width = rWidth;
                    if (noteString[i] == '1' && noteString.Length > 3) {
                        rect[i].Fill = new SolidColorBrush(color[i]);
                        rect[i].Stroke = new SolidColorBrush(border[i]);
                    }
                    rect[i].StrokeThickness = 2;
                    rect[i].Height = (endTime - startTime) * multiplier;
                    rect[i].SetValue(Canvas.BottomProperty, (double)(startTime) * multiplier);
                    subcanv.Children.Add(rect[i]);
                }
            }
        }

        private void exit_Clicked(object sender, RoutedEventArgs e) {
            Application.Current.Shutdown();
        }

        private void open_Clicked(object sender, RoutedEventArgs e) {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "MIDI Files (*.mid)|*.mid|All Files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;

            if (openFileDialog.ShowDialog().Value) {
                try {
                    midiPlayer.OnClosingOperations();
                    midiPlayer.OnClosedOperations();
                } catch (NullReferenceException ex) { }

                hideSubCanvas();
                resetSubCanvas(true);

                hideCanvasChildren();

                b_AnimationStarted = false;
                CompositionTarget.Rendering -= new EventHandler(moveCanvas);

                try {
                    loadingScreen.Close();
                } catch (NullReferenceException ex) { }

                loadingScreen = new LoadingScreen();
                loadingScreen.Show();
                i_Channel = -1;

                midiPlayer = new MidiPlayer(openFileDialog.FileName, handleMIDILoadProgressChanged,
                    handleMIDILoadCompleted, handleMIDIChannelMessagePlayed, handleMIDIPlayingCompleted);

                //midiPlayer.b_PlayPersistentChannel = true; // make it so that the user's instrument's notes don't play

                midiInfo = new MidiInfo(openFileDialog.FileName, i_Channel);
                Start.IsEnabled = true;
            }
        }

        private void debug_Clicked(object sender, RoutedEventArgs e) {
            debugConsole = new MainWindow();
            debugConsole.Show();
            //serialPortReadTimer.Start();

            try {
                debugConsole.textbox1.Text += midiInfo.l_Notes.Count + Environment.NewLine;

                /*
                debugConsole.textbox1.Text += midiInfo.s_Title + Environment.NewLine;

                debugConsole.textbox1.Text += midiInfo.i_DeltaTicksPerQuarterNote + Environment.NewLine;
                debugConsole.textbox1.Text += midiInfo.i_MicrosecondsPerQuarterNote + " " + midiInfo.d_MilisecondsPerQuarterNote + Environment.NewLine;
                debugConsole.textbox1.Text += midiInfo.d_MilisecondsPerTick + Environment.NewLine;
                */


                foreach (NAudio.Midi.MidiEvent metadata in midiInfo.l_Metadata) {
                    debugConsole.textbox1.Text += metadata.ToString();
                }

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

        private void selectChannel_Clicked(object sender, RoutedEventArgs e) {
            if (midiInfo == null) {
                MessageBox.Show("Please load a MIDI file first!");
                return;
            }

            if (midiPlayer.isPlaying()) {
                MessageBox.Show("The file is currently being played, please have it finish first.");
                return;
            }

            channelSelector = new ChannelSelector(ref midiInfo, i_Channel, channelSelectorOkClicked);
            channelSelector.Show();
        }

        private void channelSelectorOkClicked(object sender, RoutedEventArgs e) {
            i_Channel = channelSelector.getSelectedChannel();
            midiInfo.loadChannelNotes(i_Channel);
            midiPlayer.setPersistentChannel(i_Channel);
            hideSubCanvas();
            resetSubCanvas(true);
            initializeSubCanvas();
            channelSelector.Close();
        }

        private void instrument_Clicked(object sender, RoutedEventArgs e) {
            int num = Convert.ToInt32(((MenuItem)sender).Tag);
            grid.Children.Remove(r_KeyLine);
            writeSettingsToFile(num);
            instrument = num;

            //MessageBox.Show(num.ToString());

            if (num == 30 || num == 35) {
                hInst = 1;
                r_KeyLine.Height = 700;
                r_KeyLine.Width = 3;
            }
            else {
                hInst = 0;
                r_KeyLine.SetValue(Canvas.LeftProperty, 0.0);
                r_KeyLine.SetValue(Canvas.BottomProperty, 20.0);
                Canvas.SetZIndex(r_KeyLine, (int)97);
                r_KeyLine.Height = 3;
                r_KeyLine.Width = 1280;
            }

            // remove the fingering rectangles from the canvas right now, because after they are re-initialized we'll
            // lose track of the ones already on the canvas
            try {
                for (int i = 0; i < r_instrument.Length; i++) {
                    canv.Children.Remove(r_instrument[i]);
                }
            }
            catch (NullReferenceException nr_e) { }
            
            if (num == 35 || num == 41) {
                r_instrument = new Rectangle[4];
                tb_instrument = new TextBlock[4];
            }
            else if (num == 74) {
                r_instrument = new Rectangle[11];
                tb_instrument = new TextBlock[11];
            }
            else if (num == 30) {
                r_instrument = new Rectangle[6];
                tb_instrument = new TextBlock[6];
            }


            for (int i = 0; i < r_instrument.Length; i++) {
                r_instrument[i] = new Rectangle{};
                tb_instrument[i] = new TextBlock{};
                r_instrument[i].Visibility = Visibility.Hidden;
                tb_instrument[i].Visibility = Visibility.Hidden;
            }
            r_KeyLine.Fill = new SolidColorBrush(Color.FromRgb(51, 51, 51));
            grid.Children.Add(r_KeyLine);


            // this has been moved from updateFingeringDisplay --------------------
            if (num == 41) { // if violin
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
                            r_instrument[i].Fill = new SolidColorBrush(color[6]);
                            r_instrument[i].Stroke = new SolidColorBrush(border[6]);
                            break;
                        case 1:
                            r_instrument[i].Fill = new SolidColorBrush(color[2]);
                            r_instrument[i].Stroke = new SolidColorBrush(border[2]);
                            break;
                        case 2:
                            r_instrument[i].Fill = new SolidColorBrush(color[0]);
                            r_instrument[i].Stroke = new SolidColorBrush(border[0]);
                            break;
                        case 3:
                            r_instrument[i].Fill = new SolidColorBrush(color[10]);
                            r_instrument[i].Stroke = new SolidColorBrush(border[10]);
                            break;
                    }

                }
            } // end if (num == 41)
            else if (num >= 25 && num <= 32) {
                int margin = 220;
                int padding = 20;
                for (int i = 0; i < r_instrument.Length; i++) {
                    tb_instrument[i].Height = 50;
                    tb_instrument[i].Width = 50;
                    r_instrument[i].Height = 46;
                    r_instrument[i].Width = 50;

                    tb_instrument[i].SetValue(Canvas.TopProperty, (5 + margin + (i * r_instrument[i].Height * padding)));
                    r_instrument[i].SetValue(Canvas.TopProperty, (double)(margin + (i * (r_instrument[i].Height + padding))));

                    switch (i) {
                        case 5:
                            r_instrument[i].Fill = new SolidColorBrush(color[6]);
                            r_instrument[i].Stroke = new SolidColorBrush(border[6]);
                            break;
                        case 4:
                            r_instrument[i].Fill = new SolidColorBrush(color[2]);
                            r_instrument[i].Stroke = new SolidColorBrush(border[2]);
                            break;
                        case 3:
                            r_instrument[i].Fill = new SolidColorBrush(color[0]);
                            r_instrument[i].Stroke = new SolidColorBrush(border[0]);
                            break;
                        case 2:
                            r_instrument[i].Fill = new SolidColorBrush(color[10]);
                            r_instrument[i].Stroke = new SolidColorBrush(border[10]);
                            break;
                        case 1:
                            r_instrument[i].Fill = new SolidColorBrush(color[9]);
                            r_instrument[i].Stroke = new SolidColorBrush(border[9]);
                            break;
                        case 0:
                            r_instrument[i].Fill = new SolidColorBrush(color[8]);
                            r_instrument[i].Stroke = new SolidColorBrush(border[8]);
                            break;
                    }
                }
            } // end else if (num >= 25 && num <= 32) 
            // -------------------------------------------------------


            // this has been moved from the NWGUI constructor --------------------------
            for (int i = 0; i < r_instrument.Length; i++) {
                canv.Children.Add(tb_instrument[i]);
                canv.Children.Add(r_instrument[i]);
                Canvas.SetZIndex(tb_instrument[i], (int)99);
                Canvas.SetZIndex(r_instrument[i], (int)98);
            }
            // ------------------------------------------------------
        }

        private void instrument_Clicked(int num) {
            RoutedEventArgs e = new RoutedEventArgs { };
            MenuItem sender = new MenuItem { Tag = num };
            instrument_Clicked(sender, e);
        }

        private void writeSettingsToFile(int inst) {
            TextWriter tw = new StreamWriter("settings.ini");
            tw.WriteLine(inst);
            tw.Close();
        }

        private int readSettingsFromFile() {
            int returner = 0;
            try {
                TextReader tr = new StreamReader("settings.ini");
                returner = Convert.ToInt32(tr.ReadLine());
                tr.Close();
            } catch (Exception e) { }
            return returner;
        }

        private void selectSerialPort_Clicked(object sender, RoutedEventArgs e) {
            // if the serial port is not closed, opening once again (SerialPortSelector opens ports for testing) 
            // will cause an exception
            serialPort.Close();

            serialPortSelector = new SerialPortSelector(s_SelectedSerialPort, serialPortSelectorOkClicked);
            serialPortSelector.Show();
        }

        private void serialPortSelectorOkClicked(object sender, RoutedEventArgs e) {
            s_SelectedSerialPort = serialPortSelector.getSelectedSerialPort();
            serialPortSelector.Close();
            serialPort.Close();

            if (s_SelectedSerialPort == string.Empty) {
                MessageBox.Show("You have not selected a serial port!");
                return;
            }

            try {
                serialPort.PortName = s_SelectedSerialPort;
                serialPort.Open();
            } catch (IOException ex) {
                MessageBox.Show("The selected serial port could not be opened");
            }
        }


        private void muteSelectedChannel_Clicked(object sender, RoutedEventArgs e) {
            try {
                midiPlayer.b_PlayPersistentChannel = !(muteSelectedChannel.IsChecked);
            } catch (NullReferenceException ex) { }
        }

        private void getSerialData() {
            while (true) {
                //score.s_CurrentFingering = string.Empty;

                if (s_SelectedSerialPort != string.Empty && serialPort.IsOpen) {
                    try {
                        score.s_CurrentFingering = serialPort.ReadLine().Trim();
                    } catch (InvalidOperationException e) {
                    } catch (TimeoutException e) {
                        score.s_CurrentFingering = string.Empty;
                    }
                }

                score.updateScore(ref midiPlayer);

                /*
                if (debugConsole != null) {
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(
                        delegate() {
                            debugConsole.textbox1.Text = "DATA: " + data + Environment.NewLine;
                        }));
                    count++;
                }*/

                Thread.Sleep(5);
            }
        }

        private void about_Clicked(object sender, RoutedEventArgs e) {
            About about = new About();
            about.Show();
        }

        private void layout_Updated(object sender, EventArgs e) {
            RoutedEventArgs d = new RoutedEventArgs();
            size_Changed(this, d);
        }

        private void size_Changed(object sender, RoutedEventArgs e) {
            r_HeaderBackground.Width = window.ActualWidth;
            ScaleTransform sc;
            if (hInst == 1) {
                sc = new ScaleTransform(1, 2);
                r_KeyLine.Height = window.ActualHeight;
            }
            else {
                sc = new ScaleTransform(2, 1);
                r_KeyLine.Width = window.ActualWidth;
            }
            gridlines.LayoutTransform = sc;
            gridlines.UpdateLayout();
            canv.Width = window.ActualWidth;
            r_KeyLine.Margin = new Thickness((r_instrument[0].PointToScreen(new Point(r_instrument[0].ActualWidth, r_instrument[0].ActualHeight)) - r_instrument[0].PointToScreen(new Point(0, 0))).X, 0, 0, 0);

        }

        private void NWGUI_KeyUp(object sender, KeyEventArgs e) {
            try {
                if (!midiPlayer.isPlaying())
                    return;

                if (e.Key.ToString() == "M") {
                    midiPlayer.unmuteOtherChannels();
                }
            } catch (NullReferenceException ex) { }
        }

        private void NWGUI_KeyDown(object sender, KeyEventArgs e) {
            try {
                if (!midiPlayer.isPlaying())
                    return;

                if (e.Key.ToString() == "M") {
                    midiPlayer.muteOtherChannels();
                }
            } catch (NullReferenceException ex) { }
        }

        private void handleMIDILoadProgressChanged(object sender, ProgressChangedEventArgs e) {
            try {
                loadingScreen.setProgress(e.ProgressPercentage);
            } catch (NullReferenceException ex) { }
        }


        private void handleMIDILoadCompleted(object sender, AsyncCompletedEventArgs e) {
            try {
                loadingScreen.Close();
            } catch (NullReferenceException ex) { }

            channelSelector = new ChannelSelector(ref midiInfo, i_Channel, channelSelectorOkClicked);
            channelSelector.Show();
        }

        private void handleMIDIChannelMessagePlayed(object sender, ChannelMessageEventArgs e) {
            //i_NumNotesPlayed++;
        }

        private void handleMIDIPlayingCompleted(object sender, EventArgs e) {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate() {
                Start.IsEnabled = true;
                Stop.IsEnabled = false;
                Instruments.IsEnabled = true;
                instrument_Clicked(instrument);

                b_AnimationStarted = false;
                CompositionTarget.Rendering -= new EventHandler(moveCanvas);
            }));
        }

        // override some program event handlers to ensure extra things are loaded/closed properly on start/close
        protected override void OnClosing(CancelEventArgs e) {
            serialPortReadThread.Abort();

            try {
                midiPlayer.OnClosingOperations();
            } catch (NullReferenceException ex) { }

            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e) {
            try {
                midiPlayer.OnClosedOperations();
            } catch (NullReferenceException ex) { }

            base.OnClosed(e);
        }

        private void initializeCanvas() {
            tb_SongTitle.Text = midiInfo.s_Title;
            showCanvasChildren();
        }

        private void initializeSubCanvas() {
            if (i_Channel < 0)
                return;
            int j = 0;
            long lastNote = 0;
            long firstStart = 0;
            long smallestNoteLength = 0;
            foreach (Note note in midiInfo.l_Notes) {
                j++;
                if (smallestNoteLength == 0)
                    smallestNoteLength = note.li_EndTime - note.li_BeginTime;
                else if (smallestNoteLength > (note.li_EndTime - note.li_BeginTime))
                    smallestNoteLength = (note.li_EndTime - note.li_BeginTime);
                if (note.li_EndTime > lastNote)
                    lastNote = note.li_EndTime;
            }

            if (smallestNoteLength < 300)
                multiplier = 300 / smallestNoteLength;

            if (midiInfo.i_TimeSignatureNumerator == 0)
                MessageBox.Show(Convert.ToString("Warning! Time Signature is 0"));


            drawGridLines(lastNote, (int)(midiInfo.i_TempoInBPM * multiplier), 4);
            for (int i = j - 1; i >= 0; i--) {
                if (i == j - 1) {
                    firstStart = midiInfo.l_Notes[i].li_BeginTime / 10;
                }
                fingering(midiInfo.l_Notes[i].i_NoteNumber, instrument, (long)midiInfo.l_Notes[i].li_BeginTime / 10, (long)midiInfo.l_Notes[i].li_EndTime / 10);
            }
        }

        long previousTime;
        long currentTime;
        long frameCount = 0;
        private void moveCanvas(object sender, EventArgs e) {
            updateScoreDisplay(sender, e);
            updateFingeringDisplay(sender, e);
            frameCount++;
            long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            currentTime = milliseconds;

            if (frameCount % 10 == 0)
                FPS.Header = 1000 / (currentTime - previousTime) + " FPS";

            previousTime = currentTime;

            long currentDelta = (milliseconds - starterTime) / 10;
            if (hInst == 0) {
                double mover = 625;
                Canvas.SetTop(subcanv,  (double)(currentDelta * multiplier) + mover);
                Canvas.SetTop(gridlines, (double)(currentDelta * multiplier) + mover);
            }
            else {
                double mover = 75;
                Canvas.SetLeft(subcanv,(double)(-1 * currentDelta * multiplier) + mover);
                Canvas.SetLeft(gridlines, (double)(-1 * currentDelta * multiplier) + mover);
            }
        }

        private void hideCanvasChildren() {
            r_HeaderBackground.Visibility = Visibility.Hidden;
            r_KeyLine.Visibility = Visibility.Hidden;
            tb_ScoreDisplay.Visibility = Visibility.Hidden;
            tb_SongTitle.Visibility = Visibility.Hidden;
            for (int i = 0; i < r_instrument.Length; i++) {
                r_instrument[i].Visibility = Visibility.Hidden;
                tb_instrument[i].Visibility = Visibility.Hidden;
            }
        }

        private void showCanvasChildren() {
            r_HeaderBackground.Visibility = Visibility.Visible;
            r_KeyLine.Visibility = Visibility.Visible;
            tb_ScoreDisplay.Visibility = Visibility.Visible;
            tb_SongTitle.Visibility = Visibility.Visible;
            for (int i = 0; i < r_instrument.Length; i++) {
                r_instrument[i].Visibility = Visibility.Visible;
                tb_instrument[i].Visibility = Visibility.Visible;
            }
        }

        private void resetSubCanvas(bool clearCanvasChildren) {
            if (clearCanvasChildren) {
                subcanv.Children.Clear();
                gridlines.Children.Clear();
            }
            subcanv.SetValue(Canvas.TopProperty, i_InitialCanvasPosY);
            gridlines.SetValue(Canvas.TopProperty, i_InitialCanvasPosY);
        }

        private void showSubCanvas() {
            subcanv.Visibility = Visibility.Visible;
            gridlines.Visibility = Visibility.Visible;
        }

        private void hideSubCanvas() {
            subcanv.Visibility = Visibility.Hidden;
            gridlines.Visibility = Visibility.Hidden;
        }

        private void updateScoreDisplay(object sender, EventArgs e) {
            try {
                tb_ScoreDisplay.Text = score.i_NumNotesScored + "/" + midiPlayer.i_NumChannelNotesPlayed + " notes correct ~ " + score.scoreGrade(midiPlayer.i_NumChannelNotesPlayed);
            } catch (NullReferenceException ex) { }
        }

        private void updateFingeringDisplay(object sender, EventArgs e) {
            for (int i = 0; i < r_instrument.Length; i++) {
                r_instrument[i].StrokeThickness = 2;
                tb_instrument[i].FontSize = 26;
                tb_instrument[i].FontWeight = FontWeights.Bold;
                tb_instrument[i].TextAlignment = TextAlignment.Center;
                tb_instrument[i].Foreground = new SolidColorBrush(Colors.White);
            }

            // instrument is violin
            if (instrument == 41) {
                for (int i = 0; i < r_instrument.Length; i++) {
                    try {
                        if (score.s_CurrentFingering.Length == 4)
                            tb_instrument[i].Text = Convert.ToString(score.s_CurrentFingering[i]);
                    }
                    catch (Exception ex) { }
                }
            }
            else if (instrument >= 25 && instrument <= 32) {
                for (int i = 0; i < r_instrument.Length; i++) {
                    try {
                        if (score.s_CurrentFingering.Length == 6)
                            tb_instrument[i].Text = Convert.ToString(score.s_CurrentFingering[i]);
                    }
                    catch (Exception ex) {}
                }
            }
        }

        public struct ColorRGB {
            public byte R;
            public byte G;
            public byte B;

            public ColorRGB(Color value) {
                this.R = value.R;
                this.G = value.G;
                this.B = value.B;
            }

            public static implicit operator Color(ColorRGB rgb) {
                Color c = Color.FromArgb(1, rgb.R, rgb.G, rgb.B);
                return c;
            }

            public static explicit operator ColorRGB(Color c) {
                return new ColorRGB(c);
            }
        }

        public ColorRGB HSL2RGB(double h, double sl, double l) {
            double v;
            double r, g, b;
            r = l;   // default to gray
            g = l;
            b = l;
            v = (l <= 0.5) ? (l * (1.0 + sl)) : (l + sl - l * sl);
            if (v > 0) {
                double m;
                double sv;
                int sextant;
                double fract, vsf, mid1, mid2;

                m = l + l - v;
                sv = (v - m) / v;
                h *= 6.0;
                sextant = (int)h;
                fract = h - sextant;
                vsf = v * sv * fract;
                mid1 = m + vsf;
                mid2 = v - vsf;
                switch (sextant) {
                    case 0:
                        r = v;
                        g = mid1;
                        b = m;
                        break;
                    case 1:
                        r = mid2;
                        g = v;
                        b = m;
                        break;
                    case 2:
                        r = m;
                        g = v;
                        b = mid1;
                        break;
                    case 3:
                        r = m;
                        g = mid2;
                        b = v;
                        break;
                    case 4:
                        r = mid1;
                        g = m;
                        b = v;
                        break;
                    case 5:
                        r = v;
                        g = m;
                        b = mid2;
                        break;
                }
            }
            ColorRGB rgb;
            rgb.R = Convert.ToByte(r * 255.0f);
            rgb.G = Convert.ToByte(g * 255.0f);
            rgb.B = Convert.ToByte(b * 255.0f);
            return rgb;
        }
    }

    public static class ExtensionMethods {
        private static Action EmptyDelegate = delegate() { };
        public static void Refresh(this UIElement uiElement) {
            uiElement.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
        }
    }

}