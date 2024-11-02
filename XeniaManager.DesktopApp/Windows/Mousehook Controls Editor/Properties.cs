using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

// Imported
using Serilog;

namespace XeniaManager.DesktopApp.Windows
{
    /// <summary>
    /// Class used to display the keybindings and their values
    /// </summary>
    public class KeyBindingItem : INotifyPropertyChanged
    {
        public string Key { get; set; }

        private string _value;

        public string Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    OnPropertyChanged(nameof(Value)); // Notify the UI of the change
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public partial class MousehookControlsEditor : Window
    {
        /// <summary>
        /// Keybindings
        /// Key = Xbox360 Key
        /// Value = Keyboard & Mouse
        /// </summary>
        private ObservableCollection<KeyBindingItem> KeyBindings { get; set; } = new ObservableCollection<KeyBindingItem>();

        /// <summary>
        /// Holds all of the game bindings
        /// </summary>
        private List<GameBinding> gameBindings { get; set; }

        // Used to send a signal that this window has been closed
        private TaskCompletionSource<bool> closeWindowCheck = new TaskCompletionSource<bool>();

        // Check for listening to keys when changing keybinding
        private bool isListeningForKey = false;

        // Current keybinding we're changing
        private KeyBindingItem currentBindingItem;

        // Constructor
        public MousehookControlsEditor(List<GameBinding> gameKeyBindings)
        {
            InitializeComponent();
            this.gameBindings = gameKeyBindings;
            InitializeAsync();
        }
    }
}