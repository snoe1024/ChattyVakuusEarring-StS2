using MegaCrit.Sts2.Core.Localization;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter;

/// <summary>
/// バニラの囁きのイヤリングが元々持っている台詞(<c>relics</c>テーブルの<c>WHISPERING_EARRING.*</c>)。
/// </summary>
/// <remarks>
/// 台詞の本体はこのmod側のローカライゼーションに移してあるが、mod側の台詞が1つも見つからない場合
/// (未対応の言語など)に、元の挙動を失わないためのフォールバックとして使う。
/// </remarks>
internal static class VanillaEarringLines
{
    private const string Table = "relics";

    private const string ApprovalKey = "WHISPERING_EARRING.approval";

    private const string WarningKey = "WHISPERING_EARRING.warning";

    /// <summary>バニラのイヤリングが、代打ちの締めに言おうとした台詞か。</summary>
    public static bool IsVanillaClosingLine(LocString line)
    {
        return line.LocTable == Table && (line.LocEntryKey is ApprovalKey or WarningKey);
    }

    /// <summary>「なかなか上手いものでしょう？」</summary>
    public static LocString Approval() => new(Table, ApprovalKey);

    /// <summary>「もういいでしょう。」(13枚の上限まで代打ちした時)</summary>
    public static LocString Warning() => new(Table, WarningKey);
}
