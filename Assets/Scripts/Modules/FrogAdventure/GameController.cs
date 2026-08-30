using UnityEngine;
using UnityEngine.SceneManagement;

namespace FrogAdventure {

  public class GameController
  {
    private static GameController _instance;
    public static GameController Instance => _instance ??= new GameController();

    // 是否在被击退
    public bool IsKnockBack = false;

    // 关卡
    public int Level = 1;
    public void LoadScene(int sceneIndex)
    {
      Level = sceneIndex;
      SceneManager.LoadScene(sceneIndex);
      EventBus.Emit("UPDATE_LEVEL");
    }

    // 血量
    public int MaxHp = 3;
    public void UpdateHp(int damage)
    {
      MaxHp += damage;
      if (MaxHp < 0) MaxHp = 0;
      Debug.Log("当前血量：" + MaxHp);
      EventBus.Emit("UPDATE_HP");
    }

    public void ResetHp()
    {
      MaxHp = 3;
      EventBus.Emit("UPDATE_HP");
    }
  }
}
