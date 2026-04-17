using System;
using Godot;
using HarmonyLib;

namespace Hcxmmx.SakuyaMod.Scripts; // ✅ 极其规范的新命名空间

[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect.NCharacterSelectScreen), "SelectCharacter")]
internal static class NCharacterSelectScreen_SelectCharacter_Patch
{
    private static void Prefix(object characterModel, ref string __state)
    {
        var entryName = SakuyaGlobals.GetCharacterEntry(characterModel);
        if (!string.Equals(entryName, SakuyaGlobals.TargetCharacterId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        __state = Traverse.Create(characterModel).Property("CharacterSelectSfx").GetValue<string>()
            ?? Traverse.Create(characterModel).Field("CharacterSelectSfx").GetValue<string>();

        try { Traverse.Create(characterModel).Property("CharacterSelectSfx").SetValue(""); } catch { }
        try { Traverse.Create(characterModel).Field("CharacterSelectSfx").SetValue(""); } catch { }
        try { Traverse.Create(characterModel).Field("<CharacterSelectSfx>k__BackingField").SetValue(""); } catch { }
    }

    private static void Postfix(MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect.NCharacterSelectScreen __instance, Node charSelectButton, object characterModel, ref string __state)
    {
        var entryName = SakuyaGlobals.GetCharacterEntry(characterModel);
        if (!string.Equals(entryName, SakuyaGlobals.TargetCharacterId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        GD.Print("\n====== 🎯 选人界面雷达：侦测到完美女仆登场！启动视觉劫持！ ======");

        var bgContainer = Traverse.Create(__instance).Field("_bgContainer").GetValue<Control>();
        var nameLabel = Traverse.Create(__instance).Field("_name").GetValue();
        var descLabel = Traverse.Create(__instance).Field("_description").GetValue<RichTextLabel>();

        if (bgContainer != null)
        {
            foreach (Node child in bgContainer.GetChildren())
            {
                if (child is CanvasItem canvasItem)
                {
                    canvasItem.Hide();
                }
            }

            // 🚨 极其关键的路径：长官记得在 Godot 里捏一个叫 SakuyaSelectScreen.tscn 的背景场景！
            var sakuyaScreenScene = ResourceLoader.Load<PackedScene>("res://mods/Hcxmmx_Touhou_Sakuya_Skin/Scenes/SakuyaSelectScreen.tscn");
            if (sakuyaScreenScene != null)
            {
                var sakuyaScreen = sakuyaScreenScene.Instantiate<Control>();
                sakuyaScreen.SetAnchorsPreset(Control.LayoutPreset.FullRect);
                bgContainer.AddChild(sakuyaScreen);
                GD.Print("✅ 红魔馆背景铺设完毕！");

                var voicePlayer = new AudioStreamPlayer();
                voicePlayer.Stream = ResourceLoader.Load<AudioStream>("res://mods/Hcxmmx_Touhou_Sakuya_Skin/Audio/Vo_select_sakuya.wav");
                sakuyaScreen.AddChild(voicePlayer); // 把播放器挂在UI图层上
                voicePlayer.Play();
                GD.Print("📢 选人语音播报：时间差不多了，我们出发吧！");
            }
        }

        if (nameLabel != null)
        {
            // 🎯 修改为咲夜的名字
            Traverse.Create(nameLabel).Method("SetTextAutoSize", new object[] { "十六夜 咲夜" }).GetValue();
        }

        if (descLabel != null)
        {
            // 🎯 修改为咲夜的专属介绍
            descLabel.Text = "完美潇洒的从者，拥有操纵时间程度的能力。";
        }

        if (__state != null)
        {
            try { Traverse.Create(characterModel).Property("CharacterSelectSfx").SetValue(__state); } catch { }
            try { Traverse.Create(characterModel).Field("CharacterSelectSfx").SetValue(__state); } catch { }
            try { Traverse.Create(characterModel).Field("<CharacterSelectSfx>k__BackingField").SetValue(__state); } catch { }
        }

        GD.Print("🎉 UI 篡改与防崩溃战术静音协议极其完美地执行完毕！");
    }
}

[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect.NCharacterSelectButton), "Init")]
internal static class NCharacterSelectButton_Init_Patch
{
    private static void Postfix(object __instance, object character)
    {
        var entryName = SakuyaGlobals.GetCharacterEntry(character);
        if (!string.Equals(entryName, SakuyaGlobals.TargetCharacterId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        GD.Print("\n====== 🎯 头像雷达：锁定咲夜选人按钮！启动物理换脸！ ======");

        // 🚨 极其关键的路径：长官需要准备一张咲夜的头像图片放在 Assets 文件夹里！
        var customAvatar = ResourceLoader.Load<Texture2D>("res://mods/Hcxmmx_Touhou_Sakuya_Skin/Assets/Sakuya_Avatar.png");
        if (customAvatar == null)
        {
            GD.PrintErr("💥 找不到咲夜的头像图片！长官检查一下路径和文件名喵？");
            return;
        }

        var iconNode = Traverse.Create(__instance).Field("_icon").GetValue();
        if (iconNode == null)
        {
            return;
        }

        Traverse.Create(iconNode).Property("Texture").SetValue(customAvatar);
        GD.Print("✅ 咲夜头像极其完美地贴上去了！");
    }
}