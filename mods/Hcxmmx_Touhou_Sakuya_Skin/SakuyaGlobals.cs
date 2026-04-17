using System;
using System.Runtime.CompilerServices;
using Godot;

namespace Hcxmmx.SakuyaMod.Scripts; // ✅ 极其规范的新命名空间

// 🚨 咲夜专属赛博数据中枢：所有全局状态、常量和弹药库全在这里！
public static class SakuyaGlobals
{
    public enum RuntimeProfile
    {
        Release,
        Debug
    }

    // 发布前默认保持 Release；本地调试时改为 Debug 即可。
    public static RuntimeProfile CurrentProfile = RuntimeProfile.Release;
    public static bool EnableVerboseLogs = false;

    // ==========================================
    // 1. 动态状态监视器
    // ==========================================
    public static bool IsInShop = false;
    public static bool IsDead = false;
    public static bool NextAttackIsFinale = false;
    
    // 联机卫星阵列：记录所有在场的咲夜机甲
    public static System.Collections.Generic.HashSet<AnimatedSprite2D> ActiveSakuyaSprites = new();

    // ==========================================
    // 2. 核心系统常量
    // ==========================================
    public const string TargetCharacterId = "SILENT"; // 🎯 极其精准地夺舍静默猎手！

    // 🚨 极其安全的防撞车路径：加上了长官的专属番号！
    public const string SakuyaScenePath = "res://mods/Hcxmmx_Touhou_Sakuya_Skin/Scenes/SakuyaCharacter.tscn";
    public const string HarmonyId = "sts2.hcxmmx.sakuya.visuals";

    // ==========================================
    // 3. 全局随机数发生器
    // ==========================================
    public static readonly Random Rng = new Random();

    // 全局场景缓存：由 Entry.Init 预加载，Patch 中可直接复用。
    public static PackedScene? SakuyaScene;

    // ==========================================
    // 4. 动作动画池 (装填长官昨日极其辛苦切好的弹药！)
    // ==========================================
    public static readonly string[] AttackPool = { "Attack_1", "Attack_2", "Attack_3", "Attack_5", "Attack_6", "Attack_7" };
    public static readonly string[] HitPool = { "Hit_1", "Hit_2", "Hit_3" };
    public static readonly string[] CastPool = { "Cast_1", "Cast_2", "Cast_3", "Cast_4", "Cast_5" };
    public static readonly string[] ShivPool = { "Shiv_1", "Shiv_2", "Shiv_3" };
    public static readonly string[] VfxNames = { "CastEffect_1", "CastEffect_2", "CastEffect_3", "CastEffect_4", "CastEffect_5" };

    // ==========================================
    // 5. 语音光盘库 (极其华丽的装填完毕！)
    // ==========================================
    public static readonly string[] IntroVoicePool = { 
        "res://mods/Hcxmmx_Touhou_Sakuya_Skin/Audio/Vo_start_sakuya.wav",
        "res://mods/Hcxmmx_Touhou_Sakuya_Skin/Audio/Vo_ready_sakuya.wav",
        "res://mods/Hcxmmx_Touhou_Sakuya_Skin/Audio/Vo_rock_sakuya.wav"
    };

    // 🚨 胜利语音池我们不再随机抽取，而是交由底层雷达智能分配，所以这里可以直接留空或者删掉
    // public static readonly string[] VictoryVoicePool = { }; 

    public static readonly string[] AttackVoicePool = { 
        "res://mods/Hcxmmx_Touhou_Sakuya_Skin/Audio/Vo_attack_a_sakuya.wav",
        "res://mods/Hcxmmx_Touhou_Sakuya_Skin/Audio/Vo_slide_sakuya.wav",
        "res://mods/Hcxmmx_Touhou_Sakuya_Skin/Audio/Vo_spark_sakuya.wav",
    };
    public static readonly string[] HitVoicePool = { 
        "res://mods/Hcxmmx_Touhou_Sakuya_Skin/Audio/Vo_damage_s1_sakuya.wav",
        "res://mods/Hcxmmx_Touhou_Sakuya_Skin/Audio/Vo_damage_s2_sakuya.wav",
        "res://mods/Hcxmmx_Touhou_Sakuya_Skin/Audio/Vo_damage_s3_sakuya.wav"
    };
    public static readonly string[] CastVoicePool = { 
        "res://mods/Hcxmmx_Touhou_Sakuya_Skin/Audio/Vo_spell_a_sakuya.wav",
        "res://mods/Hcxmmx_Touhou_Sakuya_Skin/Audio/Vo_command_sakuya.wav",
        "res://mods/Hcxmmx_Touhou_Sakuya_Skin/Audio/Vo_climax_sakuya.wav",
        "res://mods/Hcxmmx_Touhou_Sakuya_Skin/Audio/Vo_w_slide_sakuya.wav"
    };
    public static readonly string[] ShivVoicePool = { 
        "res://mods/Hcxmmx_Touhou_Sakuya_Skin/Audio/Vo_shot_ch_sakuya.wav",
        "res://mods/Hcxmmx_Touhou_Sakuya_Skin/Audio/Vo_shot_no_sakuya.wav"
    };

    private static readonly System.Collections.Generic.Dictionary<string, AudioStream> AudioStreamCache =
        new(System.StringComparer.Ordinal);
    private static readonly ConditionalWeakTable<object, string> CardEntryCache = new();
    private static readonly System.Collections.Generic.List<AnimatedSprite2D> InvalidSpriteBuffer = new();
    // ==========================================
    // 底层工具方法 (保持不变)
    // ==========================================
    public static T? FindFirstNode<T>(Node root, Func<T, bool>? predicate = null) where T : Node
    {
        var queue = new System.Collections.Generic.Queue<Node>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current is T matched && (predicate == null || predicate(matched)))
            {
                return matched;
            }

            for (int i = 0; i < current.GetChildCount(); i++)
            {
                queue.Enqueue(current.GetChild(i));
            }
        }

        return null;
    }

    public static string? GetCharacterEntry(object? model)
    {
        if (model == null) return null;

        var idObj = HarmonyLib.Traverse.Create(model).Property("Id").GetValue()
            ?? HarmonyLib.Traverse.Create(model).Field("Id").GetValue();

        return HarmonyLib.Traverse.Create(idObj).Property("Entry").GetValue<string>()
            ?? HarmonyLib.Traverse.Create(idObj).Field("Entry").GetValue<string>();
    }

    public static string? GetCardEntry(object? card)
    {
        if (card == null) return null;

        if (CardEntryCache.TryGetValue(card, out var cachedEntry))
        {
            return cachedEntry;
        }

        try {
            var idObj = HarmonyLib.Traverse.Create(card).Property("Id").GetValue() ?? HarmonyLib.Traverse.Create(card).Field("Id").GetValue();
            var entry = HarmonyLib.Traverse.Create(idObj).Property("Entry").GetValue<string>() ?? HarmonyLib.Traverse.Create(idObj).Field("Entry").GetValue<string>();
            if (!string.IsNullOrEmpty(entry))
            {
                try
                {
                    CardEntryCache.Add(card, entry);
                }
                catch (ArgumentException)
                {
                    // 其他线程可能已先写入缓存，直接忽略即可。
                }
            }

            return entry;
        } catch { return null; }
    }

    public static void CleanupActiveSakuyaSprites()
    {
        if (ActiveSakuyaSprites.Count == 0) return;

        InvalidSpriteBuffer.Clear();
        foreach (var sprite in ActiveSakuyaSprites)
        {
            if (!GodotObject.IsInstanceValid(sprite))
            {
                InvalidSpriteBuffer.Add(sprite);
            }
        }

        for (int i = 0; i < InvalidSpriteBuffer.Count; i++)
        {
            ActiveSakuyaSprites.Remove(InvalidSpriteBuffer[i]);
        }
        InvalidSpriteBuffer.Clear();
    }

    public static AudioStream? GetAudioStreamCached(string? resourcePath)
    {
        if (string.IsNullOrEmpty(resourcePath)) return null;

        if (AudioStreamCache.TryGetValue(resourcePath, out var cached))
        {
            return cached;
        }

        var loaded = ResourceLoader.Load<AudioStream>(resourcePath);
        if (loaded != null)
        {
            AudioStreamCache[resourcePath] = loaded;
        }

        return loaded;
    }

    public static void VerboseLog(string message)
    {
        if (EnableVerboseLogs)
        {
            GD.Print(message);
        }
    }

    public static void ApplyRuntimeProfile()
    {
        EnableVerboseLogs = CurrentProfile == RuntimeProfile.Debug;
    }
}