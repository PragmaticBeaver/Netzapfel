namespace Netzapfel.Console;

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

public static class Server
{
  private const int maxSimultaneousConnections = 20;
  private static readonly Semaphore semaphore = new Semaphore(maxSimultaneousConnections, maxSimultaneousConnections);
  private static readonly HttpListener listener;

  static Server()
  {
    var localIps = GetLocalIPs();
    listener = InitializeListener(localIps);
  }

  public static void Start()
  {
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
    listener.Prefixes.Add("http://localhost/");

    localIps.ForEach(ip =>
    {
      var url = $"http://{ip}/";
      Console.WriteLine($"listening to {url}");
      listener.Prefixes.Add(url);
    });

    return listener;
  }

  private static async void StartConnectionListener(HttpListener listener)
  {
    var context = await listener.GetContextAsync();
    // release semaphore which allows another listener to be initialized/started
    semaphore.Release();

    Console.WriteLine(context.ToString());

    var response = "Hello Browser, this is Netzapfel!";
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
  /// WARNING; app/user needs permission to listen!
  /// linux; dev workaround => use sudo when starting application
  /// windows; configure using "netsh"
  /// </summary>
  private static void StartListening(HttpListener listener)
  {
    listener.Start();
    Task.Run(() => RunServer(listener));
  }

}
