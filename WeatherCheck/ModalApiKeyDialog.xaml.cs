using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WeatherCheck
{
    /// <summary>
    /// Interaction logic for ModalApiKeyDialog.xaml -dialog for asking the user to
    /// enter an API -key.
    /// </summary>
    public partial class ModalApiKeyDialog : Window
    {
        public string ApiKey { get; set; }

        public ModalApiKeyDialog()
        {
            InitializeComponent();
        }

        private void btnCancel_click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnOK_click(object sender, RoutedEventArgs e)
        {
            ApiKey = tbApiKey.Text.ToString();
            Close();
        }
    }
}
