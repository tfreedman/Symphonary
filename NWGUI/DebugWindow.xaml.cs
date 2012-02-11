using System.Windows;
using System.ComponentModel;

namespace Symphonary 
{
    /// <summary>
    /// Interaction logic for DebugWindow
    /// </summary>
    public partial class DebugWindow : Window 
    {
        public delegate void ChangeTextDelegate(string text);

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
            textbox1.Text = textbox1.Text + text;
            textbox1.ScrollToEnd();
        }

        public void WindowClosing(object sender, CancelEventArgs e)
        {
            this.Hide();
            e.Cancel = true;
        }
    }
}
