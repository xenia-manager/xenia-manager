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
using Xenia_Manager.Classes;

namespace Xenia_Manager.Windows
{
    /// <summary>
    /// Interaction logic for EditGamePatch.xaml
    /// </summary>
    public partial class EditGamePatch : Window
    {
        /// <summary>
        /// Constructor of this window 
        /// </summary>
        /// <param name="selectedGame">Game whose patch will be edited</param>
        public EditGamePatch(InstalledGame selectedGame)
        {
            InitializeComponent();
        }

        /// <summary>
        /// Exits this window
        /// </summary>
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
