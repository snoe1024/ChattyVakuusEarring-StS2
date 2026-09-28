using System;
using System.Text.RegularExpressions;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.CardRewardChatter;

/// <summary>
/// カード報酬画面でヴァクーの吹き出しを実際に表示する部分。特定のカードを名指しする時は
/// <c>ShopChatter.ShopVakuuBubble</c>と同じ、棘(しっぽ)付きの<see cref="NSpeechBubbleVfx"/>を、
/// 内容を問わない汎用的な一言は棘の無い<see cref="NThoughtBubbleVfx"/>(独り言・思考吹き出し)を使う
/// (下記「吹き出しの種類」参照)。
/// </summary>
internal static class CardRewardVakuuBubble
{
    /// <summary>非強制発言の最短表示時間(秒)。</summary>
    private const double MinDurationSeconds = 1.5;

    /// <summary>1文字あたりの表示時間(秒)。<c>TalkCmd</c>の計算式(戦闘中)に合わせている。</summary>
    private const double SecondsPerChar = 0.12;

    /// <summary>
    /// 特定のカードについて言う時、棘の先端をカードのどこに置くか(カード上辺中央からの微小オフセット。
    /// 完全に0だとカード上辺と重なって見えるので、少しだけ浮かせている)。
    /// </summary>
    private static readonly Vector2 CardEdgeOffset = new(0f, -8f);

    /// <summary>
    /// カードの当たり判定(<c>scenes/cards/holders/grid_card_holder.tscn</c>の<c>%Hitbox</c>)の中心から
    /// 上辺までの距離。<c>Hitbox</c>は<c>offset_top=-211</c>/<c>offset_bottom=211</c>で
    /// <c>NGridCardHolder</c>自身の原点を中心に上下対称に配置されているため、
    /// <c>NGridCardHolder</c>のローカル座標(0, -211)がちょうどカード上辺の中央になる
    /// (<see cref="GetCardEdgeAnchor"/>参照)。
    /// </summary>
    private const float CardHalfHeight = 211f;

    /// <summary>上記と同じ<c>Hitbox</c>の、中心から左右の辺までの距離(<c>offset_left=-150</c>/<c>offset_right=150</c>)。</summary>
    private const float CardHalfWidth = 150f;

    /// <summary>
    /// 棘の先端を、カード上辺の中央から本体が浮く側と**同じ**方向にどれだけ寄せるか(<see cref="CardHalfWidth"/>
    /// に対する比率)。<see cref="NSpeechBubbleVfx"/>の本体は棘の先端から常に斜め上に固定オフセットで浮くため、
    /// 先端を上辺のど真ん中に置くと、本体がその場でカード自身に重なってしまいやすい。
    /// </summary>
    /// <remarks>
    /// <b>2026-09、逆方向に寄せていたのを修正</b>: 当初は「本体が浮ぶ側と**逆**方向」に先端を寄せていた
    /// (隣のカードに到達するまでの距離を稼ぐ狙い)。しかし3枚中2枚目のカードで実機確認したところ、
    /// 「しっぽが吹き出し本体の左側から右に向かって出ているのに、先端は右端にくっついている」ため、
    /// 本体(先端から見て逆側、この場合は左)がカード自身の上に乗ってしまうと指摘された。逆方向に寄せる
    /// 設計は「隣のカードとの重なり」は減らせても、先端からの浮き先(=本体)が結局カード自身の内側を
    /// 通ることになり、**自分自身のカードとの重なり**を必ず生んでしまう欠陥があった。そこで
    /// 本体が浮ぶ側と同じ方向に先端を寄せる方式に変えた(両端のカード用の<see cref="OuterCardEdgeBiasRatio"/>
    /// と同じ考え方に統一)。これにより本体はカードの外へ向かって浮くようになるが、代わりに隣のカードへの
    /// 接近は多少増えるため、値の大きさ(<see cref="OuterCardEdgeBiasRatio"/>とは独立)はユーザー側での
    /// 微調整に委ねている。
    /// </remarks>
    private const float TailAnchorHorizontalBiasRatio = 0.6f;

    /// <summary>
    /// 一番左・一番右のカード(<see cref="GetCardEdgeAnchor"/>参照)について、棘の先端をカード自身の
    /// 外側の辺にどれだけ寄せるか(<see cref="CardHalfWidth"/>に対する比率)。1.0でちょうど辺の位置。
    /// 中央寄りのカード群(<see cref="TailAnchorHorizontalBiasRatio"/>)とは別の定数にしてあるので、
    /// 両端だけ個別に微調整できる。
    /// </summary>
    private const float OuterCardEdgeBiasRatio = 1.0f;

    /// <summary>
    /// 「カードを選択」のリボン(<c>scenes/ui/common_banner.tscn</c>)の画面中心からの半幅。
    /// リボンは中心から左右対称に654px幅(=327px)で、常に画面中心の上側(y=中心-334〜中心-172)に
    /// 表示される(anchors_preset=8で中心アンカー、offset_leftとoffset_rightがちょうど±327)。
    /// </summary>
    private const float BannerHalfWidth = 400f;

    /// <summary>汎用発言の棘の先端を、リボンの外側にどれだけ余分に離すか。</summary>
    private const float BannerClearanceMargin = 60f;

    /// <summary>汎用発言の棘の先端の、画面中心からの上方向オフセット(だいたいリボンと同じ高さを狙う)。</summary>
    private const float TopCornerUpwardOffset = 180f;

    /// <summary>
    /// <paramref name="anchorCard"/>が指定されていればそのカードの縁から棘付きの吹き出しを、
    /// 無ければ画面上部の左右どちらかの隅から棘の無い吹き出しを出す。
    /// </summary>
    /// <returns>実際に表示できたらtrue。</returns>
    public static bool TryShow(CardRewardObserver observer, CardModel? anchorCard, Utterance utterance)
    {
        string text = utterance.Line.GetFormattedText();
        double duration = Math.Max(MinDurationSeconds, GetRawCharCount(text) * SecondsPerChar);

        (Vector2 CardEdge, DialogueSide Side)? cardAnchor =
            anchorCard != null ? GetCardEdgeAnchor(observer, anchorCard) : null;

        Control? bubble;
        Vector2 position;
        if (cardAnchor != null)
        {
            position = cardAnchor.Value.CardEdge;
            // 色は本家の囁きのイヤリングに合わせる(戦闘中・ショップと同じ)。
            bubble = NSpeechBubbleVfx.Create(text, cardAnchor.Value.Side, position, duration, VfxColor.Purple);
        }
        else
        {
            (Vector2 Position, DialogueSide Side) corner = GetRandomTopCornerAnchor(observer);
            position = corner.Position;
            NThoughtBubbleVfx? thoughtBubble = NThoughtBubbleVfx.Create(text, corner.Side, duration);
            if (thoughtBubble?.GetNodeOrNull<Node2D>("%Tail") is { } tail)
            {
                tail.Visible = false;
            }

            bubble = thoughtBubble;
        }

        if (bubble == null)
        {
            return false;
        }

        // screenではなくオーバーレイ層自体に足す(理由はクラスのドキュメント参照)。
        // そのため画面が閉じられても自動では片付かないが、吹き出し自身がdurationで自己消滅する。
        NOverlayStack.Instance?.AddChildSafely(bubble);

        // NThoughtBubbleVfxはCreateの引数に座標を取らないので、木に入れた後で自分で設定する
        // (本家NEventOptionButtonの使い方と同じ順序)。
        bubble.GlobalPosition = position;

        return true;
    }

    /// <summary>
    /// 指定したカードの上辺沿いを棘の先端に、本体が浮ぶ側を決めて返す。
    /// </summary>
    private static (Vector2 CardEdge, DialogueSide Side)? GetCardEdgeAnchor(CardRewardObserver observer, CardModel card)
    {
        Control? holder = observer.Screen.FindCardHolder(card);
        if (holder == null)
        {
            return null;
        }

        int index = -1;
        for (int i = 0; i < observer.Cards.Count; i++)
        {
            if (observer.Cards[i] == card)
            {
                index = i;
                break;
            }
        }

        int count = observer.Cards.Count;
        DialogueSide side;
        float localX;
        if (index == 0)
        {
            // 一番左のカード: 本体は左(画面外側)に浮かせるのでRight、先端はカード自身の左端寄り。
            side = DialogueSide.Right;
            localX = -CardHalfWidth * OuterCardEdgeBiasRatio;
        }
        else if (index == count - 1)
        {
            // 一番右のカード: 本体は右(画面外側)に浮かせるのでLeft、先端はカード自身の右端寄り。
            side = DialogueSide.Left;
            localX = CardHalfWidth * OuterCardEdgeBiasRatio;
        }
        else
        {
            // 中央寄りのカード。本体が浮ぶ側と同じ方向に先端を寄せる(理由は下記ドキュメント参照)。
            bool isRightHalf = index >= 0 && index >= count / 2;
            side = isRightHalf ? DialogueSide.Right : DialogueSide.Left;
            localX = isRightHalf ? -CardHalfWidth * TailAnchorHorizontalBiasRatio : CardHalfWidth * TailAnchorHorizontalBiasRatio;
        }

        // holder(NGridCardHolder)自身のGlobalPosition/Sizeはカードの見た目の矩形と一致しない
        // (NGridCardHolderの原点はカードの"中心"にあり、子のHitboxが上下左右対称にoffsetされているだけで、
        // holder自身にはoffsetが無いためSizeは実質0になる。2026-09、実機で「カードの中央に吹き出しが出る」
        // というユーザー報告から判明した不具合)。そこでholderのGlobalTransformを使い、カード上辺沿いの
        // ローカル座標を実際の画面座標に変換する。GlobalTransformにはholder自身のScale(SmallScale)や
        // 祖先の位置も全て織り込まれているので、スケールを別途掛け直す必要が無い。
        Vector2 cardTopEdge = holder.GetGlobalTransform() * new Vector2(localX, -CardHalfHeight);

        return (cardTopEdge + CardEdgeOffset, side);
    }

    /// <summary>
    /// 画面上部の左端・右端のどちらかをランダムに選んで返す(「カードを選択」のリボンの外側)。
    /// 左端なら本体が左に浮くよう<c>Right</c>、右端なら本体が右に浮くよう<c>Left</c>を組にして返す
    /// (棘の位置決め共通の左右反転規則。<see cref="GetCardEdgeAnchor"/>参照)。
    /// </summary>
    private static (Vector2 Position, DialogueSide Side) GetRandomTopCornerAnchor(CardRewardObserver observer)
    {
        Rect2 viewport = observer.Screen.GetViewportRect();
        Vector2 center = viewport.Position + viewport.Size * 0.5f;
        float clearX = BannerHalfWidth + BannerClearanceMargin;

        bool useLeft = ChatterRandom.NextDouble() < 0.5;
        Vector2 position = center + new Vector2(useLeft ? -clearX : clearX, -TopCornerUpwardOffset);
        DialogueSide side = useLeft ? DialogueSide.Right : DialogueSide.Left;
        return (position, side);
    }

    /// <summary>
    /// <c>TalkCmd.GetRawCharCount</c>と同じ計算(BBCode・改行・空白を除いた文字数)。あちらはprivateなので複製
    /// (<c>ShopChatter.ShopVakuuBubble</c>と同じ複製)。
    /// </summary>
    private static int GetRawCharCount(string bbcodeText)
    {
        string text = Regex.Replace(bbcodeText, "\\[/?[^\\]]+\\]", "");
        return text.Replace("\n", "").Replace("\r", "").Replace(" ", "").Length;
    }
}
