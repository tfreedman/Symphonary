using System;
using System.Collections.Generic;
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
        int temp = 0;
        Rectangle r = new Rectangle();
        public NWGUI()
        {
            InitializeComponent();
            //MainWindow window = new MainWindow();
            //window.ShowDialog();
            //System.Windows.Threading.Dispatcher.Run();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
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
        }

        void drawFrame(object sender, EventArgs e)
        {
            temp++;
            r.SetValue(Canvas.TopProperty, (double)temp);
            canv.InvalidateVisual();
        }

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
    public class Fingering {
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
