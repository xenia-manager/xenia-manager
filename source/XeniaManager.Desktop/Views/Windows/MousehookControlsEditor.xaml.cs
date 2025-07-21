using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;



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
    private string _pendingAddKey;
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
            // Check for duplicate before updating
            bool isDuplicate = ViewModel.KeyBindings.Any(item =>
                item != currentBindingItem && // Exclude the current item being edited
                item.Key == currentBindingItem.Key &&
                string.Equals(item.Value, e.Key, StringComparison.OrdinalIgnoreCase));

            if (isDuplicate)
            {
                Dispatcher.BeginInvoke(() =>
                {
                    CustomMessageBox.Show("Duplicate Binding", $"The binding '{e.Key}' is already assigned to '{currentBindingItem.Key}'.");
                });
            }
            else
            {
                // Update the Value property
                currentBindingItem.Value = e.Key;
            }
        }

        // Exit listening mode
        isListeningForKey = false;

        InputListener.Stop(); // Stop InputListener
                              // Detach event handlers
        InputListener.KeyPressed -= InputListener_KeyPressedListener;
        InputListener.MouseClicked -= InputListener_KeyPressedListener;
    }

    private void TextBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
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

    private void BtnAddKeybindingButton_Click(object sender, RoutedEventArgs e)
    {
        string newKey = ViewModel.SelectedGamePadKey;

        // Check for duplicate empty binding for this key
        if (ViewModel.KeyBindings.Any(kb => kb.Key == newKey && string.IsNullOrEmpty(kb.Value)))
        {
            return;
        }

        // Notify user to press a key or mouse button
        Application.Current.Dispatcher.Invoke(() =>
        {
            CustomMessageBox.Show(LocalizationHelper.GetUiText("MessageBox_InputRequired"), string.Format(LocalizationHelper.GetUiText("MessageBox_InputRequiredText"), newKey));
        });

        // Start listening for input
        InputListener.Start();
        InputListener.KeyPressed += AddKeybinding_KeyPressedListener;
        InputListener.MouseClicked += AddKeybinding_KeyPressedListener;

        // Store the key being added (as a field)
        _pendingAddKey = newKey;
    }

    private void AddKeybinding_KeyPressedListener(object? sender, InputListener.KeyEventArgs e)
    {
        // Remove event handlers
        InputListener.KeyPressed -= AddKeybinding_KeyPressedListener;
        InputListener.MouseClicked -= AddKeybinding_KeyPressedListener;
        InputListener.Stop();

        string newKey = _pendingAddKey;
        string newBinding = e.Key;

        // Always do collection modifications on the UI thread!
        Application.Current.Dispatcher.Invoke(() =>
        {
            // Check for duplicate binding
            if (ViewModel.KeyBindings.Any(kb => kb.Key == newKey && kb.Value == newBinding))
            {
                CustomMessageBox.Show(LocalizationHelper.GetUiText("MessageBox_Duplicate"), string.Format(LocalizationHelper.GetUiText("MessageBox_DuplicateBinding"), newBinding, newKey));
                return;
            }

            // Add the new keybinding
            ViewModel.AddKeyBinding(newKey, newBinding);
            CustomMessageBox.Show(LocalizationHelper.GetUiText("MessageBox_KeybindingAdded"), string.Format(LocalizationHelper.GetUiText("MessageBox_KeybindingAddedText"), newKey, newBinding));
        });
    }

    private void BtnDeleteKeybinding_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.DataContext is KeyBindingItem item)
        {
            // Remove the keybinding from the ViewModel
            ViewModel.RemoveKeyBinding(item.Key, item.Value);
        }
    }
}