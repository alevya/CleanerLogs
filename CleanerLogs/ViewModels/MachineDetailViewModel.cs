using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CleanerLogs.ViewModel;

namespace CleanerLogs.ViewModels
{
  internal class MachineDetailViewModel : BaseViewModel
  {
    public MachineDetailViewModel(string number, string ip)
    {
      Number = number;
      Ip = ip;
    }

    #region Properties

    public string Number { get; }
    public string Ip { get; }

    #endregion
  }
}
