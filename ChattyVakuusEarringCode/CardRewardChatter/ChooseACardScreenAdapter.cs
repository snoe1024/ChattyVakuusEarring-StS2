using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode.CardRewardChatter;

/// <summary>
/// <see cref="ICardChoiceScreenAdapter"/>の「カードを1枚選ぶ」画面(<c>NChooseACardSelectionScreen</c>、
/// アタックポーション等)版。あちらは<c>NCardRewardSelectionScreen.GetCardHolder</c>のような公開APIを
/// 持たず、カードの並び(<c>_cardRow</c>)がprivateフィールドのままなので、Harmonyの<c>AccessTools</c>で
/// 読み出す(パッチではなく単なる読み取りなので<c>[HarmonyPatch]</c>は不要)。
/// </summary>
internal sealed class ChooseACardScreenAdapter : ICardChoiceScreenAdapter
{
    private static readonly AccessTools.FieldRef<NChooseACardSelectionScreen, Control> CardRowRef =
        AccessTools.FieldRefAccess<NChooseACardSelectionScreen, Control>("_cardRow");

    private readonly NChooseACardSelectionScreen _screen;

    public ChooseACardScreenAdapter(NChooseACardSelectionScreen screen)
    {
        _screen = screen;
    }

    public Rect2 GetViewportRect() => _screen.GetViewportRect();

    public Control? FindCardHolder(CardModel card) =>
        CardRowRef(_screen).GetChildren().OfType<NGridCardHolder>().FirstOrDefault(h => h.CardModel == card);
}
