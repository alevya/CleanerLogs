using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
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

    public IEnumerable<string> ListFiles(string path)
    {
      var listFiles = new List<string>();
      var ftpState = new FtpState();
      var request = GetRequest(path, WebRequestMethods.Ftp.ListDirectory);

      
      using (var response = (FtpWebResponse)request.GetResponse())
      {
        using (var stream = response.GetResponseStream())
        {
          using (var reader = new StreamReader(stream, true))
          {
            while (!reader.EndOfStream)
            {
              listFiles.Add(reader.ReadLine());
            }
          }
        }
      }
      return listFiles;
    }

    public async Task<IEnumerable<string>> ListFilesAsync(string path)
    {
      var listFiles = new List<string>();
      var request = GetRequest(path, WebRequestMethods.Ftp.ListDirectory);

      using (var response = (FtpWebResponse)await request.GetResponseAsync())
      {
        using (var stream = response.GetResponseStream())
        {
          using (var reader = new StreamReader(stream, true))
          {
            while (!reader.EndOfStream)
            {
              listFiles.Add(await reader.ReadLineAsync());
            }
          }
          
        }
      }
      return listFiles;
    }

    /*
    public Task ReadAsync(Stream stream, IEnumerable<string> list)
    {
      var tcs = new TaskCompletionSource<int>();
      stream.
    }
    */

    public string DownloadFile(string source, string target)
    {
      var request = GetRequest(source, WebRequestMethods.Ftp.DownloadFile);

      using (var response = (FtpWebResponse)request.GetResponse())
      {
        using (var stream = response.GetResponseStream())
        {
          using (var fs = new FileStream(target, FileMode.Create, FileAccess.Write))
          {
            stream.CopyTo(fs, 4096);
          }
          return response.StatusDescription;
        }
      }
    }

    public async Task<string> DownloadFileAsync(string source, string target)
    {
      var request = GetRequest(source, WebRequestMethods.Ftp.DownloadFile);

      using (var response = (FtpWebResponse)await request.GetResponseAsync())
      {
        using (var stream = response.GetResponseStream())
        {
          using (var fs = new FileStream(target, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, 4096, FileOptions.Asynchronous ))
          {
            await stream.CopyToAsync(fs, 4096);
          }
        }
        return response.StatusDescription;
      }

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

    private FtpWebRequest GetRequest(string path, string method)
    {
      var uri = GetServerUri(path);

      var request = (FtpWebRequest)WebRequest.Create(uri);
      request.Method = method;
      request.Credentials = new NetworkCredential("root", "admin");//new NetworkCredential("anonymous", "anonymous");

      return request;
    }

  }
}
