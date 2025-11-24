public class GameController
{
  private static GameController _instance;
  public static GameController Instance => _instance ??= new GameController();

  // 是否在被击退
  public bool IsKnockBack = false;
}