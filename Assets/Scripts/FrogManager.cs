public class FrogManager
{
  private static FrogManager _instance;
  public static FrogManager Instance => _instance ??= new FrogManager();

  public int Score = 0;
}