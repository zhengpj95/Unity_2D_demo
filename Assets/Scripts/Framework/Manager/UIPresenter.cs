using UnityEngine;

// UI 视图基类 (挂载在 UI Prefab 上)
public abstract class UIView : MonoBehaviour
{
  // 用于组件引用的绑定（也可以在 Inspector 中拖拽）
  public virtual void InitView()
  {
  }
}

// UI 控制器/逻辑基类 (纯 C# 类)
public abstract class UIPresenter
{
  public UIView View { get; private set; }
  public bool IsVisible { get; private set; }

  // 初始化（加载 Prefab 后调用一次）
  public virtual void OnInit(UIView view)
  {
    View = view;
  }

  // 打开界面
  public virtual void OnOpen(object args = null)
  {
    IsVisible = true;
    if (View != null) View.gameObject.SetActive(true);
  }

  // 关闭界面
  public virtual void OnClose()
  {
    IsVisible = false;
    if (View != null) View.gameObject.SetActive(false);
  }

  // 销毁界面
  public virtual void OnDestroy()
  {
    if (View != null)
    {
      UnityEngine.Object.Destroy(View.gameObject);
      View = null;
    }
  }
}