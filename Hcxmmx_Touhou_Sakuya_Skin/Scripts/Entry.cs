using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

// 极其规范的专属命名空间
namespace Hcxmmx.SakuyaMod.Scripts;

[ModInitializer("Init")]
public class Entry
{
    public static void Init()
    {
        SakuyaGlobals.ApplyRuntimeProfile();

        if (SakuyaGlobals.EnableVerboseLogs)
        {
            GD.Print("\n====================================");
            GD.Print("Hcxmmx Sakuya Project: 完美女仆核心极其华丽地点火！");
            GD.Print("====================================\n");
        }

        // 极其唯一的 Harmony ID，带上了长官的专属签名！
        var harmony = new Harmony(SakuyaGlobals.HarmonyId);
        harmony.PatchAll();

        // 预加载场景（如果 SakuyaGlobals 准备好了的话）
        SakuyaGlobals.SakuyaScene = ResourceLoader.Load<PackedScene>(SakuyaGlobals.SakuyaScenePath);

        Log.Debug($"Sakuya Maid Skin initialized. Profile={SakuyaGlobals.CurrentProfile}");
    }
}