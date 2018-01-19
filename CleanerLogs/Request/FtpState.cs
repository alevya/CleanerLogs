using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace CleanerLogs.Request
{
  internal class FtpState
  {
    public FtpState()
    {

      Wait = new ManualResetEvent(false);
    }

    public ManualResetEvent Wait { get; set; }
    public FtpWebRequest Request { get; set; }
    public Exception OperationException { get; set; }
    public string Status { get; set; }
    public string FileName { get; set; }
  }
}
