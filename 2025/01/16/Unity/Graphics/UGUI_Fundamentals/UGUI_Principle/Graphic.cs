#region Assembly UnityEngine.UI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// location unknown
// Decompiled with ICSharpCode.Decompiler 8.1.1.7464
#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Pool;
using UnityEngine.Serialization;
using UnityEngine.UI.CoroutineTween;

namespace UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
[ExecuteAlways]
public abstract class Graphic : UIBehaviour, ICanvasElement
{
    protected static Material s_DefaultUI = null;

    protected static Texture2D s_WhiteTexture = null;

    [FormerlySerializedAs("m_Mat")]
    [SerializeField]
    protected Material m_Material;

    [SerializeField]
    private Color m_Color = Color.white;

    [NonSerialized]
    protected bool m_SkipLayoutUpdate;

    [NonSerialized]
    protected bool m_SkipMaterialUpdate;

    [SerializeField]
    private bool m_RaycastTarget = true;

    private bool m_RaycastTargetCache = true;

    [SerializeField]
    private Vector4 m_RaycastPadding;

    [NonSerialized]
    private RectTransform m_RectTransform;

    [NonSerialized]
    private CanvasRenderer m_CanvasRenderer;

    [NonSerialized]
    private Canvas m_Canvas;

    [NonSerialized]
    private bool m_VertsDirty;

    [NonSerialized]
    private bool m_MaterialDirty;

    [NonSerialized]
    protected UnityAction m_OnDirtyLayoutCallback;

    [NonSerialized]
    protected UnityAction m_OnDirtyVertsCallback;

    [NonSerialized]
    protected UnityAction m_OnDirtyMaterialCallback;

    [NonSerialized]
    protected static Mesh s_Mesh;

    [NonSerialized]
    private static readonly VertexHelper s_VertexHelper = new VertexHelper();

    [NonSerialized]
    protected Mesh m_CachedMesh;

    [NonSerialized]
    protected Vector2[] m_CachedUvs;

    [NonSerialized]
    private readonly TweenRunner<ColorTween> m_ColorTweenRunner;

    public static Material defaultGraphicMaterial
    {
        get
        {
            if (s_DefaultUI == null)
            {
                s_DefaultUI = Canvas.GetDefaultCanvasMaterial();
            }

            return s_DefaultUI;
        }
    }

    public virtual Color color
    {
        get
        {
            return m_Color;
        }
        set
        {
            if (SetPropertyUtility.SetColor(ref m_Color, value))
            {
                SetVerticesDirty();
            }
        }
    }

    public virtual bool raycastTarget
    {
        get
        {
            return m_RaycastTarget;
        }
        set
        {
            if (value != m_RaycastTarget)
            {
                if (m_RaycastTarget)
                {
                    GraphicRegistry.UnregisterRaycastGraphicForCanvas(canvas, this);
                }

                m_RaycastTarget = value;
                if (m_RaycastTarget && base.isActiveAndEnabled)
                {
                    GraphicRegistry.RegisterRaycastGraphicForCanvas(canvas, this);
                }
            }

            m_RaycastTargetCache = value;
        }
    }

    public Vector4 raycastPadding
    {
        get
        {
            return m_RaycastPadding;
        }
        set
        {
            m_RaycastPadding = value;
        }
    }

    protected bool useLegacyMeshGeneration { get; set; }

    public int depth => canvasRenderer.absoluteDepth;

    public RectTransform rectTransform
    {
        get
        {
            if ((object)m_RectTransform == null)
            {
                m_RectTransform = GetComponent<RectTransform>();
            }

            return m_RectTransform;
        }
    }

    public Canvas canvas
    {
        get
        {
            if (m_Canvas == null)
            {
                CacheCanvas();
            }

            return m_Canvas;
        }
    }

    public CanvasRenderer canvasRenderer
    {
        get
        {
            if ((object)m_CanvasRenderer == null)
            {
                m_CanvasRenderer = GetComponent<CanvasRenderer>();
                if ((object)m_CanvasRenderer == null)
                {
                    m_CanvasRenderer = base.gameObject.AddComponent<CanvasRenderer>();
                }
            }

            return m_CanvasRenderer;
        }
    }

    public virtual Material defaultMaterial => defaultGraphicMaterial;

    public virtual Material material
    {
        get
        {
            if (!(m_Material != null))
            {
                return defaultMaterial;
            }

            return m_Material;
        }
        set
        {
            if (!(m_Material == value))
            {
                m_Material = value;
                SetMaterialDirty();
            }
        }
    }

    public virtual Material materialForRendering
    {
        get
        {
            List<IMaterialModifier> list = CollectionPool<List<IMaterialModifier>, IMaterialModifier>.Get();
            GetComponents(list);
            Material modifiedMaterial = material;
            for (int i = 0; i < list.Count; i++)
            {
                modifiedMaterial = list[i].GetModifiedMaterial(modifiedMaterial);
            }

            CollectionPool<List<IMaterialModifier>, IMaterialModifier>.Release(list);
            return modifiedMaterial;
        }
    }

    public virtual Texture mainTexture => s_WhiteTexture;

    protected static Mesh workerMesh
    {
        get
        {
            if (s_Mesh == null)
            {
                s_Mesh = new Mesh();
                s_Mesh.name = "Shared UI Mesh";
            }

            return s_Mesh;
        }
    }

    protected Graphic()
    {
        if (m_ColorTweenRunner == null)
        {
            m_ColorTweenRunner = new TweenRunner<ColorTween>();
        }

        m_ColorTweenRunner.Init(this);
        useLegacyMeshGeneration = true;
    }

    public virtual void SetAllDirty()
    {
        if (m_SkipLayoutUpdate)
        {
            m_SkipLayoutUpdate = false;
        }
        else
        {
            SetLayoutDirty();
        }

        if (m_SkipMaterialUpdate)
        {
            m_SkipMaterialUpdate = false;
        }
        else
        {
            SetMaterialDirty();
        }

        SetVerticesDirty();
        SetRaycastDirty();
    }

    public virtual void SetLayoutDirty()
    {
        if (IsActive())
        {
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
            if (m_OnDirtyLayoutCallback != null)
            {
                m_OnDirtyLayoutCallback();
            }
        }
    }

    public virtual void SetVerticesDirty()
    {
        if (IsActive())
        {
            m_VertsDirty = true;
            CanvasUpdateRegistry.RegisterCanvasElementForGraphicRebuild(this);
            if (m_OnDirtyVertsCallback != null)
            {
                m_OnDirtyVertsCallback();
            }
        }
    }

    public virtual void SetMaterialDirty()
    {
        if (IsActive())
        {
            m_MaterialDirty = true;
            CanvasUpdateRegistry.RegisterCanvasElementForGraphicRebuild(this);
            if (m_OnDirtyMaterialCallback != null)
            {
                m_OnDirtyMaterialCallback();
            }
        }
    }

    public void SetRaycastDirty()
    {
        if (m_RaycastTargetCache != m_RaycastTarget)
        {
            if (m_RaycastTarget && base.isActiveAndEnabled)
            {
                GraphicRegistry.RegisterRaycastGraphicForCanvas(canvas, this);
            }
            else if (!m_RaycastTarget)
            {
                GraphicRegistry.UnregisterRaycastGraphicForCanvas(canvas, this);
            }
        }

        m_RaycastTargetCache = m_RaycastTarget;
    }

    protected override void OnRectTransformDimensionsChange()
    {
        if (base.gameObject.activeInHierarchy)
        {
            if (CanvasUpdateRegistry.IsRebuildingLayout())
            {
                SetVerticesDirty();
                return;
            }

            SetVerticesDirty();
            SetLayoutDirty();
        }
    }

    protected override void OnBeforeTransformParentChanged()
    {
        GraphicRegistry.UnregisterGraphicForCanvas(canvas, this);
        LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
    }

    protected override void OnTransformParentChanged()
    {
        base.OnTransformParentChanged();
        m_Canvas = null;
        if (IsActive())
        {
            CacheCanvas();
            GraphicRegistry.RegisterGraphicForCanvas(canvas, this);
            SetAllDirty();
        }
    }

    private void CacheCanvas()
    {
        List<Canvas> list = CollectionPool<List<Canvas>, Canvas>.Get();
        base.gameObject.GetComponentsInParent(includeInactive: false, list);
        if (list.Count > 0)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].isActiveAndEnabled)
                {
                    m_Canvas = list[i];
                    break;
                }

                if (i == list.Count - 1)
                {
                    m_Canvas = null;
                }
            }
        }
        else
        {
            m_Canvas = null;
        }

        CollectionPool<List<Canvas>, Canvas>.Release(list);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        CacheCanvas();
        GraphicRegistry.RegisterGraphicForCanvas(canvas, this);
        GraphicRebuildTracker.TrackGraphic(this);
        if (s_WhiteTexture == null)
        {
            s_WhiteTexture = Texture2D.whiteTexture;
        }

        SetAllDirty();
    }

    protected override void OnDisable()
    {
        GraphicRebuildTracker.UnTrackGraphic(this);
        GraphicRegistry.DisableGraphicForCanvas(canvas, this);
        CanvasUpdateRegistry.DisableCanvasElementForRebuild(this);
        if (canvasRenderer != null)
        {
            canvasRenderer.Clear();
        }

        LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        base.OnDisable();
    }

    protected override void OnDestroy()
    {
        GraphicRebuildTracker.UnTrackGraphic(this);
        GraphicRegistry.UnregisterGraphicForCanvas(canvas, this);
        CanvasUpdateRegistry.UnRegisterCanvasElementForRebuild(this);
        if ((bool)m_CachedMesh)
        {
            Object.Destroy(m_CachedMesh);
        }

        m_CachedMesh = null;
        base.OnDestroy();
    }

    protected override void OnCanvasHierarchyChanged()
    {
        Canvas canvas = m_Canvas;
        m_Canvas = null;
        if (!IsActive())
        {
            GraphicRegistry.UnregisterGraphicForCanvas(canvas, this);
            return;
        }

        CacheCanvas();
        if (canvas != m_Canvas)
        {
            GraphicRegistry.UnregisterGraphicForCanvas(canvas, this);
            if (IsActive())
            {
                GraphicRegistry.RegisterGraphicForCanvas(this.canvas, this);
            }
        }
    }

    public virtual void OnCullingChanged()
    {
        if (!canvasRenderer.cull && (m_VertsDirty || m_MaterialDirty))
        {
            CanvasUpdateRegistry.RegisterCanvasElementForGraphicRebuild(this);
        }
    }

    public virtual void Rebuild(CanvasUpdate update)
    {
        if (!(canvasRenderer == null) && !canvasRenderer.cull && update == CanvasUpdate.PreRender)
        {
            if (m_VertsDirty)
            {
                UpdateGeometry();
                m_VertsDirty = false;
            }

            if (m_MaterialDirty)
            {
                UpdateMaterial();
                m_MaterialDirty = false;
            }
        }
    }

    public virtual void LayoutComplete()
    {
    }

    public virtual void GraphicUpdateComplete()
    {
    }

    protected virtual void UpdateMaterial()
    {
        if (IsActive())
        {
            canvasRenderer.materialCount = 1;
            canvasRenderer.SetMaterial(materialForRendering, 0);
            canvasRenderer.SetTexture(mainTexture);
        }
    }

    protected virtual void UpdateGeometry()
    {
        if (useLegacyMeshGeneration)
        {
            DoLegacyMeshGeneration();
        }
        else
        {
            DoMeshGeneration();
        }
    }

    private void DoMeshGeneration()
    {
        if (rectTransform != null && rectTransform.rect.width >= 0f && rectTransform.rect.height >= 0f)
        {
            OnPopulateMesh(s_VertexHelper);
        }
        else
        {
            s_VertexHelper.Clear();
        }

        List<Component> list = CollectionPool<List<Component>, Component>.Get();
        GetComponents(typeof(IMeshModifier), list);
        for (int i = 0; i < list.Count; i++)
        {
            ((IMeshModifier)list[i]).ModifyMesh(s_VertexHelper);
        }

        CollectionPool<List<Component>, Component>.Release(list);
        s_VertexHelper.FillMesh(workerMesh);
        canvasRenderer.SetMesh(workerMesh);
    }

    private void DoLegacyMeshGeneration()
    {
        if (rectTransform != null && rectTransform.rect.width >= 0f && rectTransform.rect.height >= 0f)
        {
            OnPopulateMesh(workerMesh);
        }
        else
        {
            workerMesh.Clear();
        }

        List<Component> list = CollectionPool<List<Component>, Component>.Get();
        GetComponents(typeof(IMeshModifier), list);
        for (int i = 0; i < list.Count; i++)
        {
            ((IMeshModifier)list[i]).ModifyMesh(workerMesh);
        }

        CollectionPool<List<Component>, Component>.Release(list);
        canvasRenderer.SetMesh(workerMesh);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Use OnPopulateMesh instead.", true)]
    protected virtual void OnFillVBO(List<UIVertex> vbo)
    {
    }

    [Obsolete("Use OnPopulateMesh(VertexHelper vh) instead.", false)]
    protected virtual void OnPopulateMesh(Mesh m)
    {
        OnPopulateMesh(s_VertexHelper);
        s_VertexHelper.FillMesh(m);
    }

    protected virtual void OnPopulateMesh(VertexHelper vh)
    {
        Rect pixelAdjustedRect = GetPixelAdjustedRect();
        Vector4 vector = new Vector4(pixelAdjustedRect.x, pixelAdjustedRect.y, pixelAdjustedRect.x + pixelAdjustedRect.width, pixelAdjustedRect.y + pixelAdjustedRect.height);
        Color32 color = this.color;
        vh.Clear();
        vh.AddVert(new Vector3(vector.x, vector.y), color, new Vector2(0f, 0f));
        vh.AddVert(new Vector3(vector.x, vector.w), color, new Vector2(0f, 1f));
        vh.AddVert(new Vector3(vector.z, vector.w), color, new Vector2(1f, 1f));
        vh.AddVert(new Vector3(vector.z, vector.y), color, new Vector2(1f, 0f));
        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(2, 3, 0);
    }

    public virtual void OnRebuildRequested()
    {
        m_SkipLayoutUpdate = true;
        MonoBehaviour[] components = base.gameObject.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour monoBehaviour in components)
        {
            if (!(monoBehaviour == null))
            {
                MethodInfo method = monoBehaviour.GetType().GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (method != null)
                {
                    method.Invoke(monoBehaviour, null);
                }
            }
        }

        m_SkipLayoutUpdate = false;
    }

    protected override void Reset()
    {
        SetAllDirty();
    }

    protected override void OnDidApplyAnimationProperties()
    {
        SetAllDirty();
    }

    public virtual void SetNativeSize()
    {
    }

    public virtual bool Raycast(Vector2 sp, Camera eventCamera)
    {
        if (!base.isActiveAndEnabled)
        {
            return false;
        }

        Transform transform = base.transform;
        List<Component> list = CollectionPool<List<Component>, Component>.Get();
        bool flag = false;
        bool flag2 = true;
        while (transform != null)
        {
            transform.GetComponents(list);
            for (int i = 0; i < list.Count; i++)
            {
                Canvas canvas = list[i] as Canvas;
                if (canvas != null && canvas.overrideSorting)
                {
                    flag2 = false;
                }

                if (!(list[i] is ICanvasRaycastFilter canvasRaycastFilter))
                {
                    continue;
                }

                bool flag3 = true;
                CanvasGroup canvasGroup = list[i] as CanvasGroup;
                if (canvasGroup != null)
                {
                    if (!canvasGroup.enabled)
                    {
                        continue;
                    }

                    if (!flag && canvasGroup.ignoreParentGroups)
                    {
                        flag = true;
                        flag3 = canvasRaycastFilter.IsRaycastLocationValid(sp, eventCamera);
                    }
                    else if (!flag)
                    {
                        flag3 = canvasRaycastFilter.IsRaycastLocationValid(sp, eventCamera);
                    }
                }
                else
                {
                    flag3 = canvasRaycastFilter.IsRaycastLocationValid(sp, eventCamera);
                }

                if (!flag3)
                {
                    CollectionPool<List<Component>, Component>.Release(list);
                    return false;
                }
            }

            transform = (flag2 ? transform.parent : null);
        }

        CollectionPool<List<Component>, Component>.Release(list);
        return true;
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        SetAllDirty();
    }

    public Vector2 PixelAdjustPoint(Vector2 point)
    {
        if (!canvas || canvas.renderMode == RenderMode.WorldSpace || canvas.scaleFactor == 0f || !canvas.pixelPerfect)
        {
            return point;
        }

        return RectTransformUtility.PixelAdjustPoint(point, base.transform, canvas);
    }

    public Rect GetPixelAdjustedRect()
    {
        if (!canvas || canvas.renderMode == RenderMode.WorldSpace || canvas.scaleFactor == 0f || !canvas.pixelPerfect)
        {
            return rectTransform.rect;
        }

        return RectTransformUtility.PixelAdjustRect(rectTransform, canvas);
    }

    public virtual void CrossFadeColor(Color targetColor, float duration, bool ignoreTimeScale, bool useAlpha)
    {
        CrossFadeColor(targetColor, duration, ignoreTimeScale, useAlpha, useRGB: true);
    }

    public virtual void CrossFadeColor(Color targetColor, float duration, bool ignoreTimeScale, bool useAlpha, bool useRGB)
    {
        if (!(canvasRenderer == null) && (useRGB || useAlpha))
        {
            if (canvasRenderer.GetColor().Equals(targetColor))
            {
                m_ColorTweenRunner.StopTween();
                return;
            }

            ColorTween.ColorTweenMode tweenMode = ((!(useRGB && useAlpha)) ? (useRGB ? ColorTween.ColorTweenMode.RGB : ColorTween.ColorTweenMode.Alpha) : ColorTween.ColorTweenMode.All);
            ColorTween colorTween = default(ColorTween);
            colorTween.duration = duration;
            colorTween.startColor = canvasRenderer.GetColor();
            colorTween.targetColor = targetColor;
            ColorTween info = colorTween;
            info.AddOnChangedCallback(canvasRenderer.SetColor);
            info.ignoreTimeScale = ignoreTimeScale;
            info.tweenMode = tweenMode;
            m_ColorTweenRunner.StartTween(info);
        }
    }

    private static Color CreateColorFromAlpha(float alpha)
    {
        Color black = Color.black;
        black.a = alpha;
        return black;
    }

    public virtual void CrossFadeAlpha(float alpha, float duration, bool ignoreTimeScale)
    {
        CrossFadeColor(CreateColorFromAlpha(alpha), duration, ignoreTimeScale, useAlpha: true, useRGB: false);
    }

    public void RegisterDirtyLayoutCallback(UnityAction action)
    {
        m_OnDirtyLayoutCallback = (UnityAction)Delegate.Combine(m_OnDirtyLayoutCallback, action);
    }

    public void UnregisterDirtyLayoutCallback(UnityAction action)
    {
        m_OnDirtyLayoutCallback = (UnityAction)Delegate.Remove(m_OnDirtyLayoutCallback, action);
    }

    public void RegisterDirtyVerticesCallback(UnityAction action)
    {
        m_OnDirtyVertsCallback = (UnityAction)Delegate.Combine(m_OnDirtyVertsCallback, action);
    }

    public void UnregisterDirtyVerticesCallback(UnityAction action)
    {
        m_OnDirtyVertsCallback = (UnityAction)Delegate.Remove(m_OnDirtyVertsCallback, action);
    }

    public void RegisterDirtyMaterialCallback(UnityAction action)
    {
        m_OnDirtyMaterialCallback = (UnityAction)Delegate.Combine(m_OnDirtyMaterialCallback, action);
    }

    public void UnregisterDirtyMaterialCallback(UnityAction action)
    {
        m_OnDirtyMaterialCallback = (UnityAction)Delegate.Remove(m_OnDirtyMaterialCallback, action);
    }

    Transform ICanvasElement.get_transform()
    {
        return base.transform;
    }
}
#if false // Decompilation log
'243' items in cache
------------------
Resolve: 'netstandard, Version=2.1.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Found single assembly: 'netstandard, Version=2.1.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Load from: 'F:\Unity\Unity Editor\2022.3.20f1c1\Editor\Data\NetStandard\ref\2.1.0\netstandard.dll'
------------------
Resolve: 'UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'F:\Unity\Unity Editor\2022.3.20f1c1\Editor\Data\Managed\UnityEngine\UnityEngine.CoreModule.dll'
------------------
Resolve: 'UnityEngine.UIModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'UnityEngine.UIModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'F:\Unity\Unity Editor\2022.3.20f1c1\Editor\Data\Managed\UnityEngine\UnityEngine.UIModule.dll'
------------------
Resolve: 'UnityEngine.TextRenderingModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'UnityEngine.TextRenderingModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'F:\Unity\Unity Editor\2022.3.20f1c1\Editor\Data\Managed\UnityEngine\UnityEngine.TextRenderingModule.dll'
------------------
Resolve: 'UnityEngine.PhysicsModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'UnityEngine.PhysicsModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'F:\Unity\Unity Editor\2022.3.20f1c1\Editor\Data\Managed\UnityEngine\UnityEngine.PhysicsModule.dll'
------------------
Resolve: 'UnityEngine.Physics2DModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'UnityEngine.Physics2DModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'F:\Unity\Unity Editor\2022.3.20f1c1\Editor\Data\Managed\UnityEngine\UnityEngine.Physics2DModule.dll'
------------------
Resolve: 'UnityEngine.IMGUIModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'UnityEngine.IMGUIModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'F:\Unity\Unity Editor\2022.3.20f1c1\Editor\Data\Managed\UnityEngine\UnityEngine.IMGUIModule.dll'
------------------
Resolve: 'UnityEngine.AnimationModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'UnityEngine.AnimationModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'F:\Unity\Unity Editor\2022.3.20f1c1\Editor\Data\Managed\UnityEngine\UnityEngine.AnimationModule.dll'
------------------
Resolve: 'UnityEngine.UIElementsModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'UnityEngine.UIElementsModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'F:\Unity\Unity Editor\2022.3.20f1c1\Editor\Data\Managed\UnityEngine\UnityEngine.UIElementsModule.dll'
------------------
Resolve: 'UnityEngine.InputLegacyModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'UnityEngine.InputLegacyModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'F:\Unity\Unity Editor\2022.3.20f1c1\Editor\Data\Managed\UnityEngine\UnityEngine.InputLegacyModule.dll'
------------------
Resolve: 'UnityEditor.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'UnityEditor.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'F:\Unity\Unity Editor\2022.3.20f1c1\Editor\Data\Managed\UnityEngine\UnityEditor.CoreModule.dll'
------------------
Resolve: 'UnityEngine.TilemapModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'UnityEngine.TilemapModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'F:\Unity\Unity Editor\2022.3.20f1c1\Editor\Data\Managed\UnityEngine\UnityEngine.TilemapModule.dll'
------------------
Resolve: 'UnityEngine.SpriteShapeModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'UnityEngine.SpriteShapeModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'F:\Unity\Unity Editor\2022.3.20f1c1\Editor\Data\Managed\UnityEngine\UnityEngine.SpriteShapeModule.dll'
------------------
Resolve: 'System.Runtime.InteropServices, Version=2.1.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'System.Runtime.InteropServices, Version=4.1.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '2.1.0.0', Got: '4.1.2.0'
Load from: 'F:\Unity\Unity Editor\2022.3.20f1c1\Editor\Data\NetStandard\compat\2.1.0\shims\netstandard\System.Runtime.InteropServices.dll'
------------------
Resolve: 'System.Runtime.CompilerServices.Unsafe, Version=2.1.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'System.Runtime.CompilerServices.Unsafe, Version=2.1.0.0, Culture=neutral, PublicKeyToken=null'
#endif
