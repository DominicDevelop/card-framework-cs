using Godot;
using System.Collections.Generic;

namespace CardFramework;

/// <summary>
/// A draggable object that supports mouse interaction with state-based animation system.
///
/// This class provides a robust state machine for handling mouse interactions including
/// hover effects, drag operations, and programmatic movement using Tween animations.
/// All interactive cards and objects extend this base class to inherit consistent
/// drag-and-drop behavior.
///
/// Key Features:
/// - State machine with safe transitions (Idle -> Hovering -> Holding -> Moving)
/// - Tween-based animations for smooth hover effects and movement
/// - Mouse interaction handling with proper event management
/// - Z-index management for visual layering during interactions
/// - Extensible design with virtual methods for customization
///
/// State Transitions:
/// - Idle: Default state, ready for interaction
/// - Hovering: Mouse over with visual feedback (scale, rotation, position)
/// - Holding: Active drag state following mouse movement
/// - Moving: Programmatic movement ignoring user input
/// </summary>
public partial class DraggableObject : Control
{
    /// <summary>Enumeration of possible interaction states for the draggable object.</summary>
    public enum DraggableState
    {
        /// <summary>Default state - no interaction</summary>
        Idle,
        /// <summary>Mouse over state - visual feedback</summary>
        Hovering,
        /// <summary>Dragging state - follows mouse</summary>
        Holding,
        /// <summary>Programmatic move state - ignores input</summary>
        Moving
    }

    #region Settings (visible in Inspector)

    /// <summary>The speed at which the object moves.</summary>
    [Export] public int MovingSpeed { get; set; } = (int)CardFrameworkSettings.AnimationMoveSpeed;
    /// <summary>Whether the object can be interacted with.</summary>
    [Export] public bool CanBeInteractedWith { get; set; } = true;
    /// <summary>The distance the object hovers when interacted with.</summary>
    [Export] public int HoverDistance { get; set; } = (int)CardFrameworkSettings.PhysicsHoverDistance;
    /// <summary>The scale multiplier when hovering.</summary>
    [Export] public float HoverScale { get; set; } = CardFrameworkSettings.AnimationHoverScale;
    /// <summary>The rotation in degrees when hovering.</summary>
    [Export] public float HoverRotation { get; set; } = CardFrameworkSettings.AnimationHoverRotation;
    /// <summary>The duration for hover animations.</summary>
    [Export] public float HoverDuration { get; set; } = CardFrameworkSettings.AnimationHoverDuration;

    #endregion

    // Legacy variables - kept for compatibility but no longer used in state machine
    public bool IsPressed { get; set; }
    public bool IsHolding { get; set; }

    private int _storedZIndex;
    public int StoredZIndex
    {
        get => _storedZIndex;
        set
        {
            ZIndex = value;
            _storedZIndex = value;
        }
    }

    // State Machine
    public DraggableState CurrentState { get; set; } = DraggableState.Idle;

    // Mouse tracking
    public bool IsMouseInside { get; set; }

    // Movement state tracking
    public bool IsMovingToDestination { get; set; }
    public bool IsReturningToOriginal { get; set; }

    // Position and animation tracking
    public Vector2 CurrentHoldingMousePosition { get; set; }
    public Vector2 OriginalPosition { get; set; }
    public Vector2 OriginalScale { get; set; }
    public float OriginalHoverRotation { get; set; }
    public Vector2 CurrentHoverPosition { get; set; }

    // Move operation tracking
    public Vector2 TargetDestination { get; set; }
    public float TargetRotation { get; set; }
    public Vector2 OriginalDestination { get; set; }
    public float OriginalRotationValue { get; set; }
    public float DestinationDegree { get; set; }

    // Tween objects
    protected Tween MoveTween { get; set; }
    protected Tween HoverTween { get; set; }

    // State transition rules
    private static readonly Dictionary<DraggableState, DraggableState[]> AllowedTransitions = new()
    {
        { DraggableState.Idle, new[] { DraggableState.Hovering, DraggableState.Holding, DraggableState.Moving } },
        { DraggableState.Hovering, new[] { DraggableState.Idle, DraggableState.Holding, DraggableState.Moving } },
        { DraggableState.Holding, new[] { DraggableState.Idle, DraggableState.Moving } },
        { DraggableState.Moving, new[] { DraggableState.Idle } }
    };

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        MouseEntered += OnMouseEnter;
        MouseExited += OnMouseExit;
        GuiInput += OnGuiInput;

        OriginalDestination = GlobalPosition;
        OriginalRotationValue = Rotation;
        OriginalPosition = Position;
        OriginalScale = Scale;
        OriginalHoverRotation = Rotation;
        StoredZIndex = ZIndex;
    }

    public bool ChangeState(DraggableState newState)
    {
        if (newState == CurrentState)
            return true;

        if (!AllowedTransitions.TryGetValue(CurrentState, out var allowed))
            return false;

        bool found = false;
        foreach (var s in allowed)
        {
            if (s == newState) { found = true; break; }
        }
        if (!found)
            return false;

        ExitState(CurrentState);

        var oldState = CurrentState;
        CurrentState = newState;

        EnterState(newState, oldState);

        return true;
    }

    protected virtual void EnterState(DraggableState state, DraggableState fromState)
    {
        switch (state)
        {
            case DraggableState.Idle:
                ZIndex = StoredZIndex;
                MouseFilter = MouseFilterEnum.Stop;
                break;

            case DraggableState.Hovering:
                ZIndex = StoredZIndex + CardFrameworkSettings.VisualDragZOffset;
                StartHoverAnimation();
                break;

            case DraggableState.Holding:
                if (fromState == DraggableState.Hovering)
                    PreserveHoverPosition();

                CurrentHoldingMousePosition = GetLocalMousePosition();
                ZIndex = StoredZIndex + CardFrameworkSettings.VisualDragZOffset;
                Rotation = 0;
                break;

            case DraggableState.Moving:
                if (HoverTween != null && HoverTween.IsValid())
                {
                    HoverTween.Kill();
                    HoverTween = null;
                }
                ZIndex = StoredZIndex + CardFrameworkSettings.VisualDragZOffset;
                MouseFilter = MouseFilterEnum.Ignore;
                break;
        }
    }

    protected virtual void ExitState(DraggableState state)
    {
        switch (state)
        {
            case DraggableState.Hovering:
                ZIndex = StoredZIndex;
                StopHoverAnimation();
                break;

            case DraggableState.Holding:
                ZIndex = StoredZIndex;
                Scale = OriginalScale;
                Rotation = OriginalHoverRotation;
                break;

            case DraggableState.Moving:
                MouseFilter = MouseFilterEnum.Stop;
                break;
        }
    }

    public override void _Process(double delta)
    {
        if (CurrentState == DraggableState.Holding)
        {
            GlobalPosition = GetGlobalMousePosition() - CurrentHoldingMousePosition;
        }
    }

    private void FinishMove()
    {
        IsMovingToDestination = false;
        Rotation = DestinationDegree;

        if (!IsReturningToOriginal)
        {
            OriginalDestination = TargetDestination;
            OriginalRotationValue = TargetRotation;
        }

        IsReturningToOriginal = false;

        ChangeState(DraggableState.Idle);

        OnMoveDone();
    }

    protected virtual void OnMoveDone()
    {
    }

    private void StartHoverAnimation()
    {
        if (HoverTween != null && HoverTween.IsValid())
        {
            HoverTween.Kill();
            HoverTween = null;
            Position = OriginalPosition;
            Scale = OriginalScale;
            Rotation = OriginalHoverRotation;
        }

        OriginalPosition = Position;
        OriginalScale = Scale;
        OriginalHoverRotation = Rotation;
        CurrentHoverPosition = Position;

        HoverTween = CreateTween();
        HoverTween.SetParallel(true);

        var targetPosition = new Vector2(Position.X, Position.Y - HoverDistance);
        HoverTween.TweenProperty(this, "position", targetPosition, HoverDuration);
        HoverTween.TweenProperty(this, "scale", OriginalScale * HoverScale, HoverDuration);
        HoverTween.TweenProperty(this, "rotation", Mathf.DegToRad(HoverRotation), HoverDuration);
        HoverTween.TweenMethod(Callable.From<Vector2>(UpdateHoverPosition), Position, targetPosition, HoverDuration);
    }

    private void StopHoverAnimation()
    {
        if (HoverTween != null && HoverTween.IsValid())
        {
            HoverTween.Kill();
            HoverTween = null;
        }

        HoverTween = CreateTween();
        HoverTween.SetParallel(true);

        HoverTween.TweenProperty(this, "position", OriginalPosition, HoverDuration);
        HoverTween.TweenProperty(this, "scale", OriginalScale, HoverDuration);
        HoverTween.TweenProperty(this, "rotation", OriginalHoverRotation, HoverDuration);
        HoverTween.TweenMethod(Callable.From<Vector2>(UpdateHoverPosition), Position, OriginalPosition, HoverDuration);
    }

    private void UpdateHoverPosition(Vector2 pos)
    {
        CurrentHoverPosition = pos;
    }

    private void PreserveHoverPosition()
    {
        if (HoverTween != null && HoverTween.IsValid())
        {
            HoverTween.Kill();
            HoverTween = null;
        }

        Position = CurrentHoverPosition;
    }

    protected virtual bool CanStartHovering()
    {
        return true;
    }

    private void OnMouseEnter()
    {
        IsMouseInside = true;
        if (CanBeInteractedWith && CanStartHovering())
            ChangeState(DraggableState.Hovering);
    }

    private void OnMouseExit()
    {
        IsMouseInside = false;
        if (CurrentState == DraggableState.Hovering)
            ChangeState(DraggableState.Idle);
    }

    private void OnGuiInput(InputEvent @event)
    {
        if (!CanBeInteractedWith)
            return;

        if (@event is InputEventMouseButton mouseEvent)
            HandleMouseButton(mouseEvent);
    }

    public void Move(Vector2 targetDestination, float degree)
    {
        if (GlobalPosition == targetDestination && Rotation == degree)
            return;

        ChangeState(DraggableState.Moving);

        if (MoveTween != null && MoveTween.IsValid())
        {
            MoveTween.Kill();
            MoveTween = null;
        }

        TargetDestination = targetDestination;
        TargetRotation = degree;

        Rotation = 0;
        DestinationDegree = degree;
        IsMovingToDestination = true;

        var distance = GlobalPosition.DistanceTo(targetDestination);
        var duration = distance / MovingSpeed;

        MoveTween = CreateTween();
        MoveTween.TweenProperty(this, "global_position", targetDestination, duration);
        MoveTween.TweenCallback(Callable.From(FinishMove));
    }

    private void HandleMouseButton(InputEventMouseButton mouseEvent)
    {
        if (mouseEvent.ButtonIndex != MouseButton.Left)
            return;

        if (CurrentState == DraggableState.Moving)
            return;

        if (mouseEvent.IsPressed())
            HandleMousePressed();

        if (mouseEvent.IsReleased())
            HandleMouseReleased();
    }

    public void ReturnToOriginal()
    {
        IsReturningToOriginal = true;
        Move(OriginalDestination, OriginalRotationValue);
    }

    protected virtual void HandleMousePressed()
    {
        IsPressed = true;
        switch (CurrentState)
        {
            case DraggableState.Hovering:
                ChangeState(DraggableState.Holding);
                break;
            case DraggableState.Idle:
                if (IsMouseInside && CanBeInteractedWith && CanStartHovering())
                    ChangeState(DraggableState.Holding);
                break;
        }
    }

    protected virtual void HandleMouseReleased()
    {
        IsPressed = false;
        if (CurrentState == DraggableState.Holding)
            ChangeState(DraggableState.Idle);
    }
}
