using System.ComponentModel;
using System.IO;
using System.Windows;


// Imported Libraries
using Wpf.Ui.Controls;
using XeniaManager.Core;
using XeniaManager.Core.Constants;
using XeniaManager.Core.Constants.Emulators;
using XeniaManager.Core.Game;
using XeniaManager.Core.Mousehook;
using XeniaManager.Desktop.Components;
using XeniaManager.Desktop.Utilities;
using XeniaManager.Desktop.ViewModel.Windows;

namespace XeniaManager.Desktop.Views.Windows;
/// <summary>
/// Interaction logic for MousehookControlsEditor.xaml
/// </summary>
public partial class MousehookControlsEditor : FluentWindow
{
    public MousehookControlsEditorViewModel ViewModel { get; set; }
    /// <summary>
    /// Check for listening to keys when changing keybinding
    /// </summary>
    private bool isListeningForKey = false;

    /// <summary>
    /// Current keybinding we're changing
    /// </summary>
    private KeyBindingItem currentBindingItem;

    public MousehookControlsEditor(Game game, List<GameKeyMapping> gameKeyBindings)
    {
        InitializeComponent();
        this.ViewModel = new MousehookControlsEditorViewModel(game, gameKeyBindings);
        this.DataContext = ViewModel;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        ViewModel.SaveKeyBindingChanges();
        BindingsParser.Save(App.Settings.MousehookBindings, Path.Combine(DirectoryPaths.Base, XeniaMousehook.BindingsLocation));
        base.OnClosing(e);
    }

    private void InputListener_KeyPressedListener(object? sender, InputListener.KeyEventArgs e)
    {
        if (currentBindingItem != null)
        {
            // Update the Value property
            currentBindingItem.Value = e.Key;
        }

        // Exit listening mode
        isListeningForKey = false;

        InputListener.Stop(); // Stop InputListener
                              // Detach event handlers
        InputListener.KeyPressed -= InputListener_KeyPressedListener;
        InputListener.MouseClicked -= InputListener_KeyPressedListener;
        CustomMessageBox.ShowAsync("", $"Key binding updated to {e.Key}");
    }

    private void TextBox_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // Simple check to only listen for a key once at a time
        if (!isListeningForKey)
        {
            InputListener.Start(); // Start InputListener
                                   // Get the KeyBindingItem associated with this TextBox
            currentBindingItem = (KeyBindingItem)((FrameworkElement)sender).DataContext;

            // Enable listening mode
            isListeningForKey = true;

            // Attach key and mouse listeners
            InputListener.KeyPressed += InputListener_KeyPressedListener;
            InputListener.MouseClicked += InputListener_KeyPressedListener;
        }

        e.Handled = true; // Prevent the default behavior of the TextBox
    }
}