using System;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.Patch;

/// <summary>
/// 音叉(<see cref="TuningFork"/>)・扇子(<see cref="OrnamentalFan"/>)・銀河の塵(<see cref="GalacticDust"/>)・
/// 波紋の鉢(<see cref="RippleBasin"/>)・マントの留め具(<see cref="CloakClasp"/>)が「発動した」瞬間を捉える。
/// </summary>
/// <remarks>
/// <para>
/// これらのレリックが実際に発動したかどうかは、各レリック内部のprivateなカウンタ判定の結果でしかなく、
/// 外部から公開イベントや<c>CombatHistory</c>(<c>BlockGainedEntry</c>は「誰が」ブロックを得たかは記録するが、
/// 「どのレリックが原因か」までは記録しない)経由で知る手段が無い。素直にやるなら5レリックそれぞれの
/// トリガーメソッド(<c>AfterCardPlayed</c>/<c>AfterStarsSpent</c>/<c>BeforeSideTurnEnd</c>)を個別にパッチして
/// 発動条件をこちら側で再現する必要があるが、その条件はprivateフィールド依存かつ将来のバランス変更で
/// 静かにズレる可能性がある。
/// </para>
/// <para>
/// 代わりに、この5レリックが**発動した瞬間に必ず呼ぶ共通のメソッド**である
/// <c>RelicModel.Flash()</c>(引数無しオーバーロード。発動演出の光エフェクト)を1箇所だけHarmonyで捉える。
/// ソースを確認した限り、5レリックとも「発動条件を満たした判定の直後・実際にブロックを与える<c>await</c>より前」に
/// 同期的に<c>Flash()</c>を呼んでいる(3レリックは<c>TaskHelper.RunSafely</c>経由の`async`メソッド内からだが、
/// `await`前の部分は呼び出し元スレッドで同期実行されるため、結局このPostfixは対応する
/// <c>BlockGainedEntry</c>が履歴に記録される前に必ず先に走る)。
/// </para>
/// <para>
/// <c>Flash()</c>自体は他の多くのレリックも様々な理由(無関係な演出)で呼んでおり、このパッチは
/// それら全ての呼び出しでも一度動く。ただし処理は型チェック1つで即終了するうえ、レリックの発動自体が
/// 頻繁に起きるものではないため、負荷は無視できる。
/// </para>
/// </remarks>
[HarmonyPatch(typeof(RelicModel), nameof(RelicModel.Flash), new Type[] { })]
public static class BlockRelicFlashPatch
{
    [HarmonyPostfix]
    public static void Postfix(RelicModel __instance)
    {
        if (__instance is TuningFork or OrnamentalFan or GalacticDust or RippleBasin or CloakClasp)
        {
            ChatterHub.OnBlockRelicActivated(__instance);
        }
    }
}
