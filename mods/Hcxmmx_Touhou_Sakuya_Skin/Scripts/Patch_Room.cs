using System;
using System.Collections;
using Godot;
using HarmonyLib;

namespace Hcxmmx.SakuyaMod.Scripts; // ✅ 极其规范的新命名空间

[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Nodes.Rooms.NMerchantRoom), nameof(MegaCrit.Sts2.Core.Nodes.Rooms.NMerchantRoom._Ready))]
internal static class NMerchantRoom_Ready_Patch
{
    private static void Postfix(MegaCrit.Sts2.Core.Nodes.Rooms.NMerchantRoom __instance)
    {
        GD.Print("\n====== 侦测到进入商店！启动精准鸠占鹊巢协议！ ======");
        SakuyaGlobals.IsInShop = true;

        var players = Traverse.Create(__instance).Field("_players").GetValue<System.Collections.IList>();
        var playerVisuals = Traverse.Create(__instance).Field("_playerVisuals").GetValue<System.Collections.IList>();
        if (players == null || playerVisuals == null || players.Count != playerVisuals.Count)
        {
            GD.PrintErr("💥 商店雷达中断：_players 或 _playerVisuals 数据异常或不匹配！");
            return;
        }

        bool hasSakuya = false;
        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            var character = Traverse.Create(player).Property("Character").GetValue() ?? Traverse.Create(player).Field("Character").GetValue();
            var entryName = SakuyaGlobals.GetCharacterEntry(character);
            if (string.Equals(entryName, SakuyaGlobals.TargetCharacterId, StringComparison.OrdinalIgnoreCase))
            {
                hasSakuya = true;
                GD.Print("🎯 商店 DNA 匹配成功！发现完美女仆！");
                break;
            }
        }

        if (!hasSakuya)
        {
            GD.Print("拦截：队伍里没有咲夜，保留原版队伍！");
            return;
        }

        var characterContainer = __instance.GetNodeOrNull<Control>("%CharacterContainer");
        if (characterContainer == null)
        {
            return;
        }

        var scene = SakuyaGlobals.SakuyaScene ?? ResourceLoader.Load<PackedScene>(SakuyaGlobals.SakuyaScenePath);
        SakuyaGlobals.SakuyaScene = scene;
        if (scene == null)
        {
            return;
        }

        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            var character = Traverse.Create(player).Property("Character").GetValue() ?? Traverse.Create(player).Field("Character").GetValue();
            var entryName = SakuyaGlobals.GetCharacterEntry(character);
            
            // 查身份证！不是咲夜就极其冷酷地跳过
            if (!string.Equals(entryName, SakuyaGlobals.TargetCharacterId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            GD.Print($"🎯 精准锁定！玩家 {i} 是完美女仆咲夜！");
            
            // 极其关键：直接从 _playerVisuals 里拿对应的 UI 肉体，绝对不会错位！
            var targetChild = playerVisuals[i] as Godot.Node2D;
            if (targetChild == null) continue;

            // 抹杀原版肉体
            targetChild.Hide();
            
            // 注入咲夜商店机甲
            var sakuyaShopMecha = scene.Instantiate<Node2D>();
            sakuyaShopMecha.Name = $"SakuyaShopMecha_{i}";
            characterContainer.AddChild(sakuyaShopMecha);

            // 极其精准地继承原位置
            sakuyaShopMecha.Position = targetChild.Position + new Vector2(0, -200f);
            sakuyaShopMecha.Scale = new Vector2(0.7f, 0.7f);

            var combatSprite = SakuyaGlobals.FindFirstNode<AnimatedSprite2D>(sakuyaShopMecha);
            var shopSprite = SakuyaGlobals.FindFirstNode<Sprite2D>(sakuyaShopMecha, s => s.Name == "ShopSprite");
            var animPlayer = SakuyaGlobals.FindFirstNode<AnimationPlayer>(sakuyaShopMecha);

            if (combatSprite != null) combatSprite.Visible = false;
            if (shopSprite != null) shopSprite.Visible = true;
            if (animPlayer != null) animPlayer.Play("Shop_Idle");
        }
    }
}

[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Nodes.Rooms.NMerchantRoom), "HideScreen")]
internal static class NMerchantRoom_HideScreen_Patch
{
    private static void Prefix()
    {
        GD.Print("\n====== 侦测到离开商店！摘除物理锁！ ======");
        SakuyaGlobals.IsInShop = false;
    }
}

[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Nodes.Rooms.NRestSiteRoom), nameof(MegaCrit.Sts2.Core.Nodes.Rooms.NRestSiteRoom._Ready))]
internal static class NRestSiteRoom_Ready_Patch
{
    private static void Postfix(MegaCrit.Sts2.Core.Nodes.Rooms.NRestSiteRoom __instance)
    {
        GD.Print("\n====== 📡 篝火雷达：侦测到进入篝火！启动双人摸鱼协议！ ======");
        SakuyaGlobals.IsInShop = true;

        var runState = Traverse.Create(__instance).Field("_runState").GetValue();
        if (runState == null)
        {
            GD.PrintErr("💥 雷达中断：找不到 _runState！");
            return;
        }

        var players = Traverse.Create(runState).Property("Players").GetValue<System.Collections.IList>()
            ??
            Traverse.Create(runState).Field("Players").GetValue<System.Collections.IList>();
        if (players == null)
        {
            GD.PrintErr("💥 雷达中断：找不到 Players 列表！");
            return;
        }

        var scene = SakuyaGlobals.SakuyaScene ?? ResourceLoader.Load<PackedScene>(SakuyaGlobals.SakuyaScenePath);
        SakuyaGlobals.SakuyaScene = scene;
        if (scene == null)
        {
            GD.PrintErr("💥 雷达中断：找不到咲夜的 Godot PCK 场景！");
            return;
        }

        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            var character = Traverse.Create(player).Property("Character").GetValue() ?? Traverse.Create(player).Field("Character").GetValue();
            var entryName = SakuyaGlobals.GetCharacterEntry(character);

            GD.Print($"🔍 扫描玩家 {i} 的 DNA (Entry): {entryName ?? "NULL"}");
            if (!string.Equals(entryName, SakuyaGlobals.TargetCharacterId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string containerPath = $"BgContainer/Character_{i + 1}";
            var container = __instance.GetNodeOrNull<Control>(containerPath);
            if (container == null)
            {
                GD.PrintErr($"💥 雷达中断：找不到官方坑位 {containerPath}！");
                continue;
            }

            
            for (int j = 0; j < container.GetChildCount(); j++)
            {
             if (container.GetChild(j) is CanvasItem canvasItem)
             {
             // 🚨 启动光学迷彩协议！
             // 绝对不要用 canvasItem.Hide();！
             // 直接把颜色的 Alpha 通道（透明度）降到 0！
             canvasItem.Modulate = new Color(1f, 1f, 1f, 0f);
             }
            }

            var sakuyaCampMecha = scene.Instantiate<Node2D>();
            sakuyaCampMecha.Name = $"SakuyaCampMecha_{i}";
            container.AddChild(sakuyaCampMecha);

            // 这里可以单独调整篝火小人的大小和位置
            sakuyaCampMecha.Scale = new Vector2(0.7f, 0.7f);
            sakuyaCampMecha.Position = new Vector2(0, 0f);
            bool needsFlip = (i % 2 == 1);

            var combatSprite = SakuyaGlobals.FindFirstNode<AnimatedSprite2D>(sakuyaCampMecha);
            
            // 🚨 极其核心的修改：这里不再找 ShopSprite，而是找咱们刚建的 CampfireSprite！
            var campfireSprite = SakuyaGlobals.FindFirstNode<Sprite2D>(sakuyaCampMecha, s => s.Name == "CampfireSprite");
            var companionSprite = SakuyaGlobals.FindFirstNode<Sprite2D>(sakuyaCampMecha, s => s.Name == "CompanionSprite");
            var animPlayer = SakuyaGlobals.FindFirstNode<AnimationPlayer>(sakuyaCampMecha);

            if (combatSprite != null)
            {
                combatSprite.Visible = false;
            }

            // 点亮篝火专属的咲夜
            if (campfireSprite != null)
            {
                campfireSprite.Visible = true;
                if (needsFlip)
                {
                    campfireSprite.FlipH = !campfireSprite.FlipH;
                }
            }

            // 点亮呼呼大睡的美铃
            if (companionSprite != null)
            {
                companionSprite.Visible = true;
                if (needsFlip)
                {
                    companionSprite.FlipH = !companionSprite.FlipH;
                }
            }

            if (animPlayer != null)
            {
                animPlayer.Play("Campfire_Idle");
            }
        }
    }
}

[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Nodes.Rooms.NRestSiteRoom), "OnProceedButtonReleased")]
internal static class NRestSiteRoom_Exit_Patch
{
    private static void Prefix()
    {
        GD.Print("\n====== 侦测到玩家点击前进！极其优雅地摘除篝火物理锁！ ======");
        SakuyaGlobals.IsInShop = false;
    }
}