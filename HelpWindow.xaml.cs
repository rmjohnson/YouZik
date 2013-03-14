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

namespace YouZik
{
    /// <summary>
    /// Interaction logic for HelpWindow.xaml
    /// </summary>
    public partial class HelpWindow : Window
    {
        public HelpWindow()
        {
            InitializeComponent();
        }

        private void changePage(object sender, SelectionChangedEventArgs e)
        {
            switch (pageSelector.SelectedIndex)
            {
                case 0:
                    //Basic search
                    break;
                case 1:
                    //Artists
                    break;
                case 2:
                    //Playlists
                    break;
            }
        }
    }
}
