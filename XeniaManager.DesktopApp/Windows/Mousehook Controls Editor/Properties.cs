using System.Collections.ObjectModel;
using System.Windows;

// Imported
using Serilog;

namespace XeniaManager.DesktopApp.Windows
{
    /// <summary>
    /// Class used to display the keybindings and their values
    /// </summary>
    public class KeyBindingItem
    {
        public string Key { get; set; }
        public string Value { get; set; }
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
        
        // Constructor
        public MousehookControlsEditor(List<GameBinding> gameKeyBindings)
        {
            InitializeComponent();
            this.gameBindings = gameKeyBindings;
            InitializeAsync();
        }
    }
}