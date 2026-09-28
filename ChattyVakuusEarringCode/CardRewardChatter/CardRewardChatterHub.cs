using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Config;
using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Runs;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.CardRewardChatter;

/// <summary>
/// カード報酬画面・「カードを1枚選ぶ」画面(ポーション等)が表示された時に<see cref="CardRewardDetector"/>群を
/// 回し、当選した発言があれば<see cref="CardRewardVakuuBubble"/>で表示する入口。
/// <c>Chatter.ChatterHub</c>/<c>ShopChatter.ShopChatterHub</c>のカード報酬画面版だが、こちらは持続する
/// セッションを持たない。画面が表示された瞬間だけで完結する(「入室〜放置〜退室」のような時間経過が
/// この画面には無いため)。
/// </summary>
/// <remarks>
/// <c>Patch.CardRewardShownPatch</c>(<c>NCardRewardSelectionScreen.ShowScreen</c>のPostfix)、
/// <c>Patch.ChooseACardShownPatch</c>(<c>NChooseACardSelectionScreen.ShowScreen</c>のPostfix)から呼ばれる。
/// 両画面は型として無関係だが処理はほぼ共通なので、<see cref="Handle"/>1箇所にまとめ、画面ごとの違いは
/// <see cref="ICardChoiceScreenAdapter"/>と<see cref="CardRewardDetectorTrigger"/>の値だけで吸収している。
/// </remarks>
internal static class CardRewardChatterHub
{
    /// <summary>
    /// 非強制発言同士の最短間隔(秒)。<c>VakuuSpeaker.MinInterval</c>と同じ値。高速でカード報酬画面を
    /// 開き直した時に、前の吹き出し(後述の理由で画面が閉じても連動して消えない)と新しい吹き出しが
    /// 同じ場所に重なって出るのを防ぐためだけに使う(下記「なぜVakuuSpeakerを使わないか」参照)。
    /// </summary>
    private static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(2.5);

    /// <summary>
    /// カードの並びが表示されてから、実際にカードの座標を読んで吹き出しを出すまでの遅延(秒)。
    /// <c>NCardRewardSelectionScreen</c>/<c>NChooseACardSelectionScreen</c>はどちらもカードの位置を
    /// 0.5秒の<c>Tween</c>でアニメーションさせる(<c>_Ready()</c>内、初期位置からグリッド位置へ)。
    /// このTweenは生成された直後は1フレームも進んでいないため、<c>ShowScreen</c>のHarmonyフックが
    /// 発火した瞬間に<c>NGridCardHolder.Position</c>を読むと、まだアニメーション開始前の初期値
    /// (=カード列のローカル原点。奇数枚では偶然にも中央スロットの最終着地位置と一致する)しか得られない
    /// (2026-09、ユーザー報告: 「初期設定だと全部一枚目から生えているように見える」。狙ったカードとは
    /// 無関係な、ほぼ同じ1点に全カードの吹き出しが集まって見えるのはこれが原因)。そこで判定・発言の可否
    /// 自体は即座に行うが、実際にカード座標を読んで吹き出しを出す(<see cref="EvaluateAndSpeak"/>)のは
    /// Tweenの所要時間(0.5秒)より少し長く待ってから行う。
    /// </summary>
    private const double CardLayoutSettleDelaySeconds = 0.6;

    private static long? _lastSpokenTimestamp;

    /// <summary><see cref="Emit"/>が参照する、今まさに喋ろうとしている提案の観測結果・対象カード。</summary>
    private static CardRewardObserver? _currentObserver;

    private static CardModel? _currentAnchorCard;

    /// <summary>
    /// 既に一度でも意見した画面(カード報酬・「カードを1枚選ぶ」画面のどちらも)の先頭カード
    /// (参照の同一性で比較)。同じ選択肢を何度も開き直しても二度目以降は無言にするために使う
    /// (ユーザー指摘: 同じ選択肢に何度も言及したり、逆に最初は無言だった選択肢に後から言及したりするのは
    /// 不自然)。<see cref="Initialize"/>が購読する<c>RunManager.RoomEntered</c>
    /// (部屋=だいたい階層をまたぐたびに発火)でクリアされる(下記「記録のリセット」参照)。
    /// </summary>
    private static readonly HashSet<CardModel> SeenFirstCards = new(ReferenceEqualityComparer.Instance);

    /// <summary>
    /// <c>RunManager.RoomEntered</c>(部屋の出入りイベント。ショップの<c>ShopChatterHub</c>と同じ購読先)を
    /// 購読し、部屋(だいたい階層)が変わるたびに<see cref="SeenFirstCards"/>と発言間隔をリセットする
    /// (ユーザー指摘: 「同じ報酬」の判定はプロセス起動中ずっと持ち越さなくてよく、階層が変わった・
    /// ランを保存して終了して読み込み直した、といった節目でリセットして構わない。新しいランの最初の
    /// 部屋に入った時にも発火するので、ラン跨ぎのリセットも自然に兼ねる)。
    /// </summary>
    public static void Initialize()
    {
        RunManager.Instance.RoomEntered += () => Guard(ResetPerRoomState);
    }

    private static void ResetPerRoomState()
    {
        SeenFirstCards.Clear();
        _lastSpokenTimestamp = null;
    }

    public static void OnCardRewardShown(NCardRewardSelectionScreen screen, IReadOnlyList<CardCreationResult> options)
    {
        Guard(() =>
        {
            if (options.Count == 0)
            {
                return;
            }

            IReadOnlyList<CardModel> cards = options.Select(o => o.Card).ToList();
            Handle(new CardRewardScreenAdapter(screen), cards, canSkip: true, CardRewardDetectorTrigger.Shown);
        });
    }

    /// <summary>
    /// アタックポーション等の「カードを1枚選ぶ」画面(<c>NChooseACardSelectionScreen</c>)版の入口
    /// (2026-09追加)。処理内容はカード報酬画面と共通の<see cref="Handle"/>にまとめてあり、違いは
    /// <see cref="ICardChoiceScreenAdapter"/>実装と<see cref="CardRewardDetectorTrigger.ChooseACardShown"/>
    /// だけ。
    /// </summary>
    public static void OnChooseACardShown(NChooseACardSelectionScreen screen, IReadOnlyList<CardModel> cards, bool canSkip)
    {
        Guard(() =>
        {
            if (cards.Count == 0)
            {
                return;
            }

            Handle(new ChooseACardScreenAdapter(screen), cards, canSkip, CardRewardDetectorTrigger.ChooseACardShown);
        });
    }

    private static void Handle(ICardChoiceScreenAdapter screen, IReadOnlyList<CardModel> cards, bool canSkip, CardRewardDetectorTrigger trigger)
    {
        // この画面の持ち主(=この画面を見ているプレイヤー)。マルチプレイでは自分自身の分だけ扱う
        // (戦闘中のChatterHub/ShopChatterHubと同じ方針)。
        Player owner = cards[0].Owner;
        if (!LocalContext.IsMe(owner))
        {
            return;
        }

        if (owner.GetRelic<WhisperingEarring>() == null && !ChattyVakuusEarringConfig.AllowWhisperingWithoutEarring)
        {
            return;
        }

        // 同じ選択肢(先頭カードの参照で識別)を開き直した2度目以降は、何も判定せず即終了する
        // (最初に無言だった=このSetに入っていない、という理由で後から言及するのも避けたいので、
        // 判定するかどうかに関わらずここで一度きりの通過にする)。1回の戦闘で複数のカード報酬が
        // 独立して発生する場合(長持ちキャンディ等)は、それぞれ別のCardModelインスタンスを持つので、
        // 参照の同一性で見ているこの判定は自然に「新しい報酬ごとに1回」を実現する。この判定自体は
        // カードの座標を必要としないので即座に行う(下記CardLayoutSettleDelaySecondsのドキュメント参照)。
        if (!SeenFirstCards.Add(cards[0]))
        {
            return;
        }

        if (Engine.GetMainLoop() is SceneTree tree)
        {
            SceneTreeTimer timer = tree.CreateTimer(CardLayoutSettleDelaySeconds, processAlways: false);
            timer.Timeout += () => Guard(() => EvaluateAndSpeak(screen, cards, canSkip, trigger, owner));
        }
        else
        {
            EvaluateAndSpeak(screen, cards, canSkip, trigger, owner);
        }
    }

    private static void EvaluateAndSpeak(
        ICardChoiceScreenAdapter screen, IReadOnlyList<CardModel> cards, bool canSkip, CardRewardDetectorTrigger trigger, Player owner)
    {
        var observer = new CardRewardObserver(owner, screen, cards, canSkip);
        var proposals = new List<CardRewardProposal>();

        foreach (CardRewardDetector detector in CardRewardDetectorRegistry.CreateAll())
        {
            if ((detector.Triggers & trigger) == 0)
            {
                continue;
            }

            try
            {
                CardRewardProposal? proposal = detector.Detect(observer, trigger);
                if (proposal != null)
                {
                    proposal.Utterance.DetectorId = detector.Id;
                    proposals.Add(proposal);
                }
            }
            catch (Exception e)
            {
                MainFile.Logger.Error($"{MainFile.ModId}: {detector.Id}.Detect threw: {e}");
            }
        }

        _currentObserver = observer;

        // 強制発言 → 優先度の高い順に判断する。同順位は毎回同じDetectorが優先されないようシャッフルする。
        foreach (CardRewardProposal proposal in proposals
                     .OrderByDescending(p => p.Utterance.Force)
                     .ThenByDescending(p => p.Utterance.Priority)
                     .ThenBy(_ => ChatterRandom.NextDouble()))
        {
            _currentAnchorCard = proposal.AnchorCard;
            TrySpeak(proposal.Utterance);
        }

        _currentObserver = null;
        _currentAnchorCard = null;
    }

    /// <summary>
    /// <c>VakuuSpeaker</c>は使わず、確率判定と最短間隔だけを自前で行う。
    /// </summary>
    /// <remarks>
    /// <b>なぜ<c>VakuuSpeaker</c>を使わないか(2026-09)</b>: 一度は「高速に開き直した時の吹き出しの重複」対策として
    /// <c>VakuuSpeaker</c>をstaticフィールドとして使い回す形にしたが、これは同時に<c>VakuuSpeaker</c>本来の
    /// 「直前と同じ話題(タグ)を6秒以内に繰り返さない」「同じ台詞を直近6回以内に繰り返さない」という
    /// ルールまでカード報酬画面をまたいで持ち越してしまい、話題キーの選択肢が3つ前後しか無い
    /// (`CARD_REWARD_GENERIC`は3種)このシステムでは、全ての選択肢を使い切った時点で二度と喋れなくなる
    /// 実質的なデッドロックを引き起こしていた(ユーザー報告: 起動後、最初の1回しか喋らない)。
    /// 戦闘・ショップは話題キーの種類も台詞の変種も豊富なので起きにくいが、カード報酬はそうではない。
    /// そこで<c>VakuuSpeaker.PassesEtiquette</c>のうち、この画面で本当に必要な「確率判定」と
    /// 「直近の発言からの最短間隔(重複表示防止)」の2つだけを自前で行う形に切り替えた。
    /// </remarks>
    private static void TrySpeak(Utterance utterance)
    {
        if (!utterance.Force)
        {
            if (_lastSpokenTimestamp is { } last && Stopwatch.GetElapsedTime(last) < MinInterval)
            {
                return;
            }

            if (ChatterRandom.NextDouble() >= utterance.Probability)
            {
                return;
            }
        }

        if (Emit(utterance))
        {
            _lastSpokenTimestamp = Stopwatch.GetTimestamp();
        }
    }

    /// <summary>
    /// 実際の表示処理。<see cref="_currentObserver"/>/<see cref="_currentAnchorCard"/>
    /// (直前に<see cref="Handle"/>がセットした値)を見て<see cref="CardRewardVakuuBubble.TryShow"/>に渡す。
    /// </summary>
    private static bool Emit(Utterance utterance) =>
        _currentObserver != null
        && CardRewardVakuuBubble.TryShow(_currentObserver, _currentAnchorCard, utterance);

    private static void Guard(Action handler)
    {
        try
        {
            handler();
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"{MainFile.ModId}: unhandled exception in card reward chatter hub: {e}");
        }
    }
}
