using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CleanerLogs.ViewModel
{
  internal class MainViewModel : BaseViewModel
  {
    private string _savePath;

    public MainViewModel()
    {
      _savePath = Path.GetTempPath();
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


    #endregion
  }
}
