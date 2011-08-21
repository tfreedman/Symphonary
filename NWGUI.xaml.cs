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

        LoadingScreen loadingScreen;

        int i_Channel = 0;
        MidiPlayer midiPlayer;
        MidiInfo midiInfo;

        public NWGUI()
        {
            InitializeComponent();

            initializeCanvas();
        }

        private void animate_Clicked(object sender, RoutedEventArgs e)
        {
            if (b_AnimationStarted)
            {
                MessageBox.Show("The animation already started");
                return;
            }

            DispatcherTimer dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Interval = TimeSpan.FromSeconds(0.0167);
            dispatcherTimer.Tick += new EventHandler(moveCanvas);
            dispatcherTimer.Start();

            b_AnimationStarted = true;
        }

        private void startMusic_Clicked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (midiPlayer.isFinishedLoading())
                {
                    midiPlayer.startPlaying();
                }
                else
                {
                    MessageBox.Show("Please wait for the MIDI file to finish loading");
                }
            }
            catch (NullReferenceException ex)
            {
                MessageBox.Show("Please load a MIDI file first!");
            }
            
            /*
            canv.Margin = new Thickness(0, 0, 0, 0);
            canv.Background = new SolidColorBrush(Colors.White);

            //The Rectangle
            Rectangle[] note = new Rectangle[82];

            r.Fill = new SolidColorBrush(Color.FromRgb(144,187,69));
            r.Stroke = new SolidColorBrush(Color.FromRgb(114, 148, 55));
            r.StrokeThickness = 2;
            r.Width = 46;
            r.Height = 160;
            r.SetValue(Canvas.LeftProperty, (double)122);
            r.SetValue(Canvas.TopProperty, (double)temp);
            canv.Children.Add(r);
            //canv.InvalidateVisual();
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(0.0167);
            timer.Tick += new EventHandler(drawFrame);
            timer.Start();
             */
        }


        private void open_Clicked(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "MIDI Files (*.mid)|*.mid|All Files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;

            if (openFileDialog.ShowDialog().Value)
            {
                try
                {
                    midiPlayer.OnClosingOperations();
                    midiPlayer.OnClosedOperations();
                }
                catch (NullReferenceException ex) { }

                try
                {
                    loadingScreen.Close();
                }
                catch (NullReferenceException ex) { }

                loadingScreen = new LoadingScreen();
                loadingScreen.Show();
                midiPlayer = new MidiPlayer(openFileDialog.FileName, handleMIDILoadProgressChanged, handleMIDILoadCompleted);
                midiInfo = new MidiInfo(openFileDialog.FileName, i_Channel);
            }
        }

        private void debug_Clicked(object sender, RoutedEventArgs e)
        {
            
        }

        private void about_Clicked(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("This will be implemented :D");
        }

        private void NWGUI_KeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                if (!midiPlayer.isPlaying())
                    return;

                if (e.Key.ToString() == "M")
                {
                    midiPlayer.unmuteOtherTracks();
                }
            }
            catch (NullReferenceException ex)
            {
            }
        }

        private void NWGUI_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (!midiPlayer.isPlaying())
                    return;

                if (e.Key.ToString() == "M")
                {
                    midiPlayer.muteOtherTracks();
                }
            }
            catch (NullReferenceException ex)
            {
            }
        }


        private void handleMIDILoadProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            try
            {
                loadingScreen.setProgress(e.ProgressPercentage);
            }
            catch (NullReferenceException ex) { }
        }


        private void handleMIDILoadCompleted(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                loadingScreen.Close();
            }
            catch (NullReferenceException ex) { }
        }

        

        // override some program event handlers to ensure extra things are loaded/closed properly on start/close

        protected override void OnClosing(CancelEventArgs e)
        {
            try
            {
                midiPlayer.OnClosingOperations();
            }
            catch (NullReferenceException ex) { }

            base.OnClosing(e);
        }


        protected override void OnClosed(EventArgs e)
        {
            try
            {
                midiPlayer.OnClosedOperations();
            }
            catch (NullReferenceException ex) { }

            base.OnClosed(e);
        }


        private void initializeCanvas()
        {
            canv.Background = new SolidColorBrush(Colors.Black);

            Rectangle rect1 = new Rectangle();
            rect1.Height = 50;
            rect1.Width = 600;
            rect1.Fill = new SolidColorBrush(Colors.Blue);
            rect1.SetValue(Canvas.LeftProperty, (double)50);
            rect1.SetValue(Canvas.TopProperty, (double)400);

            Rectangle rect2 = new Rectangle();
            rect2.Height = 80;
            rect2.Width = 30;
            rect2.Fill = new SolidColorBrush(Colors.Red);
            rect2.SetValue(Canvas.LeftProperty, (double)350);
            rect2.SetValue(Canvas.TopProperty, (double)100);

            Rectangle rect3 = new Rectangle();
            rect3.Height = 80;
            rect3.Width = 30;
            rect3.Fill = new SolidColorBrush(Colors.Green);
            rect3.SetValue(Canvas.LeftProperty, (double)200);
            rect3.SetValue(Canvas.TopProperty, (double)200);

            subcanv.Children.Add(rect2);
            subcanv.Children.Add(rect3);

            canv.Children.Add(rect1);
        }


        private void moveCanvas(object sender, EventArgs e)
        {
            double i_CurPosY = (double)(subcanv.GetValue(Canvas.TopProperty));
            
            if ((i_CanvasMoveDirection == -1 && i_CurPosY <= -200) || (i_CanvasMoveDirection == 1 && i_CurPosY >= 400))
                i_CanvasMoveDirection = -i_CanvasMoveDirection;

            subcanv.SetValue(Canvas.TopProperty, i_CurPosY + 3 * i_CanvasMoveDirection);
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

            public ColorRGB(Color value)
            {
                this.R = value.R;
                this.G = value.G;
                this.B = value.B;
            }

            public static implicit operator Color(ColorRGB rgb)
            {
                Color c = Color.FromArgb(1, rgb.R, rgb.G, rgb.B);
                return c;
            }

            public static explicit operator ColorRGB(Color c)
            {
                return new ColorRGB(c);
            }
        }

        public ColorRGB HSL2RGB(double h, double sl, double l)
        {
            double v;
            double r,g,b;
            r = l;   // default to gray
            g = l;
            b = l;
            v = (l <= 0.5) ? (l * (1.0 + sl)) : (l + sl - l * sl);
            if (v > 0)
            {
                  double m;
                  double sv;
                  int sextant;
                  double fract, vsf, mid1, mid2;
 
                  m = l + l - v;
                  sv = (v - m ) / v;
                  h *= 6.0;
                  sextant = (int)h;
                  fract = h - sextant;
                  vsf = v * sv * fract;
                  mid1 = m + vsf;
                  mid2 = v - vsf;
                  switch (sextant)
                  {
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


    public class Fingering 
    {
        public Fingering(string note, int instrument, int startTime, int endTime)
        {
            if (instrument == 41)
            {//This is a violin.


            }
        }
    }

    public static class ExtensionMethods
    {
        private static Action EmptyDelegate = delegate() { };
        public static void Refresh(this UIElement uiElement)
        {
            uiElement.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
        }
    }
}
