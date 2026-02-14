using Godot;

namespace CardFramework;

/// <summary>
/// Abstract base class for card creation factories using the Factory design pattern.
///
/// CardFactory defines the interface for creating cards in the card framework.
/// Concrete implementations like JsonCardFactory provide specific card creation
/// logic while maintaining consistent behavior across different card types and
/// data sources.
///
/// Design Pattern: Factory Method
/// This abstract factory allows the card framework to create cards without
/// knowing the specific implementation details. Different factory types can
/// support various data sources (JSON files, databases, hardcoded data, etc.).
///
/// Key Responsibilities:
/// - Define card creation interface for consistent behavior
/// - Manage card data caching for performance optimization
/// - Provide card size configuration for uniform scaling
/// - Support preloading mechanisms for reduced runtime I/O
///
/// Subclass Implementation Requirements:
/// - Override CreateCard() to implement specific card creation logic
/// - Override PreloadCardData() to implement data initialization
/// </summary>
public abstract partial class CardFactoryA : Node
{
    /// <summary>Default size for cards created by this factory. Applied to all created cards unless overridden.</summary>
    public Vector2 CardSize { get; set; }

    /// <summary>
    /// Creates a card instance and adds it to a container.
    /// Must be implemented by concrete factory subclasses.
    /// </summary>
    /// <param name="cardName">Identifier for the card to create.</param>
    /// <param name="target">CardContainer where the created card will be added.</param>
    /// <returns>Created Card instance or null if creation failed.</returns>
    public abstract Card CreateCard(string cardName, CardContainer target);

    /// <summary>
    /// Preloads card data into the factory's cache.
    /// Concrete implementations should override this to load card definitions
    /// from their respective data sources for faster card creation during gameplay.
    /// </summary>
    public abstract void PreloadCardData();
}
