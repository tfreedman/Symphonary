using NAudio;
using NAudio.CoreAudioApi;
using NAudio.Midi;
using Microsoft.Win32;
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
using System.Threading;
using System.Windows.Threading;


namespace NiceWindow 
{
    public partial class NWGUI : Window 
    {
        //int temp = 0;
        //Rectangle r = new Rectangle();
        bool b_AnimationStarted = false;
        int i_CanvasMoveDirection = 1;

        MainWindow debugConsole;
        ChannelSelector channelSelector;
        LoadingScreen loadingScreen;


        int i_Channel = -1;
        MidiPlayer midiPlayer;
        MidiInfo midiInfo;

        public NWGUI() {
            InitializeComponent();
        }

        private void start_Clicked(object sender, RoutedEventArgs e) 
        {
            if (midiPlayer.isPlaying()) {
                MessageBox.Show("The file is currently being played, please have it finish first.");
                return;
            }
            
            try 
            {
                if (midiPlayer.isFinishedLoading()) 
                {
                    if (i_Channel != -1)
                        midiPlayer.setPersistentChannel(i_Channel);

                    midiPlayer.startPlaying();

                    initializeCanvas();

                    int i = 0;

                    long maxEndTime = 0;

                    foreach (Note note in midiInfo.l_Notes) {
                        if (note.li_EndTime > maxEndTime)
                            maxEndTime = note.li_EndTime;
                    }
                    //MessageBox.Show(Convert.ToString(maxEndTime));

                    foreach (Note note in midiInfo.l_Notes) {
                        i++;
                        prevTime += (note.li_EndTime - note.li_BeginTime);
                        fingering(note.i_NoteNumber, 41, (long)note.li_BeginTime, (long)note.li_EndTime);
                        string temp = Convert.ToString(note.li_EndTime - note.li_BeginTime) + " " + Convert.ToString(note.li_BeginTime) + " " + Convert.ToString(note.li_EndTime);

                        //MessageBox.Show(temp);
                    }

                    DispatcherTimer dispatcherTimer = new DispatcherTimer();
                    dispatcherTimer.Interval = TimeSpan.FromSeconds(0.0167);
                    dispatcherTimer.Tick += new EventHandler(moveCanvas);
                    dispatcherTimer.Start();

                    b_AnimationStarted = true;
                }
                else {
                    MessageBox.Show("Please wait for the MIDI file to finish loading");
                }


            }
            catch (NullReferenceException ex) {
                MessageBox.Show("Please load a MIDI file first!");
            }
            
        }

        private void stop_Clicked(object sender, RoutedEventArgs e)
        {
            try {
                //midiPlayer.OnClosingOperations();
                //midiPlayer.OnClosedOperations();
                midiPlayer.stopPlaying();
            }
            catch (NullReferenceException ex) { }
        }
        
        long prevTime = 0;
        private void fingering(int note, int instrument, long startTime, long endTime) 
        {
            if (instrument == 41) {
                int margin = 300;
                int noteNumber = note % 7;
                string noteString = "";

                if (noteNumber == 0) { noteString = "0"; }
                else if (noteNumber == 1) { noteString = "1-"; }
                else if (noteNumber == 2) {noteString = "1";}
                else if (noteNumber == 3) {noteString = "2-";}
                else if (noteNumber == 4) {noteString = "2";}
                else if (noteNumber == 5) {noteString = "3";}
                else if (noteNumber == 6) {noteString = "3+";}
                else if (noteNumber == 7) {noteString = "4";}
                int padding = 30;
                Rectangle r = new Rectangle();
                TextBlock textBlock = new TextBlock();
                textBlock.Text = noteString;
                textBlock.Height = 50;
                textBlock.Width = 50;
                r.Width = 46;
          
                if (note >= 55 && note < 62) {
                    textBlock.Foreground = new SolidColorBrush(Colors.White);
                    textBlock.SetValue(Canvas.LeftProperty, (margin + r.Width));
                    r.SetValue(Canvas.LeftProperty, (double)(margin + r.Width));
                    r.Fill = new SolidColorBrush(Color.FromRgb(144, 187, 69));
                    r.Stroke = new SolidColorBrush(Color.FromRgb(114, 148, 55));
                }

                else if (note >= 62 && note < 69) {
                    textBlock.Foreground = new SolidColorBrush(Colors.White);
                    textBlock.SetValue(Canvas.LeftProperty, (margin + (2 * (r.Width + padding))));
                    r.SetValue(Canvas.LeftProperty, (double)(margin + (2 * (r.Width + padding))));
                    r.Fill = new SolidColorBrush(Color.FromRgb(250, 181, 65));
                    r.Stroke = new SolidColorBrush(Color.FromRgb(227, 165, 59));
                }

                else if (note >= 69 && note < 76) {
                    textBlock.Foreground = new SolidColorBrush(Colors.White);
                    textBlock.SetValue(Canvas.LeftProperty, (margin + (3 * (r.Width + padding))));
                    r.SetValue(Canvas.LeftProperty, (double)(margin + (3 * (r.Width + padding))));
                    r.Fill = new SolidColorBrush(Color.FromRgb(220, 42, 62));
                    r.Stroke = new SolidColorBrush(Color.FromRgb(189, 36, 54));
                }

                else if (note >= 76 && note < 83) {
                    textBlock.Foreground = new SolidColorBrush(Colors.White);
                    textBlock.SetValue(Canvas.LeftProperty, (margin + (4 * (r.Width + padding))));
                    r.SetValue(Canvas.LeftProperty, (double)(margin + (4 * (r.Width + padding))));
                    r.Fill = new SolidColorBrush(Color.FromRgb(26, 98, 179));
                    r.Stroke = new SolidColorBrush(Color.FromRgb(24, 82, 148));
                }

                else {
                    textBlock.Foreground = new SolidColorBrush(Colors.White);
                    textBlock.SetValue(Canvas.LeftProperty, (margin + (5 * (r.Width + padding))));
                    r.SetValue(Canvas.LeftProperty, (double)(margin + (5 * (r.Width + padding))));
                    r.Fill = new SolidColorBrush(Colors.Black);
                    r.Stroke = new SolidColorBrush(Colors.Black);
                }

                r.StrokeThickness = 2;
                Canvas.SetZIndex(textBlock, (int)99);
                r.Height = (endTime - startTime);
                textBlock.FontSize = 30;
                textBlock.FontWeight = FontWeights.Bold;
                textBlock.TextAlignment = TextAlignment.Center;
                textBlock.SetValue(Canvas.TopProperty, (double)(-1 * prevTime) + r.Height - 50);
                r.SetValue(Canvas.TopProperty, (double)(-1 * prevTime));
                subcanv.Children.Add(r);
                subcanv.Children.Add(textBlock);
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

                try {
                    loadingScreen.Close();
                }
                catch (NullReferenceException ex) { }

                loadingScreen = new LoadingScreen();
                loadingScreen.Show();
                i_Channel = -1;
                midiPlayer = new MidiPlayer(openFileDialog.FileName, handleMIDILoadProgressChanged, handleMIDILoadCompleted);
                midiInfo = new MidiInfo(openFileDialog.FileName, i_Channel);
            }
        }

        private void debug_Clicked(object sender, RoutedEventArgs e)
        {
            debugConsole = new MainWindow();
            debugConsole.Show();

            try {
                debugConsole.textbox1.Text = midiInfo.s_TimeSignature + " " + midiInfo.i_TimeSignatureNumerator + " " + midiInfo.i_TimeSignatureDenominator + Environment.NewLine;
                /*foreach (MidiEvent entry in midiInfo.l_Metadata) {
                    debugConsole.textbox1.Text += entry.ToString();
                }*/
            }
            catch (NullReferenceException ex) { }
        }


        private void selectChannel_Clicked(object sender, RoutedEventArgs e)
        {
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


        private void channelSelectorOkClicked(object sender, RoutedEventArgs e)
        {
            i_Channel = channelSelector.getSelectedChannel();
            midiInfo.loadChannelNotes(i_Channel);

            channelSelector.Close();
        }


        private void about_Clicked(object sender, RoutedEventArgs e) 
        {
            MessageBox.Show("This will be implemented :D");
        }

        private void NWGUI_KeyUp(object sender, KeyEventArgs e) 
        {
            try {
                if (!midiPlayer.isPlaying())
                    return;

                if (e.Key.ToString() == "M") {
                    midiPlayer.unmuteOtherChannels();
                }
            }
            catch (NullReferenceException ex) { }
        }

        private void NWGUI_KeyDown(object sender, KeyEventArgs e) 
        {
            try {
                if (!midiPlayer.isPlaying())
                    return;

                if (e.Key.ToString() == "M") {
                    midiPlayer.muteOtherChannels();
                }
            }
            catch (NullReferenceException ex) {
            }
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
        }

        // override some program event handlers to ensure extra things are loaded/closed properly on start/close
        protected override void OnClosing(CancelEventArgs e) {
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
            canv.Background = new SolidColorBrush(Colors.White);
            Rectangle rect1 = new Rectangle();
            rect1.Height = 5;
            rect1.Width = 1024;
            rect1.Fill = new SolidColorBrush(Colors.Black);
            rect1.SetValue(Canvas.TopProperty, (double)500);
            canv.Children.Add(rect1);
        }

        private void moveCanvas(object sender, EventArgs e) {
            double i_CurPosY = (double)(subcanv.GetValue(Canvas.TopProperty));
            subcanv.SetValue(Canvas.TopProperty, i_CurPosY + 10);
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
