using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;

/// <summary>
/// ヴァクーの「口」。Detectorから<see cref="Utterance"/>を受け取り、喋るかどうかを判断して、喋るなら吹き出しを出す。
/// </summary>
/// <remarks>
/// 喋る/黙るの判断だけを担当する。何を喋るか(台詞選択)はDetector、何が起きたか(観察)はObserverの仕事。
/// 判断基準は、前回の発言からの経過時間(短い間隔で喋るとうるさい)、直前と同じ話題(タグ)・同じ台詞の抑制、
/// 発言確率(毎回同じことを言うと不自然)。<see cref="Utterance.Force"/>はこれら全てを無視する。
/// </remarks>
public sealed class VakuuSpeaker
{
    /// <summary>非強制の発言同士の最短間隔。</summary>
    private static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(2.5);

    /// <summary>直前の発言と同じタグを持つ発言を弾く期間。これより時間が経っていれば同じ話題でも許す。</summary>
    private static readonly TimeSpan SameTagWindow = TimeSpan.FromSeconds(6);

    /// <summary>同じ台詞の再発言を避けるために覚えておく、直近の発言数。</summary>
    private const int RepeatMemory = 6;

    private readonly Creature _voice;

    private readonly List<SpokenRecord> _recent = new();

    /// <summary>ヴァクーが喋る吹き出しの出所となるクリーチャー(=囁きのイヤリングの持ち主)。</summary>
    public VakuuSpeaker(Creature voice)
    {
        _voice = voice;
    }

    /// <summary>
    /// このクラス自身が<c>TalkCmd.Play</c>を呼んでいる最中だけtrue。バニラのイヤリング台詞を抑制するパッチが、
    /// 自分たちの発言まで誤って抑制しないための目印。
    /// </summary>
    internal static bool IsEmitting { get; private set; }

    /// <returns>実際に喋ったらtrue。</returns>
    public bool TrySpeak(Utterance utterance)
    {
        if (!utterance.Force && !PassesEtiquette(utterance, out string reason))
        {
            MainFile.Logger.Debug($"{MainFile.ModId}: skipped {utterance.Line.LocEntryKey} ({utterance.DetectorId}): {reason}");
            return false;
        }

        if (!Emit(utterance))
        {
            return false;
        }

        _recent.Add(new SpokenRecord(utterance.Line.LocEntryKey, utterance.Tags, Stopwatch.GetTimestamp()));
        if (_recent.Count > RepeatMemory)
        {
            _recent.RemoveAt(0);
        }

        MainFile.Logger.Info($"{MainFile.ModId}: spoke {utterance.Line.LocEntryKey} ({utterance.DetectorId}{(utterance.Force ? ", forced" : "")})");
        return true;
    }

    private bool PassesEtiquette(Utterance utterance, out string reason)
    {
        if (_recent.Count > 0)
        {
            SpokenRecord last = _recent[^1];
            TimeSpan sinceLast = Stopwatch.GetElapsedTime(last.Timestamp);

            if (sinceLast < MinInterval)
            {
                reason = $"spoke {sinceLast.TotalSeconds:F1}s ago";
                return false;
            }

            if (sinceLast < SameTagWindow && utterance.Tags.Any(tag => last.Tags.Contains(tag)))
            {
                reason = "same tag as the previous utterance";
                return false;
            }

            if (_recent.Any(r => r.Key == utterance.Line.LocEntryKey))
            {
                reason = "same line was spoken recently";
                return false;
            }
        }

        if (ChatterRandom.NextDouble() >= utterance.Probability)
        {
            reason = $"probability roll failed ({utterance.Probability:P0})";
            return false;
        }

        reason = "";
        return true;
    }

    private bool Emit(Utterance utterance)
    {
        IsEmitting = true;
        try
        {
            // 色は本家の囁きのイヤリングに合わせる。台詞の装飾([red]/[shake]等)はローカライゼーション側にある。
            return TalkCmd.Play(utterance.Line, _voice, VfxColor.Purple) != null;
        }
        finally
        {
            IsEmitting = false;
        }
    }

    private readonly record struct SpokenRecord(string Key, IReadOnlyList<UtteranceTag> Tags, long Timestamp);
}
