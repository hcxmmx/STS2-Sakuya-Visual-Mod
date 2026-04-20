using System;
using Godot;
using HarmonyLib;

namespace Hcxmmx.SakuyaMod.Scripts; 

// 🚨 极其霸气、绝对免疫手机端 AOT 裁剪的跨平台劫持！
// 抛弃不稳定的 _Ready，精准狙击接口方法 AfterOverlayOpened！
[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen.NGameOverScreen), nameof(MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen.NGameOverScreen.AfterOverlayOpened))]
internal static class GameOverScreen_Opened_Patch
{
    // ==========================================
    // 🎭 极其残忍的战损表情弹药库！
    // 长官，请把下面这些路径极其精准地替换成你真实的表情包图纸路径！
    // ==========================================
    private static readonly string[] DefeatImagePool = {
        "res://mods/Hcxmmx_Touhou_Sakuya_Skin/Assets/Lost/001.png",   
        "res://mods/Hcxmmx_Touhou_Sakuya_Skin/Assets/Lost/002.png", 
        "res://mods/Hcxmmx_Touhou_Sakuya_Skin/Assets/Lost/003.png",  
        "res://mods/Hcxmmx_Touhou_Sakuya_Skin/Assets/Lost/004.png",
        "res://mods/Hcxmmx_Touhou_Sakuya_Skin/Assets/Lost/005.png",
        // 有多少张就极其任性地往里加多少！
    };

    private static void Postfix(MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen.NGameOverScreen __instance)
    {
        GD.Print("\n====== 😭 侦测到赛博惨败！启动【多重战损反馈协议】 ======");

        // ==========================================
        // 🛡️ V1.0.2 火速抢修：终极双重防爆盾！
        // ==========================================
        // 1. 赛博清道夫：先把上一局残留的、已经被引擎销毁的机甲幻影从雷达里删掉
        SakuyaGlobals.CleanupActiveSakuyaSprites();

        // 2. 极其精准的 DNA 验证：
        // 如果【她没有被宣告阵亡】(IsDead为false) 并且 【当前场上也没有活着的机甲】(Count为0)
        // 就说明死的是铁甲战士或者天子，极其高冷地跳过演出！
        if (!SakuyaGlobals.IsDead && SakuyaGlobals.ActiveSakuyaSprites.Count == 0) 
        {
            GD.Print("当前战局并非完美女仆的受难时刻，极其高冷地跳过战损演出。");
            return; 
        }
        // ==========================================

        string scenePath = "res://mods/Hcxmmx_Touhou_Sakuya_Skin/Scenes/DamagedSakuyaOverlay.tscn"; 
        var damagedScene = ResourceLoader.Load<PackedScene>(scenePath);

        if (damagedScene == null)
        {
            GD.PrintErr("💥 严重事故：无法加载战损图纸！长官请检查路径拼写！");
            return;
        }

        // 极其暴力的注入行为！(这里假设长官极其听话地用了 CanvasLayer)
        var overlayNode = damagedScene.Instantiate<CanvasLayer>();
        __instance.AddChild(overlayNode);
        
        // ==========================================
        // 🎬 极其华丽的差分替换与淡入引擎！
        // ==========================================
        
        // 🚨 极其关键：去你的 DamagedSakuyaOverlay.tscn 里，看一眼装图片那个节点到底叫什么！
        // 如果长官没改名，默认就叫 "TextureRect"
        var sakuyaImage = overlayNode.GetNodeOrNull<TextureRect>("TextureRect");

        if (sakuyaImage != null)
        {
            // 1. 🛡️ 极其保险的防叹息之墙：强行让图片变成物理幽灵，绝对不挡鼠标点击！
            sakuyaImage.MouseFilter = Control.MouseFilterEnum.Ignore;

            // 2. 🎲 极其残忍的俄罗斯轮盘：随机抽取一张战败表情！
            if (DefeatImagePool.Length > 0)
            {
                string chosenImagePath = DefeatImagePool[SakuyaGlobals.Rng.Next(DefeatImagePool.Length)];
                var newTexture = ResourceLoader.Load<Texture2D>(chosenImagePath);
                
                if (newTexture != null)
                {
                    sakuyaImage.Texture = newTexture; // 极其无情地替换图片！
                    GD.Print($"🖼️ 极其精准地加载了战损差分: {chosenImagePath}");
                }
            }

            // 3. 👻 初始状态：极其无情地将透明度归零（完全隐身）
            sakuyaImage.Modulate = new Color(1, 1, 1, 0); 

            /// 4. 召唤赛博动画师：极其凄美的三段式生离死别演出！
              var tree = sakuyaImage.GetTree();
              if (tree == null) return;

              var tween = tree.CreateTween();
            
            // 🎬 第一幕【淡入】：透明度变到 1.0，耗时 3.0 秒（长官可极其自由地修改）
            tween.TweenProperty(sakuyaImage, "modulate:a", 1.0f, 3.0f)
                 .SetTrans(Tween.TransitionType.Sine)
                 .SetEase(Tween.EaseType.InOut);

            // ⏸️ 第二幕【极其不甘的对视】：让战损女仆在屏幕上停留 2.0 秒，让玩家极其痛心地看着她
            tween.TweenInterval(8.0f);

            // 👻 第三幕【淡出消散】：透明度重新变回 0.0，耗时 2.5 秒，极其凄美地退场
            tween.TweenProperty(sakuyaImage, "modulate:a", 0.0f, 2.5f)
                 .SetTrans(Tween.TransitionType.Sine)
                 .SetEase(Tween.EaseType.InOut);

            // (可选彩蛋) 当她彻底消散后，可以在后台默默销毁这个图片节点，释放赛博内存
            tween.Finished += () => 
            {
                if (GodotObject.IsInstanceValid(sakuyaImage))
                {
                    sakuyaImage.QueueFree();
                    GD.Print("战损残影已极其优雅地消散于高塔之中...");
                }
            };

            GD.Print("🎬 战损淡入演出启动，请长官极其痛心地欣赏这凄美的一幕！");
        }
        else
        {
            GD.PrintErr("💥 找不到名为 TextureRect 的节点！长官请核对场景树里的真名！");
        }
    }
}