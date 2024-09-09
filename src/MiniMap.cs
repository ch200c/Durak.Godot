using Godot;

namespace Durak.Godot;

public partial class MiniMap : SubViewportContainer
{
    private float _defaultAnchorLeft;
    private float _defaultAnchorBottom;
    private float _defaultAnchorTop;
    private float _defaultAnchorRight;
    private Vector2 _defaultSize;
    private Vector2 _defaultPosition;
    private bool _isFullSize = false;

    public override void _Ready()
    {
        Update();

        GetViewport().SizeChanged += MiniMap_SizeChanged;
    }

    private void Update()
    {
        _defaultAnchorLeft = AnchorLeft;
        _defaultAnchorBottom = AnchorBottom;
        _defaultAnchorTop = AnchorTop;
        _defaultAnchorRight = AnchorRight;
        _defaultSize = Size;
        _defaultPosition = Position;
    }

    private void MiniMap_SizeChanged()
    {


    }

    private void _on_gui_input(InputEvent @event)
    {
        if (@event.IsActionPressed("left_mouse_button"))
        {
            if (_isFullSize)
            {
                SetSize(_defaultSize);
                AnchorLeft = _defaultAnchorLeft;
                AnchorBottom = _defaultAnchorBottom;
                AnchorRight = _defaultAnchorRight;
                AnchorTop = _defaultAnchorTop;
                SetPosition(_defaultPosition);
            }
            else
            {
                var fullWindow = GetViewport().GetVisibleRect().Size;
                SetSize(new Vector2(fullWindow.X, fullWindow.Y * 0.6f));
                SetPosition(new Vector2(0, 0));
            }

            _isFullSize = !_isFullSize;
        }
    }
}