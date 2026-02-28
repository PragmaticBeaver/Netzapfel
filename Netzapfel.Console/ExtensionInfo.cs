namespace Netzapfel.Console;

internal class ExtensionInfo
{
  public required string ContentType { get; set; }
  public required Func<string, string, ExtensionInfo, ResponsePacket> Loader { get; set; }
}