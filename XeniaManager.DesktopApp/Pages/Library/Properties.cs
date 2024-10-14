using System;
using System.Windows.Controls;

// Imported
using Serilog;

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
            InitializeASync();
        }
    }
}