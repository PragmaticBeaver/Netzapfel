namespace Netzapfel.Console;

using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

public static class Server
{
  private const int maxSimultaneousConnections = 20;
  private const int port = 8080;
  private static readonly Semaphore semaphore = new Semaphore(maxSimultaneousConnections, maxSimultaneousConnections);
  private static readonly HttpListener listener;
  private static Router? router;

  static Server()
  {
    var localIps = GetLocalIPs();
    listener = InitializeListener(localIps);
  }

  public static string GetWebsiteDir()
  {
    var websitePath = Assembly.GetExecutingAssembly().Location;
    Console.WriteLine($"assembly-path: {websitePath}");
    // get projekt root-dir
    var path = websitePath;
    for (int i = 0; i < 5; i++)
    {
      path = Utils.SubstringBeforeLastIndex(path, '/');
    }
    Console.WriteLine($"root-dir: {path}");

    return $"{path}/Website";
  }

  public static void Start(string websitePath)
  {
    router = new Router
    {
      WebsitePath = websitePath
    };
    StartListening(listener);
  }

  private static List<IPAddress> GetLocalIPs()
  {
    var hostname = Dns.GetHostName();
    var host = Dns.GetHostEntry(hostname);
    var adresses = host.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork).ToList();
    return adresses;
  }

  private static HttpListener InitializeListener(List<IPAddress> localIps)
  {
    var listener = new HttpListener();
    listener.Prefixes.Add($"http://localhost:{port}/");

    localIps.ForEach(ip =>
    {
      var url = $"http://{ip}:{port}/";
      Console.WriteLine($"listening to {url}");
      listener.Prefixes.Add(url);
    });

    return listener;
  }

  private static void Log(HttpListenerRequest request)
  {
    var logEntry = $"{request.RemoteEndPoint} {request.HttpMethod} /{request?.Url?.AbsoluteUri}";
    Console.WriteLine(logEntry);
  }

  private static Dictionary<string, string> GetKeyValuePairs(string data, Dictionary<string, string>? pairs = null)
  {
    pairs ??= []; // compound assignment

    if (data.Length <= 0)
    {
      return pairs;
    }

    var keyValues = data.Split('&');
    foreach (var kv in keyValues)
    {
      var paramKey = Utils.SubstringBeforeLastIndex(kv, '=');
      var paramValue = Utils.SubstringAfterFirstIndex(kv, '=');
      pairs.Add(paramKey, paramValue);
    }
    return pairs;
  }

  private static async void StartConnectionListener(HttpListener listener)
  {
    var context = await listener.GetContextAsync();
    // release semaphore which allows another listener to be initialized/started
    semaphore.Release();
    Log(context.Request);

    // var request = context.Request;
    // var rawUrl = request.RawUrl ?? "";
    // var path = SubstringBeforeLastIndex(rawUrl, '?');
    // var method = request.HttpMethod ?? "";
    // var paramString = SubstringAfterFirstIndex(rawUrl, '?');
    // var kvParams = GetKeyValuePairs(paramString);

    // router.Route(method, path, paramString);


    string response = $@"
    <html>
      <head>
        <meta http-equiv='content-type' content='text/html; charset=utf-8'/>
      </ head>
      <body>
        <p>Hello Browser, this is Netzapfel!</p>
      </body>
    </html>";

    // var response = "Hello Browser, this is Netzapfel!";
    var encodedResponse = Encoding.UTF8.GetBytes(response);
    context.Response.ContentLength64 = encodedResponse.Length;
    context.Response.OutputStream.Write(encodedResponse, 0, encodedResponse.Length);
    context.Response.OutputStream.Close();
  }


  private static void RunServer(HttpListener listener)
  {
    while (true)
    {
      semaphore.WaitOne();
      StartConnectionListener(listener);

    }
  }

  /// <summary>
  /// Listen to connections on a separate worker thread.
  /// </summary>
  private static void StartListening(HttpListener listener)
  {
    listener.Start();
    Task.Run(() => RunServer(listener));
  }

}
