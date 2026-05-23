using UnityEngine;

public class DataManager
{
    private static DataManager _instance;
    public static DataManager Instance
    {
        get
        {
            if (_instance == null) _instance = new DataManager();
            return _instance;
        }
    }

    // 存档的 Key
    private const string LEVEL_KEY = "CurrentLevel";
    private const string BOMB_KEY = "Item_Bomb";
    private const string LIGHTNING_KEY = "Item_Lightning";
    private const string POTION_KEY = "Item_Potion";

    // --- 关卡数据 ---
    public int CurrentLevel
    {
        get => PlayerPrefs.GetInt(LEVEL_KEY, 1);
        set => PlayerPrefs.SetInt(LEVEL_KEY, value);
    }

    // --- 道具数量数据 ---
    // 🌟 核心改动：在后面的参数里填上 5。 
    // 意思是：如果注册表里找不到这个 Key（新玩家），就默认返回 5；如果找得到（老玩家），就返回存下的数字。

    public int BombCount
    {
        get => PlayerPrefs.GetInt("BombCount", 5); // 👈 默认值改为 5
        set { PlayerPrefs.SetInt("BombCount", value);}
    }

    public int LightningCount
    {
        get => PlayerPrefs.GetInt("LightningCount", 5); // 👈 默认值改为 5
        set { PlayerPrefs.SetInt("LightningCount", value);  }
    }

    public int PotionCount
    {
        get => PlayerPrefs.GetInt("PotionCount", 5); // 👈 默认值改为 5
        set { PlayerPrefs.SetInt("PotionCount", value); }
    }

    // --- 核心方法 ---
    public void StartNewGame()
    {
        // 1. 🌟 只重置关卡进度回第 1 关
        CurrentLevel = 1;

        // 2. ❌ 删掉或者注释掉重置道具数量的代码！
        // BombCount = 3;       <-- 把这种代码删掉或注释掉
        // LightningCount = 3;  <-- 这样道具数量就会保持玩家原本持有的数值不变
        // PotionCount = 3;

        // 3. 核心：立刻保存关卡数据到本地，覆盖掉老关卡
        SaveAll();
    }

    public void SaveAll()
    {
        PlayerPrefs.Save();
    }
}