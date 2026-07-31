using System.Windows;

namespace FluentTB
{
    /// <summary>Simple modal info dialog.</summary>
    public partial class Infobox : Window
    {
        public Infobox()
        {
            InitializeComponent();
        }

        private void okButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}
