using CleanerLogs.ViewModel;

namespace CleanerLogs.ViewModels
{
  internal class MachineDetailViewModel : BaseViewModel
  {
    private bool _isSelected;
    public MachineDetailViewModel(string number, string ip)
    {
      Number = number;
      Ip = ip;
    }

    #region Properties

    public string Number { get; }
    public string Ip { get; }

    public bool IsSelected
    {
      get { return _isSelected; }
      set
      {
        _isSelected = value;
        OnPropertyChanged("IsSelected");
      }
    }

    #endregion
  }
}
