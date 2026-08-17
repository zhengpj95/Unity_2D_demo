using System;

public interface IMessageModule
{
  string ModuleName { get; }
  void Register(NetworkMgr network);
}

public abstract class BaseMessageModule : IMessageModule
{
  public abstract string ModuleName { get; }

  public virtual void Register(NetworkMgr network)
  {
    if (network == null)
    {
      throw new ArgumentNullException(nameof(network));
    }
  }
}
