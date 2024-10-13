using System;
using System.Windows.Controls;

namespace XeniaManager.DesktopApp.Pages
{
    public partial class Library : Page
    {
        /// <summary>
        /// Constructor for the Library page
        /// </summary>
        public Library()
        {
            InitializeComponent();
            LoadGames();
        }
    }
}