using System.Collections.Generic;
using MegaCrit.Sts2.Core.Localization;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;

/// <summary>
/// 台詞のローカライゼーションキーを引く、Detector向けのヘルパー。
/// </summary>
/// <remarks>
/// <para>
/// キーの形式は <c>{KeyPrefix}.{topic}[.{kind}][.{index}]</c>。<c>index</c>が付いた変種は0から順に
/// 数字を振り、存在するものの中からランダムに1つ選ぶ(途中が欠番でも拾う。追加・削除でキーを詰め直さなくてよいように)。
/// <c>index</c>無しの単体キーも1つの変種として扱う。例:
/// <code>
/// CHATTY-VAKUU-EARRING.FIRST_TURN_END.approval.0
/// CHATTY-VAKUU-EARRING.FIRST_TURN_END.approval.1
/// CHATTY-VAKUU-EARRING.ORICHALCUM_COMPLAINT.0
/// </code>
/// </para>
/// <para>
/// テーブルは<c>relics</c>固定。modが独自ローカライズを追加できるのは本家に実在するテーブル名のみで、
/// 独自ファイル名は黙って無視されるため(sts2_dev_knowledge/topics/modconfig-and-localization.md)。
/// </para>
/// </remarks>
public static class SpeechTable
{
    public const string Table = "relics";

    public const string KeyPrefix = "CHATTY-VAKUU-EARRING";

    /// <summary>1つの話題あたり走査する変種の上限(0〜この値-1)。</summary>
    private const int MaxVariants = 32;

    /// <summary>
    /// <paramref name="topic"/>(と任意の<paramref name="kind"/>)に対応する台詞の変種から、ランダムに1つ選ぶ。
    /// </summary>
    /// <param name="topic">話題キー(例: <c>ORICHALCUM_COMPLAINT</c>)。</param>
    /// <param name="kind">話題内の種別(例: <c>approval</c>)。使わなければnull。</param>
    /// <param name="fallback">変種が1つも見つからなかった時に返す台詞(バニラの既存キーなど)。</param>
    /// <returns>選ばれた台詞。変種が無く<paramref name="fallback"/>も無ければnull(=Detectorは喋らないでよい)。</returns>
    public static LocString? Pick(string topic, string? kind = null, LocString? fallback = null)
    {
        string baseKey = kind == null ? $"{KeyPrefix}.{topic}" : $"{KeyPrefix}.{topic}.{kind}";

        var keys = new List<string>();
        if (LocString.Exists(Table, baseKey))
        {
            keys.Add(baseKey);
        }

        for (int i = 0; i < MaxVariants; i++)
        {
            string variantKey = $"{baseKey}.{i}";
            if (LocString.Exists(Table, variantKey))
            {
                keys.Add(variantKey);
            }
        }

        if (keys.Count == 0)
        {
            return fallback;
        }

        return new LocString(Table, keys[ChatterRandom.Next(keys.Count)]);
    }
}
