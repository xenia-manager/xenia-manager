using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace XeniaManager.Desktop.ViewModel.Pages
{
    public class AboutPageViewModel : INotifyPropertyChanged
    {
        #region Variables

        private string _applicationVersion = $"v{App.AppSettings.Settings.GetCurrentVersion()}";

        public string ApplicationVersion
        {
            get => _applicationVersion;
            set
            {
                _applicationVersion = value;
                OnPropertyChanged();
            }
        }
        
        #endregion

        #region Constructors

        public AboutPageViewModel()
        {

        }
        #endregion

        #region Functions
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
