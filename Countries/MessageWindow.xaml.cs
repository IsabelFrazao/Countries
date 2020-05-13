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

namespace Countries
{
    /// <summary>
    /// Interaction logic for MessageWindow.xaml
    /// </summary>
    public partial class MessageWindow : Window
    {
        public MessageWindow()
        {
            InitializeComponent();

            lbl_Message.Content = "Do you wish to Update the Database?\n This may take a few minutes\nand you will not be able to close the Program.";
        }

        private void btnNo_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mwi = new MainWindow();

            mwi.Close();

            MainWindow mwis = new MainWindow();

            mwis.Show();

            this.Close();            
        }

        private void btnYes_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
