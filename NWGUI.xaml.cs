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
        
        double i_InitialCanvasPosY;

        MainWindow debugConsole;
        ChannelSelector channelSelector;
        LoadingScreen loadingScreen;


        int i_Channel = -1;
        MidiPlayer midiPlayer;
        MidiInfo midiInfo;

        DispatcherTimer dispatcherTimer = new DispatcherTimer();       

        public NWGUI() 
        {
            InitializeComponent();

            i_InitialCanvasPosY = (double)(subcanv.GetValue(Canvas.TopProperty));

            dispatcherTimer.Interval = TimeSpan.FromSeconds(0.0167);
            dispatcherTimer.Tick += new EventHandler(moveCanvas);
        }

        private void start_Clicked(object sender, RoutedEventArgs e) 
        {
            try 
            {
                if (midiPlayer.isPlaying()) {
                    MessageBox.Show("The file is currently being played, please have it finish first.");
                    return;
                }
                
                if (midiPlayer.isFinishedLoading()) 
                {
                    if (i_Channel != -1)
                        midiPlayer.setPersistentChannel(i_Channel);

                    midiPlayer.startPlaying();

                    initializeCanvas();

                    int j = 0;

                    foreach (Note note in midiInfo.l_Notes)
                        j++;
                    for (int i = j - 1; i >= 0; i--) {
                        fingering(midiInfo.l_Notes[i].i_NoteNumber, 41, (long)midiInfo.l_Notes[i].li_BeginTime, (long)midiInfo.l_Notes[i].li_EndTime);
                    }

                    //MessageBox.Show(Convert.ToString(maxEndTime));
                    
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

        private void stop_Clicked(object sender, RoutedEventArgs e)
        {
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


        private void fingering(int note, int instrument, long startTime, long endTime) {
            if (instrument == 41) {
                int margin = 300;
                int noteNumber = note % 7;
                string noteString = "";

                if (noteNumber == 0) { noteString = "0"; }
                else if (noteNumber == 1) { noteString = "1-"; }
                else if (noteNumber == 2) { noteString = "1"; }
                else if (noteNumber == 3) { noteString = "2-"; }
                else if (noteNumber == 4) { noteString = "2"; }
                else if (noteNumber == 5) { noteString = "3"; }
                else if (noteNumber == 6) { noteString = "3+"; }
                else if (noteNumber == 7) { noteString = "4"; }
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
                textBlock.SetValue(Canvas.TopProperty, (double)(-1 * startTime) + r.Height - 50);
                r.SetValue(Canvas.TopProperty, (double)(-1 * startTime));
                subcanv.Children.Add(r);
                subcanv.Children.Add(textBlock);
            }
            else if (instrument == 74) {
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
                    r[i].Height = (endTime - startTime);
                    r[i].SetValue(Canvas.TopProperty, (double)(-1 * startTime));
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

                b_AnimationStarted = false;
                dispatcherTimer.Stop();

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
                foreach (Note note in midiInfo.l_Notes) {
                    debugConsole.textbox1.Text += note.li_BeginTime + " " + note.li_EndTime + Environment.NewLine;
                }

                debugConsole.textbox1.Text += ">> " + midiInfo.i_EndTime + Environment.NewLine;

                foreach (MidiEvent metadata in midiInfo.l_Metadata) {
                    debugConsole.textbox1.Text += metadata.ToString();
                }

                /*
                for (int i = 0; i < midiInfo.midiEventCollection[midiInfo.a_ExistingChannelOrder[i_Channel]].Count; i++) {
                    debugConsole.textbox1.Text += midiInfo.midiEventCollection[midiInfo.a_ExistingChannelOrder[i_Channel]][i].ToString() + Environment.NewLine;
                }
                 */
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
            rect1.Height = 3;
            rect1.Width = 1024;
            rect1.Fill = new SolidColorBrush(Color.FromRgb(51, 51,51));
            rect1.SetValue(Canvas.TopProperty, (double)650);
            canv.Children.Add(rect1);

            Rectangle rect2 = new Rectangle();
            rect2.Height = 55;
            rect2.Width = 1024;
            rect2.Fill = new SolidColorBrush(Color.FromRgb(51,51,51));
            rect2.SetValue(Canvas.TopProperty, (double)19);
            canv.Children.Add(rect2);

            TextBlock textBlock = new TextBlock();
            textBlock.Text = "Example Song";
            textBlock.Height = 50;
            textBlock.Width = 400;
            textBlock.Foreground = new SolidColorBrush(Colors.White);
            textBlock.FontStyle = FontStyles.Italic;
            textBlock.FontSize = 30;
            textBlock.TextAlignment = TextAlignment.Left;
            textBlock.SetValue(Canvas.TopProperty, (double)25);
            textBlock.SetValue(Canvas.LeftProperty, (double)10);
            canv.Children.Add(textBlock);

            TextBlock score = new TextBlock();
            score.Text = "50/52 notes correct ~ A+";
            score.Height = 50;
            score.Width = 400;
            score.Foreground = new SolidColorBrush(Colors.White);
            score.FontSize = 30;
            score.TextAlignment = TextAlignment.Right;
            score.SetValue(Canvas.TopProperty, (double)25);
            score.SetValue(Canvas.LeftProperty, (double)600);
            canv.Children.Add(score);
        }

        private void moveCanvas(object sender, EventArgs e) {
            double i_CurPosY = (double)(subcanv.GetValue(Canvas.TopProperty));
            subcanv.SetValue(Canvas.TopProperty, i_CurPosY + 10);
        }


        private void resetSubCanvas()
        {
            subcanv.Children.Clear();
            subcanv.SetValue(Canvas.TopProperty, i_InitialCanvasPosY);
        }


        /*void drawFrame(object sender, EventArgs e)
        {
            temp++;
            r.SetValue(Canvas.TopProperty, (double)temp);
            canv.InvalidateVisual();
        }
         */

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
        private static Action EmptyDelegate = delegate() { };
        public static void Refresh(this UIElement uiElement) {
            uiElement.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
        }
    }
}
