using Godot;
using Godot.Collections;

namespace CardFramework;

/// <summary>
/// A card object that represents a single playing card with drag-and-drop functionality.
///
/// The Card class extends DraggableObject to provide interactive card behavior including
/// hover effects, drag operations, and visual state management. Cards can display
/// different faces (front/back) and integrate with the card framework's container system.
///
/// Key Features:
/// - Visual state management (front/back face display)
/// - Drag-and-drop interaction with state machine
/// - Integration with CardContainer for organized card management
/// - Hover animation and visual feedback
/// </summary>
public partial class Card : DraggableObject
{
    // Static counters for global card state tracking
    public static int HoveringCardCount { get; set; }
    public static int HoldingCardCount { get; set; }

    /// <summary>The name of the card.</summary>
    [Export] public string CardName { get; set; }
    /// <summary>The size of the card.</summary>
    [Export] public Vector2 CardSize { get; set; } = CardFrameworkSettings.LayoutDefaultCardSize;
    /// <summary>The texture for the front face of the card.</summary>
    [Export] public Texture2D FrontImage { get; set; }
    /// <summary>The texture for the back face of the card.</summary>
    [Export] public Texture2D BackImage { get; set; }

    private bool _showFront = true;
    /// <summary>
    /// Whether the front face of the card is shown.
    /// If true, the front face is visible; otherwise, the back face is visible.
    /// </summary>
    [Export]
    public bool ShowFront
    {
        get => _showFront;
        set
        {
            _showFront = value;
            UpdateFaceVisibility();
        }
    }

    /// <summary>
    /// The TextureRect node for displaying the front face of the card.
    /// If not assigned, will fallback to FrontFace/TextureRect for backward compatibility.
    /// </summary>
    [Export] public TextureRect FrontFaceTexture { get; set; }
    /// <summary>
    /// The TextureRect node for displaying the back face of the card.
    /// If not assigned, will fallback to BackFace/TextureRect for backward compatibility.
    /// </summary>
    [Export] public TextureRect BackFaceTexture { get; set; }
    
    // Card data and container reference
    public Dictionary CardInfo { get; set; } = new();
    public CardContainer CardContainerRef { get; set; }
    
    public override void _Ready()
    {
        base._Ready();
    
        // Fallback to hardcoded paths if not assigned (backward compatibility)
        FrontFaceTexture ??= HasNode("FrontFace/TextureRect") ? GetNode<TextureRect>("FrontFace/TextureRect") : null;
        BackFaceTexture ??= HasNode("BackFace/TextureRect") ? GetNode<TextureRect>("BackFace/TextureRect") : null;
    
        if (FrontFaceTexture == null || BackFaceTexture == null)
        {
            GD.PushError("Card requires front_face_texture and back_face_texture to be assigned or FrontFace/TextureRect and BackFace/TextureRect nodes to exist");
            return;
        }
    
        FrontFaceTexture.Size = CardSize;
        BackFaceTexture.Size = CardSize;
        if (FrontImage != null)
            FrontFaceTexture.Texture = FrontImage;
        if (BackImage != null)
            BackFaceTexture.Texture = BackImage;
        PivotOffset = CardSize / 2;
    
        UpdateFaceVisibility();
    }
    
    private void UpdateFaceVisibility()
    {
        if (FrontFaceTexture != null && BackFaceTexture != null)
        {
            FrontFaceTexture.Visible = _showFront;
            BackFaceTexture.Visible = !_showFront;
        }
    }
    
    protected override void OnMoveDone()
    {
        CardContainerRef.OnCardMoveDone(this);
    }
    
    public void SetFaces(Texture2D frontFace, Texture2D backFace)
    {
        FrontFaceTexture.Texture = frontFace;
        BackFaceTexture.Texture = backFace;
    }
    
    public void ReturnCard()
    {
        ReturnToOriginal();
    }
    
    protected override void EnterState(DraggableState state, DraggableState fromState)
    {
        base.EnterState(state, fromState);
    
        switch (state)
        {
            case DraggableState.Hovering:
                HoveringCardCount++;
                break;
            case DraggableState.Holding:
                HoldingCardCount++;
                if (CardContainerRef != null)
                    CardContainerRef.HoldCard(this);
                break;
        }
    }
    
    protected override void ExitState(DraggableState state)
    {
        switch (state)
        {
            case DraggableState.Hovering:
                HoveringCardCount--;
                break;
            case DraggableState.Holding:
                HoldingCardCount--;
                break;
        }
    
        base.ExitState(state);
    }
    
    public void SetHolding()
    {
        if (CardContainerRef != null)
            CardContainerRef.HoldCard(this);
    }
    
    public string GetString()
    {
        return CardName;
    }
    
    protected override bool CanStartHovering()
    {
        return HoveringCardCount == 0 && HoldingCardCount == 0;
    }
    
    protected override void HandleMousePressed()
    {
        CardContainerRef.OnCardPressed(this);
        base.HandleMousePressed();
    }
    
    protected override void HandleMouseReleased()
    {
        base.HandleMouseReleased();
        if (CardContainerRef != null)
            CardContainerRef.ReleaseHoldingCards();
    }
}
