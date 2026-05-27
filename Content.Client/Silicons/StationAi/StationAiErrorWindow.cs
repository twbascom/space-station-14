using System.Numerics;
using Content.Client.Resources;
using Robust.Client.Audio;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Audio;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Player;

namespace Content.Client.Silicons.StationAi;

public sealed class StationAiErrorWindow : BaseWindow
{
    private const float ScaleFactor = 2.0f;

    public StationAiErrorWindow()
    {
        SetSize = new Vector2(256 * ScaleFactor, 256 * ScaleFactor);
        MinSize = new Vector2(256 * ScaleFactor, 256 * ScaleFactor);
        RectClipContent = true;

        var resCache = IoCManager.Resolve<IResourceCache>();
        var entManager = IoCManager.Resolve<IEntityManager>();

        var windowBg = resCache.GetTexture("/Textures/Mobs/Silicon/windows/malf_window_error.png");
        var xspriteTex = resCache.GetTexture("/Textures/Mobs/Silicon/windows/xsprite.png");

        var normalTexture = new AtlasTexture(xspriteTex, UIBox2.FromDimensions(new Vector2(0, 0), new Vector2(11, 10)));
        var pressedTexture = new AtlasTexture(xspriteTex, UIBox2.FromDimensions(new Vector2(11, 0), new Vector2(11, 10)));

        var layout = new LayoutContainer
        {
            MouseFilter = MouseFilterMode.Pass
        };

        // Background texture rect
        var bg = new TextureRect
        {
            Texture = windowBg,
            Stretch = TextureRect.StretchMode.Scale,
            SetSize = new Vector2(256 * ScaleFactor, 256 * ScaleFactor),
            MouseFilter = MouseFilterMode.Ignore
        };
        layout.AddChild(bg);

        // Custom Close Button
        var closeButton = new TextureButton
        {
            TextureNormal = normalTexture,
            Scale = new Vector2(ScaleFactor, ScaleFactor)
        };
        closeButton.OnButtonDown += _ => closeButton.TextureNormal = pressedTexture;
        closeButton.OnButtonUp += _ => closeButton.TextureNormal = normalTexture;
        closeButton.OnPressed += _ => Close();
        LayoutContainer.SetPosition(closeButton, new Vector2(207 * ScaleFactor, 37 * ScaleFactor));
        layout.AddChild(closeButton);

        AddChild(layout);

        // Play errorsfx.ogg when popup activates
        var audioSystem = entManager.System<AudioSystem>();
        audioSystem.PlayGlobal(new SoundPathSpecifier("/Audio/Silicon/errorsfx.ogg"), Filter.Local(), false, AudioParams.Default.WithVolume(10f));
    }

    protected override DragMode GetDragModeFor(Vector2 relativeMousePos)
    {
        // Exclude the close button area from window dragging
        if (relativeMousePos.X >= 207 * ScaleFactor && relativeMousePos.X <= 218 * ScaleFactor &&
            relativeMousePos.Y >= 37 * ScaleFactor && relativeMousePos.Y <= 47 * ScaleFactor)
        {
            return DragMode.None;
        }

        // Drag only if clicked in the title bar area of the custom drawn window (X: 36..220, Y: 37..46)
        if (relativeMousePos.X >= 36 * ScaleFactor && relativeMousePos.X <= 220 * ScaleFactor &&
            relativeMousePos.Y >= 37 * ScaleFactor && relativeMousePos.Y <= 46 * ScaleFactor)
        {
            return DragMode.Move;
        }

        return DragMode.None;
    }
}
