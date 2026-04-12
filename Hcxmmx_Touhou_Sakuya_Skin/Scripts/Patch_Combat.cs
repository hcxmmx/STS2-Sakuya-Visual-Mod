using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Hcxmmx.SakuyaMod.Scripts;

internal static class SakuyaCombatNodeCache
{
    private static readonly StringName SpriteKey = new("SakuyaSpriteRef");
    private static readonly StringName VoiceKey = new("SakuyaVoiceRef");
    private static readonly StringName VfxKey = new("SakuyaVFXRef");

    internal static void Store(Node2D mecha, AnimatedSprite2D? sprite, AudioStreamPlayer2D? voice, AnimatedSprite2D? vfx)
    {
        if (sprite != null) mecha.SetMeta(SpriteKey, sprite);
        if (voice != null) mecha.SetMeta(VoiceKey, voice);
        if (vfx != null) mecha.SetMeta(VfxKey, vfx);
    }

    internal static AnimatedSprite2D? GetSprite(Node2D mecha)
    {
        var sprite = GetCachedNode<AnimatedSprite2D>(mecha, SpriteKey);
        if (sprite != null) return sprite;

        sprite = SakuyaGlobals.FindFirstNode<AnimatedSprite2D>(mecha);
        if (sprite != null) mecha.SetMeta(SpriteKey, sprite);
        return sprite;
    }

    internal static AudioStreamPlayer2D? GetVoice(Node2D mecha)
    {
        var voice = GetCachedNode<AudioStreamPlayer2D>(mecha, VoiceKey);
        if (voice != null) return voice;

        voice = SakuyaGlobals.FindFirstNode<AudioStreamPlayer2D>(mecha, n => n.Name == "SakuyaVoice");
        if (voice != null) mecha.SetMeta(VoiceKey, voice);
        return voice;
    }

    internal static AnimatedSprite2D? GetVfx(Node2D mecha)
    {
        var vfx = GetCachedNode<AnimatedSprite2D>(mecha, VfxKey);
        if (vfx != null) return vfx;

        vfx = SakuyaGlobals.FindFirstNode<AnimatedSprite2D>(mecha, n => n.Name == "SakuyaVFX");
        if (vfx != null) mecha.SetMeta(VfxKey, vfx);
        return vfx;
    }

    private static T? GetCachedNode<T>(Node2D mecha, StringName key) where T : Node
    {
        if (!mecha.HasMeta(key)) return null;

        var cachedObject = mecha.GetMeta(key).AsGodotObject();
        return cachedObject is T node && GodotObject.IsInstanceValid(node) ? node : null;
    }
}

[HarmonyPatch(typeof(NCreature), nameof(NCreature._Ready))]
internal static class NCreature_Ready_Patch
{
    // 赛博保险丝：本局内Ready阶段VFX缺失错误仅播报一次。
    private static bool _hasPrintedReadyVfxError = false;

    private static void Postfix(NCreature __instance)
    {
        SakuyaGlobals.IsDead = false;
        SakuyaGlobals.IsInShop = false;
        SakuyaGlobals.VerboseLog($"\n---> Sakuya Maid Project: 侦测到 NCreature 试图活化！节点名称 = {__instance.Name} <---");

        var scene = SakuyaGlobals.SakuyaScene ?? ResourceLoader.Load<PackedScene>(SakuyaGlobals.SakuyaScenePath);
        SakuyaGlobals.SakuyaScene = scene;

        if (scene == null || __instance.Entity == null) return;

        var player = __instance.Entity.Player;
        if (player == null || !string.Equals(player.Character?.Id?.Entry, SakuyaGlobals.TargetCharacterId, StringComparison.OrdinalIgnoreCase)) return;

        var visuals = __instance.Visuals;
        if (visuals == null) return;

        SakuyaGlobals.VerboseLog("====== 突破所有防线！强行挂载完美女仆！ ======");
        var originalBody = visuals.GetNodeOrNull<Node2D>("%Visuals");
        originalBody?.Hide();

        var sakuyaNode = scene.Instantiate<Node2D>();
        if (sakuyaNode == null) return;

        sakuyaNode.Name = "SakuyaMecha";
        visuals.AddChild(sakuyaNode);
        sakuyaNode.Position = Vector2.Zero;
        sakuyaNode.Scale = new Vector2(3.0f, 3.0f);
        sakuyaNode.Visible = true;

        var sakuyaSprite = SakuyaGlobals.FindFirstNode<AnimatedSprite2D>(sakuyaNode);
        var sakuyaVoice = SakuyaGlobals.FindFirstNode<AudioStreamPlayer2D>(sakuyaNode, n => n.Name == "SakuyaVoice");

        // ==========================================
        // 🎇 极其清爽的单轨特效节点抓取！
        // ==========================================
        var sakuyaVFX = SakuyaGlobals.FindFirstNode<AnimatedSprite2D>(sakuyaNode, n => n.Name == "SakuyaVFX");
        SakuyaCombatNodeCache.Store(sakuyaNode, sakuyaSprite, sakuyaVoice, sakuyaVFX);

        if (sakuyaVFX != null)
        {
            sakuyaVFX.Visible = false;
            // 🚨 播完即焚的隐身协议
            sakuyaVFX.AnimationFinished += () => { sakuyaVFX.Visible = false; sakuyaVFX.Stop(); };
        }
        else
        {
            // 🚨 赛博听诊器：如果还抓不到，这行红字绝对会暴露真凶！
            if (!_hasPrintedReadyVfxError)
            {
                GD.PrintErr("💥 严重事故：全图扫描未发现 'SakuyaVFX' 节点！长官请检查 Godot 里节点名字是否拼写正确！(为防卡顿，此错误本局仅播报一次)");
                _hasPrintedReadyVfxError = true;
            }
        }

        if (sakuyaSprite != null)
        {
            SakuyaGlobals.CleanupActiveSakuyaSprites();
            SakuyaGlobals.ActiveSakuyaSprites.Add(sakuyaSprite);

            sakuyaSprite.AnimationFinished += () =>
            {
                if (sakuyaSprite.Animation != "Idle" && sakuyaSprite.Animation != "Die" && sakuyaSprite.Animation != "Victory")
                {
                    sakuyaSprite.Play("Idle");
                    sakuyaNode.Position = Vector2.Zero;
                }
            };

            sakuyaSprite.Play("Intro");

            if (sakuyaVoice != null)
            {
                string chosenIntroVoice = SakuyaGlobals.IntroVoicePool[SakuyaGlobals.Rng.Next(SakuyaGlobals.IntroVoicePool.Length)];
                sakuyaVoice.Stream = SakuyaGlobals.GetAudioStreamCached(chosenIntroVoice);
                sakuyaVoice.Play();
                SakuyaGlobals.VerboseLog($"📢 入场播报：极其优雅地播放了 {chosenIntroVoice} !");
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
            if (!GodotObject.IsInstanceValid(bodyRef) || !GodotObject.IsInstanceValid(sakuyaRef))
            {
                syncTimer.Stop();
                syncTimer.QueueFree();
                return;
            }

            float targetSign = Mathf.Sign(bodyRef.Scale.X);
            float currentSign = Mathf.Sign(sakuyaRef.Scale.X);

            if (targetSign != currentSign && targetSign != 0)
            {
                float absX = Mathf.Abs(sakuyaRef.Scale.X);
                sakuyaRef.Scale = new Vector2(absX * targetSign, sakuyaRef.Scale.Y);
            }
        };
        SakuyaGlobals.VerboseLog("咲夜物理矫正完毕！");
    }
}

[HarmonyPatch(typeof(NCreature), nameof(NCreature.SetAnimationTrigger))]
internal static class NCreature_SetAnimationTrigger_Patch
{
    // 赛博保险丝：本局内VFX缺失错误仅播报一次，避免高频刷屏。
    private static bool _hasPrintedVfxError = false;

    private static void Postfix(NCreature __instance, string trigger)
    {
        if (SakuyaGlobals.IsInShop || SakuyaGlobals.IsDead) return;

        var player = __instance?.Entity?.Player;
        if (player == null || !string.Equals(player.Character?.Id?.Entry, SakuyaGlobals.TargetCharacterId, StringComparison.OrdinalIgnoreCase)) return;

        var visuals = __instance?.Visuals;
        var sakuyaMecha = visuals?.GetNodeOrNull<Node2D>("SakuyaMecha");
        if (sakuyaMecha == null) return;

        var sakuyaSprite = SakuyaCombatNodeCache.GetSprite(sakuyaMecha);
        var sakuyaVoice = SakuyaCombatNodeCache.GetVoice(sakuyaMecha);

        if (sakuyaSprite == null) return;

        SakuyaGlobals.VerboseLog($"---> Sakuya Maid Project: 收到动作指令: {trigger} <---");

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
                    SakuyaGlobals.VerboseLog("极其优雅地拦截了普通攻击，转换为华丽收场处决！");
                    SakuyaGlobals.NextAttackIsFinale = false; // 用完立刻销毁防连发

                    sakuyaSprite.Stop();
                    sakuyaSprite.Play("Victory"); // 极其优雅的鞠躬！

                    if (sakuyaVoice != null && SakuyaGlobals.CastVoicePool.Length > 0)
                    {
                        string chosenFinaleVoice = SakuyaGlobals.CastVoicePool[SakuyaGlobals.Rng.Next(SakuyaGlobals.CastVoicePool.Length)];
                        sakuyaVoice.Stream = SakuyaGlobals.GetAudioStreamCached(chosenFinaleVoice);
                        sakuyaVoice.Play();
                    }
                    sakuyaMecha.Position = Vector2.Zero;
                    break; // 🚨 极其关键的 break，阻止播放普通攻击！
                }
                // ==========================================

                string chosenAttack = SakuyaGlobals.AttackPool[SakuyaGlobals.Rng.Next(SakuyaGlobals.AttackPool.Length)];
                string chosenAttackVoice = SakuyaGlobals.AttackVoicePool[SakuyaGlobals.Rng.Next(SakuyaGlobals.AttackVoicePool.Length)];
                
                SakuyaGlobals.VerboseLog($"极其华丽地抽中了: {chosenAttack} !");
                sakuyaSprite.Stop();
                sakuyaSprite.Play(chosenAttack);

                if (sakuyaVoice != null)
                {
                    sakuyaVoice.Stream = SakuyaGlobals.GetAudioStreamCached(chosenAttackVoice);
                    sakuyaVoice.Play();
                }

                sakuyaMecha.Position = Vector2.Zero;
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
                
                SakuyaGlobals.VerboseLog($"极其精准地掷出了小刀: {chosenShiv} !");
                sakuyaSprite.Stop();
                sakuyaSprite.Play(chosenShiv);

                if (sakuyaVoice != null && !string.IsNullOrEmpty(chosenShivVoice))
                {
                    sakuyaVoice.Stream = SakuyaGlobals.GetAudioStreamCached(chosenShivVoice);
                    sakuyaVoice.Play();
                }

                sakuyaMecha.Position = Vector2.Zero;
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
                    sakuyaVoice.Stream = SakuyaGlobals.GetAudioStreamCached(chosenHitVoice);
                    sakuyaVoice.Play();
                }

                sakuyaMecha.Position = Vector2.Zero;
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
                    sakuyaVoice.Stream = SakuyaGlobals.GetAudioStreamCached(chosenCastVoice);
                    sakuyaVoice.Play();
                }

                // ==========================================
                // 🎇 极其纯粹的五连抽特效引爆器！
                // ==========================================
                var sakuyaVFX = SakuyaCombatNodeCache.GetVfx(sakuyaMecha);

                if (sakuyaVFX != null)
                {
                    int randomVfxIndex = SakuyaGlobals.Rng.Next(SakuyaGlobals.VfxNames.Length);
                    string selectedVfx = SakuyaGlobals.VfxNames[randomVfxIndex];
                    
                    sakuyaVFX.Visible = true;
                    sakuyaVFX.Play(selectedVfx);
                    SakuyaGlobals.VerboseLog($"🎇 极其华丽地抽中并引爆了特效：{selectedVfx} !");
                }
                else
                {
                    if (!_hasPrintedVfxError)
                    {
                        GD.PrintErr("💥 严重事故：全图扫描未发现 'SakuyaVFX' 节点！(为防卡顿，此错误本局仅播报一次)");
                        _hasPrintedVfxError = true;
                    }
                }
                sakuyaMecha.Position = Vector2.Zero;
                break;
            }
            case "Die":
            case "Death":
            case "Dead":
                SakuyaGlobals.IsDead = true;
                sakuyaSprite.Stop();
                sakuyaSprite.Play("Die");
                sakuyaMecha.Position = Vector2.Zero;
                break;
            default:
                sakuyaSprite.Play("Idle");
                sakuyaMecha.Position = Vector2.Zero;
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

        var sakuyaSprite = SakuyaCombatNodeCache.GetSprite(sakuyaMecha);
        if (sakuyaSprite == null) return;

        SakuyaGlobals.IsDead = true;
        sakuyaSprite.Stop();
        sakuyaSprite.Play("Die");
        sakuyaMecha.Position = Vector2.Zero;
    }
}

[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Combat.CombatManager), "EndCombatInternal")]
internal static class CombatManager_EndCombatInternal_Patch
{
    private static void Prefix(object __instance) 
    {
        SakuyaGlobals.VerboseLog("\n====== 🏆 侦测到底层宣布战斗结束！通过红魔馆频道呼叫全体女仆！ ======");
        SakuyaGlobals.CleanupActiveSakuyaSprites();

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
                SakuyaGlobals.VerboseLog("💃 侦测到咲夜正在行礼，极其平滑地跳过重复播放！");
            }

            var mechaNode = sprite.GetParent();
            AudioStreamPlayer2D? voiceNode = null;
            if (mechaNode != null)
            {
                if (mechaNode is Node2D mecha2D)
                {
                    voiceNode = SakuyaCombatNodeCache.GetVoice(mecha2D);
                }
                else
                {
                    voiceNode = SakuyaGlobals.FindFirstNode<AudioStreamPlayer2D>(mechaNode, n => n.Name == "SakuyaVoice");
                }
            }

            if (voiceNode != null)
            {
                voiceNode.Stream = SakuyaGlobals.GetAudioStreamCached(chosenVictoryVoice);
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
            SakuyaGlobals.VerboseLog("🎯 赛博雷达极其刺耳地警报：侦测到【华丽收场】即将打出！处决动作预热完毕！");
            SakuyaGlobals.NextAttackIsFinale = true;
        }
    }
}