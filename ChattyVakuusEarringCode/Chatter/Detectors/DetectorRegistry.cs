using System;
using System.Collections.Generic;
using System.Linq;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Detectors;

/// <summary>
/// このアセンブリ内の<see cref="PlayDetector"/>継承クラスを自動で見つけてインスタンス化する。
/// Detectorを増やす時に、登録処理を書かなくて済むようにするためのもの。
/// </summary>
internal static class DetectorRegistry
{
    private static Type[]? _detectorTypes;

    /// <summary>全Detectorの新規インスタンスを作る(戦闘・持ち主ごとに1回ずつ呼ばれる)。</summary>
    public static List<PlayDetector> CreateAll()
    {
        _detectorTypes ??= typeof(PlayDetector).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && t.IsSubclassOf(typeof(PlayDetector))
                        && t.GetConstructor(Type.EmptyTypes) != null)
            .OrderBy(t => t.FullName, StringComparer.Ordinal)
            .ToArray();

        var detectors = new List<PlayDetector>(_detectorTypes.Length);
        foreach (Type type in _detectorTypes)
        {
            try
            {
                detectors.Add((PlayDetector)Activator.CreateInstance(type)!);
            }
            catch (Exception e)
            {
                MainFile.Logger.Error($"{MainFile.ModId}: failed to create detector {type.Name}: {e}");
            }
        }

        return detectors;
    }
}
