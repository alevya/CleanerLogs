using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Configuration;
using System.Linq;
using System.Windows;
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
      CleanCommand = new DelegateCommand(CleanAsync);
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

      var ftpLoader = new FtpClient.FtpClient(md.Ip);
      var list = ftpLoader.ListFiles("USBDisk/Foreman7");
      foreach (var fileSrc in list)
      {
        var pathSrc = Path.Combine("USBDisk/Foreman7", fileSrc);
        var pathTrg = Path.Combine(@"K:\TempFtp", fileSrc);
        ftpLoader.DownloadFile(pathSrc, pathTrg);
      }

    }

    private async void CleanAsync(object obj)
    {
      var md = MachinesDetails.SingleOrDefault(item => item.IsSelected);
      if (md == null) return;

      var ftpLoader = new FtpClient.FtpClient(md.Ip);
      string pathUSBDisk = @"USBDisk\Foreman7";
      string pathNandFlash = @"NandFlash\Foreman7";

      var listUSBDisk = await ftpLoader.ListFilesAsync(pathUSBDisk);
      var listNandFlash = await ftpLoader.ListFilesAsync(pathNandFlash);

      var diUSBDisk = Directory.CreateDirectory(Path.Combine(@"K:\TempFtp", "USBDisk"));
      var diNandFlash = Directory.CreateDirectory(Path.Combine(@"K:\TempFtp", "NandFlash"));

      foreach (var fileSrc in listUSBDisk.Where(file => file.EndsWith(".log")))
      {
        var pathSrc = Path.Combine(pathUSBDisk, fileSrc);
        var pathTrg = Path.Combine(diUSBDisk.FullName,fileSrc);
        await ftpLoader.DownloadFileAsync(pathSrc, pathTrg);
      }

      foreach (var fileSrc in listNandFlash.Where(file => file.EndsWith(".log")))
      {
        var pathSrc = Path.Combine(pathNandFlash, fileSrc);
        var pathTrg = Path.Combine(diNandFlash.FullName, fileSrc);
        string res = await ftpLoader.DownloadFileAsync(pathSrc, pathTrg);
      }
      MessageBox.Show("Success");
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
