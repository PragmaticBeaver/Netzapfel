namespace Netzapfel.Console;

public enum ServerError
{
  NoError,
  InternalError,
  FileNotFound,
  PageNotFound,
  UnknownType,
  SessionExpired,
  NotAuthorized,
}