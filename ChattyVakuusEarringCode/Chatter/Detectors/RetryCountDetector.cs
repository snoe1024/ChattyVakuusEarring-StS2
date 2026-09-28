using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Observer;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Saves;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors;

/// <summary>
/// 同じラン・同じ階の戦闘に何度目かで挑んでいる(=セーブスカムでやり直している)時、戦闘開始時に
/// 他の全ての一言より優先して言う(三層のヴァクー本人との邂逅セリフを模倣)。
/// </summary>
public sealed class RetryCountDetector : PlayDetector
{
    private const string Topic = "RETRY_COUNT";
    private const string TopicWithCount = "RETRY_COUNT_IS_";
    private const int TopicCountMax = 4;
    
    private const double TopicCountUseProbability = 0.75;

    private const int RetryPriority = 20;

    private const string DataFileName = "chattyvakuusearring_retry_counts.json";

    /// <summary>プロセス起動後、起動時スイープをまだ行っていないか(1プロセスにつき1度だけ行う)。</summary>
    private static bool _didStartupSweep;

    /// <summary>この戦闘(このDetectorインスタンス=この戦闘限り)で、記録の確定を既に済ませたか。</summary>
    private bool _confirmedThisAttempt;

    public override DetectorTrigger Triggers => DetectorTrigger.PlayerTurnStarted | DetectorTrigger.CardPlayed;

    public override Utterance? Detect(PlayObserver observer, DetectorTrigger trigger)
    {
        if (trigger == DetectorTrigger.CardPlayed)
        {
            ConfirmAttemptOnce(observer);
            return null;
        }

        if (!observer.IsFirstTurn)
        {
            return null;
        }

        int attempt = PeekAttemptNumber(observer);
        if (attempt < 2)
        {
            return null;
        }

        // 特定の回数以下なら専用のセリフがあるので、一定の確率でそちらを使う。定義に抜けがあってもfallbackする。
        if (attempt <= TopicCountMax && Rng.Chaotic.NextDouble() < TopicCountUseProbability)
        {
            LocString? line = SpeechTable.Pick(TopicWithCount + attempt.ToString(), fallback: SpeechTable.Pick(Topic));
            if (line == null)
            {
                return null;
            }

            return new Utterance
            {
                Line = line,
                Force = true,
                Priority = RetryPriority,
            };
        }
        else
        {
            LocString? line = SpeechTable.Pick(Topic);
            if (line == null)
            {
                return null;
            }

            line.Add("Count", attempt);
            return new Utterance
            {
                Line = line,
                Force = true,
                Priority = RetryPriority,
            };
        }
    }

    /// <summary>
    /// この戦闘(ラン×階)について、既に確定済みの回数+1(=もし今回も確定すれば何度目になるか)を返す。
    /// 読むだけで、記録は更新しない(更新は<see cref="ConfirmAttemptOnce"/>が行う)。
    /// </summary>
    private static int PeekAttemptNumber(PlayObserver observer)
    {
        string path = SaveManager.Instance.GetProfileScopedPath(DataFileName);
        Dictionary<string, int> counts = LoadCountsWithStartupSweep(path);

        counts.TryGetValue(GetKey(observer), out int confirmed);
        return confirmed + 1;
    }

    /// <summary>
    /// この戦闘で1枚以上手動でカードをプレイした、最初の瞬間に1度だけ記録を確定(+1して保存)させる。
    /// 2回目以降の手動プレイでは何もしない(<see cref="_confirmedThisAttempt"/>で1度きりに絞る)。
    /// </summary>
    private void ConfirmAttemptOnce(PlayObserver observer)
    {
        if (_confirmedThisAttempt)
        {
            return;
        }

        _confirmedThisAttempt = true;

        string path = SaveManager.Instance.GetProfileScopedPath(DataFileName);
        Dictionary<string, int> counts = LoadCountsWithStartupSweep(path);

        string key = GetKey(observer);
        counts.TryGetValue(key, out int previous);
        counts[key] = previous + 1;

        SaveCounts(path, counts);
    }

    private static string GetKey(PlayObserver observer)
    {
        var runState = observer.Owner.RunState;
        return $"{runState.Rng.StringSeed}:{runState.TotalFloor}";
    }

    /// <summary>読み込みに加え、プロセスで最初の呼び出しの時だけ<see cref="SweepStaleRuns"/>を行い、消した分を保存する。</summary>
    private static Dictionary<string, int> LoadCountsWithStartupSweep(string path)
    {
        Dictionary<string, int> counts = LoadCounts(path);

        if (!_didStartupSweep)
        {
            _didStartupSweep = true;
            SweepStaleRuns(counts);
            SaveCounts(path, counts);
        }

        return counts;
    }

    /// <summary>
    /// 中断セーブが無い、またはシングルプレイの中断セーブのシードと一致しない記録を取り除く。
    /// マルチプレイの中断セーブがある場合は、シードを安全に読み取る手段が無いので何もしない。
    /// </summary>
    private static void SweepStaleRuns(Dictionary<string, int> counts)
    {
        bool hasSinglePlayerSave = SaveManager.Instance.HasRunSave;
        bool hasMultiplayerSave = SaveManager.Instance.HasMultiplayerRunSave;

        if (!hasSinglePlayerSave && !hasMultiplayerSave)
        {
            counts.Clear();
            return;
        }

        if (hasMultiplayerSave)
        {
            return;
        }

        var result = SaveManager.Instance.LoadRunSave();
        if (result is not { Success: true, SaveData.SerializableRng.Seed: { } seed })
        {
            return;
        }

        string prefix = seed + ":";
        foreach (string key in counts.Keys.Where(k => !k.StartsWith(prefix)).ToList())
        {
            counts.Remove(key);
        }
    }

    /// <summary>ランが完了した(勝利・敗北いずれか)時に、そのシードに紐づく記録を全て消す。</summary>
    public static void ForgetRun(string? seed)
    {
        if (string.IsNullOrEmpty(seed))
        {
            return;
        }

        string path = SaveManager.Instance.GetProfileScopedPath(DataFileName);
        Dictionary<string, int> counts = LoadCounts(path);

        string prefix = seed + ":";
        foreach (string key in counts.Keys.Where(k => k.StartsWith(prefix)).ToList())
        {
            counts.Remove(key);
        }

        SaveCounts(path, counts);
    }

    private static Dictionary<string, int> LoadCounts(string path)
    {
        if (!Godot.FileAccess.FileExists(path))
        {
            return new Dictionary<string, int>();
        }

        using Godot.FileAccess? file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
        if (file == null)
        {
            return new Dictionary<string, int>();
        }

        return JsonSerializer.Deserialize<Dictionary<string, int>>(file.GetAsText()) ?? new Dictionary<string, int>();
    }

    private static void SaveCounts(string path, Dictionary<string, int> counts)
    {
        string dir = path.GetBaseDir();
        if (!DirAccess.DirExistsAbsolute(dir))
        {
            DirAccess.MakeDirRecursiveAbsolute(dir);
        }

        using Godot.FileAccess? file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write);
        file?.StoreString(JsonSerializer.Serialize(counts));
    }
}
