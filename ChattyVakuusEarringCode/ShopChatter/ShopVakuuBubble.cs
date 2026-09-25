using System;
using System.Text.RegularExpressions;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.ShopChatter;

/// <summary>
/// ショップでヴァクーの吹き出しを実際に表示する部分。戦闘中の<c>TalkCmd.Play</c>に相当するが、
/// あちらは<c>Creature.GetVfxContainer()</c>(戦闘中の<c>NCombatRoom</c>と図鑑の<c>NBestiary</c>しか見ない)
/// に依存していて商人の部屋では使えないため、代わりに商人自身の台詞(<c>NMerchantButton.PlayDialogue</c>)と
/// 同じ、座標を直接指定する<see cref="NSpeechBubbleVfx.Create(string,DialogueSide,Vector2,double,VfxColor)"/>を使う。
/// </summary>
internal static class ShopVakuuBubble
{
    /// <summary>非強制発言の最短表示時間(秒)。</summary>
    private const double MinDurationSeconds = 1.5;

    /// <summary>1文字あたりの表示時間(秒)。<c>TalkCmd</c>の計算式(戦闘中)に合わせている。</summary>
    private const double SecondsPerChar = 0.12;

    /// <summary>
    /// 頭部を示すノード(名前に"head"を含む<see cref="Node2D"/>)が見つかった時、そこからさらに右に出すオフセット。
    /// </summary>
    private static readonly Vector2 HeadNudge = new(40f, -20f);

    /// <summary>
    /// 頭部を示すノードが見つからないキャラクター向けの、大まかな見た目調整用の固定オフセット
    /// (キャラクター原点=足元からの相対位置)。「全身の上から2割の高さ・右端よりやや外側」を狙った推定値
    /// (ネクロバインダーの頭部装飾ノードの実測位置から逆算)。5キャラクター中4体はこちらを使う想定。
    /// 実機で見た目を確認しながら調整してよい。
    /// </summary>
    private static readonly Vector2 FallbackOffset = new(140f, -370f);

    /// <summary>
    /// ショップの立ち絵の頭上に吹き出しを出す。<paramref name="owner"/>は常にこの端末のローカルプレイヤーの前提
    /// (<see cref="ShopChatterHub"/>参照)。
    /// </summary>
    /// <returns>実際に表示できたらtrue(ノードが見つからない等の理由で出せなかったらfalse)。</returns>
    public static bool TryShow(Utterance utterance)
    {
        NMerchantCharacter? visual = GetLocalPlayerVisual();
        if (visual == null)
        {
            MainFile.Logger.Debug($"{MainFile.ModId}: shop speech skipped, no player visual found");
            return false;
        }

        string text = utterance.Line.GetFormattedText();
        Vector2 position = GetSpeechPosition(visual);
        double duration = Math.Max(MinDurationSeconds, GetRawCharCount(text) * SecondsPerChar);

        // 色は本家の囁きのイヤリングに合わせる(戦闘中のVakuuSpeakerと同じ)。
        NSpeechBubbleVfx? bubble = NSpeechBubbleVfx.Create(text, DialogueSide.Left, position, duration, VfxColor.Purple);
        if (bubble == null)
        {
            return false;
        }

        NMerchantRoom.Instance?.AddChildSafely(bubble);
        return true;
    }

    /// <summary>
    /// ローカルプレイヤーの立ち絵。<c>NMerchantRoom.AfterRoomIsLoaded</c>が必ずローカルプレイヤーを先頭に並べ替えて
    /// から立ち絵を生成しているため、常に0番目がローカルプレイヤーになる。
    /// </summary>
    private static NMerchantCharacter? GetLocalPlayerVisual()
    {
        var visuals = NMerchantRoom.Instance?.PlayerVisuals;
        return visuals is { Count: > 0 } ? visuals[0] : null;
    }

    private static Vector2 GetSpeechPosition(NMerchantCharacter visual)
    {
        Node2D? head = FindHeadNode(visual);
        if (head != null)
        {
            return head.GlobalPosition + HeadNudge;
        }

        return visual.GlobalPosition + FallbackOffset;
    }

    /// <summary>名前に"head"を含む<see cref="Node2D"/>子孫を再帰的に探す(ネクロバインダーの"HeadBoneNode"等)。</summary>
    private static Node2D? FindHeadNode(Node root)
    {
        foreach (Node child in root.GetChildren())
        {
            if (child is Node2D node2D && child.Name.ToString().Contains("head", StringComparison.OrdinalIgnoreCase))
            {
                return node2D;
            }

            Node2D? foundInChild = FindHeadNode(child);
            if (foundInChild != null)
            {
                return foundInChild;
            }
        }

        return null;
    }

    /// <summary>
    /// <c>TalkCmd.GetRawCharCount</c>と同じ計算(BBCode・改行・空白を除いた文字数)。あちらはprivateなので複製。
    /// </summary>
    private static int GetRawCharCount(string bbcodeText)
    {
        string text = Regex.Replace(bbcodeText, "\\[/?[^\\]]+\\]", "");
        return text.Replace("\n", "").Replace("\r", "").Replace(" ", "").Length;
    }
}
