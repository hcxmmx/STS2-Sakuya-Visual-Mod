using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Hcxmmx.SakuyaMod.Scripts;

[HarmonyPatch(typeof(NCreature), nameof(NCreature._Ready))]
internal static class NCreature_Ready_Patch
{
    private static void Postfix(NCreature __instance)
    {
        SakuyaGlobals.IsDead = false;
        SakuyaGlobals.IsInShop = false;
        GD.Print($"\n---> Sakuya Maid Project: 侦测到 NCreature 试图活化！节点名称 = {__instance.Name} <---");

        var scene = SakuyaGlobals.SakuyaScene ?? ResourceLoader.Load<PackedScene>(SakuyaGlobals.SakuyaScenePath);
        SakuyaGlobals.SakuyaScene = scene;

        if (scene == null || __instance.Entity == null) return;

        var player = __instance.Entity.Player;
        if (player == null || !string.Equals(player.Character?.Id?.Entry, SakuyaGlobals.TargetCharacterId, StringComparison.OrdinalIgnoreCase)) return;

        var visuals = __instance.Visuals;
        if (visuals == null) return;

        GD.Print("====== 突破所有防线！强行挂载完美女仆！ ======");
        var originalBody = visuals.GetNodeOrNull<Node2D>("%Visuals");
        originalBody?.Hide();

        var sakuyaNode = scene.Instantiate<Node2D>();
        if (sakuyaNode == null) return;

        sakuyaNode.Name = "SakuyaMecha";
        visuals.AddChild(sakuyaNode);
        sakuyaNode.Position = new Vector2(0, 0);
        sakuyaNode.Scale = new Vector2(3.0f, 3.0f);
        sakuyaNode.Visible = true;

        var sakuyaSprite = SakuyaGlobals.FindFirstNode<AnimatedSprite2D>(sakuyaNode);
        var sakuyaVoice = SakuyaGlobals.FindFirstNode<AudioStreamPlayer2D>(sakuyaNode, n => n.Name == "SakuyaVoice");

        // ==========================================
        // 🎇 极其清爽的单轨特效节点抓取！
        // ==========================================
        var sakuyaVFX = SakuyaGlobals.FindFirstNode<AnimatedSprite2D>(sakuyaNode, n => n.Name == "SakuyaVFX");

        if (sakuyaVFX != null)
        {
            sakuyaVFX.Visible = false;
            // 🚨 播完即焚的隐身协议
            sakuyaVFX.AnimationFinished += () => { sakuyaVFX.Visible = false; sakuyaVFX.Stop(); };
        }
        else
        {
            // 🚨 赛博听诊器：如果还抓不到，这行红字绝对会暴露真凶！
            GD.PrintErr("💥 严重事故：全图扫描未发现 'SakuyaVFX' 节点！长官请检查 Godot 里节点名字是否拼写正确！");
        }

        if (sakuyaSprite != null)
        {
            SakuyaGlobals.ActiveSakuyaSprites.RemoveWhere(s => !GodotObject.IsInstanceValid(s));
            SakuyaGlobals.ActiveSakuyaSprites.Add(sakuyaSprite);

            sakuyaSprite.AnimationFinished += () =>
            {
                if (sakuyaSprite.Animation != "Idle" && sakuyaSprite.Animation != "Die" && sakuyaSprite.Animation != "Victory")
                {
                    sakuyaSprite.Play("Idle");
                    sakuyaNode.Position = new Vector2(0, 0);
                }
            };

            sakuyaSprite.Play("Intro");

            if (sakuyaVoice != null)
            {
                string chosenIntroVoice = SakuyaGlobals.IntroVoicePool[SakuyaGlobals.Rng.Next(SakuyaGlobals.IntroVoicePool.Length)];
                sakuyaVoice.Stream = ResourceLoader.Load<AudioStream>(chosenIntroVoice);
                sakuyaVoice.Play();
                GD.Print($"📢 入场播报：极其优雅地播放了 {chosenIntroVoice} !");
            }
        }

        var syncTimer = new Godot.Timer();
        syncTimer.Name = "SakuyaDirectionRadar";
        syncTimer.WaitTime = 0.05f;
        syncTimer.Autostart = true;
        sakuyaNode.AddChild(syncTimer);

        Node2D? bodyRef = visuals.GetNodeOrNull<Node2D>("%Visuals");
        Node2D? sakuyaRef = sakuyaNode;

        syncTimer.Timeout += () =>
        {
            if (GodotObject.IsInstanceValid(bodyRef) && GodotObject.IsInstanceValid(sakuyaRef))
            {
                float targetSign = Mathf.Sign(bodyRef.Scale.X);
                float currentSign = Mathf.Sign(sakuyaRef.Scale.X);

                if (targetSign != currentSign && targetSign != 0)
                {
                    float absX = Mathf.Abs(sakuyaRef.Scale.X);
                    sakuyaRef.Scale = new Vector2(absX * targetSign, sakuyaRef.Scale.Y);
                }
            }
        };
        GD.Print("咲夜物理矫正完毕！");
    }
}

[HarmonyPatch(typeof(NCreature), nameof(NCreature.SetAnimationTrigger))]
internal static class NCreature_SetAnimationTrigger_Patch
{
    private static void Postfix(NCreature __instance, string trigger)
    {
        if (SakuyaGlobals.IsInShop || SakuyaGlobals.IsDead) return;

        var player = __instance?.Entity?.Player;
        if (player == null || !string.Equals(player.Character?.Id?.Entry, SakuyaGlobals.TargetCharacterId, StringComparison.OrdinalIgnoreCase)) return;

        var visuals = __instance?.Visuals;
        var sakuyaMecha = visuals?.GetNodeOrNull<Node2D>("SakuyaMecha");
        if (sakuyaMecha == null) return;

        var sakuyaSprite = SakuyaGlobals.FindFirstNode<AnimatedSprite2D>(sakuyaMecha);
        var sakuyaVoice = SakuyaGlobals.FindFirstNode<AudioStreamPlayer2D>(sakuyaMecha, n => n.Name == "SakuyaVoice");

        if (sakuyaSprite == null) return;

        GD.Print($"---> Sakuya Maid Project: 收到动作指令: {trigger} <---");

        switch (trigger)
        {
            case "Attack":
            case "AttackSingle":
            case "AttackTriple":
            {
                // ==========================================
                // 🎭 极其致命的【华丽收场】拦截协议（补回遗漏！）
                // ==========================================
                if (SakuyaGlobals.NextAttackIsFinale)
                {
                    GD.Print("极其优雅地拦截了普通攻击，转换为华丽收场处决！");
                    SakuyaGlobals.NextAttackIsFinale = false; // 用完立刻销毁防连发

                    sakuyaSprite.Stop();
                    sakuyaSprite.Play("Victory"); // 极其优雅的鞠躬！

                    if (sakuyaVoice != null && SakuyaGlobals.CastVoicePool.Length > 0)
                    {
                        string chosenFinaleVoice = SakuyaGlobals.CastVoicePool[SakuyaGlobals.Rng.Next(SakuyaGlobals.CastVoicePool.Length)];
                        sakuyaVoice.Stream = ResourceLoader.Load<AudioStream>(chosenFinaleVoice);
                        sakuyaVoice.Play();
                    }
                    sakuyaMecha.Position = new Vector2(0, 0);
                    break; // 🚨 极其关键的 break，阻止播放普通攻击！
                }
                // ==========================================

                string chosenAttack = SakuyaGlobals.AttackPool[SakuyaGlobals.Rng.Next(SakuyaGlobals.AttackPool.Length)];
                string chosenAttackVoice = SakuyaGlobals.AttackVoicePool[SakuyaGlobals.Rng.Next(SakuyaGlobals.AttackVoicePool.Length)];
                
                GD.Print($"极其华丽地抽中了: {chosenAttack} !");
                sakuyaSprite.Stop();
                sakuyaSprite.Play(chosenAttack);

                if (sakuyaVoice != null)
                {
                    sakuyaVoice.Stream = ResourceLoader.Load<AudioStream>(chosenAttackVoice);
                    sakuyaVoice.Play();
                }

                sakuyaMecha.Position = new Vector2(0, 0);
                break;
            }
            case "Shiv":
            {
                string chosenShiv = SakuyaGlobals.ShivPool[SakuyaGlobals.Rng.Next(SakuyaGlobals.ShivPool.Length)];
                string chosenShivVoice = "";

                if (SakuyaGlobals.ShivVoicePool.Length > 0)
                {
                    chosenShivVoice = SakuyaGlobals.ShivVoicePool[SakuyaGlobals.Rng.Next(SakuyaGlobals.ShivVoicePool.Length)];
                }
                
                GD.Print($"极其精准地掷出了小刀: {chosenShiv} !");
                sakuyaSprite.Stop();
                sakuyaSprite.Play(chosenShiv);

                if (sakuyaVoice != null && !string.IsNullOrEmpty(chosenShivVoice))
                {
                    sakuyaVoice.Stream = ResourceLoader.Load<AudioStream>(chosenShivVoice);
                    sakuyaVoice.Play();
                }

                sakuyaMecha.Position = new Vector2(0, 0);
                break;
            }
            case "Hit":
            {
                string chosenHit = SakuyaGlobals.HitPool[SakuyaGlobals.Rng.Next(SakuyaGlobals.HitPool.Length)];
                string chosenHitVoice = SakuyaGlobals.HitVoicePool[SakuyaGlobals.Rng.Next(SakuyaGlobals.HitVoicePool.Length)];
                
                sakuyaSprite.Stop();
                sakuyaSprite.Play(chosenHit);

                if (sakuyaVoice != null)
                {
                    sakuyaVoice.Stream = ResourceLoader.Load<AudioStream>(chosenHitVoice);
                    sakuyaVoice.Play();
                }

                sakuyaMecha.Position = new Vector2(0, 0);
                break;
            }
            case "Cast":
            {
                string chosenCast = SakuyaGlobals.CastPool[SakuyaGlobals.Rng.Next(SakuyaGlobals.CastPool.Length)];
                string chosenCastVoice = SakuyaGlobals.CastVoicePool[SakuyaGlobals.Rng.Next(SakuyaGlobals.CastVoicePool.Length)];
                
                sakuyaSprite.Stop();
                sakuyaSprite.Play(chosenCast);

                if (sakuyaVoice != null)
                {
                    sakuyaVoice.Stream = ResourceLoader.Load<AudioStream>(chosenCastVoice);
                    sakuyaVoice.Play();
                }

                // ==========================================
                // 🎇 极其纯粹的五连抽特效引爆器！
                // ==========================================
                var sakuyaVFX = SakuyaGlobals.FindFirstNode<AnimatedSprite2D>(sakuyaMecha, n => n.Name == "SakuyaVFX");

                if (sakuyaVFX != null)
                {
                    // 🎲 极其关键的极客知识：C# 的 Rng.Next(min, max) 包含 min，但不包含 max！
                    // 所以要想抽出 1, 2, 3, 4, 5，必须极其精准地写成 (1, 6)！
                    int randomVfxIndex = SakuyaGlobals.Rng.Next(1, 6); 
                    
                    sakuyaVFX.Visible = true;
                    sakuyaVFX.Play($"CastEffect_{randomVfxIndex}"); 
                    GD.Print($"🎇 极其华丽地抽中并引爆了特效：CastEffect_{randomVfxIndex} !");
                }
                else
                {
                    GD.PrintErr("💥 严重事故：全图扫描未发现 'SakuyaVFX' 节点！");
                }
                sakuyaMecha.Position = new Vector2(0, 0);
                break;
            }
            case "Die":
            case "Death":
            case "Dead":
                SakuyaGlobals.IsDead = true;
                sakuyaSprite.Stop();
                sakuyaSprite.Play("Die");
                sakuyaMecha.Position = new Vector2(0, 0);
                break;
            default:
                sakuyaSprite.Play("Idle");
                sakuyaMecha.Position = new Vector2(0, 0);
                break;
        }
    }
}

[HarmonyPatch(typeof(NCreature), "AnimDie")]
internal static class NCreature_AnimDie_Patch
{
    private static void Prefix(NCreature __instance)
    {
        var player = __instance.Entity?.Player;
        if (player == null || !string.Equals(player.Character?.Id?.Entry, SakuyaGlobals.TargetCharacterId, StringComparison.OrdinalIgnoreCase)) return;

        var visuals = __instance.Visuals;
        var sakuyaMecha = visuals?.GetNodeOrNull<Node2D>("SakuyaMecha");
        if (sakuyaMecha == null) return;

        var sakuyaSprite = SakuyaGlobals.FindFirstNode<AnimatedSprite2D>(sakuyaMecha);
        if (sakuyaSprite == null) return;

        SakuyaGlobals.IsDead = true;
        sakuyaSprite.Stop();
        sakuyaSprite.Play("Die");
        sakuyaMecha.Position = new Vector2(0, 0);
    }
}

[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Combat.CombatManager), "EndCombatInternal")]
internal static class CombatManager_EndCombatInternal_Patch
{
    private static void Prefix(object __instance) 
    {
        GD.Print("\n====== 🏆 侦测到底层宣布战斗结束！通过红魔馆频道呼叫全体女仆！ ======");
        SakuyaGlobals.ActiveSakuyaSprites.RemoveWhere(s => !GodotObject.IsInstanceValid(s));

        if (SakuyaGlobals.ActiveSakuyaSprites.Count <= 0) return;

        string roomTypeStr = "Monster";
        try
        {
            var combatState = Traverse.Create(__instance).Property("CombatState").GetValue() ??
                              Traverse.Create(__instance).Field("CombatState").GetValue();
            if (combatState != null)
            {
                var encounter = Traverse.Create(combatState).Property("Encounter").GetValue() ??
                                Traverse.Create(combatState).Field("Encounter").GetValue();
                if (encounter != null)
                {
                    var roomTypeEnum = Traverse.Create(encounter).Property("RoomType").GetValue();
                    roomTypeStr = roomTypeEnum?.ToString() ?? "Monster";
                }
            }
        }
        catch { }

        string chosenVictoryVoice = "res://Hcxmmx_Touhou_Sakuya_Skin/Audio/Vo_tedium_sakuya.wav";
        if (roomTypeStr == "Boss") chosenVictoryVoice = "res://Hcxmmx_Touhou_Sakuya_Skin/Audio/Vo_beat_sakuya.wav"; 
        else if (roomTypeStr == "Elite") chosenVictoryVoice = "res://Hcxmmx_Touhou_Sakuya_Skin/Audio/Vo_win_sakuya.wav";

        // 播报系统执行
        foreach (var sprite in SakuyaGlobals.ActiveSakuyaSprites)
        {
            bool isAlreadyBowing = (sprite.Animation == "Victory");
            if (!isAlreadyBowing)
            {
                sprite.Stop();
                sprite.Play("Victory");
            }
            else
            {
                GD.Print("💃 侦测到咲夜正在行礼，极其平滑地跳过重复播放！");
            }

            var mechaNode = sprite.GetParent();
            AudioStreamPlayer2D? voiceNode = null;
            if (mechaNode != null)
            {
                voiceNode = SakuyaGlobals.FindFirstNode<AudioStreamPlayer2D>(mechaNode, n => n.Name == "SakuyaVoice");
            }

            if (voiceNode != null)
            {
                voiceNode.Stream = ResourceLoader.Load<AudioStream>(chosenVictoryVoice);
                voiceNode.Play();
            }
        }
    }
}

// 🚨 这里就是刚才说的卡牌拦截，长官写得极其完美！
[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Models.CardModel), "OnPlayWrapper")]
internal static class CardModel_OnPlayWrapper_Patch
{
    private static void Prefix(object __instance) 
    {
        var entryName = SakuyaGlobals.GetCardEntry(__instance);
        if (entryName == "GRAND_FINALE") 
        {
            GD.Print("🎯 赛博雷达极其刺耳地警报：侦测到【华丽收场】即将打出！处决动作预热完毕！");
            SakuyaGlobals.NextAttackIsFinale = true;
        }
    }
}