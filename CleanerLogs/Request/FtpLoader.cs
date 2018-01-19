using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace CleanerLogs.Request
{
  internal class FtpLoader
  {
    public string Server { get;}

    public FtpLoader(string server)
    {
      Server = server;
    }

    public void ListFilesAsync(string path)
    {
      var uri = GetServerUri(path);
      var ftpState = new FtpState();
      var request = (FtpWebRequest) WebRequest.Create(uri);
      request.Method = WebRequestMethods.Ftp.ListDirectory;
      request.Credentials = new NetworkCredential("anonymous", "anonymous");

      ftpState.Request = request;

    }

    private Uri GetServerUri(string path)
    {
      return new Uri($"ftp://{Server}{'/'}{path}");
    }

  }
}
