using Microsoft.Win32;
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
using WpfAnimatedControl;

namespace Symphonary 
{
    public partial class NWGUI : Window, INotifyPropertyChanged
    {
        private bool b_AnimationStarted = false;
        private int i_CanvasMoveDirection = 1;

        private double i_InitialCanvasPosY;

        private DebugWindow debugConsole = new DebugWindow();
        
        private long starterTime = 0;
        
        private int i_Channel = -1;
        
        private double scrollSpeed = 2.00000;
        private double multiplier = 1;
        private MidiPlayer midiPlayer, midiPlayerForPreview;
        private MidiInfo midiInfo;

        private Rectangle[] r_instrument;
        private TextBlock[] tb_instrument;
        
        //int i_NumNotesPlayed;
        private int i_NumNotesScored;
        private string s_ScoreGrade;

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

        private ChannelSelector channelSelector;
        private SerialPortSelector serialPortSelector;

        private StringAllocator stringAllocator = new StringAllocator();
        
        /// <summary>
        /// Constructor for window
        /// </summary>
        public NWGUI() 
        {
            InitializeComponent();

            midiInfo = new MidiInfo(debugConsole);
            
            Stop.IsEnabled = false;

            instrument = ReadSettingsFromFile();
            if (instrument != 0) {
                Instrument_Clicked(instrument);
                foreach (MenuItem item in Instruments.Items)
                {
                  if (instrument == Convert.ToInt32(item.Tag))
                    item.IsChecked = true;
                }
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
            HideCanvasChildren();

            serialPort.ReadTimeout = 5;
            serialPortReadThread = new Thread(new ThreadStart(GetSerialData));
            serialPortReadThread.Start();

            channelSelector = new ChannelSelector(channelsListView, ChannelsListViewSelectionChanged);
            channelsListView.DataContext = channelSelector.Channels;
            serialPortSelector = new SerialPortSelector(serialPortsListView);
            serialPortsListView.DataContext = serialPortSelector.SerialPorts;

            //System.Console.WriteLine("{0}, {1}", window.ActualWidth, window.ActualHeight);
            //System.Console.WriteLine("{0}, {1}", LogoPositionX, LogoPositionY);
        }


        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        /// <summary>
        /// Margin property for logo and normalLogo
        /// </summary>
        public Thickness LogoMargin
        {
            get
            {
                int logoX = (int) (((window.ActualWidth/2) - 310)/9)*9;
                int logoY = (int) (((window.ActualHeight/2) - 125)/9)*9;
                return new Thickness(logoX + 1, logoY + 1, 0, 0);
            }
        }

        /// <summary>
        /// Margin property for progressBar
        /// </summary>
        public Thickness ProgressBarMargin
        {
            get
            {
                int progressBarX = (int) (((window.ActualWidth/2) - 200)/9)*9;
                int progressBarY = (int) (((window.ActualHeight/2) - 4)/9)*9;
                return new Thickness(progressBarX + 1, progressBarY + 1, 0, 0);
            }
        }

        /// <summary>
        /// Margin property for keyLine
        /// </summary>
        public Thickness KeyLineMargin
        {
            get
            {
                if (r_instrument != null)
                {
                    return new Thickness(
                        (r_instrument[0].PointToScreen(new Point(r_instrument[0].ActualWidth,
                                                                 r_instrument[0].ActualHeight)) -
                         r_instrument[0].PointToScreen(new Point(0, 0))).X, 0, 0, 0);
                }

                return new Thickness();
            }
        }

        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="endTime"></param>
        /// <param name="bpm"></param>
        /// <param name="count"></param>
        private void DrawGridLines(long endTime, int bpm, int count) 
        {
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


        /// <summary>
        /// Produces the graphics for displaying fingering
        /// </summary>
        /// <param name="note"></param>
        /// <param name="instrument"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        private void Fingering(int note, int instrument, long startTime, long endTime, int fretNumber, int stringNumber) 
        {
            string noteString = "";
            TextBlock textBlock = new TextBlock();
            textBlock.FontSize = 26;
            textBlock.FontWeight = FontWeights.Bold;
            textBlock.TextAlignment = TextAlignment.Center;
            textBlock.Foreground = new SolidColorBrush(Colors.White);
            //RoutedEventArgs e = new RoutedEventArgs();
            //Size_Changed(this, e);
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
                int[,] guitar = new int[6, 2] { { 64, 80 }, { 59, 63 }, { 55, 58 }, { 50, 54 }, { 45, 49 }, { 40, 44 } };
                int[] ctrl_guitar = { 1, 8, 9, 10, 0, 2, 6 }; // color data begins at index 1
                /*
                for (int i = guitar.GetLength(0); i > 0; i--)
                {
                    if (note >= guitar[i - 1, 0] && note <= guitar[i - 1, 1])
                    {
                        textBlock.Text = Convert.ToString(note - guitar[i - 1, 0]);
                        r.Fill = new SolidColorBrush(color[ctrl_guitar[i]]);
                        r.Stroke = new SolidColorBrush(border[ctrl_guitar[i]]);
                        textBlock.SetValue(Canvas.TopProperty, (5 + margin + ((i - 1)*(r.Height + padding))));
                        r.SetValue(Canvas.TopProperty, (double) (margin + ((i - 1)*(r.Height + padding))));
                    }
                }
                */
                /*
                // index adjusted loop
                for (int i = guitar.GetLength(0) - 1; i >= 0; i--) {
                    if (note >= guitar[i, 0] && note <= guitar[i, 1]) {
                        textBlock.Text = Convert.ToString(note - guitar[i, 0]);
                        r.Fill = new SolidColorBrush(color[ctrl_guitar[i + 1]]);
                        r.Stroke = new SolidColorBrush(border[ctrl_guitar[i + 1]]);
                        textBlock.SetValue(Canvas.TopProperty, (5 + margin + (i * (r.Height + padding))));
                        r.SetValue(Canvas.TopProperty, (margin + (i * (r.Height + padding))));
                    }
                }
                */

                // replaces loop above, makes use of given string and fret numbers
                textBlock.Text = fretNumber.ToString();
                textBlock.SetValue(Canvas.TopProperty, (5 + margin + (stringNumber * (r.Height + padding))));
                r.Fill = new SolidColorBrush(color[ctrl_guitar[stringNumber + 1]]);
                r.Stroke = new SolidColorBrush(border[ctrl_guitar[stringNumber + 1]]);
                r.SetValue(Canvas.TopProperty, (margin + (stringNumber * (r.Height + padding))));


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

                int[,] bass = new int[4, 2] { { 28, 32 }, { 33, 37 }, { 38, 42 }, { 43, 47 } };
                int[] ctrl_bass = { 1, 6, 2, 0, 10 };
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


        /// <summary>
        /// Event handler for the "Preview Channel" button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PreviewChannel_Clicked(object sender, RoutedEventArgs e)
        {
            midiPlayer.PersistentChannel = channelSelector.SelectedChannel;

            if (midiPlayer.PersistentChannel >= 0)
            {
                midiPlayer.StartPlaying();
                previewChannelButton.Foreground = Brushes.Green;
            }
        }

        /// <summary>
        /// Event handler for the "Done" button below the two listviews for channel and serial port selection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListViewGridDone_Clicked(object sender, RoutedEventArgs e)
        {
            i_Channel = channelSelector.SelectedChannel;
            midiPlayer.PersistentChannel = i_Channel;

            MidiPlayerExitPreviewMode();
            midiPlayer.StopPlaying();

            HideSubCanvas();
            ResetSubCanvas(true);
            InitializeSubCanvas();

            s_SelectedSerialPort = serialPortSelector.SelectedSerialPort;
            serialPort.Close();

            if (s_SelectedSerialPort != string.Empty)
            {
                try
                {
                    serialPort.PortName = s_SelectedSerialPort;
                    serialPort.Open();
                }
                catch (IOException ex)
                {
                    MessageBox.Show("The selected serial port could not be opened");
                }
            }

            listViewGrid.Visibility = Visibility.Hidden;
            Start_Clicked(sender, e);
            //MessageBox.Show(i_Channel.ToString());
        }

        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="num"></param>
        private void Instrument_Clicked(int num) 
        {
            RoutedEventArgs e = new RoutedEventArgs { };
            MenuItem sender = new MenuItem { Tag = num };
            Instrument_Clicked(sender, e);
        }


        /// <summary>
        /// Configure player for preview mode, stashes settings for which channels are played,
        /// detaches event handlers related to gameplay, attaches preview completion event handler
        /// </summary>
        private void MidiPlayerEnterPreviewMode()
        {
            midiPlayer.StashChannelPlaySettings();
            midiPlayer.UnHookExternalPlaybackEventHandles();
            midiPlayer.PlayPersistentChannel = true;
            midiPlayer.PlayOtherChannels = false;            
            midiPlayer.Sequencer.PlayingCompleted += HandleMIDIPreviewPlayingCompleted;
        }

        /// <summary>
        /// Resets player configuration from preview mode, restores settings for which channels are played,
        /// re-attaches event handlers related to gameplay, detaches preview completion event handler
        /// </summary>
        private void MidiPlayerExitPreviewMode()
        {
            midiPlayer.RecoverChannelPlaySettings();
            midiPlayer.ReattachExternalPlaybackEventHandles();
            //midiPlayer.Sequencer.ChannelMessagePlayed += HandleMIDIChannelMessagePlayed;
            //midiPlayer.Sequencer.PlayingCompleted += HandleMIDIPlayingCompleted;
            midiPlayer.Sequencer.PlayingCompleted -= HandleMIDIPreviewPlayingCompleted;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="inst"></param>
        private void WriteSettingsToFile(int inst) 
        {
            TextWriter tw = new StreamWriter("settings.ini");
            tw.WriteLine(inst);
            tw.Close();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private int ReadSettingsFromFile() 
        {
            int returner = 0;
            try {
                TextReader tr = new StreamReader("settings.ini");
                returner = Convert.ToInt32(tr.ReadLine());
                tr.Close();
            } catch (Exception) { }
            return returner;
        }

        /// <summary>
        /// Event handler for "Select Serial Port" menu item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectSerialPort_Clicked(object sender, RoutedEventArgs e) 
        {
            // if the serial port is not closed, opening once again (SerialPortSelector opens ports for testing) 
            // will cause an exception
            serialPort.Close();

            serialPortSelector.Refresh(s_SelectedSerialPort);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SerialPortSelectorOkClicked(object sender, RoutedEventArgs e) 
        {
            s_SelectedSerialPort = serialPortSelector.SelectedSerialPort;
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

        

        /// <summary>
        /// Thread that gets the serial port data
        /// </summary>
        private void GetSerialData()
        {
            while (true)
            {
                //score.s_CurrentFingering = string.Empty;

                if (s_SelectedSerialPort != string.Empty && serialPort.IsOpen)
                {
                    try
                    {
                        score.s_CurrentFingering = serialPort.ReadLine().Trim();
                    }
                    catch (InvalidOperationException e)
                    {
                    }
                    catch (TimeoutException e)
                    {
                        score.s_CurrentFingering = string.Empty;
                    }
                }

                score.UpdateScore(ref midiPlayer);

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



        /// <summary>
        /// Event handler for when the window layout is updated
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Layout_Updated(object sender, EventArgs e) 
        {
            RoutedEventArgs d = new RoutedEventArgs();
            Size_Changed(this, d);
        }

        /// <summary>
        /// Event handler for when the window size is changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Size_Changed(object sender, RoutedEventArgs e)
        {
            NotifyPropertyChanged("LogoMargin");
            NotifyPropertyChanged("ProgressBarMargin");
            NotifyPropertyChanged("KeyLineMargin");

            r_HeaderBackground.Width = window.ActualWidth;
            ScaleTransform sc;
            if (hInst == 1) {
                sc = new ScaleTransform(1, 2);
                keyLine.Height = window.ActualHeight;
            }
            else {
                sc = new ScaleTransform(2, 1);
                keyLine.Width = window.ActualWidth;
            }
            
            gridlines.LayoutTransform = sc;
            gridlines.UpdateLayout();
            canv.Width = window.ActualWidth;
        }

        /// <summary>
        /// Event hander for when a key is pressed down
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NWGUI_KeyUp(object sender, KeyEventArgs e) 
        {
            try {
                if (!midiPlayer.IsPlaying)
                    return;

                if (e.Key.ToString() == "M") {
                    midiPlayer.UnmuteOtherChannels();
                }
            } catch (NullReferenceException ex) { }
        }

        /// <summary>
        /// Event hander for when a key is restored up
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NWGUI_KeyDown(object sender, KeyEventArgs e) 
        {
            try {
                if (!midiPlayer.IsPlaying)
                    return;

                if (e.Key.ToString() == "M") {
                    midiPlayer.MuteOtherChannels();
                }
            } catch (NullReferenceException ex) { }
        }

        /// <summary>
        /// Event handler for when MIDI file loading has updated its progress (Sanford)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleMIDILoadProgressChanged(object sender, ProgressChangedEventArgs e) 
        {
            try {
                SetProgress(e.ProgressPercentage);
            } catch (NullReferenceException ex) { }
        }

        /// <summary>
        /// Event handler for when MIDI file loading is completed (Sanford)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleMIDILoadCompleted(object sender, AsyncCompletedEventArgs e) 
        {
            try {
                loadingScreen.Visibility = Visibility.Hidden;
            } catch (NullReferenceException ex) { }

            channelSelector.Refresh(ref midiInfo, i_Channel);

            // if the serial port is not closed, opening once again (SerialPortSelector opens ports for testing) 
            // will cause an exception
            serialPort.Close();
            serialPortSelector.Refresh(s_SelectedSerialPort);
            
            listViewGrid.Visibility = Visibility.Visible;

            MidiPlayerEnterPreviewMode();
        }

        /// <summary>
        /// Event handler for MIDI player - channel message played
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleMIDIChannelMessagePlayed(object sender, ChannelMessageEventArgs e) 
        {
            //i_NumNotesPlayed++;
        }

        /// <summary>
        /// Do these procedures when MIDI has finished playing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleMIDIPlayingCompleted(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                                   new Action(delegate()
                                                  {
                                                      debugConsole.ChangeText("");
                                                      Stop.IsEnabled = false;
                                                      Instruments.IsEnabled = true;
                                                      Instrument_Clicked(instrument);
                                                      HideSubCanvas();
                                                      normal.Visibility = Visibility.Visible;
                                                      b_AnimationStarted = false;
                                                      CompositionTarget.Rendering -=
                                                          new EventHandler(MoveCanvas);
                                                  }));
        }

        /// <summary>
        /// Do these procedures when the channel preview has finished playing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleMIDIPreviewPlayingCompleted(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                                   new Action(delegate()
                                                  {
                                                      previewChannelButton.Foreground
                                                          = Brushes.Black;
                                                  }));
        }

        /// <summary>
        /// When the selection on the Channels list view of the Channel Selector changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChannelsListViewSelectionChanged(object sender, EventArgs e)
        {
            midiPlayer.StopPlaying();
            previewChannelButton.Foreground = Brushes.Black;
        }

        /// <summary>
        /// Override some program event handlers to ensure extra things are properly processes when application is closing 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosing(CancelEventArgs e) 
        {
            serialPortReadThread.Abort();

            try {
                midiPlayer.OnClosingOperations();
            } catch (NullReferenceException ex) { }

            base.OnClosing(e);
        }

        /// <summary>
        /// Override some program event handlers to ensure extra things are properly processes when application is closed
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosed(EventArgs e) 
        {
            try {
                midiPlayer.OnClosedOperations();
            } catch (NullReferenceException ex) { }

            base.OnClosed(e);
        }

        /// <summary>
        /// Initializes the main canvas that contains the scrolling canvas
        /// </summary>
        private void InitializeCanvas() 
        {
            tb_SongTitle.Text = midiInfo.Title;
            ShowCanvasChildren();
        }

        /// <summary>
        /// Initializes the inner scrolling canvas
        /// </summary>
        private void InitializeSubCanvas()
        {
            if (i_Channel < 0)
                return;

            int j = 0;
            long lastNote = 0;
            long firstStart = 0;
            long smallestNoteLength = 0;
            foreach (Note note in midiInfo.notesForAllChannels[i_Channel]) {
                j++;
                if (smallestNoteLength == 0)
                    smallestNoteLength = note.EndTime - note.BeginTime;
                else if (smallestNoteLength > (note.EndTime - note.BeginTime))
                    smallestNoteLength = (note.EndTime - note.BeginTime);
                if (note.EndTime > lastNote)
                    lastNote = note.EndTime;
            }

            if (smallestNoteLength < 300)
                multiplier = 300 / smallestNoteLength;

            if (midiInfo.i_TimeSignatureNumerator == 0)
                MessageBox.Show(Convert.ToString("Warning! Time Signature is 0"));


            DrawGridLines(lastNote, (int)(midiInfo.i_TempoInBPM * multiplier), 4);


            Note[] notesTempArray = new Note[midiInfo.notesForAllChannels[i_Channel].Count];
            //MessageBox.Show("1");
            {
                int i = 0;
                foreach (Note note in midiInfo.notesForAllChannels[i_Channel])
                {
                    notesTempArray[i] = note;
                    i++;
                }
            }

            // Transpose for guitar
            Transposer.TransposeReturnStatus transposeReturnStatus = Transposer.Transpose(notesTempArray, 40, 80);

            if (transposeReturnStatus == Transposer.TransposeReturnStatus.AllNotesAlreadyInRange)
            {
                debugConsole.AddText("All channel notes in range, no need to transpose.\n");
            }
            else if (transposeReturnStatus == Transposer.TransposeReturnStatus.TransposeUnsuccessful)
            {
                debugConsole.AddText("WARNING: Transpose was unsuccessful.\n");
            }
            else
            {
                debugConsole.AddText("Transpose was successful.\n");
            }


            // Allocate strings for guitar
            stringAllocator.Clear();
            foreach (Note note in notesTempArray) {
                stringAllocator.AddNote(note);
            }

            debugConsole.AddText(string.Format("StringAllocator # dropped notes: {0}\n", stringAllocator.NumDroppedNotes));
            debugConsole.AddText(string.Format("StringAllocator # out of range notes: {0}\n", stringAllocator.NumOutOfRangeNotes));


            /*
            Note noteTemp;
            for (int i = notesTempArray.Length - 1; i >= 0; i--) {
                noteTemp = notesTempArray[i];
                if (i == notesTempArray.Length - 1) {
                    firstStart = noteTemp.BeginTime / 10;
                }
                Fingering(noteTemp.NoteNumber, instrument, noteTemp.BeginTime / 10, noteTemp.EndTime / 10);
            }
            */

            for (int i = 0; i < stringAllocator.Alloc.Length; i++)
            {
                foreach (GuitarNote guitarNote in stringAllocator.Alloc[i])
                {
                    Fingering(guitarNote.NoteNumber, instrument, guitarNote.BeginTime / 10, guitarNote.EndTime / 10, 
                        guitarNote.FretNumber, guitarNote.StringNumber);
                }
            }
        }

        /// <summary>
        /// Sets the loading progress display 
        /// </summary>
        /// <param name="i_ProgressPercentage"></param>
        public void SetProgress(int i_ProgressPercentage) 
        {
            progressBar.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 255, 0));
            progressBar.BorderThickness = new Thickness(i_ProgressPercentage * 4, 0, 0, 0);
            progressBar.Background = new SolidColorBrush(Color.FromRgb(255, 0, 0));
            progressBar.Height = 8;
            percentageText.Text = i_ProgressPercentage + "% Complete";
        }


        private long previousTime;
        private long currentTime;
        private long frameCount = 0;
        /// <summary>
        /// Updates canvas position
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MoveCanvas(object sender, EventArgs e) 
        {
            UpdateScoreDisplay(sender, e);
            UpdateFingeringDisplay(sender, e);
            frameCount++;
            long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            currentTime = milliseconds;

            if (frameCount % 10 == 0 && currentTime != previousTime)
                FPS.Header = string.Format("{0} FPS", 1000 / (currentTime - previousTime));

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


        /// <summary>
        /// Hides everything drawn on canvas
        /// </summary>
        private void HideCanvasChildren() 
        {
            r_HeaderBackground.Visibility = Visibility.Hidden;
            keyLine.Visibility = Visibility.Hidden;
            tb_ScoreDisplay.Visibility = Visibility.Hidden;
            tb_SongTitle.Visibility = Visibility.Hidden;
            for (int i = 0; i < r_instrument.Length; i++) {
                r_instrument[i].Visibility = Visibility.Hidden;
                tb_instrument[i].Visibility = Visibility.Hidden;
            }
        }

        /// <summary>
        /// Shows everything drawn on canvas
        /// </summary>
        private void ShowCanvasChildren() 
        {
            r_HeaderBackground.Visibility = Visibility.Visible;
            keyLine.Visibility = Visibility.Visible;
            tb_ScoreDisplay.Visibility = Visibility.Visible;
            tb_SongTitle.Visibility = Visibility.Visible;
            for (int i = 0; i < r_instrument.Length; i++) {
                r_instrument[i].Visibility = Visibility.Visible;
                tb_instrument[i].Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Resets the subcanvas by clearing its children and restoring its original position
        /// </summary>
        /// <param name="clearCanvasChildren"></param>
        private void ResetSubCanvas(bool clearCanvasChildren) 
        {
            if (clearCanvasChildren) {
                subcanv.Children.Clear();
                gridlines.Children.Clear();
            }
            subcanv.SetValue(Canvas.TopProperty, i_InitialCanvasPosY);
            gridlines.SetValue(Canvas.TopProperty, i_InitialCanvasPosY);
        }

        /// <summary>
        /// Make the subcanvas visible
        /// </summary>
        private void ShowSubCanvas() 
        {
            subcanv.Visibility = Visibility.Visible;
            gridlines.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Make the subcanvas hidden
        /// </summary>
        private void HideSubCanvas() 
        {
            r_HeaderBackground.Visibility = Visibility.Hidden;
            keyLine.Visibility = Visibility.Hidden;
            subcanv.Visibility = Visibility.Hidden;
            gridlines.Visibility = Visibility.Hidden;
        }


        /// <summary>
        /// Updates the score being displayed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateScoreDisplay(object sender, EventArgs e) 
        {
            try
            {
                tb_ScoreDisplay.Text = string.Format("{0}/{1} notes correct ~ {2}", score.i_NumNotesScored,
                                                     midiPlayer.NumChannelNotesPlayed,
                                                     score.ScoreGrade(midiPlayer.NumChannelNotesPlayed));
            }
            catch (NullReferenceException ex) { }
        }

        /// <summary>
        /// Kills all open windows on app shutdown
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void WindowClosing(object sender, CancelEventArgs e)
        {
            App.Current.Shutdown();
        }

        /// <summary>
        /// Updates the fingerings being displayed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateFingeringDisplay(object sender, EventArgs e) 
        {
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

        /// <summary>
        /// 
        /// </summary>
        public struct ColorRGB 
        {
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="h"></param>
        /// <param name="sl"></param>
        /// <param name="l"></param>
        /// <returns></returns>
        public ColorRGB HSL2RGB(double h, double sl, double l) 
        {
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

    public static class ExtensionMethods 
    {
        private static Action EmptyDelegate = delegate { };
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="uiElement"></param>
        public static void Refresh(this UIElement uiElement) 
        {
            uiElement.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
        }
    }

}