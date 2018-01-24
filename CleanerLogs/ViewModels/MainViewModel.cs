using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Configuration;
using System.IO.Packaging;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CleanerLogs.Commands;

namespace CleanerLogs.ViewModels
{
  internal class MainViewModel : BaseViewModel
  {

    private readonly ConfigurationApp _configurationApp;
    private const string FILE_EXTENSIONS = ".log";
    private const string FOREMAN = "Foreman7";
    private const string USBDISK = "USBDisk";
    private const string NANDFLASH = "NandFlash";
    private readonly Func<string> _openFileFunc;

    private bool _cursor;

    public MainViewModel(Func<string> openFileFunc)
    {
      _configurationApp = new ConfigurationApp();
      _openFileFunc = openFileFunc;
      FileOpenCommand = new DelegateCommand(FileOpen);
      CleanCommand = new DelegateCommand(CleanAsync);
      SelectAllCommand = new DelegateCommand(SelectAll);
    }

    #region Properties

    public string SavePath
    {
      get
      {
        var cSavePath = _configurationApp.SavePath;
        return string.IsNullOrEmpty(cSavePath) ? Path.GetTempPath() : cSavePath;
      }
      set
      {
          _configurationApp.SavePath = value;
        OnPropertyChanged();
      }
    }

    public bool RemoveFromBlocks
    {
      get { return _configurationApp.RemoveFromBlocks; }
    }

    public bool Zipped
    {
      get { return _configurationApp.Zipped; }
      set
      {
          _configurationApp.Zipped = value;
        OnPropertyChanged();
      }
    }

    public bool Cursor
    {
      get { return _cursor; }
      set
      {
        _cursor = value;
        OnPropertyChanged();
      }
    }

    public IProgress<TaskProgress> Progress { get; set; }

    public ObservableCollection<MachineDetailViewModel> MachinesDetails { get; set; }
    

    #endregion

    #region Command
    public ICommand FileOpenCommand { get; }
    public ICommand CleanCommand { get; }
    public ICommand SelectAllCommand { get; }
    #endregion

    #region Methods

    public void InitConfig()
    {
      var m = ConfigurationManager.GetSection("StartupMachines") as MachinesConfigSection;
      if (m == null || m.MachineItems.Count == 0)
      {
        return;
      }

      MachinesDetails = new ObservableCollection<MachineDetailViewModel>();
      foreach (MachineElement item in m.MachineItems)
      {
        MachinesDetails.Add(new MachineDetailViewModel(item.MachineNumber, item.MachineIp));
      }

      Progress = new Progress<TaskProgress>(ReportProgress);
    }

    private void ReportProgress(TaskProgress progress)
    {
      var md = MachinesDetails.SingleOrDefault(item => item.IsSelected && item.Ip == progress.CurrentValue);
      md.Message = progress.CurrentProgressMessage;
    }

    private void FileOpen(object obj)
    {
        var res = _openFileFunc?.Invoke();

    }

    private async void CleanAsync(object obj)
    {
        Cursor = true;
        var listMd = MachinesDetails.Where(item => item.IsSelected).ToList();

        const int CONCURRENCY_LEVEL = 3;
        var mapTasks = new Dictionary<Task, string>(); 
        int nextIndex = 0;

        while (nextIndex < CONCURRENCY_LEVEL && nextIndex < listMd.Count)
        {
        string ip = listMd.ElementAt(nextIndex).Ip;
        mapTasks.Add(DownloadAndDeleteAsync(ip), ip);
        nextIndex++;
        }
        while (mapTasks.Count > 0)
        {
        string ipValue = String.Empty;
        try
        {
            Task resultTask = await Task.WhenAny(mapTasks.Keys);
            mapTasks.TryGetValue(resultTask, out ipValue);
            mapTasks.Remove(resultTask);
            await resultTask;
         
            Progress.Report(new TaskProgress{CurrentProgress = nextIndex
                                            ,TotalProgress = listMd.Count
                                            ,CurrentProgressMessage = $"On {ipValue} ",CurrentValue = ipValue
            });
        }
        catch (Exception exc)
        {
            Progress.Report(new TaskProgress
            {
            CurrentProgress = nextIndex
            ,
            TotalProgress = listMd.Count
            ,
            CurrentProgressMessage = exc.Message
            ,
            CurrentValue = ipValue
            });
        }

        if(nextIndex >= listMd.Count) continue;

        string ip = listMd.ElementAt(nextIndex).Ip;
        mapTasks.Add(DownloadAndDeleteAsync(ip), ip);
        nextIndex++;
        }

        Cursor = false;
    }

    private async Task DownloadAndDeleteAsync(string ip)
    {
      var ftpLoader = new FtpClient.FtpClient(ip, _configurationApp.RequestTimeout, _configurationApp.ReadWriteTimeout);
    
      var listUsbDisk = await ftpLoader.ListFilesAsync(BuildRemotePath(USBDISK));
      var listNandFlash = await ftpLoader.ListFilesAsync(BuildRemotePath(NANDFLASH));

      var logsUsbDisk = listUsbDisk.Where(file => file.EndsWith(FILE_EXTENSIONS));
      var logsNandDisk = listNandFlash.Where(file => file.EndsWith(FILE_EXTENSIONS));

      var rootPathTrg = Path.Combine(SavePath, ip);

      if (Zipped)
      {
        using (var package = Package.Open(rootPathTrg + ".zip", FileMode.Create, FileAccess.ReadWrite))
        {
          foreach (var fileSrc in logsUsbDisk)
          {
            await DoDownloadZipAsync(fileSrc, package, USBDISK, ftpLoader);
          }
          foreach (var fileSrc in logsNandDisk)
          {
            await DoDownloadZipAsync(fileSrc, package, NANDFLASH, ftpLoader);
          }
        }
      }
      else
      {
        var diUsbDisk = Directory.CreateDirectory(Path.Combine(rootPathTrg, USBDISK));
        var diNandFlash = Directory.CreateDirectory(Path.Combine(rootPathTrg, NANDFLASH));

        foreach (var fileSrc in logsUsbDisk)
        {
          var pathTrg = Path.Combine(diUsbDisk.FullName, fileSrc);
          await DoDownloadAsync(fileSrc, pathTrg, USBDISK, ftpLoader);
        }
        foreach (var fileSrc in logsNandDisk)
        {
          var pathTrg = Path.Combine(diNandFlash.FullName, fileSrc);
          await DoDownloadAsync(fileSrc, pathTrg, NANDFLASH, ftpLoader);
        }
      }
    }

    private async Task DoDownloadAsync(string fileNameSrc, string pathTrg, string nameRootStorage, FtpClient.FtpClient ftpLoader)
    {
      var pathSrc = Path.Combine(BuildRemotePath(nameRootStorage), fileNameSrc);

      var result = await ftpLoader.DownloadFileAsync(pathSrc, pathTrg);

      if (result == FtpStatusCode.ClosingData && RemoveFromBlocks)
      {
        await ftpLoader.DeleteFileAsync(pathSrc);
      }

    }

    private async Task DoDownloadZipAsync(string fileNameSrc, Package zipPackage, string nameRootStorage,  FtpClient.FtpClient ftpLoader)
    {
      var pathSrc = Path.Combine(BuildRemotePath(nameRootStorage), fileNameSrc);
      
      var partUri = PackUriHelper.CreatePartUri(new Uri(Path.Combine(nameRootStorage, fileNameSrc), UriKind.Relative));
      var packagePart = zipPackage.CreatePart(partUri, System.Net.Mime.MediaTypeNames.Text.Plain, CompressionOption.Normal);
      var result = await ftpLoader.DownloadFileAsync(pathSrc, packagePart.GetStream());
     
      if (result == FtpStatusCode.ClosingData && RemoveFromBlocks)
      {
        await ftpLoader.DeleteFileAsync(pathSrc);
      }
    }

    private string BuildRemotePath(string nameRootPath)
    {
      return Path.Combine(nameRootPath, FOREMAN);
    }

    private void SelectAll(object obj)
    {
      if(MachinesDetails == null) return;

      var isChecked = (bool) obj;
      foreach (var item in MachinesDetails)
      {
        item.IsSelected = isChecked;
      }
    }
    #endregion
  }

  public class TaskProgress
  {
    public int CurrentProgress { get; set; }
    public int TotalProgress { get; set; }
    public string CurrentProgressMessage { get; set; }
    public string CurrentValue { get; set; }
  }

}
