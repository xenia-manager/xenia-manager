using System;
using System.Windows;

namespace XeniaManager.Updater;

public partial class MainWindow : Window
{
    private string downloadUrl = "https://github.com/xenia-manager/xenia-manager/releases/latest/download/xenia_manager.zip";
    public MainWindow()
    {
        InitializeComponent();
    }
}