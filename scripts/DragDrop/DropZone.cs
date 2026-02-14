using Godot;
using System.Collections.Generic;

namespace CardFramework;

/// <summary>
/// Interactive drop zone system with sensor partitioning and visual debugging.
///
/// DropZone provides sophisticated drag-and-drop target detection with configurable
/// sensor areas, partitioning systems, and visual debugging capabilities. It integrates
/// with CardContainer to enable precise card placement and reordering operations.
///
/// Key Features:
/// - Flexible sensor sizing and positioning with dynamic adjustment
/// - Vertical/horizontal partitioning for precise drop targeting
/// - Visual debugging with colored outlines and partition indicators
/// - Mouse detection with global coordinate transformation
/// - Accept type filtering for specific draggable object types
///
/// Partitioning System:
/// - Vertical partitions: Divide sensor into left-right sections for card ordering
/// - Horizontal partitions: Divide sensor into up-down sections for layered placement
/// - Dynamic outline generation for visual feedback during development
/// </summary>
public partial class DropZone : Control
{
    // Dynamic sensor properties with automatic UI synchronization

    /// <summary>Size of the drop sensor area.</summary>
    public Vector2 SensorSize
    {
        set
        {
            _sensor.Size = value;
            SensorOutline.Size = value;
        }
    }

    /// <summary>Position offset of the drop sensor relative to DropZone.</summary>
    public Vector2 SensorPosition
    {
        set
        {
            _sensor.Position = value;
            SensorOutline.Position = value;
        }
    }

    #region Sensor/Outline
    #region Obsolete Stuff
    /// <summary>
    /// Deprecated: Since it was designed to debug the sensor, please use SensorOutlineVisible instead.
    /// </summary>
    [System.Obsolete("Use SensorOutlineVisible instead.")]
    public Texture SensorTexture
    {
        set => ((TextureRect)_sensor).Texture = value as Texture2D;
    }

    /// <summary>
    /// Deprecated: Since it was designed to debug the sensor, please use SensorOutlineVisible instead.
    /// </summary>
    [System.Obsolete("Use SensorOutlineVisible instead.")]
    public bool SensorVisible
    {
        set => _sensor.Visible = value;
    }
    #endregion
    
    /// <summary>Controls visibility of debugging outlines for sensor and partitions.</summary>
    public bool SensorOutlineVisible
    {
        set
        {
            SensorOutline.Visible = value;
            foreach (var outline in _sensorPartitionOutlines)
                outline.Visible = value;
        }
    }
    
    #endregion

    #region Core drop zone configuration and state
    
    /// <summary>Array of accepted draggable object types (e.g., ["card", "token"]).</summary>
    public List<string> AcceptTypes { get; set; } = new();
    /// <summary>Original sensor size for restoration after dynamic changes.</summary>
    public Vector2 StoredSensorSize { get; set; }
    /// <summary>Original sensor position for restoration after dynamic changes.</summary>
    public Vector2 StoredSensorPosition { get; set; }
    /// <summary>Parent container that owns this drop zone.</summary>
    public Node Parent { get; set; }
    
    #endregion

    #region UI components
    
    /// <summary>Main sensor control for hit detection (invisible).</summary>
    private Control _sensor;
    /// <summary>Debug outline for visual sensor boundary indication.</summary>
    public ReferenceRect SensorOutline { get; private set; }
    /// <summary>Array of partition outline controls for debugging.</summary>
    private readonly List<ReferenceRect> _sensorPartitionOutlines = new();
    
    #endregion

    #region Partitioning system
    
    /// <summary>Global vertical lines to divide sensing partitions (left to right direction).</summary>
    public List<float> VerticalPartition { get; set; } = new();
    /// <summary>Global horizontal lines to divide sensing partitions (up to down direction).</summary>
    public List<float> HorizontalPartition { get; set; } = new();
    
    #endregion

    public void Init(Node parent, List<string> acceptTypes = null)
    {
        Parent = parent;
        AcceptTypes = acceptTypes ?? new List<string>();

        if (_sensor == null)
        {
            _sensor = new TextureRect();
            _sensor.Name = "Sensor";
            _sensor.MouseFilter = MouseFilterEnum.Ignore;
            _sensor.ZIndex = CardFrameworkSettings.VisualSensorZIndex;
            AddChild(_sensor);
        }

        if (SensorOutline == null)
        {
            SensorOutline = new ReferenceRect();
            SensorOutline.EditorOnly = false;
            SensorOutline.Name = "SensorOutline";
            SensorOutline.MouseFilter = MouseFilterEnum.Ignore;
            SensorOutline.BorderColor = CardFrameworkSettings.DebugOutlineColor;
            SensorOutline.ZIndex = CardFrameworkSettings.VisualOutlineZIndex;
            AddChild(SensorOutline);
        }

        StoredSensorSize = Vector2.Zero;
        StoredSensorPosition = Vector2.Zero;
        VerticalPartition = new List<float>();
        HorizontalPartition = new List<float>();
    }

    public bool CheckMouseIsInDropZone()
    {
        var mousePosition = GetGlobalMousePosition();
        return _sensor.GetGlobalRect().HasPoint(mousePosition);
    }

    public void SetSensor(Vector2 size, Vector2 position, Texture texture, bool visible)
    {
        SensorSize = size;
        SensorPosition = position;
        StoredSensorSize = size;
        StoredSensorPosition = position;
        SensorTexture = texture;
        SensorVisible = visible;
    }

    public void SetSensorSizeFlexibly(Vector2 size, Vector2 position)
    {
        SensorSize = size;
        SensorPosition = position;
    }

    public void ReturnSensorSize()
    {
        SensorSize = StoredSensorSize;
        SensorPosition = StoredSensorPosition;
    }

    public void ChangeSensorPositionWithOffset(Vector2 offset)
    {
        SensorPosition = StoredSensorPosition + offset;
    }

    public void SetVerticalPartitions(List<float> positions)
    {
        VerticalPartition = positions;

        foreach (var outline in _sensorPartitionOutlines)
            outline.QueueFree();
        _sensorPartitionOutlines.Clear();

        for (int i = 0; i < VerticalPartition.Count; i++)
        {
            var outline = new ReferenceRect();
            outline.EditorOnly = false;
            outline.Name = $"VerticalPartition{i}";
            outline.ZIndex = CardFrameworkSettings.VisualOutlineZIndex;
            outline.BorderColor = CardFrameworkSettings.DebugOutlineColor;
            outline.MouseFilter = MouseFilterEnum.Ignore;
            outline.Size = new Vector2(1, _sensor.Size.Y);

            var localX = VerticalPartition[i] - GlobalPosition.X;
            outline.Position = new Vector2(localX, _sensor.Position.Y);
            outline.Visible = SensorOutline.Visible;
            AddChild(outline);
            _sensorPartitionOutlines.Add(outline);
        }
    }

    public void SetHorizontalPartitions(List<float> positions)
    {
        HorizontalPartition = positions;

        foreach (var outline in _sensorPartitionOutlines)
            outline.QueueFree();
        _sensorPartitionOutlines.Clear();

        for (int i = 0; i < HorizontalPartition.Count; i++)
        {
            var outline = new ReferenceRect();
            outline.EditorOnly = false;
            outline.Name = $"HorizontalPartition{i}";
            outline.ZIndex = CardFrameworkSettings.VisualOutlineZIndex;
            outline.BorderColor = CardFrameworkSettings.DebugOutlineColor;
            outline.MouseFilter = MouseFilterEnum.Ignore;
            outline.Size = new Vector2(_sensor.Size.X, 1);

            var localY = HorizontalPartition[i] - GlobalPosition.Y;
            outline.Position = new Vector2(_sensor.Position.X, localY);
            outline.Visible = SensorOutline.Visible;
            AddChild(outline);
            _sensorPartitionOutlines.Add(outline);
        }
    }

    public int GetVerticalLayers()
    {
        if (!CheckMouseIsInDropZone())
            return -1;

        if (VerticalPartition == null || VerticalPartition.Count == 0)
            return -1;

        var mousePosition = GetGlobalMousePosition();
        int currentIndex = 0;

        for (int i = 0; i < VerticalPartition.Count; i++)
        {
            if (mousePosition.X >= VerticalPartition[i])
                currentIndex++;
            else
                break;
        }

        return currentIndex;
    }

    public int GetHorizontalLayers()
    {
        if (!CheckMouseIsInDropZone())
            return -1;

        if (HorizontalPartition == null || HorizontalPartition.Count == 0)
            return -1;

        var mousePosition = GetGlobalMousePosition();
        int currentIndex = 0;

        for (int i = 0; i < HorizontalPartition.Count; i++)
        {
            if (mousePosition.Y >= HorizontalPartition[i])
                currentIndex++;
            else
                break;
        }

        return currentIndex;
    }
}
