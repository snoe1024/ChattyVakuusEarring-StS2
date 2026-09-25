using System;
using System.Collections.Generic;
using System.Linq;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.ShopChatter;

/// <summary>
/// このアセンブリ内の<see cref="ShopDetector"/>継承クラスを自動で見つけてインスタンス化する。
/// <c>Chatter.Detectors.DetectorRegistry</c>のショップ版。
/// </summary>
internal static class ShopDetectorRegistry
{
    private static Type[]? _detectorTypes;

    /// <summary>全Detectorの新規インスタンスを作る(ショップに入室するたびに1回呼ばれる)。</summary>
    public static List<ShopDetector> CreateAll()
    {
        _detectorTypes ??= typeof(ShopDetector).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && t.IsSubclassOf(typeof(ShopDetector))
                        && t.GetConstructor(Type.EmptyTypes) != null)
            .OrderBy(t => t.FullName, StringComparer.Ordinal)
            .ToArray();

        var detectors = new List<ShopDetector>(_detectorTypes.Length);
        foreach (Type type in _detectorTypes)
        {
            try
            {
                detectors.Add((ShopDetector)Activator.CreateInstance(type)!);
            }
            catch (Exception e)
            {
                MainFile.Logger.Error($"{MainFile.ModId}: failed to create shop detector {type.Name}: {e}");
            }
        }

        return detectors;
    }
}
