#region Assembly UnityEngine.UIModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// location unknown
// Decompiled with ICSharpCode.Decompiler 8.1.1.7464
#endregion

using System;
using System.Runtime.CompilerServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine;

//
// Summary:
//     Element that can be used for screen rendering.
[NativeHeader("Modules/UI/Canvas.h")]
[NativeHeader("Modules/UI/CanvasManager.h")]
[RequireComponent(typeof(RectTransform))]
[NativeClass("UI::Canvas")]
[NativeHeader("Modules/UI/UIStructs.h")]
public sealed class Canvas : Behaviour
{
    public delegate void WillRenderCanvases();

    //
    // Summary:
    //     Is the Canvas in World or Overlay mode?
    public extern RenderMode renderMode
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        get;
        [MethodImpl(MethodImplOptions.InternalCall)]
        set;
    }

    //
    // Summary:
    //     Is this the root Canvas?
    public extern bool isRootCanvas
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        get;
    }

    //
    // Summary:
    //     Get the render rect for the Canvas.
    public Rect pixelRect
    {
        get
        {
            get_pixelRect_Injected(out var ret);
            return ret;
        }
    }

    //
    // Summary:
    //     Used to scale the entire canvas, while still making it fit the screen. Only applies
    //     with renderMode is Screen Space.
    public extern float scaleFactor
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        get;
        [MethodImpl(MethodImplOptions.InternalCall)]
        set;
    }

    //
    // Summary:
    //     The number of pixels per unit that is considered the default.
    public extern float referencePixelsPerUnit
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        get;
        [MethodImpl(MethodImplOptions.InternalCall)]
        set;
    }

    //
    // Summary:
    //     Allows for nested canvases to override pixelPerfect settings inherited from parent
    //     canvases.
    public extern bool overridePixelPerfect
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        get;
        [MethodImpl(MethodImplOptions.InternalCall)]
        set;
    }

    //
    // Summary:
    //     Should the Canvas vertex color always be in gamma space before passing to the
    //     UI shaders in linear color space work flow.
    public extern bool vertexColorAlwaysGammaSpace
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        get;
        [MethodImpl(MethodImplOptions.InternalCall)]
        set;
    }

    //
    // Summary:
    //     Force elements in the canvas to be aligned with pixels. Only applies with renderMode
    //     is Screen Space.
    public extern bool pixelPerfect
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        get;
        [MethodImpl(MethodImplOptions.InternalCall)]
        set;
    }

    //
    // Summary:
    //     How far away from the camera is the Canvas generated.
    public extern float planeDistance
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        get;
        [MethodImpl(MethodImplOptions.InternalCall)]
        set;
    }

    //
    // Summary:
    //     The render order in which the canvas is being emitted to the Scene. (Read Only)
    public extern int renderOrder
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        get;
    }

    //
    // Summary:
    //     Override the sorting of canvas.
    public extern bool overrideSorting
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        get;
        [MethodImpl(MethodImplOptions.InternalCall)]
        set;
    }

    //
    // Summary:
    //     Canvas' order within a sorting layer.
    public extern int sortingOrder
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        get;
        [MethodImpl(MethodImplOptions.InternalCall)]
        set;
    }

    //
    // Summary:
    //     For Overlay mode, display index on which the UI canvas will appear.
    public extern int targetDisplay
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        get;
        [MethodImpl(MethodImplOptions.InternalCall)]
        set;
    }

    //
    // Summary:
    //     Unique ID of the Canvas' sorting layer.
    public extern int sortingLayerID
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        get;
        [MethodImpl(MethodImplOptions.InternalCall)]
        set;
    }

    //
    // Summary:
    //     Cached calculated value based upon SortingLayerID.
    public extern int cachedSortingLayerValue
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        get;
    }

    //
    // Summary:
    //     Get or set the mask of additional shader channels to be used when creating the
    //     Canvas mesh.
    public extern AdditionalCanvasShaderChannels additionalShaderChannels
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        get;
        [MethodImpl(MethodImplOptions.InternalCall)]
        set;
    }

    //
    // Summary:
    //     Name of the Canvas' sorting layer.
    public extern string sortingLayerName
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        get;
        [MethodImpl(MethodImplOptions.InternalCall)]
        set;
    }

    //
    // Summary:
    //     Returns the Canvas closest to root, by checking through each parent and returning
    //     the last canvas found. If no other canvas is found then the canvas will return
    //     itself.
    public extern Canvas rootCanvas
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        get;
    }

    //
    // Summary:
    //     Returns the canvas display size based on the selected render mode and target
    //     display.
    public Vector2 renderingDisplaySize
    {
        get
        {
            get_renderingDisplaySize_Injected(out var ret);
            return ret;
        }
    }

    //
    // Summary:
    //     Should the Canvas size be updated based on the render target when a manual Camera.Render
    //     call is performed.
    public extern StandaloneRenderResize updateRectTransformForStandalone
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        get;
        [MethodImpl(MethodImplOptions.InternalCall)]
        set;
    }

    internal static Action<int> externBeginRenderOverlays { get; set; }

    internal static Action<int, int> externRenderOverlaysBefore { get; set; }

    internal static Action<int> externEndRenderOverlays { get; set; }

    //
    // Summary:
    //     Camera used for sizing the Canvas when in Screen Space - Camera. Also used as
    //     the Camera that events will be sent through for a World Space Canvas.
    [NativeProperty("Camera", false, TargetType.Function)]
    public extern Camera worldCamera
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        get;
        [MethodImpl(MethodImplOptions.InternalCall)]
        set;
    }

    //
    // Summary:
    //     The normalized grid size that the canvas will split the renderable area into.
    [NativeProperty("SortingBucketNormalizedSize", false, TargetType.Function)]
    public extern float normalizedSortingGridSize
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        get;
        [MethodImpl(MethodImplOptions.InternalCall)]
        set;
    }

    //
    // Summary:
    //     The normalized grid size that the canvas will split the renderable area into.
    [Obsolete("Setting normalizedSize via a int is not supported. Please use normalizedSortingGridSize", false)]
    [NativeProperty("SortingBucketNormalizedSize", false, TargetType.Function)]
    public extern int sortingGridNormalizedSize
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        get;
        [MethodImpl(MethodImplOptions.InternalCall)]
        set;
    }

    internal extern byte stagePriority
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        get;
        [MethodImpl(MethodImplOptions.InternalCall)]
        set;
    }

    public static event WillRenderCanvases preWillRenderCanvases;

    public static event WillRenderCanvases willRenderCanvases;

    [MethodImpl(MethodImplOptions.InternalCall)]
    [FreeFunction("UI::CanvasManager::SetExternalCanvasEnabled")]
    internal static extern void SetExternalCanvasEnabled(bool enabled);

    //
    // Summary:
    //     Returns the default material that can be used for rendering text elements on
    //     the Canvas.
    [MethodImpl(MethodImplOptions.InternalCall)]
    [FreeFunction("UI::GetDefaultUIMaterial")]
    [Obsolete("Shared default material now used for text and general UI elements, call Canvas.GetDefaultCanvasMaterial()", false)]
    public static extern Material GetDefaultCanvasTextMaterial();

    //
    // Summary:
    //     Returns the default material that can be used for rendering normal elements on
    //     the Canvas.
    [MethodImpl(MethodImplOptions.InternalCall)]
    [FreeFunction("UI::GetDefaultUIMaterial")]
    public static extern Material GetDefaultCanvasMaterial();

    //
    // Summary:
    //     Gets or generates the ETC1 Material.
    //
    // Returns:
    //     The generated ETC1 Material from the Canvas.
    [MethodImpl(MethodImplOptions.InternalCall)]
    [FreeFunction("UI::GetETC1SupportedCanvasMaterial")]
    public static extern Material GetETC1SupportedCanvasMaterial();

    [MethodImpl(MethodImplOptions.InternalCall)]
    internal extern void UpdateCanvasRectTransform(bool alignWithCamera);

    //
    // Summary:
    //     Force all canvases to update their content.
    public static void ForceUpdateCanvases()
    {
        SendPreWillRenderCanvases();
        SendWillRenderCanvases();
    }

    [RequiredByNativeCode]
    private static void SendPreWillRenderCanvases()
    {
        Canvas.preWillRenderCanvases?.Invoke();
    }

    [RequiredByNativeCode]
    private static void SendWillRenderCanvases()
    {
        Canvas.willRenderCanvases?.Invoke();
    }

    [RequiredByNativeCode]
    private static void BeginRenderExtraOverlays(int displayIndex)
    {
        externBeginRenderOverlays?.Invoke(displayIndex);
    }

    [RequiredByNativeCode]
    private static void RenderExtraOverlaysBefore(int displayIndex, int sortingOrder)
    {
        externRenderOverlaysBefore?.Invoke(displayIndex, sortingOrder);
    }

    [RequiredByNativeCode]
    private static void EndRenderExtraOverlays(int displayIndex)
    {
        externEndRenderOverlays?.Invoke(displayIndex);
    }

    [MethodImpl(MethodImplOptions.InternalCall)]
    [SpecialName]
    private extern void get_pixelRect_Injected(out Rect ret);

    [MethodImpl(MethodImplOptions.InternalCall)]
    [SpecialName]
    private extern void get_renderingDisplaySize_Injected(out Vector2 ret);
}
#if false // Decompilation log
'243' items in cache
------------------
Resolve: 'netstandard, Version=2.1.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Found single assembly: 'netstandard, Version=2.1.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Load from: 'F:\Unity\Unity Editor\2022.3.20f1c1\Editor\Data\NetStandard\ref\2.1.0\netstandard.dll'
------------------
Resolve: 'UnityEngine.SharedInternalsModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'UnityEngine.SharedInternalsModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'F:\Unity\Unity Editor\2022.3.20f1c1\Editor\Data\Managed\UnityEngine\UnityEngine.SharedInternalsModule.dll'
------------------
Resolve: 'UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'F:\Unity\Unity Editor\2022.3.20f1c1\Editor\Data\Managed\UnityEngine\UnityEngine.CoreModule.dll'
------------------
Resolve: 'UnityEngine.TextRenderingModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'UnityEngine.TextRenderingModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'F:\Unity\Unity Editor\2022.3.20f1c1\Editor\Data\Managed\UnityEngine\UnityEngine.TextRenderingModule.dll'
------------------
Resolve: 'System.Runtime.InteropServices, Version=2.1.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'System.Runtime.InteropServices, Version=4.1.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '2.1.0.0', Got: '4.1.2.0'
Load from: 'F:\Unity\Unity Editor\2022.3.20f1c1\Editor\Data\NetStandard\compat\2.1.0\shims\netstandard\System.Runtime.InteropServices.dll'
------------------
Resolve: 'System.Runtime.CompilerServices.Unsafe, Version=2.1.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'System.Runtime.CompilerServices.Unsafe, Version=2.1.0.0, Culture=neutral, PublicKeyToken=null'
#endif
