namespace Netzapfel.Console;

public static class Utils
{

  internal static string SubstringBeforeLastIndex(string value, char separator)
  {
    var index = value.LastIndexOf(separator);
    var result = index > 0 ? value[..index] : value;
    return result;
  }

  internal static string SubstringAfterFirstIndex(string value, char separator)
  {
    var index = value.IndexOf(separator);
    var result = index > 0 ? value[index..] : value;
    return result;
  }

}