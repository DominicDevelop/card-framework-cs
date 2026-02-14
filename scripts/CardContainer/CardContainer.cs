using Godot;
using System.Collections.Generic;
using System.Linq;

namespace CardFramework;

/// <summary>
/// Abstract base class for all card containers in the card framework.
///
/// CardContainer provides the foundational functionality for managing collections of cards,
/// including drag-and-drop operations, position management, and container interactions.
/// All specialized containers (Hand, Pile, etc.) extend this class.
///
/// Key Features:
/// - Card collection management with position tracking
/// - Drag-and-drop integration with DropZone system
/// - History tracking for undo/redo operations
/// - Extensible layout system through virtual methods
/// - Visual debugging support for development
///
/// Virtual Methods to Override:
/// - CardCanBeAdded(): Define container-specific rules
/// - UpdateTargetPositions(): Implement container layout logic
/// - OnCardMoveDone(): Handle post-movement processing
/// </summary>
public partial class CardContainer : Control
{
    private static int _nextId;

    [ExportGroup("drop_zone")]
    /// <summary>Enables or disables the drop zone functionality.</summary>
    [Export] public bool EnableDropZone { get; set; } = true;
    [ExportSubgroup("Sensor")]
    /// <summary>The size of the sensor. If not set, it will follow the size of the card.</summary>
    [Export] public Vector2 SensorSize { get; set; }
    /// <summary>The position of the sensor.</summary>
    [Export] public Vector2 SensorPosition { get; set; }
    /// <summary>The texture used for the sensor.</summary>
    [Export] public Texture SensorTexture { get; set; }
    /// <summary>
    /// Determines whether the sensor is visible or not.
    /// Since the sensor can move following the status, please use it for debugging.
    /// </summary>
    [Export] public bool SensorVisibility { get; set; }

    // Container identification and management
    public int UniqueId { get; private set; }
    private PackedScene _dropZoneScene;
    public DropZone DropZoneNode { get; set; }

    // Card collection and state
    // Internal so CardManager can access for history tracking
    internal List<Card> HeldCards { get; set; } = new();
    private List<Card> _holdingCards = new();

    // Scene references
    public Control CardsNode { get; set; }
    public CardManager CardManager { get; set; }
    public bool DebugMode { get; set; }

    public CardContainer()
    {
        UniqueId = _nextId++;
    }

    public override void _Ready()
    {
        _dropZoneScene = GD.Load<PackedScene>("res://Scripts/dependencies/card-framework/drop_zone.tscn");

        if (HasNode("Cards"))
        {
            CardsNode = GetNode<Control>("Cards");
        }
        else
        {
            CardsNode = new Control();
            CardsNode.Name = "Cards";
            CardsNode.MouseFilter = MouseFilterEnum.Pass;
            AddChild(CardsNode);
        }

        FindAndRegisterCardManager();
    }

    public override void _ExitTree()
    {
        CardManager?.DeleteCardContainer(UniqueId);
    }

    public virtual void AddCard(Card card, int index = -1)
    {
        if (index == -1)
            AssignCardToContainer(card);
        else
            InsertCardToContainer(card, index);
        MoveObject(card, CardsNode, index);
    }

    public virtual bool RemoveCard(Card card)
    {
        int index = HeldCards.IndexOf(card);
        if (index == -1)
            return false;
        HeldCards.RemoveAt(index);
        UpdateCardUi();
        return true;
    }

    public int GetCardCount()
    {
        return HeldCards.Count;
    }

    public bool HasCard(Card card)
    {
        return HeldCards.Contains(card);
    }

    public void ClearCards()
    {
        foreach (var card in HeldCards)
            RemoveObject(card);
        HeldCards.Clear();
        UpdateCardUi();
    }

    public bool CheckCardCanBeDropped(List<Card> cards)
    {
        if (!EnableDropZone)
            return false;
        if (DropZoneNode == null)
            return false;
        if (!DropZoneNode.AcceptTypes.Contains(CardManager.CardAcceptType))
            return false;
        if (!DropZoneNode.CheckMouseIsInDropZone())
            return false;
        return CardCanBeAdded(cards);
    }

    public int GetPartitionIndex()
    {
        int verticalIndex = DropZoneNode.GetVerticalLayers();
        if (verticalIndex != -1)
            return verticalIndex;
        int horizontalIndex = DropZoneNode.GetHorizontalLayers();
        if (horizontalIndex != -1)
            return horizontalIndex;
        return -1;
    }

    public void Shuffle()
    {
        FisherYatesShuffle(HeldCards);
        for (int i = 0; i < HeldCards.Count; i++)
        {
            CardsNode.MoveChild(HeldCards[i], i);
        }
        UpdateCardUi();
    }

    public virtual bool MoveCards(List<Card> cards, int index = -1, bool withHistory = true)
    {
        if (!CardCanBeAdded(cards))
            return false;
        if (!cards.All(card => HeldCards.Contains(card)) && withHistory)
            CardManager.AddHistory(this, cards);
        MoveCardsInternal(cards, index);
        return true;
    }

    public void Undo(List<Card> cards, List<int> fromIndices = null)
    {
        fromIndices ??= new List<int>();

        if (fromIndices.Count > 0 && cards.Count != fromIndices.Count)
        {
            GD.PushError("Mismatched cards and indices arrays in undo operation!");
            MoveCardsInternal(cards);
            return;
        }

        if (fromIndices.Count == 0)
        {
            MoveCardsInternal(cards);
            return;
        }

        foreach (var idx in fromIndices)
        {
            if (idx < 0)
            {
                GD.PushError($"Invalid index found during undo: {idx}");
                MoveCardsInternal(cards);
                return;
            }
        }

        var sortedIndices = new List<int>(fromIndices);
        sortedIndices.Sort();
        bool isConsecutive = true;
        for (int i = 1; i < sortedIndices.Count; i++)
        {
            if (sortedIndices[i] != sortedIndices[i - 1] + 1)
            {
                isConsecutive = false;
                break;
            }
        }

        if (isConsecutive && sortedIndices.Count > 1)
        {
            int lowestIndex = sortedIndices[0];

            var cardIndexPairs = new List<(Card card, int index)>();
            for (int i = 0; i < cards.Count; i++)
                cardIndexPairs.Add((cards[i], fromIndices[i]));

            cardIndexPairs.Sort((a, b) => a.index.CompareTo(b.index));

            for (int i = 0; i < cardIndexPairs.Count; i++)
            {
                int targetIndex = Mathf.Min(lowestIndex + i, HeldCards.Count);
                MoveCardsInternal(new List<Card> { cardIndexPairs[i].card }, targetIndex);
            }
        }
        else
        {
            var cardIndexPairs = new List<(Card card, int index, int originalOrder)>();
            for (int i = 0; i < cards.Count; i++)
                cardIndexPairs.Add((cards[i], fromIndices[i], i));

            cardIndexPairs.Sort((a, b) =>
            {
                if (a.index == b.index)
                    return a.originalOrder.CompareTo(b.originalOrder);
                return b.index.CompareTo(a.index);
            });

            foreach (var pair in cardIndexPairs)
            {
                int targetIndex = Mathf.Min(pair.index, HeldCards.Count);
                MoveCardsInternal(new List<Card> { pair.card }, targetIndex);
            }
        }
    }

    public virtual void HoldCard(Card card)
    {
        if (HeldCards.Contains(card))
            _holdingCards.Add(card);
    }

    public void ReleaseHoldingCards()
    {
        if (_holdingCards.Count == 0)
            return;

        foreach (var card in _holdingCards)
            card.ChangeState(DraggableObject.DraggableState.Idle);

        var copiedHoldingCards = new List<Card>(_holdingCards);
        CardManager?.OnDragDropped(copiedHoldingCards);
        _holdingCards.Clear();
    }

    public string GetString()
    {
        return $"card_container: {UniqueId}";
    }

    public virtual void OnCardMoveDone(Card card)
    {
    }

    public virtual void OnCardPressed(Card card)
    {
    }

    private void AssignCardToContainer(Card card)
    {
        if (card.CardContainerRef != this)
            card.CardContainerRef = this;
        if (!HeldCards.Contains(card))
            HeldCards.Add(card);
        UpdateCardUi();
    }

    private void InsertCardToContainer(Card card, int index)
    {
        if (card.CardContainerRef != this)
            card.CardContainerRef = this;
        if (!HeldCards.Contains(card))
        {
            if (index < 0) index = 0;
            else if (index > HeldCards.Count) index = HeldCards.Count;
            HeldCards.Insert(index, card);
        }
        UpdateCardUi();
    }

    private void MoveToCardContainer(Card card, int index = -1)
    {
        card.CardContainerRef?.RemoveCard(card);
        AddCard(card, index);
    }

    private static void FisherYatesShuffle<T>(List<T> array)
    {
        for (int i = array.Count - 1; i > 0; i--)
        {
            int j = (int)(GD.Randi() % (uint)(i + 1));
            (array[i], array[j]) = (array[j], array[i]);
        }
    }

    protected void MoveCardsInternal(List<Card> cards, int index = -1)
    {
        int curIndex = index;
        for (int i = cards.Count - 1; i >= 0; i--)
        {
            var card = cards[i];
            if (curIndex == -1)
                MoveToCardContainer(card);
            else
            {
                MoveToCardContainer(card, curIndex);
                curIndex++;
            }
        }
    }

    public bool CanAddCards(List<Card> cards) => CardCanBeAdded(cards);

    protected virtual bool CardCanBeAdded(List<Card> cards)
    {
        return true;
    }

    public void UpdateCardUi()
    {
        UpdateTargetZIndex();
        UpdateTargetPositions();
    }

    protected virtual void UpdateTargetZIndex()
    {
    }

    protected virtual void UpdateTargetPositions()
    {
    }

    private void MoveObject(Node target, Node to, int index = -1)
    {
        if (target.GetParent() == to)
        {
            if (index != -1)
                to.MoveChild(target, index);
            else
                to.MoveChild(target, to.GetChildCount() - 1);
            return;
        }

        var globalPos = ((Control)target).GlobalPosition;
        target.GetParent()?.RemoveChild(target);
        if (index != -1)
        {
            to.AddChild(target);
            to.MoveChild(target, index);
        }
        else
        {
            to.AddChild(target);
        }
        ((Control)target).GlobalPosition = globalPos;
    }

    private void FindAndRegisterCardManager()
    {
        if (CardManager != null)
            return;

        var sceneRoot = GetTree().CurrentScene;
        if (sceneRoot != null && sceneRoot.HasMeta("card_manager"))
        {
            CardManager = sceneRoot.GetMeta("card_manager").As<CardManager>();
            if (DebugMode)
                GD.Print($"CardContainer found CardManager via scene root meta: {Name}");
        }
        else
        {
            CardManager = FindCardManagerInParents();
            if (CardManager != null && DebugMode)
                GD.Print($"CardContainer found CardManager via parent traversal: {Name}");
        }

        if (CardManager == null)
        {
            GD.PushError($"CardContainer '{Name}' could not find CardManager.\n" +
                "SOLUTION: Ensure CardManager is positioned ABOVE CardContainers in scene tree.");
            return;
        }

        CardManager.AddCardContainer(UniqueId, this);
        InitializeDropZone();
    }

    private CardManager FindCardManagerInParents()
    {
        var parent = GetParent();
        while (parent != null)
        {
            if (parent is CardManager cm)
                return cm;
            parent = parent.GetParent();
        }
        return null;
    }

    private void InitializeDropZone()
    {
        if (!EnableDropZone)
            return;

        DropZoneNode = _dropZoneScene.Instantiate<DropZone>();
        AddChild(DropZoneNode);
        DropZoneNode.Init(this, new List<string> { CardManager.CardAcceptType });

        if (SensorSize == Vector2.Zero)
            SensorSize = CardManager.CardSize;

        DropZoneNode.SetSensor(SensorSize, SensorPosition, SensorTexture as Texture2D, SensorVisibility);
        SensorOutline ??= DropZoneNode.SensorOutline;
        DropZoneNode.SensorOutline.Visible = DebugMode;
    }

    // Helper for accessing SensorOutline from DropZone without shadowing
    private ReferenceRect SensorOutline { get; set; }

    private void RemoveObject(Node target)
    {
        target.GetParent()?.RemoveChild(target);
        target.QueueFree();
    }
}
