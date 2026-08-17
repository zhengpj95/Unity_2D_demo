using System;
using System.Collections.Generic;

public static class MessageCommandTable
{
  private static readonly Dictionary<uint, string> _routes = new();

  public static void Register(uint cmd, string moduleName)
  {
    if (string.IsNullOrWhiteSpace(moduleName))
    {
      throw new ArgumentException("Module name cannot be empty.", nameof(moduleName));
    }

    _routes[cmd] = moduleName;
  }

  public static bool TryGetModuleName(uint cmd, out string moduleName)
  {
    return _routes.TryGetValue(cmd, out moduleName);
  }

  public static string GetModuleName(uint cmd)
  {
    return _routes.TryGetValue(cmd, out string moduleName) ? moduleName : "Unregistered";
  }

  public static bool Contains(uint cmd)
  {
    return _routes.ContainsKey(cmd);
  }

  public static void Unregister(uint cmd)
  {
    _routes.Remove(cmd);
  }

  public static IReadOnlyDictionary<uint, string> Routes => _routes;
}
