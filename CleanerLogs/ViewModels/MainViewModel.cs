using System.IO;
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
      SelectPathCommand = new DelegateCommand(SelectPath);
      CleanCommand = new DelegateCommand(Clean);
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

    public ICommand SelectPathCommand { get; }
    public ICommand CleanCommand { get; }

    #endregion

    #region Methods

    private void SelectPath(object obj)
    {

     
    }

    private void Clean(object obj)
    {

    }


    #endregion
  }
}
