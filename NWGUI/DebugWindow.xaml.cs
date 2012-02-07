using System.Windows;

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
    }
}
