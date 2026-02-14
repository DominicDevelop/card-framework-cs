using Godot;
using System.Collections.Generic;

namespace CardFramework;

/// <summary>
/// History tracking element for card movement operations with precise undo support.
///
/// HistoryElement stores complete state information for card movements to enable
/// accurate undo/redo operations. It tracks source and destination containers,
/// moved cards, and their original indices for precise state restoration.
///
/// Key Features:
/// - Complete movement state capture for reliable undo operations
/// - Precise index tracking to restore original card positions
/// - Support for multi-card movement operations
/// - Detailed string representation for debugging and logging
///
/// Used By:
/// - CardManager for history management and undo operations
/// - CardContainer.Undo() for precise card position restoration
///
/// Index Precision:
/// The FromIndices list stores the exact original positions of cards in their
/// source container. This enables precise restoration even when multiple cards
/// are moved simultaneously or containers have been modified since the operation.
/// </summary>
public class HistoryElement
{
    /// <summary>Source container where cards originated (null for newly created cards).</summary>
    public CardContainer From { get; set; }
    /// <summary>Destination container where cards were moved.</summary>
    public CardContainer To { get; set; }
    /// <summary>List of Card instances that were moved in this operation.</summary>
    public List<Card> Cards { get; set; } = new();
    /// <summary>Original indices of cards in the source container for precise undo restoration.</summary>
    public List<int> FromIndices { get; set; } = new();

    public string GetString()
    {
        var fromStr = From?.GetString() ?? "null";
        var toStr = To?.GetString() ?? "null";

        var cardStrings = new List<string>();
        foreach (var c in Cards)
        {
            cardStrings.Add(c.GetString());
        }

        var cardsStr = string.Join(", ", cardStrings);
        var indicesStr = FromIndices.Count > 0 ? $"[{string.Join(", ", FromIndices)}]" : "[]";
        return $"from: [{fromStr}], to: [{toStr}], cards: [{cardsStr}], indices: {indicesStr}";
    }
}
