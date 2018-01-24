using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Configuration;
using System.IO.Packaging;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Input;
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
        private readonly IDictionary<string, MachineDetailViewModel> _dictMachineDetails = new Dictionary<string, MachineDetailViewModel>();

        private bool _cursorWait;
        private bool _enabledGui = true;

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

        public bool CursorWait
        {
            get { return _cursorWait; }
            set
            {
                _cursorWait = value;
                OnPropertyChanged();
            }
        }

        public bool EnabledGui
        {
            get { return _enabledGui; }
            set
            {
                _enabledGui = value;
                OnPropertyChanged();
            }
        }
    
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

            foreach (MachineElement item in m.MachineItems)
            {
                _dictMachineDetails.Add(item.MachineNumber, new MachineDetailViewModel(item.MachineNumber, item.MachineIp));
            }

            MachinesDetails = new ObservableCollection<MachineDetailViewModel>(_dictMachineDetails.Values);
        }

        private void FileOpen(object obj)
        {
            var res = _openFileFunc?.Invoke();
        }

        private async void CleanAsync(object obj)
        {
            ActionProgress();

            var listMd = MachinesDetails.Where(item => item.IsSelected).ToList();

            const int CONCURRENCY_LEVEL = 10;
            var mapTasks = new Dictionary<Task, string>(); 
            int nextIndex = 0;

            while (nextIndex < CONCURRENCY_LEVEL && nextIndex < listMd.Count)
            {
                var md = listMd.ElementAt(nextIndex);
                mapTasks.Add(DownloadAndDeleteAsync(md.Ip), md.Number);
                nextIndex++;
            }
            while (mapTasks.Count > 0)
            {
                string numberValue = String.Empty;
                try
                {
                    Task resultTask = await Task.WhenAny(mapTasks.Keys);
                    mapTasks.TryGetValue(resultTask, out numberValue);
                    mapTasks.Remove(resultTask);
                    await resultTask;

                    _updateMachineItemMessage(numberValue, "Успешно!");
                }
                catch (Exception exc)
                {
                    _updateMachineItemMessage(numberValue, exc.Message);
                }

                if(nextIndex >= listMd.Count) continue;

                var md = listMd.ElementAt(nextIndex);
                mapTasks.Add(DownloadAndDeleteAsync(md.Ip), md.Number);
                nextIndex++;
            }

            ActionCompleted();
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

        private void _updateMachineItemMessage(string number, string message)
        {
            _dictMachineDetails.TryGetValue(number, out MachineDetailViewModel md);
            if (md != null) md.Message = message;
        }

        private void ActionProgress()
        {
            EnabledGui = false;
            CursorWait = true;
            
        }

        private void ActionCompleted()
        {
            EnabledGui = true;
            CursorWait = false;
            
        }
   #endregion
  }

}
