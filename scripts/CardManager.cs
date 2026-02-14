using Godot;
using System.Collections.Generic;

namespace CardFramework;

/// <summary>
/// Central orchestrator for the card framework system.
///
/// CardManager coordinates all card-related operations including drag-and-drop,
/// history management, and container registration. It serves as the root node
/// for card game scenes and manages the lifecycle of cards and containers.
///
/// Key Responsibilities:
/// - Card factory management and initialization
/// - Container registration and coordination
/// - Drag-and-drop event handling and routing
/// - History tracking for undo/redo operations
/// - Debug mode and visual debugging support
///
/// Setup Requirements:
/// - Must be positioned ABOVE CardContainers in scene tree hierarchy
/// - Requires CardFactory to be assigned in inspector
/// - Configure CardSize to match your card assets
/// </summary>
public partial class CardManager : Control
{
    #region Constants

    public const string CardAcceptType = "card";

    #endregion

    #region Settings

    /// <summary>Default size for all cards in the game.</summary>
    [Export] public Vector2 CardSize { get; set; } = CardFrameworkSettings.LayoutDefaultCardSize;
    /// <summary>Enables visual debugging for drop zones and interactions.</summary>
    [Export] public bool DebugMode { get; set; }

    #endregion

    #region Core system components

    /// <summary>The card factory implementation used to create card instances.</summary>
    [Export] public CardFactoryA CardFactory { get; set; }
    
    private Dictionary<int, CardContainer> _cardContainerDict = new();
    private List<HistoryElement> _history = new();
    
    #endregion

    public override void _Ready()
    {
        if (CardFactory == null)
        {
            GD.PrintErr("Card factory not set!");
            return;
        }

        var sceneRoot = GetTree().CurrentScene;
        if (sceneRoot != null)
        {
            sceneRoot.SetMeta("card_manager", this);
            if (DebugMode)
                GD.Print($"CardManager registered to scene root: {sceneRoot.Name}");
        }

        CardFactory.CardSize = CardSize;
        CardFactory.PreloadCardData();
    }

    public void Undo()
    {
        if (_history.Count == 0)
            return;

        var last = _history[^1];
        _history.RemoveAt(_history.Count - 1);
        last.From?.Undo(last.Cards, last.FromIndices);
    }

    public void ResetHistory()
    {
        _history.Clear();
    }

    public override void _ExitTree()
    {
        var sceneRoot = GetTree().CurrentScene;
        if (sceneRoot != null && sceneRoot.HasMeta("card_manager"))
        {
            sceneRoot.RemoveMeta("card_manager");
            if (DebugMode)
                GD.Print("CardManager unregistered from scene root");
        }
    }

    public void AddCardContainer(int id, CardContainer cardContainer)
    {
        _cardContainerDict[id] = cardContainer;
        cardContainer.DebugMode = DebugMode;
    }

    public void DeleteCardContainer(int id)
    {
        _cardContainerDict.Remove(id);
    }

    public void OnDragDropped(List<Card> cards)
    {
        if (cards.Count == 0)
            return;

        var originalMouseFilters = new Dictionary<Card, Control.MouseFilterEnum>();
        foreach (var card in cards)
        {
            originalMouseFilters[card] = card.MouseFilter;
            card.MouseFilter = MouseFilterEnum.Ignore;
        }

        foreach (var key in _cardContainerDict.Keys)
        {
            var cardContainer = _cardContainerDict[key];
            if (cardContainer.CheckCardCanBeDropped(cards))
            {
                int index = cardContainer.GetPartitionIndex();
                foreach (var card in cards)
                    card.MouseFilter = originalMouseFilters[card];
                cardContainer.MoveCards(cards, index);
                return;
            }
        }

        foreach (var card in cards)
        {
            card.MouseFilter = originalMouseFilters[card];
            card.ReturnCard();
        }
    }

    public void AddHistory(CardContainer to, List<Card> cards)
    {
        CardContainer from = null;
        var fromIndices = new List<int>();

        for (int i = 0; i < cards.Count; i++)
        {
            var c = cards[i];
            var current = c.CardContainerRef;
            if (i == 0)
            {
                from = current;
            }
            else if (from != current)
            {
                GD.PushError("All cards must be from the same container!");
                return;
            }

            if (from != null)
            {
                int originalIndex = from.HeldCards.IndexOf(c);
                if (originalIndex == -1)
                {
                    GD.PushError("Card not found in source container during history recording!");
                    return;
                }
                fromIndices.Add(originalIndex);
            }
        }

        var historyElement = new HistoryElement
        {
            From = from,
            To = to,
            Cards = cards,
            FromIndices = fromIndices
        };
        _history.Add(historyElement);
    }
}
