using System.ComponentModel;


namespace CleanerLogs.ViewModel
{
  internal abstract class BaseViewModel : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected virtual void OnPropertyChanged(PropertyChangedEventArgs args)
    {
      PropertyChanged?.Invoke(this, args);
    }
  }
}
