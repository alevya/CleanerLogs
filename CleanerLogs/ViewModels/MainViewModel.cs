using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Configuration;
using System.Linq;
using System.Windows.Input;
using CleanerLogs.Commands;
using CleanerLogs.Request;
using CleanerLogs.ViewModel;

namespace CleanerLogs.ViewModels
{
  internal class MainViewModel : BaseViewModel
  {
    private string _savePath;

    public MainViewModel()
    {
      CleanCommand = new DelegateCommand(Clean);
      SelectAllCommand = new DelegateCommand(SelectAll);
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
    //public ObservableCollection<MachineElement> MachinesDetails { get; set; }
    #endregion

    #region Command
    public ICommand CleanCommand { get; }
    public ICommand SelectAllCommand { get; }
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
      var md = MachinesDetails.SingleOrDefault(item => item.IsSelected);
      if(md == null) return;

      var ftpLoader = new FtpLoader(md.Ip);
      ftpLoader.ListFilesAsync("USBDisk/Foreman7");
    }

    private void SelectAll(object obj)
    {
      var isChecked = (bool) obj;
      foreach (var item in MachinesDetails)
      {
        item.IsSelected = isChecked;
      }
    }
    #endregion
  }
}
