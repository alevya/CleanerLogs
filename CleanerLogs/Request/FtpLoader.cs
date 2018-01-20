using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

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
      request.Credentials = new NetworkCredential("root", "admin");//new NetworkCredential("anonymous", "anonymous");

      ftpState.Request = request;
      request.BeginGetResponse(CallbackList, ftpState);

      //ftpState.Request = request;

    }

    private void CallbackList(IAsyncResult ar)
    {
      var state = (FtpState) ar.AsyncState;
      Stream reqStream = null;
      try
      {
        reqStream = state.Request.EndGetRequestStream(ar);

      }
      catch (Exception e)
      {
        
        throw;
      }
    }

    private Uri GetServerUri(string path)
    {
      return new Uri($"ftp://{Server}{'/'}{path}");
    }

  }
}
