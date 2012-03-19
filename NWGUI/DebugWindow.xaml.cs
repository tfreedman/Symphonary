using System.Windows;
using System.ComponentModel;
using System.Windows.Input;

namespace Symphonary 
{
    /// <summary>
    /// Interaction logic for DebugWindow
    /// </summary>
    public partial class DebugWindow : Window 
    {
        public delegate void ChangeTextDelegate(string text);
        bool consoleNowCloseable = false;

        public DebugWindow() 
        {
            InitializeComponent();
        }

        public void ChangeText(string text) 
        {
            textbox1.Text = text;
            textbox1.ScrollToEnd();
        }

        public void AddText(string text)
        {
            textbox1.Text = textbox1.Text + "\n" + text;
            textbox1.ScrollToEnd();
        }

        public void WindowClosing(object sender, CancelEventArgs e)
        {
            this.Hide();
            e.Cancel = true;
        }


        private void Window_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.OemTilde) {
                consoleNowCloseable = true;
            }
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.OemTilde && consoleNowCloseable) {
                    consoleNowCloseable = false;
                    NWGUI.isConsoleOpen = false;
                    this.Hide();
            }
        }


    }
}
