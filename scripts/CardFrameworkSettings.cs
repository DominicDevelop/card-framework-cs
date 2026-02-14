using Godot;

namespace CardFramework;

/// <summary>
/// Card Framework configuration constants class.
///
/// This class provides centralized constant values for all Card Framework components
/// without requiring Autoload. All values are defined as constants to ensure
/// consistent behavior across the framework.
///
/// <example>
/// <code>
/// // Reference constants directly
/// var speed = CardFrameworkSettings.AnimationMoveSpeed;
/// var zOffset = CardFrameworkSettings.VisualDragZOffset;
/// </code>
/// </example>
/// </summary>
public static class CardFrameworkSettings
{
    // Animation Constants
    /// <summary>Speed of card movement animations in pixels per second.</summary>
    public const float AnimationMoveSpeed = 2000.0f;
    /// <summary>Duration of hover animations in seconds.</summary>
    public const float AnimationHoverDuration = 0.10f;
    /// <summary>Scale multiplier applied during hover effects.</summary>
    public const float AnimationHoverScale = 1.1f;
    /// <summary>Rotation in degrees applied during hover effects.</summary>
    public const float AnimationHoverRotation = 0.0f;

    // Physics & Interaction Constants
    /// <summary>Distance threshold for hover detection in pixels.</summary>
    public const float PhysicsHoverDistance = 10.0f;
    /// <summary>Distance cards move up during hover in pixels.</summary>
    public const float PhysicsCardHoverDistance = 30.0f;

    // Visual Layout Constants
    /// <summary>Z-index offset applied to cards during drag operations.</summary>
    public const int VisualDragZOffset = 1000;
    /// <summary>Z-index for pile cards to ensure proper layering.</summary>
    public const int VisualPileZIndex = 3000;
    /// <summary>Z-index for drop zone sensors (below everything).</summary>
    public const int VisualSensorZIndex = -1000;
    /// <summary>Z-index for debug outlines (above UI).</summary>
    public const int VisualOutlineZIndex = 1200;

    // Container Layout Constants
    /// <summary>Default card size used throughout the framework.</summary>
    public static readonly Vector2 LayoutDefaultCardSize = new(150, 210);
    /// <summary>Distance between stacked cards in piles.</summary>
    public const int LayoutStackGap = 8;
    /// <summary>Maximum cards to display in stack before hiding.</summary>
    public const int LayoutMaxStackDisplay = 6;
    /// <summary>Maximum number of cards in hand containers.</summary>
    public const int LayoutMaxHandSize = 10;
    /// <summary>Maximum pixel spread for hand arrangements.</summary>
    public const int LayoutMaxHandSpread = 700;

    // Color Constants for Debugging
    /// <summary>Color used for sensor outlines and debug indicators.</summary>
    public static readonly Color DebugOutlineColor = new(1, 0, 0, 1);
}
