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

                if (string.IsNullOrEmpty(cSavePath))
                {
                    return Path.GetTempPath();
                }
                var dirInfo = new DirectoryInfo(cSavePath);
                return dirInfo.Root.Exists ? cSavePath : Path.GetTempPath();
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

        private bool _allSelected;

        public bool AllSelected
        {
            get
            {
                return _allSelected;
            }
            set
            {
                _allSelected = value;
                foreach (var item in MachinesDetails)
                {
                    item.IsSelected = value;
                }
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

            var machineItems = _configurationApp.MachineItems;
            if (machineItems == null || machineItems.Count == 0)
            {
                return;
            }
            _dictMachineDetails.Clear();
            foreach (MachineElement item in machineItems)
            {
                var md = new MachineDetailViewModel(item.MachineNumber, item.MachineIp);
                if (!_dictMachineDetails.ContainsKey(item.MachineNumber))
                {
                    _dictMachineDetails.Add(item.MachineNumber, md);
                }
                else
                {
                    _dictMachineDetails[item.MachineNumber] = md;
                }
            }
            
            MachinesDetails = new ObservableCollection<MachineDetailViewModel>(_dictMachineDetails.Values);
        }

        private void FileOpen(object obj)
        {
            var res = _openFileFunc?.Invoke();
            _configurationApp.Load(res);
            InitConfig();

        }

        private async void CleanAsync(object obj)
        {
            MachinesDetailClear();
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
                    Task resultTask = await Task.WhenAny(mapTasks.Keys).ConfigureAwait(true);
                    mapTasks.TryGetValue(resultTask, out numberValue);
                    mapTasks.Remove(resultTask);
                    await resultTask;

                    UpdateMachineDetailItem(numberValue, "Успешно!", true);
                }
                catch (Exception exc)
                {
                    UpdateMachineDetailItem(numberValue, exc.Message.TrimEnd(), false);
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
            var logFolderName = BuildLogFolderName();
            var rootPathTrg = Path.Combine(SavePath, ip);
         
            var ftpLoader = new FtpClient.FtpClient(ip, _configurationApp.RequestTimeout, _configurationApp.ReadWriteTimeout);
    
            var listUsbDisk = await ftpLoader.ListFilesAsync(BuildRemotePath(USBDISK));
            var listNandFlash = await ftpLoader.ListFilesAsync(BuildRemotePath(NANDFLASH));

            var logsUsbDisk = listUsbDisk.Where(file => file.EndsWith(FILE_EXTENSIONS));
            var logsNandDisk = listNandFlash.Where(file => file.EndsWith(FILE_EXTENSIONS));

            if (Zipped)
            {
                Directory.CreateDirectory(rootPathTrg);
                var pathPackage = Path.Combine(rootPathTrg, logFolderName);
                using (var package = Package.Open(pathPackage + ".zip", FileMode.Create, FileAccess.ReadWrite))
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
                var diUsbDisk = Directory.CreateDirectory(Path.Combine(rootPathTrg, logFolderName, USBDISK));
                var diNandFlash = Directory.CreateDirectory(Path.Combine(rootPathTrg, logFolderName, NANDFLASH));

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

        private string BuildLogFolderName(string name = "")
        {
            return string.Format("log_{0}", DateTime.Today.ToShortDateString());
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

        private void UpdateMachineDetailItem(string number, string message, bool success)
        {
            _dictMachineDetails.TryGetValue(number, out MachineDetailViewModel md);
            if (md != null)
            {
                md.Message = message;
                if (!success) md.IsSelected = false;
            }
        }

        private void MachinesDetailClear()
        {
            foreach (var item in _dictMachineDetails.Values)
            {
                item.Message = string.Empty;
            }
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
