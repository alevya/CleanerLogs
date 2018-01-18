using System.IO;
using System.Configuration;
using System.Windows.Input;
using CleanerLogs.Commands;
using CleanerLogs.ViewModel;

namespace CleanerLogs.ViewModels
{
  internal class MainViewModel : BaseViewModel
  {
    private string _savePath;

    public MainViewModel()
    {
      _savePath = Path.GetTempPath();
      CleanCommand = new DelegateCommand(Clean);

      var m = ConfigurationManager.GetSection("Machines");
    }

    #region Properties

    public string SavePath
    {
      get { return _savePath; }
      set
      {
        _savePath = value;
        OnPropertyChanged("SavePath");
      }
    }

    #endregion

    #region Command
    public ICommand CleanCommand { get; }

    #endregion

    #region Methods

    private void Clean(object obj)
    {

    }
    #endregion
  }
}
