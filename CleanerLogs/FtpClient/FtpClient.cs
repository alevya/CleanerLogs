using System;
using System.IO;
using System.Net;
using CleanerLogs.Request;

namespace CleanerLogs.FtpClient
{
  internal class FtpClient
  {
    public string Server { get;}

    public FtpClient(string server)
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
