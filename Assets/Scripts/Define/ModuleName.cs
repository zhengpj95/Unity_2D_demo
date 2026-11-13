/// <summary>
/// 业务模块的唯一标识。
/// C#中枚举不可以使用字符串，所以这里使用数字来表示模块的唯一标识。
/// 为了统一 ModuleName(四位数) + ViewType(1或2位数) 的处理，用四位数表示一个模块，所以从1000开始。
/// </summary>
public enum ModuleName
{
  None = 1000,
  Login = 1001,
  Misc = 1002,
  Bag = 1003,
  Survivor = 1004,
  Guide = 1005,
}