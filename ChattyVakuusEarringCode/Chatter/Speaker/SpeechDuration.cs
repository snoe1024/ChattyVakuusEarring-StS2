using System;
using System.Text.RegularExpressions;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Config;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;

/// <summary>
/// 吹き出しの表示時間の計算。本家<c>TalkCmd.Play</c>の計算式(文字数×秒/文字、ファストモードなら0.1秒/文字・
/// 通常なら0.12秒/文字)をベースに、言語圏による自動倍率(<see cref="_speechMultiplier"/>)と
/// ユーザー設定の倍率(<see cref="ChattyVakuusEarringConfig.SpeechDurationMultiplier"/>)を両方掛けたもの。
/// 戦闘中(<see cref="VakuuSpeaker"/>)・ショップ(<c>ShopChatter.ShopVakuuBubble</c>)・
/// カード報酬(<c>CardRewardChatter.CardRewardVakuuBubble</c>)の3箇所全てで共有する。
/// </summary>
/// <remarks>
/// 2026-09追加: 友人からのフィードバック「台詞の分量に対して表示時間が短い」を受けて新設。
/// これ以前は3箇所それぞれが同じ計算式(<c>GetRawCharCount</c>込み)を個別に複製していた
/// (戦闘中は本家<c>TalkCmd.Play</c>に直接committedしていたためファストモード考慮込み、ショップ・カード報酬は
/// 独自複製でファストモード未考慮という差異があった)。倍率を一箇所に集約する良い機会でもあったため、
/// ここに一本化しファストモード考慮も3箇所共通にした。
/// </remarks>
/// <remarks>
/// 2026-09追加(言語別倍率): 当初は<c>ChattyVakuusEarringConfig.SpeechDurationMultiplier</c>自体を
/// 言語圏に応じて初期化しようとしたが、それには<c>[ModInitializer]</c>実行時点で<c>LocManager.Instance</c>を
/// 読む必要があり(番兵値経由でもRunStarted経由でも)、この時点ではまだ<c>LocManager.Instance</c>が
/// 利用不可でクラッシュする(sts2_dev_knowledge/topics/modconfig-and-localization.md)。そのため設定値とは
/// 独立した、セッションごとに再計算する<see cref="_speechMultiplier"/>として実装し直した。
/// <see cref="Initialize"/>で<c>RunManager.Instance.RunStarted</c>(本家の「ラン開始」イベント。デイリー/
/// カスタム等のゲームモードもすべて<c>RunState.GameMode</c>として同じ<c>RunManager.Launch()</c>を通るので、
/// ゲームモードを問わず一度は必ず呼ばれる)まで初期化を遅延させる。
/// </remarks>
internal static class SpeechDuration
{
    private const double SecondsPerChar = 0.12;

    private const double FastModeSecondsPerChar = 0.1;

    private const double CjkSpeechMultiplier = 1.75;
    private const double OtherSpeechMultiplier = 1.0;

    private static double _speechMultiplier = 1.0;

    /// <summary>
    /// 文字量に対して読む分量が多い言語コードを定義しちゃう
    /// </summary>
    private static readonly HashSet<string> CjkLanguageCodes =
        new(StringComparer.OrdinalIgnoreCase) { "JPN", "ZHS", "ZHT", "KOR" };

    /// <summary>
    /// <c>MainFile.Initialize()</c>から呼ぶ。イベント購読自体はMod-Init時点でも安全(<c>RunManager.Instance</c>は
    /// 触れる)。実際に<c>LocManager.Instance</c>を読む<see cref="InitializeSpeechMultiplier"/>は
    /// ラン開始まで遅延される。
    /// </summary>
    public static void Initialize()
    {
        RunManager.Instance.RunStarted += _ => InitializeSpeechMultiplier();
    }

    private static void InitializeSpeechMultiplier()
    {
        _speechMultiplier = CjkLanguageCodes.Contains(LocManager.Instance.Language)
            ? CjkSpeechMultiplier : OtherSpeechMultiplier;
    }

    /// <summary>
    /// 話題ごとの最短表示時間・秒/文字を掛けた基準時間を求め、ユーザー設定の倍率を掛ける。
    /// </summary>
    /// <param name="formattedText">BBCode込みの、実際に表示するテキスト(<c>LocString.GetFormattedText()</c>)。</param>
    /// <param name="minSeconds">
    /// 呼び出し元ごとの最短表示時間(本家<c>TalkCmd.Play</c>は0.5秒、ショップ・カード報酬は1.5秒。
    /// 倍率を掛ける前の下限として使う)。
    /// </param>
    public static double Compute(string formattedText, double minSeconds)
    {
        double secondsPerChar = (SaveManager.Instance.PrefsSave.FastMode == FastModeType.Fast
            ? FastModeSecondsPerChar : SecondsPerChar) * _speechMultiplier;
        double baseSeconds = Math.Max(minSeconds, GetRawCharCount(formattedText) * secondsPerChar);

        // 通常はChattyVakuusEarringConfig.Initialize()が初回ラン開始時に番兵値(0)から実際の値へ
        // 置き換えているはずだが、万一まだ番兵値のままでもゼロ秒(=一瞬で消える)にはしない安全策。
        float multiplier = ChattyVakuusEarringConfig.SpeechDurationMultiplier;
        return baseSeconds * (multiplier > 0f ? multiplier : 1f);
    }

    /// <summary>
    /// <c>TalkCmd.GetRawCharCount</c>と同じ計算(BBCode・改行・空白を除いた文字数)。あちらはprivateなので複製。
    /// </summary>
    public static int GetRawCharCount(string bbcodeText)
    {
        string text = Regex.Replace(bbcodeText, "\\[/?[^\\]]+\\]", "");
        return text.Replace("\n", "").Replace("\r", "").Replace(" ", "").Length;
    }
}
