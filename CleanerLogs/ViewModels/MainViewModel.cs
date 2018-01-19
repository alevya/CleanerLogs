using System;
using System.Collections.ObjectModel;
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

    public ObservableCollection<MachineDetailViewModel> MachinesDetails { get; set; }
    #endregion

    #region Command
    public ICommand CleanCommand { get; }

    #endregion

    #region Methods

    public void InitConfig()
    {
      var m = ConfigurationManager.GetSection("StartupMachines") as MachinesConfigSection;
      if (m == null || m.MachineItems.Count == 0)
      {
        throw new Exception("");
      }
      string confSavePath = ConfigurationManager.AppSettings["SavePath"];
      if (string.IsNullOrEmpty(confSavePath))
      {
        SavePath = Path.GetTempPath();
      }
      else
      {
        
      }
      MachinesDetails = new ObservableCollection<MachineDetailViewModel>();
      foreach (MachineElement item in m.MachineItems)
      {
        MachinesDetails.Add(new MachineDetailViewModel(item.MachineNumber, item.MachineIp));
      }
    }

    private void Clean(object obj)
    {

    }
    #endregion
  }
}
