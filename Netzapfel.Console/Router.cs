namespace Netzapfel.Console;

using System;
using System.Runtime.InteropServices;
using System.Text;

public class Router
{
  private readonly string websitePath;
  private readonly Dictionary<string, ExtensionInfo> extensionDirMap;

  public Router(string path)
  {
    websitePath = path;
    extensionDirMap = new Dictionary<string, ExtensionInfo>()
    {
      {"ico", new ExtensionInfo() {Loader=ImageLoader, ContentType="image/ico"}},
      {"png", new ExtensionInfo() {Loader=ImageLoader, ContentType="image/png"}},
      {"jpg", new ExtensionInfo() {Loader=ImageLoader, ContentType="image/jpg"}},
      {"gif", new ExtensionInfo() {Loader=ImageLoader, ContentType="image/gif"}},
      {"bmp", new ExtensionInfo() {Loader=ImageLoader, ContentType="image/bmp"}},
      {"html", new ExtensionInfo() {Loader=PageLoader, ContentType="text/html"}},
      {"css", new ExtensionInfo() {Loader=FileLoader, ContentType="text/css"}},
      {"js", new ExtensionInfo() {Loader=FileLoader, ContentType="text/javascript"}},
      {"", new ExtensionInfo() {Loader=PageLoader, ContentType="text/html"}}, // unknown / non-existing will be handled as HTML
    };
  }

  public ResponsePacket Route(string httpMethod, string path, Dictionary<string, string>? kvParams = null)
  {
    string extention = Utils.GetFileExtention(path);
    ExtensionInfo? extInfo = null;
    ResponsePacket? response = null;

    if (extensionDirMap.TryGetValue(extention, out extInfo))
    {
      var webPath = websitePath;
      if (String.IsNullOrEmpty(webPath))
      {
        Console.WriteLine("Website path is invalid!");
        webPath = "";
      }

      // Strip off leading '/' and reformat as with windows path separator.
      string fullPath = Path.Join(webPath, path);
      response = extInfo.Loader(fullPath, extention, extInfo);
    }
    else
    {
      response = new ResponsePacket()
      {
        Error = ServerError.UnknownType
      };
    }

    return response;
  }

  private ResponsePacket ImageLoader(string fullPath, string fileExtention, ExtensionInfo extInfo)
  {
    if (!File.Exists(fullPath))
    {
      Console.WriteLine($"Error; Image not found: {fullPath}");
      return new ResponsePacket() { Error = ServerError.FileNotFound };
    }
    FileStream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
    BinaryReader reader = new BinaryReader(stream);
    ResponsePacket response = new ResponsePacket() { Data = reader.ReadBytes((int)stream.Length), ContentType = extInfo.ContentType };
    reader.Close();
    stream.Close();
    return response;
  }

  private ResponsePacket FileLoader(string fullPath, string fileExtention, ExtensionInfo extInfo)
  {
    if (!File.Exists(fullPath))
    {
      Console.WriteLine($"Error; File not found: {fullPath}");
      return new ResponsePacket() { Error = ServerError.FileNotFound };
    }
    string text = File.ReadAllText(fullPath);
    ResponsePacket response = new ResponsePacket() { Data = Encoding.UTF8.GetBytes(text), ContentType = extInfo.ContentType, Encoding = Encoding.UTF8 };
    return response;
  }

  private ResponsePacket PageLoader(string fullPath, string fileExtention, ExtensionInfo extInfo)
  {
    var response = new ResponsePacket();

    if (fullPath == websitePath) // If nothing follows the domain name or IP, then default to loading index.html.
    {
      response = Route("GET", "/index.html");
    }
    else
    {
      if (String.IsNullOrEmpty(fileExtention))
      {
        // No extension, so we make it ".html"
        fullPath = $"{fullPath}.html";
      }

      // Inject the "Pages" folder into the path
      var index = websitePath.Length;
      fullPath = Path.Join(websitePath, "Pages", fullPath[index..]);

      if (!File.Exists(fullPath))
      {
        Console.WriteLine($"Error; Page not found: {fullPath}");
        return new ResponsePacket() { Error = ServerError.PageNotFound };
      }
      response = FileLoader(fullPath, fileExtention, extInfo);
    }

    return response;
  }
}