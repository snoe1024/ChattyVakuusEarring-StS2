using System;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;

/// <summary>
/// Detectorが判定を行うタイミング。Detectorは自分が関心のあるタイミングだけを<c>Triggers</c>で宣言し、
/// そのタイミングで<see cref="PlayObserver"/>が「今のプレイヤーの状況」を提示する。
/// </summary>
/// <remarks>
/// 新しいタイミングが欲しくなったら、値を足して<c>ChatterHub</c>側でゲーム本体のイベントに結びつける。
/// </remarks>
[Flags]
public enum DetectorTrigger
{
    None = 0,

    /// <summary>
    /// プレイヤーのターンが始まり、操作可能になった時(最初のターンでは、ヴァクーの代打ちも済んだ後)。
    /// 戦闘がそのターンの開始前に終わっていた場合は発火しない。
    /// </summary>
    PlayerTurnStarted = 1 << 0,

    /// <summary>プレイヤーがカードを手動でプレイし終えた時(<c>PlayObserver.LastManualCardPlay</c>に内容が入る)。</summary>
    CardPlayed = 1 << 1,

    /// <summary>
    /// プレイヤーがターン終了ボタンを押した時。ターン終了時効果(オリハルコンの自動ブロック等)が
    /// 走る前なので、「終了時にどんな状態でターンを終えようとしたか」を判定できる。
    /// </summary>
    PlayerEndedTurn = 1 << 2,

    /// <summary>
    /// 最初のターンのヴァクーの代打ちが(1枚以上)終わり、バニラなら締めの一言
    /// (「なかなか上手いものでしょう？」等)を言っていた時点。
    /// 代打ちで戦闘に勝ってしまった場合でも発火する(<see cref="PlayerTurnStarted"/>は発火しない)。
    /// </summary>
    VakuuFinishedPlaying = 1 << 3,

    /// <summary>
    /// 最初のターンで、手札が配られ終わり、ヴァクーの代打ちが始まる直前(<c>PlayerTurnPhase.AutoPrePlay</c>に入った時点)。
    /// 他の自動プレイ効果が手札を動かす前でもあるので、配られた直後の「初期手札」をそのまま見られる。
    /// 2ターン目以降は発火しない(その手札は<see cref="PlayerTurnStarted"/>で見る)。
    /// </summary>
    VakuuTurnStarted = 1 << 4,

    /// <summary>
    /// 敵が倒された時(誰がどうやって倒したかは問わない)。倒された敵は<c>PlayObserver.LastKilledEnemy</c>に入る。
    /// 倒したのが誰かは<c>PlayObserver.LastKiller</c>で、ヴァクーの代打ち中かどうかは<c>PlayObserver.IsInVakuuTurn</c>で見分ける。
    /// </summary>
    EnemyKilled = 1 << 5,

    /// <summary>
    /// 持ち主がダメージを受けた時(ブロックで全て防いだ場合も含む。誰から受けたかは問わない)。
    /// 内容は<c>PlayObserver.LastDamageTaken</c>に入る。
    /// </summary>
    DamageTaken = 1 << 6,

    /// <summary>
    /// 約1秒おきの時間経過(時間ベースのトリガー)。持ち主が操作できる間(自分のターンのプレイ中で、まだターン終了を
    /// 押しておらず、操作不能でもない間)だけ発火する。放置の判定など「何も起きていないこと」を見たい時に使う。
    /// 操作がなかった秒数は<c>PlayObserver.IdleSeconds</c>で分かる。
    /// </summary>
    Tick = 1 << 7,

    /// <summary>
    /// ターン中に自動でブロックを与える特定のレリック(音叉・扇子・銀河の塵・波紋の鉢・マントの留め具)が
    /// 発動した瞬間。<c>PlayObserver.LastActivatedBlockRelic</c>に発動したレリックが入る。
    /// <see cref="Patch.BlockRelicFlashPatch"/>がHarmonyで捉えて<c>ChatterHub.OnBlockRelicActivated</c>経由で届ける
    /// (通常の<c>ChatterHub</c>のイベント購読では、どのレリックが発動したかまでは分からないため)。
    /// </summary>
    BlockRelicActivated = 1 << 8,

    /// <summary>
    /// プレイヤーのターンが本当に終わった時(<see cref="PlayerEndedTurn"/>とは別物)。ターン終了時効果
    /// (オリハルコンの自動ブロック等)が全て終わり、これから敵のターンに切り替わる直前
    /// (<c>CombatManager.AboutToSwitchToEnemyTurn</c>)。ターン終了時効果で敵が倒れる・ブロックが増える等の
    /// 結果を踏まえて「終わってみてどうだったか」を判定したい時はこちらを使う。
    /// </summary>
    TurnEnded = 1 << 9,

    /// <summary>
    /// 持ち主がポーションを使用した時。使用したポーションは<c>PlayObserver.LastUsedPotion</c>、
    /// 対象(味方・敵どちらもありうる。対象を取らないポーションはnull)は<c>PlayObserver.LastUsedPotionTarget</c>に入る。
    /// </summary>
    PotionUsed = 1 << 10,

    /// <summary>
    /// 持ち主がターン中にカードを引いた時(最初のターン開始時の配札は含まない。<c>CardDrawnEntry.FromHandDraw</c>で
    /// 区別している)。引かれたカードは<c>PlayObserver.LastDrawnCard</c>に入る。
    /// </summary>
    CardDrawn = 1 << 11,

    /// <summary>
    ///
    /// </summary>
    FriendCardPlayed = 1 << 12,

    /// <summary>
    /// 戦闘中にプレイヤーがメインメニューへ戻る操作(ポーズメニューの「あきらめる」→確認ポップアップの「はい」、
    /// または「保存して終了」)を確定させた瞬間。<see cref="Patch.ReturnToMainMenuSpeechPatch"/>が
    /// <c>NGame.ReturnToMainMenu()</c>をHarmonyで捉えて<c>ChatterHub.OnReturnedToMainMenu</c>経由で届ける。
    /// この本家メソッドは最初の行が<c>await Transition.FadeOut()</c>(暗転)なので、Prefixで捕まえた時点では
    /// 暗転すら始まっておらず、戦闘画面がそのまま表示されている(通常通り吹き出しを出せる)。
    /// <c>RunManager.Abandon()</c>(「あきらめる」)自体をパッチしなかったのは、あちらは片付けを
    /// <c>TaskHelper.RunSafely</c>で非同期にキックするだけの薄いラッパーで、実際の暗転(<c>ReturnToMainMenu</c>)は
    /// 「保存して終了」を含む全てのメインメニュー行きの経路が最終的に通る共通の合流点であるため
    /// (最初の実装でここを間違えて<c>Abandon</c>側を直接パッチし、「保存して終了」経由(=本来のセーブスカム手順。
    /// あきらめるはセーブを消すのでスカムに使えない)で発火しない不具合になった)。
    /// 本家の乱数は全て決定論的な擬似乱数なので、この操作はいわゆる「セーブスカム」
    /// (不利な展開になった時に保存だけしてメインメニューへ戻り、後で読み込み直して乱数を引き直す行為)に使われうる。
    /// </summary>
    ReturnedToMainMenu = 1 << 13,
}
