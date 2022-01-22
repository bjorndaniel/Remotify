using System.Windows;
using System.Windows.Controls;

namespace Spotimote
{
    public partial class SpotimoteWindowControl : UserControl
    {
        public SpotimoteWindowControl()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            VS.MessageBox.Show("Spotimote", "Button clicked");
        }
    }
}