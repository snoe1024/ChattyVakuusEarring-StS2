using System;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;

/// <summary>
/// このmodの発言まわりの抽選(台詞の選択・発言確率)専用の乱数源。
/// </summary>
/// <remarks>
/// ゲーム本体の<c>RunState.Rng</c>系は決定論的で、マルチプレイでは全クライアントが同じ順序で消費することで
/// 同期を保っている。ここから消費すると、発言のたびにシード進行が食い違ってデスシンク(ズレ)を起こしうる。
/// 発言は見た目だけの演出でゲーム状態に影響しないため、同期と無関係な独立した乱数を使う。
/// </remarks>
internal static class ChatterRandom
{
    private static readonly Random Shared = new();

    public static int Next(int maxExclusive) => Shared.Next(maxExclusive);

    public static double NextDouble() => Shared.NextDouble();
}
