using Godot;
using System.Collections.Generic;

namespace CardFramework;

/// <summary>
/// A stacked card container with directional positioning and interaction controls.
///
/// Pile provides a traditional card stack implementation where cards are arranged
/// in a specific direction with configurable spacing. It supports various interaction
/// modes from full movement to top-card-only access, making it suitable for deck
/// implementations, foundation piles, and discard stacks.
///
/// Key Features:
/// - Directional stacking (up, down, left, right)
/// - Configurable stack display limits and spacing
/// - Flexible interaction controls (all cards, top only, none)
/// - Dynamic drop zone positioning following top card
/// - Visual depth management with z-index layering
///
/// Common Use Cases:
/// - Foundation piles in Solitaire games
/// - Draw/discard decks with face-down cards
/// - Tableau piles with partial card access
/// </summary>
public partial class Pile : CardContainer
{
    /// <summary>Defines the stacking direction for cards in the pile.</summary>
    public enum PileDirection
    {
        /// <summary>Cards stack upward (negative Y direction).</summary>
        Up,
        /// <summary>Cards stack downward (positive Y direction).</summary>
        Down,
        /// <summary>Cards stack leftward (negative X direction).</summary>
        Left,
        /// <summary>Cards stack rightward (positive X direction).</summary>
        Right
    }

    [ExportGroup("pile_layout")]
    /// <summary>Distance between each card in the stack display.</summary>
    [Export] public int StackDisplayGap { get; set; } = CardFrameworkSettings.LayoutStackGap;
    /// <summary>
    /// Maximum number of cards to visually display in the pile.
    /// Cards beyond this limit will be hidden under the visible stack.
    /// </summary>
    [Export] public int MaxStackDisplay { get; set; } = CardFrameworkSettings.LayoutMaxStackDisplay;
    /// <summary>Whether cards in the pile show their front face (true) or back face (false).</summary>
    [Export] public bool CardFaceUp { get; set; } = true;
    /// <summary>Direction in which cards are stacked from the pile's base position.</summary>
    [Export] public PileDirection Layout { get; set; } = PileDirection.Up;

    [ExportGroup("pile_interaction")]
    /// <summary>Whether any card in the pile can be moved via drag-and-drop.</summary>
    [Export] public bool AllowCardMovement { get; set; } = true;
    /// <summary>Restricts movement to only the top card (requires AllowCardMovement = true).</summary>
    [Export] public bool RestrictToTopCard { get; set; } = true;
    /// <summary>Whether drop zone follows the top card position (requires AllowCardMovement = true).</summary>
    [Export] public bool AlignDropZoneWithTopCard { get; set; } = true;

    public List<Card> GetTopCards(int n)
    {
        int arrSize = HeldCards.Count;
        if (n > arrSize)
            n = arrSize;

        var result = new List<Card>();
        for (int i = 0; i < n; i++)
            result.Add(HeldCards[arrSize - 1 - i]);

        return result;
    }

    protected override void UpdateTargetZIndex()
    {
        for (int i = 0; i < HeldCards.Count; i++)
        {
            var card = HeldCards[i];
            card.StoredZIndex = card.IsPressed
                ? CardFrameworkSettings.VisualPileZIndex + i
                : i;
        }
    }

    protected override void UpdateTargetPositions()
    {
        int lastIndex = HeldCards.Count - 1;
        if (lastIndex < 0)
            lastIndex = 0;
        var lastOffset = CalculateOffset(lastIndex);

        if (EnableDropZone && AlignDropZoneWithTopCard)
            DropZoneNode.ChangeSensorPositionWithOffset(lastOffset);

        for (int i = 0; i < HeldCards.Count; i++)
        {
            var card = HeldCards[i];
            var offset = CalculateOffset(i);
            var targetPos = Position + offset;

            card.ShowFront = CardFaceUp;
            card.Move(targetPos, 0);

            if (!AllowCardMovement)
            {
                card.CanBeInteractedWith = false;
            }
            else if (RestrictToTopCard)
            {
                card.CanBeInteractedWith = i == HeldCards.Count - 1;
            }
            else
            {
                card.CanBeInteractedWith = true;
            }
        }
    }

    private Vector2 CalculateOffset(int index)
    {
        int actualIndex = Mathf.Min(index, MaxStackDisplay - 1);
        int offsetValue = actualIndex * StackDisplayGap;
        var offset = Vector2.Zero;

        switch (Layout)
        {
            case PileDirection.Up:
                offset.Y -= offsetValue;
                break;
            case PileDirection.Down:
                offset.Y += offsetValue;
                break;
            case PileDirection.Right:
                offset.X += offsetValue;
                break;
            case PileDirection.Left:
                offset.X -= offsetValue;
                break;
        }

        return offset;
    }
}
