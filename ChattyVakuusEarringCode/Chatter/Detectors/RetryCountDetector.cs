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
/// <remarks>
/// <para>
/// 本家の乱数は全て<c>RunState.Rng.StringSeed</c>で決定論的に固定されるので、「同じラン(同じシード)・
/// 同じ階(<c>RunState.TotalFloor</c>)の戦闘に何度入り直したか」を数えれば、セーブスカムでのやり直し回数がわかる。
/// </para>
/// <para>
/// <b>記録するタイミング</b>: ただし「入り直した回数」を戦闘開始(<see cref="DetectorTrigger.PlayerTurnStarted"/>)の
/// たびに無条件で加算すると、一旦ゲームを中断したいだけで戦闘開始直後にゲームを終えた人まで
/// 「セーブスカムした」と誤って数えてしまう(<see cref="SaveScummingDetector"/>が
/// <c>LastManualCardPlay == null</c>の時に発言しないのと同じ懸念)。そこで、記録の確定
/// (<see cref="ConfirmAttemptOnce"/>)は「この戦闘で1枚以上手動でカードをプレイした最初の瞬間」
/// (<see cref="DetectorTrigger.CardPlayed"/>)まで遅らせる。戦闘開始時に喋るかどうかの判定
/// (<see cref="PeekAttemptNumber"/>)は、既に確定済みの回数を読むだけで、この時点では加算しない
/// (今回の入室がまだ確定していない=何もせず終える可能性があるため)。
/// </para>
/// <para>
/// この記録を<c>RunState</c>(本家のセーブデータ)に持たせると、ランの保存・同期の対象になってしまい
/// 純粋なクライアントサイドmodではなくなる。そこで本家の<c>SaveManager.GetProfileScopedPath</c>
/// (プロファイル単位のディレクトリ配下、`saves/`とは独立。sts2_dev_knowledge/topics/game-fundamentals.mdの
/// 「プロファイル単位のセーブデータ」参照)にmod専用のJSONファイルを持ち、そこに記録する。戻り値は
/// <c>user://</c>スキームのGodot仮想パスなので、生の<c>System.IO.File</c>ではなく<c>Godot.FileAccess</c>で読み書きする
/// (`System.IO.FileAccess`と同名なので<c>Godot.FileAccess</c>と完全修飾する必要がある)。
/// </para>
/// <para>
/// 読み書きに失敗した場合(初回起動でファイルが無い等)は0件として扱い、例外は投げない
/// (この機能が使えないだけで、他のDetectorやセッション自体には影響させたくないため)。
/// </para>
/// <para>
/// <b>記録の掃除</b>: このファイルは戦闘開始のたびに増える一方なので、不要になった分を2箇所で削る。
/// (1) ランが完了(勝利・敗北いずれか)した瞬間、<see cref="Patch.RunEndedRetryCleanupPatch"/>
/// (<c>RunManager.OnEnded</c>のPostfix)が<see cref="ForgetRun"/>を呼び、そのランのシードに紐づく分を消す。
/// 「あきらめる」操作はこの入口を通らないが、セーブ自体が消えるので(2)の起動時スイープが拾う。
/// (2) このDetectorが本セッションで最初に呼ばれた時(<see cref="_didStartupSweep"/>)、中断セーブが
/// 存在しない、または存在するシングルプレイのセーブと一致しない記録を<see cref="SweepStaleRuns"/>で消す
/// (クラッシュ・強制終了・セーブの手動削除等、(1)を通らずにランが失われたケースの取りこぼしを拾う)。
/// マルチプレイの中断セーブ(<c>current_run_mp.save</c>)はシードを安全に読み取る公開APIが無いため、
/// 存在する場合はこの回のスイープを見送る(=消しすぎない安全側に倒す)。
/// 起動直後ではなく「このDetectorが最初に呼ばれた時」にしているのは、<c>SaveManager.Instance</c>の
/// プロファイルがmod初期化のタイミングではまだ準備できていない場合があるため
/// (sts2_dev_knowledge/topics/game-fundamentals.mdの「プロファイル切り替えの検知」参照)。戦闘に
/// 入れている時点でプロファイルは必ず確定しているので、ここまで遅延させれば安全に判定できる。
/// </para>
/// </remarks>
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
