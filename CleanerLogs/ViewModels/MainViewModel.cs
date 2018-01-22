using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CleanerLogs.Commands;

namespace CleanerLogs.ViewModels
{
  internal class MainViewModel : BaseViewModel
  {
    private const string USBDISK_FOREMAN = @"USBDisk\Foreman7";
    private const string NANDFLASH_FOREMAN = @"NandFlash\Foreman7";

    private string _savePath;
    private bool _removeFromBlocks;

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
        OnPropertyChanged();
      }
    }

    public bool RemoveFromBlocks
    {
      get { return _removeFromBlocks;}
      set
      {
        _removeFromBlocks = value;
        OnPropertyChanged();
      }
    }

    public ObservableCollection<MachineDetailViewModel> MachinesDetails { get; set; }
  
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
      SavePath = string.IsNullOrEmpty(confSavePath) ? Path.GetTempPath() : confSavePath;

      string confRemoveFromBlocks = ConfigurationManager.AppSettings["RemoveFromBlocks"];
      RemoveFromBlocks = bool.TryParse(confRemoveFromBlocks, out _removeFromBlocks) ? _removeFromBlocks : true;


      MachinesDetails = new ObservableCollection<MachineDetailViewModel>();
      foreach (MachineElement item in m.MachineItems)
      {
        MachinesDetails.Add(new MachineDetailViewModel(item.MachineNumber, item.MachineIp));
      }
    }

    private async void CleanAsync(object obj)
    {
      var listMd = MachinesDetails.Where(item => item.IsSelected).ToList();

      const int CONCURRENCY_LEVEL = 3;
      var mapTasks = new Dictionary<Task, string>(); 
      var result = new Dictionary<string, bool>(); 
      int nextIndex = 0;

      while (nextIndex < CONCURRENCY_LEVEL && nextIndex < listMd.Count)
      {
        string ip = listMd.ElementAt(nextIndex).Ip;
        mapTasks.Add(DownloadAndDeleteAsync(ip), ip);
        nextIndex++;
      }
      while (mapTasks.Count > 0)
      {
        try
        {
          Task resultTask = await Task.WhenAny(mapTasks.Keys);
          mapTasks.TryGetValue(resultTask, out string ipValue);
          mapTasks.Remove(resultTask);

          result.Add(ipValue, resultTask.Status == TaskStatus.RanToCompletion);
          await resultTask;
        }
        catch (Exception exc)
        {
          // ignored
        }

        if(nextIndex >= listMd.Count) continue;

        string ip = listMd.ElementAt(nextIndex).Ip;
        mapTasks.Add(DownloadAndDeleteAsync(ip), ip);
        nextIndex++;

      }

      MessageBox.Show("Success");
    }

    private async Task DownloadAndDeleteAsync(string ip)
    {
      var ftpLoader = new FtpClient.FtpClient(ip);
    
      var listUSBDisk = await ftpLoader.ListFilesAsync(USBDISK_FOREMAN);
      var listNandFlash = await ftpLoader.ListFilesAsync(NANDFLASH_FOREMAN);

      var diUSBDisk = Directory.CreateDirectory(Path.Combine(SavePath, "USBDisk"));
      var diNandFlash = Directory.CreateDirectory(Path.Combine(SavePath, "NandFlash"));

      foreach (var fileSrc in listUSBDisk.Where(file => file.EndsWith(".log")))
      {
        var pathSrc = Path.Combine(USBDISK_FOREMAN, fileSrc);
        var pathTrg = Path.Combine(diUSBDisk.FullName, fileSrc);
        var result = await ftpLoader.DownloadFileAsync(pathSrc, pathTrg);
        if (result == FtpStatusCode.ClosingData && RemoveFromBlocks)
        {
          await ftpLoader.DeleteFileAsync(pathSrc);
        }
      }

      foreach (var fileSrc in listNandFlash.Where(file => file.EndsWith(".log")))
      {
        var pathSrc = Path.Combine(NANDFLASH_FOREMAN, fileSrc);
        var pathTrg = Path.Combine(diNandFlash.FullName, fileSrc);
        var result = await ftpLoader.DownloadFileAsync(pathSrc, pathTrg);

        if (result == FtpStatusCode.ClosingData && RemoveFromBlocks)
        {
          await ftpLoader.DeleteFileAsync(pathSrc);
        }
      }

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
