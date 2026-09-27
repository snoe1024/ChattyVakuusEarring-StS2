using System;
using System.Collections.Generic;
using System.Linq;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.CardRewardChatter;

/// <summary>
/// このアセンブリ内の<see cref="CardRewardDetector"/>継承クラスを自動で見つけてインスタンス化する。
/// <c>Chatter.Detectors.DetectorRegistry</c>のカード報酬画面版。
/// </summary>
internal static class CardRewardDetectorRegistry
{
    private static Type[]? _detectorTypes;

    /// <summary>全Detectorの新規インスタンスを作る(カード報酬画面が表示されるたびに1回呼ばれる)。</summary>
    public static List<CardRewardDetector> CreateAll()
    {
        _detectorTypes ??= typeof(CardRewardDetector).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && t.IsSubclassOf(typeof(CardRewardDetector))
                        && t.GetConstructor(Type.EmptyTypes) != null)
            .OrderBy(t => t.FullName, StringComparer.Ordinal)
            .ToArray();

        var detectors = new List<CardRewardDetector>(_detectorTypes.Length);
        foreach (Type type in _detectorTypes)
        {
            try
            {
                detectors.Add((CardRewardDetector)Activator.CreateInstance(type)!);
            }
            catch (Exception e)
            {
                MainFile.Logger.Error($"{MainFile.ModId}: failed to create card reward detector {type.Name}: {e}");
            }
        }

        return detectors;
    }
}
