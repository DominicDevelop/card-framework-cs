using Godot;
using System;
using System.Collections.Generic;

namespace CardFramework;

/// <summary>
/// A fan-shaped card container that arranges cards in an arc formation.
///
/// Hand provides sophisticated card layout using mathematical curves to create
/// natural-looking card arrangements. Cards are positioned in a fan pattern
/// with configurable spread, rotation, and vertical displacement.
///
/// Key Features:
/// - Fan-shaped card arrangement with customizable curves
/// - Smooth card reordering with optional swap-only mode
/// - Dynamic drop zone sizing to match hand spread
/// - Configurable card limits and hover distances
/// - Mathematical positioning using Curve resources
///
/// Curve Configuration:
/// - HandRotationCurve: Controls card rotation (linear -X to +X recommended)
/// - HandVerticalCurve: Controls vertical offset (3-point ease 0-X-0 recommended)
/// </summary>
public partial class Hand : CardContainer
{
    [ExportGroup("hand_meta_info")]
    /// <summary>Maximum number of cards that can be held.</summary>
    [Export] public int MaxHandSize { get; set; } = CardFrameworkSettings.LayoutMaxHandSize;
    /// <summary>Maximum spread of the hand.</summary>
    [Export] public int MaxHandSpread { get; set; } = CardFrameworkSettings.LayoutMaxHandSpread;
    /// <summary>Whether the card is face up.</summary>
    [Export] public bool CardFaceUp { get; set; } = true;
    /// <summary>Distance the card hovers when interacted with.</summary>
    [Export] public float CardHoverDistance { get; set; } = CardFrameworkSettings.PhysicsCardHoverDistance;

    [ExportGroup("hand_shape")]
    /// <summary>
    /// Rotation curve of the hand.
    /// This works best as a 2-point linear rise from -X to +X.
    /// </summary>
    [Export] public Curve HandRotationCurve { get; set; }
    /// <summary>
    /// Vertical curve of the hand.
    /// This works best as a 3-point ease in/out from 0 to X to 0.
    /// </summary>
    [Export] public Curve HandVerticalCurve { get; set; }

    [ExportGroup("drop_zone")]
    /// <summary>Determines whether the drop zone size follows the hand size. (requires EnableDropZone = true)</summary>
    [Export] public bool AlignDropZoneSizeWithCurrentHandSize { get; set; } = true;
    /// <summary>If true, only swap the positions of two cards when reordering (a &lt;-&gt; b), otherwise shift the range (default behavior).</summary>
    [Export] public bool SwapOnlyOnReorder { get; set; }

    private List<float> _verticalPartitionsFromOutside = new();
    private List<float> _verticalPartitionsFromInside = new();

    public override void _Ready()
    {
        base._Ready();
    }

    public List<Card> GetRandomCards(int n)
    {
        var deck = new List<Card>(HeldCards);
        // Fisher-Yates shuffle
        var rng = new Random();
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (deck[i], deck[j]) = (deck[j], deck[i]);
        }
        if (n > HeldCards.Count)
            n = HeldCards.Count;
        return deck.GetRange(0, n);
    }

    protected override bool CardCanBeAdded(List<Card> cards)
    {
        bool isAllCardsContained = true;
        foreach (var card in cards)
        {
            if (!HeldCards.Contains(card))
            {
                isAllCardsContained = false;
                break;
            }
        }

        if (isAllCardsContained)
            return true;

        return HeldCards.Count + cards.Count <= MaxHandSize;
    }

    protected override void UpdateTargetZIndex()
    {
        for (int i = 0; i < HeldCards.Count; i++)
            HeldCards[i].StoredZIndex = i;
    }

    protected override void UpdateTargetPositions()
    {
        float xMin = 0, xMax = 0, yMin = 0, yMax = 0;
        var cardSize = CardManager.CardSize;
        float w = cardSize.X;
        float h = cardSize.Y;

        _verticalPartitionsFromOutside.Clear();

        for (int i = 0; i < HeldCards.Count; i++)
        {
            var card = HeldCards[i];

            float handRatio = 0.5f;
            if (HeldCards.Count > 1)
                handRatio = (float)i / (HeldCards.Count - 1);

            var targetPos = GlobalPosition;
            int cardSpacing = MaxHandSpread / (HeldCards.Count + 1);
            targetPos.X += (i + 1) * cardSpacing - MaxHandSpread / 2.0f;

            if (HandVerticalCurve != null)
                targetPos.Y -= HandVerticalCurve.Sample(handRatio);

            float targetRotation = 0;
            if (HandRotationCurve != null)
                targetRotation = Mathf.DegToRad(HandRotationCurve.Sample(handRatio));

            // Calculate rotated card bounding box for drop zone partitioning
            float x = targetPos.X;
            float y = targetPos.Y;

            float t1 = Mathf.Atan2(h, w) + targetRotation;
            float t2 = Mathf.Atan2(h, -w) + targetRotation;
            float t3 = t1 + Mathf.Pi + targetRotation;
            float t4 = t2 + Mathf.Pi + targetRotation;

            var c = new Vector2(x + w / 2, y + h / 2);
            float r = Mathf.Sqrt(Mathf.Pow(w / 2, 2.0f) + Mathf.Pow(h / 2, 2.0f));

            var p1 = new Vector2(r * Mathf.Cos(t1), r * Mathf.Sin(t1)) + c;
            var p2 = new Vector2(r * Mathf.Cos(t2), r * Mathf.Sin(t2)) + c;
            var p3 = new Vector2(r * Mathf.Cos(t3), r * Mathf.Sin(t3)) + c;
            var p4 = new Vector2(r * Mathf.Cos(t4), r * Mathf.Sin(t4)) + c;

            float currentXMin = Mathf.Min(Mathf.Min(p1.X, p2.X), Mathf.Min(p3.X, p4.X));
            float currentXMax = Mathf.Max(Mathf.Max(p1.X, p2.X), Mathf.Max(p3.X, p4.X));
            float currentYMin = Mathf.Min(Mathf.Min(p1.Y, p2.Y), Mathf.Min(p3.Y, p4.Y));
            float currentYMax = Mathf.Max(Mathf.Max(p1.Y, p2.Y), Mathf.Max(p3.Y, p4.Y));
            float currentXMid = (currentXMin + currentXMax) / 2;
            _verticalPartitionsFromOutside.Add(currentXMid);

            if (i == 0)
            {
                xMin = currentXMin;
                xMax = currentXMax;
                yMin = currentYMin;
                yMax = currentYMax;
            }
            else
            {
                xMin = Mathf.Min(xMin, currentXMin);
                xMax = Mathf.Max(xMax, currentXMax);
                yMin = Mathf.Min(yMin, currentYMin);
                yMax = Mathf.Max(yMax, currentYMax);
            }

            card.Move(targetPos, targetRotation);
            card.ShowFront = CardFaceUp;
            card.CanBeInteractedWith = true;
        }

        // Calculate midpoints between consecutive values
        _verticalPartitionsFromInside.Clear();
        if (_verticalPartitionsFromOutside.Count > 1)
        {
            for (int j = 0; j < _verticalPartitionsFromOutside.Count - 1; j++)
            {
                float mid = (_verticalPartitionsFromOutside[j] + _verticalPartitionsFromOutside[j + 1]) / 2.0f;
                _verticalPartitionsFromInside.Add(mid);
            }
        }

        if (AlignDropZoneSizeWithCurrentHandSize)
        {
            if (HeldCards.Count == 0)
            {
                DropZoneNode.ReturnSensorSize();
            }
            else
            {
                var size = new Vector2(xMax - xMin, yMax - yMin);
                var position = new Vector2(xMin, yMin) - Position;
                DropZoneNode.SetSensorSizeFlexibly(size, position);
            }
            DropZoneNode.SetVerticalPartitions(_verticalPartitionsFromOutside);
        }
    }

    public override bool MoveCards(List<Card> cards, int index = -1, bool withHistory = true)
    {
        // Handle single card reordering within same Hand container
        if (cards.Count == 1 && HeldCards.Contains(cards[0]) && index >= 0 && index < HeldCards.Count)
        {
            int currentIndex = HeldCards.IndexOf(cards[0]);

            if (SwapOnlyOnReorder)
            {
                SwapCard(cards[0], index);
                return true;
            }

            if (currentIndex == index)
            {
                UpdateCardUi();
                RestoreMouseInteraction(cards);
                return true;
            }

            ReorderCardInHand(cards[0], currentIndex, index, withHistory);
            RestoreMouseInteraction(cards);
            return true;
        }

        return base.MoveCards(cards, index, withHistory);
    }

    public void SwapCard(Card card, int index)
    {
        int currentIndex = HeldCards.IndexOf(card);
        if (currentIndex == index)
            return;
        (HeldCards[currentIndex], HeldCards[index]) = (HeldCards[index], HeldCards[currentIndex]);
        UpdateCardUi();
    }

    private void RestoreMouseInteraction(List<Card> cards)
    {
        foreach (var card in cards)
            card.MouseFilter = MouseFilterEnum.Stop;
    }

    private void ReorderCardInHand(Card card, int fromIndex, int toIndex, bool withHistory)
    {
        if (withHistory)
            CardManager.AddHistory(this, new List<Card> { card });

        HeldCards.RemoveAt(fromIndex);
        HeldCards.Insert(toIndex, card);

        UpdateCardUi();
    }

    public override void HoldCard(Card card)
    {
        if (HeldCards.Contains(card))
            DropZoneNode.SetVerticalPartitions(_verticalPartitionsFromInside);
        base.HoldCard(card);
    }
}
