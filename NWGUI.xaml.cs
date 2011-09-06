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
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
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
using System.Threading;
using System.Windows.Threading;


namespace NiceWindow {
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
        double scrollSpeed = 1.66667;
        double multiplier = 1;
        MidiPlayer midiPlayer;
        MidiInfo midiInfo;

        Rectangle r_HeaderBackground = new Rectangle();
        Rectangle r_KeyLine = new Rectangle();
        TextBlock tb_ScoreDisplay = new TextBlock();
        TextBlock tb_SongTitle = new TextBlock();

        DispatcherTimer dispatcherTimer = new DispatcherTimer();

        //int i_NumNotesPlayed;
        int i_NumNotesScored;
        string s_ScoreGrade;

        string s_SelectedSerialPort = string.Empty;
        SerialPort serialPort = new SerialPort();
        Thread serialPortReadThread;
        int hInst = 1;

        public NWGUI() {
            InitializeComponent();

            i_InitialCanvasPosY = (double)(subcanv.GetValue(Canvas.TopProperty));

            dispatcherTimer.Interval = new TimeSpan(166667);
            dispatcherTimer.Tick += new EventHandler(moveCanvas);
            dispatcherTimer.Tick += new EventHandler(updateScoreDisplay);

            canv.Background = new SolidColorBrush(Colors.White);

            r_HeaderBackground.Height = 55;
            r_HeaderBackground.Width = 1024;
            r_HeaderBackground.Fill = new SolidColorBrush(Color.FromRgb(51, 51, 51));
            r_HeaderBackground.SetValue(Canvas.TopProperty, (double)19);

            tb_SongTitle.Height = 50;
            tb_SongTitle.Width = 400;
            tb_SongTitle.Foreground = new SolidColorBrush(Colors.White);
            tb_SongTitle.FontStyle = FontStyles.Italic;
            tb_SongTitle.FontSize = 30;
            tb_SongTitle.TextAlignment = TextAlignment.Left;
            tb_SongTitle.SetValue(Canvas.TopProperty, (double)25);
            tb_SongTitle.SetValue(Canvas.LeftProperty, (double)10);

            tb_ScoreDisplay.Height = 50;
            tb_ScoreDisplay.Width = 400;
            tb_ScoreDisplay.Foreground = new SolidColorBrush(Colors.White);
            tb_ScoreDisplay.FontSize = 30;
            tb_ScoreDisplay.TextAlignment = TextAlignment.Right;
            tb_ScoreDisplay.SetValue(Canvas.TopProperty, (double)25);
            tb_ScoreDisplay.SetValue(Canvas.LeftProperty, (double)600);

            hideCanvasChildren();
            if (hInst == 0) {
                r_KeyLine.Height = 3;
                r_KeyLine.Width = 1024;
                r_KeyLine.Fill = new SolidColorBrush(Color.FromRgb(51, 51, 51));
                r_KeyLine.SetValue(Canvas.TopProperty, (double)650);
            }
            else {
                r_KeyLine.Height = 1024;
                r_KeyLine.Width = 3;
                r_KeyLine.Fill = new SolidColorBrush(Color.FromRgb(51, 51, 51));
                r_KeyLine.SetValue(Canvas.LeftProperty, (double)10);
            }
            canv.Children.Add(r_KeyLine);
            canv.Children.Add(r_HeaderBackground);
            canv.Children.Add(tb_SongTitle);
            canv.Children.Add(tb_ScoreDisplay);

            serialPort.ReadTimeout = 5;
            serialPortReadThread = new Thread(new ThreadStart(getSerialData));
            serialPortReadThread.Start();
        }


        private void start_Clicked(object sender, RoutedEventArgs e) {
            try {
                if (midiPlayer.isPlaying()) {
                    MessageBox.Show("The file is currently being played, please have it finish first.");
                    return;
                }

                if (midiPlayer.isFinishedLoading()) {
                    if (i_Channel != -1)
                        midiPlayer.setPersistentChannel(i_Channel);

                    midiPlayer.startPlaying();
                    starterTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                    resetScore();
                    initializeCanvas();

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
                    hInst = 1;
                    drawGridLines(lastNote, (int)(midiInfo.i_TempoInBPM * multiplier), 4);
                    for (int i = j - 1; i >= 0; i--) {
                        if (i == j - 1) {
                            firstStart = midiInfo.l_Notes[i].li_BeginTime / 10;
                        }
                        fingering(midiInfo.l_Notes[i].i_NoteNumber, 30, (long)midiInfo.l_Notes[i].li_BeginTime / 10, (long)midiInfo.l_Notes[i].li_EndTime / 10);
                    }
                    dispatcherTimer.Start();
                    b_AnimationStarted = true;
                }
                else {
                    MessageBox.Show("Please wait for the MIDI file to finish loading");
                }

            }
            catch (NullReferenceException ex) {
                //if (ex.
                MessageBox.Show("Please load a MIDI file first! (or some other weird error occured, so read the proceeding message)");
                MessageBox.Show(ex.ToString());
            }

        }

        private void stop_Clicked(object sender, RoutedEventArgs e) {
            hideCanvasChildren();

            try {
                //midiPlayer.OnClosingOperations();
                //midiPlayer.OnClosedOperations();
                midiPlayer.stopPlaying();

                resetSubCanvas();

                b_AnimationStarted = false;
                dispatcherTimer.Stop();
            }
            catch (NullReferenceException ex) { }
        }
        private void drawGridLines(long endTime, int bpm, int count) {
            int runner = 0;
            for (double i = 0; i < (endTime + 1000); i = i + (1.775 * multiplier) + (bpm / count)) {
                Rectangle r = new Rectangle();
                if (hInst == 0) {
                    r.Width = 1024;
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
                    r.Height = 1024;
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
                subcanv.Children.Add(r);
                runner++;
            }
        }

        private void fingering(int note, int instrument, long startTime, long endTime) {
            if (instrument == 41) { //VIOLIN
                int margin = 300;
                int noteNumber;

                int padding = 30;
                Rectangle r = new Rectangle();
                TextBlock textBlock = new TextBlock();

                textBlock.Height = 50;
                textBlock.Width = 50;
                r.Width = 46;
                string noteString = "";
                textBlock.Foreground = new SolidColorBrush(Colors.White);
                if (note >= 55 && note < 62) {
                    textBlock.SetValue(Canvas.LeftProperty, (margin + r.Width));
                    r.SetValue(Canvas.LeftProperty, (double)(margin + r.Width));
                    r.Fill = new SolidColorBrush(Color.FromRgb(144, 187, 69));
                    r.Stroke = new SolidColorBrush(Color.FromRgb(114, 148, 55));
                    noteNumber = note - 55;
                }

                else if (note >= 62 && note < 69) {
                    textBlock.SetValue(Canvas.LeftProperty, (margin + (2 * (r.Width + padding))));
                    r.SetValue(Canvas.LeftProperty, (double)(margin + (2 * (r.Width + padding))));
                    r.Fill = new SolidColorBrush(Color.FromRgb(250, 181, 65));
                    r.Stroke = new SolidColorBrush(Color.FromRgb(227, 165, 59));
                    noteNumber = note - 62;
                }

                else if (note >= 69 && note < 76) {
                    textBlock.SetValue(Canvas.LeftProperty, (margin + (3 * (r.Width + padding))));
                    r.SetValue(Canvas.LeftProperty, (double)(margin + (3 * (r.Width + padding))));
                    r.Fill = new SolidColorBrush(Color.FromRgb(220, 42, 62));
                    r.Stroke = new SolidColorBrush(Color.FromRgb(189, 36, 54));
                    noteNumber = note - 69;
                }

                else if (note >= 76 && note < 83) {
                    textBlock.SetValue(Canvas.LeftProperty, (margin + (4 * (r.Width + padding))));
                    r.SetValue(Canvas.LeftProperty, (double)(margin + (4 * (r.Width + padding))));
                    r.Fill = new SolidColorBrush(Color.FromRgb(26, 98, 179));
                    r.Stroke = new SolidColorBrush(Color.FromRgb(24, 82, 148));
                    noteNumber = note - 76;
                }

                else {
                    textBlock.SetValue(Canvas.LeftProperty, (margin + (5 * (r.Width + padding))));
                    r.SetValue(Canvas.LeftProperty, (double)(margin + (5 * (r.Width + padding))));
                    r.Fill = new SolidColorBrush(Colors.Black);
                    r.Stroke = new SolidColorBrush(Colors.Black);
                    noteNumber = note;
                }


                if (noteNumber == 0) { noteString = "0"; }
                else if (noteNumber == 1) { noteString = "1-"; }
                else if (noteNumber == 2) { noteString = "1"; }
                else if (noteNumber == 3) { noteString = "2-"; }
                else if (noteNumber == 4) { noteString = "2"; }
                else if (noteNumber == 5) { noteString = "3"; }
                else if (noteNumber == 6) { noteString = "3+"; }
                else if (noteNumber == 7) { noteString = "4"; }
                textBlock.Text = noteString;

                r.StrokeThickness = 2;
                Canvas.SetZIndex(textBlock, (int)99);
                r.Height = (endTime - startTime) * multiplier;
                textBlock.FontSize = 26;
                textBlock.FontWeight = FontWeights.Bold;
                textBlock.TextAlignment = TextAlignment.Center;
                textBlock.SetValue(Canvas.TopProperty, (double)((-1 * startTime * multiplier) - 35));
                r.SetValue(Canvas.BottomProperty, (double)(startTime) * multiplier);
                subcanv.Children.Add(r);
                subcanv.Children.Add(textBlock);
            }

            else if (instrument >= 25 && instrument <= 32) { //GUITAR
                int margin = 100;
                int padding = 20;
                Rectangle r = new Rectangle();
                TextBlock textBlock = new TextBlock();
                textBlock.Height = 50;
                textBlock.Width = 50;
                r.Height = 46;
                String noteString = "";

                textBlock.Foreground = new SolidColorBrush(Colors.White);
                if (note >= 64 && note < 69) {
                    textBlock.SetValue(Canvas.TopProperty, (5 + margin + r.Height));
                    r.SetValue(Canvas.TopProperty, (double)(margin + r.Height));
                    r.Fill = new SolidColorBrush(Color.FromRgb(144, 187, 69));
                    r.Stroke = new SolidColorBrush(Color.FromRgb(114, 148, 55));
                    noteString = Convert.ToString(note - 64);
                }

                else if (note >= 59 && note <= 63) {
                    textBlock.SetValue(Canvas.TopProperty, (5 + margin + (2 * (r.Height + padding))));
                    r.SetValue(Canvas.TopProperty, (double)(margin + (2 * (r.Height + padding))));
                    r.Fill = new SolidColorBrush(Color.FromRgb(250, 181, 65));
                    r.Stroke = new SolidColorBrush(Color.FromRgb(227, 165, 59));
                    noteString = Convert.ToString(note - 59);
                }

                else if (note >= 55 && note < 59) {
                    textBlock.SetValue(Canvas.TopProperty, (5 + margin + (3 * (r.Height + padding))));
                    r.SetValue(Canvas.TopProperty, (double)(margin + (3 * (r.Height + padding))));
                    r.Fill = new SolidColorBrush(Color.FromRgb(220, 42, 62));
                    r.Stroke = new SolidColorBrush(Color.FromRgb(189, 36, 54));
                    noteString = Convert.ToString(note - 55);
                }

                else if (note >= 50 && note <= 54) {
                    textBlock.SetValue(Canvas.TopProperty, (5 + margin + (4 * (r.Height + padding))));
                    r.SetValue(Canvas.TopProperty, (double)(margin + (4 * (r.Height + padding))));
                    r.Fill = new SolidColorBrush(Color.FromRgb(26, 98, 179));
                    r.Stroke = new SolidColorBrush(Color.FromRgb(24, 82, 148));
                    noteString = Convert.ToString(note - 50);
                }

                else if (note >= 45 && note <= 49) {
                    textBlock.SetValue(Canvas.TopProperty, (5 + margin + (5 * (r.Height + padding))));
                    r.SetValue(Canvas.TopProperty, (double)(margin + (5 * (r.Height + padding))));
                    r.Fill = new SolidColorBrush(Color.FromRgb(82, 44, 95));
                    r.Stroke = new SolidColorBrush(Color.FromRgb(26, 14, 31));
                    noteString = Convert.ToString(note - 45);
                }

                else if (note >= 40 && note <= 44) {
                    textBlock.SetValue(Canvas.TopProperty, (5 + margin + (6 * (r.Height + padding))));
                    r.SetValue(Canvas.TopProperty, (double)(margin + (6 * (r.Height + padding))));
                    r.Fill = new SolidColorBrush(Color.FromRgb(120, 132, 161));
                    r.Stroke = new SolidColorBrush(Color.FromRgb(94, 102, 125));
                    noteString = Convert.ToString(note - 40);
                }

                else {
                    textBlock.SetValue(Canvas.TopProperty, (5 + margin + (7 * (r.Height + padding))));
                    r.SetValue(Canvas.TopProperty, (double)(margin + (7 * (r.Height + padding))));
                    r.Fill = new SolidColorBrush(Colors.Black);
                    r.Stroke = new SolidColorBrush(Colors.Black);
                    noteString = Convert.ToString(note);
                }
                textBlock.Text = noteString;
                r.StrokeThickness = 2;
                Canvas.SetZIndex(textBlock, (int)99);
                r.Width = (endTime - startTime) * multiplier;
                textBlock.FontSize = 26;
                textBlock.FontWeight = FontWeights.Bold;
                textBlock.TextAlignment = TextAlignment.Center;
                textBlock.SetValue(Canvas.LeftProperty, (double)((startTime * multiplier) - 14));
                r.SetValue(Canvas.LeftProperty, (double)(startTime) * multiplier);
                subcanv.Children.Add(r);
                subcanv.Children.Add(textBlock);
            }
            else if (instrument == 74) { //FLUTE
                int margin = 40;
                int padding = 30;

                String noteString = " ";

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

                Rectangle[] r = new Rectangle[noteString.Length];
                for (int i = 0; i < noteString.Length; i++) {
                    r[i] = new Rectangle();
                }
                Color[] color = new Color[11] { Color.FromRgb(220, 42, 62), Color.FromRgb(67, 66, 64), Color.FromRgb(250, 181, 65), Color.FromRgb(0, 100, 100), Color.FromRgb(253, 251, 230), Color.FromRgb(254, 135, 33), Color.FromRgb(144, 187, 69), Color.FromRgb(231, 107, 117), Color.FromRgb(120, 132, 161), Color.FromRgb(82, 44, 95), Color.FromRgb(26, 98, 179) };
                Color[] border = new Color[11] { Color.FromRgb(189, 36, 54), Color.FromRgb(31, 30, 29), Color.FromRgb(227, 165, 59), Color.FromRgb(0, 43, 43), Color.FromRgb(191, 190, 174), Color.FromRgb(220, 123, 37), Color.FromRgb(114, 148, 55), Color.FromRgb(194, 89, 98), Color.FromRgb(94, 102, 125), Color.FromRgb(26, 14, 31), Color.FromRgb(24, 82, 148) };

                int rWidth = 46;
                for (int i = 0; i < noteString.Length; i++) {
                    if (i == 0)
                        r[i].SetValue(Canvas.LeftProperty, (double)(margin + rWidth));
                    else
                        r[i].SetValue(Canvas.LeftProperty, (double)(margin + ((i + 1) * (rWidth + padding))));
                    r[i].Width = rWidth;
                    if (noteString[i] == '1' && noteString.Length > 3) {
                        r[i].Fill = new SolidColorBrush(color[i]);
                        r[i].Stroke = new SolidColorBrush(border[i]);
                    }
                    r[i].StrokeThickness = 2;
                    r[i].Height = (endTime - startTime) * multiplier;
                    r[i].SetValue(Canvas.BottomProperty, (double)(startTime) * multiplier);
                    subcanv.Children.Add(r[i]);
                }
            }
        }

        /*private void startMusic_Clicked(object sender, RoutedEventArgs e) {
            try {
                if (midiPlayer.isFinishedLoading()) {
                    midiPlayer.startPlaying();
                }
                else {
                    MessageBox.Show("Please wait for the MIDI file to finish loading");
                }
            }
            catch (NullReferenceException ex) {
                MessageBox.Show("Please load a MIDI file first!");
            }
        }*/

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
                }
                catch (NullReferenceException ex) { }

                resetSubCanvas();
                hideCanvasChildren();

                b_AnimationStarted = false;
                dispatcherTimer.Stop();

                try {
                    loadingScreen.Close();
                }
                catch (NullReferenceException ex) { }

                loadingScreen = new LoadingScreen();
                loadingScreen.Show();
                i_Channel = -1;

                midiPlayer = new MidiPlayer(openFileDialog.FileName, handleMIDILoadProgressChanged,
                    handleMIDILoadCompleted, handleMIDIChannelMessagePlayed);

                midiInfo = new MidiInfo(openFileDialog.FileName, i_Channel);
            }
        }

        private void debug_Clicked(object sender, RoutedEventArgs e) {
            debugConsole = new MainWindow();
            debugConsole.Show();

            //serialPortReadTimer.Start();

            try {

                /*
                debugConsole.textbox1.Text += midiInfo.s_Title + Environment.NewLine;

                debugConsole.textbox1.Text += midiInfo.i_DeltaTicksPerQuarterNote + Environment.NewLine;
                debugConsole.textbox1.Text += midiInfo.i_MicrosecondsPerQuarterNote + " " + midiInfo.d_MilisecondsPerQuarterNote + Environment.NewLine;
                debugConsole.textbox1.Text += midiInfo.d_MilisecondsPerTick + Environment.NewLine;


                foreach (NAudio.Midi.MidiEvent metadata in midiInfo.l_Metadata) {
                    debugConsole.textbox1.Text += metadata.ToString();
                }

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
                }
                 */
            }
            catch (NullReferenceException ex) { }
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

            channelSelector.Close();
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
            }
            catch (IOException ex) {
                MessageBox.Show("The selected serial port could not be opened");
            }
        }


        private void getSerialData() {
            int count = 0;
            string data;
            while (true) {
                if (s_SelectedSerialPort != string.Empty) {
                    try {
                        data = serialPort.ReadLine().Trim();
                    }
                    catch (InvalidOperationException e) {
                        data = "(serial port is closed)";
                    }
                    catch (IOException e) {
                        data = "(serial port is closed)";
                    }
                    catch (TimeoutException e) {
                        data = "(no data)";
                    }
                }
                else {
                    data = "(no serial port selected)";
                }


                if (debugConsole != null) {
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(
                        delegate() {
                            debugConsole.textbox1.Text = "DATA: " + data + Environment.NewLine;
                        }));
                    count++;
                }

                Thread.Sleep(5);
            }
        }


        private void about_Clicked(object sender, RoutedEventArgs e) {
            About about = new About();
            about.Show();
        }

        private void NWGUI_KeyUp(object sender, KeyEventArgs e) {
            try {
                if (!midiPlayer.isPlaying())
                    return;

                if (e.Key.ToString() == "M") {
                    midiPlayer.unmuteOtherChannels();
                }
            }
            catch (NullReferenceException ex) { }
        }

        private void NWGUI_KeyDown(object sender, KeyEventArgs e) {
            try {
                if (!midiPlayer.isPlaying())
                    return;

                if (e.Key.ToString() == "M") {
                    midiPlayer.muteOtherChannels();
                }
            }
            catch (NullReferenceException ex) { }
        }

        private void handleMIDILoadProgressChanged(object sender, ProgressChangedEventArgs e) {
            try {
                loadingScreen.setProgress(e.ProgressPercentage);
            }
            catch (NullReferenceException ex) { }
        }


        private void handleMIDILoadCompleted(object sender, AsyncCompletedEventArgs e) {
            try {
                loadingScreen.Close();
            }
            catch (NullReferenceException ex) { }

            channelSelector = new ChannelSelector(ref midiInfo, i_Channel, channelSelectorOkClicked);
            channelSelector.Show();
        }

        private void handleMIDIChannelMessagePlayed(object sender, ChannelMessageEventArgs e) {
            //i_NumNotesPlayed++;
        }

        // override some program event handlers to ensure extra things are loaded/closed properly on start/close
        protected override void OnClosing(CancelEventArgs e) {
            serialPortReadThread.Abort();

            try {
                midiPlayer.OnClosingOperations();
            }
            catch (NullReferenceException ex) { }

            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e) {
            try {
                midiPlayer.OnClosedOperations();
            }
            catch (NullReferenceException ex) { }

            base.OnClosed(e);
        }

        private void initializeCanvas() {
            tb_SongTitle.Text = midiInfo.s_Title;
            showCanvasChildren();
        }

        private void moveCanvas(object sender, EventArgs e) {
            long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            long delta = milliseconds - starterTime;
            if (hInst == 0) {
                double mover = 615;
                subcanv.SetValue(Canvas.TopProperty, (double)((delta / 10) * multiplier) + mover);
            }
            else {
                double mover = 25;
                subcanv.SetValue(Canvas.LeftProperty, (double)(-1 * (delta / 10) * multiplier) + mover);
            }
        }

        private void hideCanvasChildren() {
            r_HeaderBackground.Visibility = Visibility.Hidden;
            r_KeyLine.Visibility = Visibility.Hidden;
            tb_ScoreDisplay.Visibility = Visibility.Hidden;
            tb_SongTitle.Visibility = Visibility.Hidden;
        }

        private void showCanvasChildren() {
            r_HeaderBackground.Visibility = Visibility.Visible;
            r_KeyLine.Visibility = Visibility.Visible;
            tb_ScoreDisplay.Visibility = Visibility.Visible;
            tb_SongTitle.Visibility = Visibility.Visible;
        }

        private void resetSubCanvas() {
            subcanv.Children.Clear();
            subcanv.SetValue(Canvas.TopProperty, i_InitialCanvasPosY);
        }


        private void determineScoreGrade() {
            try {
                if (midiPlayer.i_NumChannelNotesPlayed == 0) {
                    s_ScoreGrade = "...";
                    return;
                }
            }
            catch (NullReferenceException e) {
                s_ScoreGrade = "...";
                return;
            }


            double percentage = ((double)i_NumNotesScored / (double)(midiPlayer.i_NumChannelNotesPlayed)) * 100;

            if (percentage >= 90)
                s_ScoreGrade = "A+";
            else if (percentage >= 85)
                s_ScoreGrade = "A";
            else if (percentage >= 80)
                s_ScoreGrade = "A-";
            else if (percentage >= 76)
                s_ScoreGrade = "B+";
            else if (percentage >= 73)
                s_ScoreGrade = "B";
            else if (percentage >= 70)
                s_ScoreGrade = "B-";
            else if (percentage >= 67)
                s_ScoreGrade = "C+";
            else if (percentage >= 63)
                s_ScoreGrade = "C";
            else if (percentage >= 60)
                s_ScoreGrade = "C-";
            else if (percentage >= 57)
                s_ScoreGrade = "D+";
            else if (percentage >= 53)
                s_ScoreGrade = "D";
            else if (percentage >= 50)
                s_ScoreGrade = "D-";
            else
                s_ScoreGrade = "F";
        }

        private void resetScore() {
            i_NumNotesScored = 0;
        }

        private void updateScoreDisplay(object sender, EventArgs e) {
            determineScoreGrade();

            try {
                tb_ScoreDisplay.Text = i_NumNotesScored + "/" + midiPlayer.i_NumChannelNotesPlayed + " notes correct ~ " + s_ScoreGrade;
            }
            catch (NullReferenceException ex) { }
        }


        /*void drawFrame(object sender, EventArgs e)
        {
            temp++;
            r.SetValue(Canvas.TopProperty, (double)temp);
            canv.InvalidateVisual();
        }
         */

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
