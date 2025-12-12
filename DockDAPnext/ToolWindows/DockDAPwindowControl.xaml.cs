using System.Windows;
using System.Windows.Controls;

namespace DockDAPnext
{
    public partial class DockDAPwindowControl : UserControl
    {
        public DockDAPwindowControl()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            VS.MessageBox.Show("DockDAPwindowControl", "Button clicked");
        }
    }
}
