using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter.Speaker;
using MegaCrit.Sts2.Core.Models;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.CardRewardChatter;

/// <summary>
/// <see cref="CardRewardDetector"/>が返す提案。戦闘・ショップの<c>Utterance</c>だけで済まないのは、
/// カード報酬画面では「どのカードについて言っているか」(吹き出しをどこに出すか)まで一緒に伝える必要があるため。
/// </summary>
/// <param name="Utterance">発言内容。</param>
/// <param name="AnchorCard">
/// この発言が特定の1枚について言っている場合、そのカード(吹き出しをそのカードの近くに出す)。
/// 任意のカード報酬全般についての発言ならnull(吹き出しは画面の端から出す)。
/// </param>
public sealed record CardRewardProposal(Utterance Utterance, CardModel? AnchorCard);
