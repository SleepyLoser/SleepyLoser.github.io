---
title: UGUI运作原理
top_img: '118645167_p1.png'
cover: '121147354_p0.jpg'
categories: 
    - Unity
      - UI
tags: 
    - UGUI
---

## 前言

* UGUI的图像显示核心是Graphic类，而Graphic又由Canvas相关类进行管理。在UGUI系统中Canvas管理UI元素的生命周期与样式变化，CanvasRenderer负责UI的显示，包括网格、材质以及Rect裁剪等。由于Canvas与CanvasRenderer真正核心代码未开源，所以只能从Graphic类一探究竟。

## UGUI运作原理

* UGUI系统的运作核心是CanvasUpdateRegistry类，它通过Canvas的willRenderCanvases回调入手，即每帧在Canvas进行渲染前更新各个Graphic的位置以及渲染信息（mesh、material等），然后根据渲染信息进行渲染，整个运作图如下所示，包括mask在内：

<img src="UGUI运作原理图.png" alt="UGUI运作原理图" style="zoom:50%;">

### CanvasUpdateRegistry

* 此类维护了两个队列，即Layout重构队列和Graphic重构队列。这两个队列分别存储了Layout的重构信息（位置、大小）和图像更改信息（mesh、材质等）。在Canvas渲染前通过PerformUpdate进行更新，更新流程如下所示：
  1. 移除队列中的非法元素，比如null或者destroyed (**通常UGUI界面操作卡大概率都是Canvas.SendWillRenderCanvases()方法耗时，需要检查界面是否存在多余或者无用的重建情况**)
  2. 进行Layout更新，会调用`m_LayoutRebuildQueue`里元素的Rebuild(继承自ICanvasElement)进行重构 (**UGUI的RawImage、Image、Text等组件都派生自Graphic类 (实际上派生自MaskableGraphic，MaskableGraphic派生自Graphic) ，并且都实现了ICanvasElement接口**)
  3. 剔除（或者说遮罩），此部分是进行RectMask2D的作用，调用ClipperRegister中的Cull方法进行
  4. 进行Graphic`(m_GraphicRebuildQueue)`的更新，因为Graphic的mesh等信息跟UI的Rect位置有关系，所以最后进行Graphic重建

* 附[CanvasUpdateRegistry源码](CanvasUpdateRegistry.cs)

### WillRenderCanvases

* 添加Canvas将会打断和之前元素DrawCall的合并，每个Canvas都会开始一个全新的DrawCall。如下代码所示，当Canvas需要重绘的时候会调用SendWillRenderCanvases()方法。

<img src="Canvas强制更新.png" alt="IWillRenderCanvases" style="zoom:50%;">

* 在CanvasUpdateRegistry的构建函数中可以看到Canvas.willRenderCanvases事件添加到this.PerformUpdate()方法中，UI发生变化一般分两种情况，一种是修改了宽高这样会影响到顶点位置需要重建Mesh，还有一种仅仅只修改了显示元素，这样并不会影响顶点位置，此时unity会在代码中区别对待。

``` C#
public class CanvasUpdateRegistry
{
    //...略
    protected CanvasUpdateRegistry()
    {
        //构造函数处委托函数到PerformUpdate()方法中
        //每次Canvas.willRenderCanvases就会执行PerformUpdate()方法
        Canvas.willRenderCanvases += PerformUpdate;
    }

    private void PerformUpdate()
    {
        //开始BeginSample()
        //在Profiler中看到的标志性函数Canvas.willRenderCanvases耗时就在这里了
        //EndSample()
        UISystemProfilerApi.BeginSample(UISystemProfilerApi.SampleType.Layout);
        CleanInvalidItems();

        m_PerformingLayoutUpdate = true;
        //需要重建的布局元素(RectTransform发生变化)，首先需要根据子对象的数量对它进行排序。
        m_LayoutRebuildQueue.Sort(s_SortLayoutFunction);
        //遍历待重建布局元素队列，开始重建
        for (int i = 0; i <= (int)CanvasUpdate.PostLayout; i++)
        {
            for (int j = 0; j < m_LayoutRebuildQueue.Count; j++)
            {
                var rebuild = instance.m_LayoutRebuildQueue[j];
                try
                {
                    if (ObjectValidForUpdate(rebuild))
                        rebuild.Rebuild((CanvasUpdate)i);//重建布局元素
                }
                catch (Exception e)
                {
                    Debug.LogException(e, rebuild.transform);
                }
            }
        }

        for (int i = 0; i < m_LayoutRebuildQueue.Count; ++i)
            m_LayoutRebuildQueue[i].LayoutComplete();

        //布局构建完成后清空队列
        instance.m_LayoutRebuildQueue.Clear();
        m_PerformingLayoutUpdate = false;

        // 布局构建结束，开始进行Mask2D裁切（详细内容下面会介绍）
        ClipperRegistry.instance.Cull();

        m_PerformingGraphicUpdate = true;

        //需要重建的Graphics元素(Image Text RawImage 发生变化)
        for (var i = (int)CanvasUpdate.PreRender; i < (int)CanvasUpdate.MaxUpdateValue; i++)
        {
            for (var k = 0; k < instance.m_GraphicRebuildQueue.Count; k++)
            {
                try
                {
                    var element = instance.m_GraphicRebuildQueue[k];
                    if (ObjectValidForUpdate(element))
                        element.Rebuild((CanvasUpdate)i);//重建UI元素
                }
                catch (Exception e)
                {
                    Debug.LogException(e, instance.m_GraphicRebuildQueue[k].transform);
                }
            }
        }

        // 这里需要思考的是，有可能一个Image对象，RectTransform和Graphics同时发生了修改，它们的更新含义不同需要区分对待
        // 1. 修改了Image的宽高，这样Mesh的顶点会发生变化，此时该对象会加入m_LayoutRebuildQueue队列
        // 2. 修改了Image的Sprite，它并不会影响顶点位置信息，此时该对象会加入m_GraphicRebuildQueue队列
        // 所以上面代码在遍历的时候会分层
        // for (int i = 0; i <= (int)CanvasUpdate.PostLayout; i++)
        // 与
        // for (var i = (int)CanvasUpdate.PreRender; i < (int)CanvasUpdate.MaxUpdateValue; i++)
        // Rebuild的时候会把层传进去，保证Image知道现在是要更新布局，还是只更新渲染。


        for (int i = 0; i < m_GraphicRebuildQueue.Count; ++i)
            m_GraphicRebuildQueue[i].GraphicUpdateComplete();

        instance.m_GraphicRebuildQueue.Clear();
        m_PerformingGraphicUpdate = false;
        UISystemProfilerApi.EndSample(UISystemProfilerApi.SampleType.Layout);
    }
}
```

* 附[Canvas源码](Canvas.cs)

### Graphic/MaskableGraphic

* **CanvasUpdateRegistry--流程2**图示

<img src="Graphic_Rebuild.png" alt="Graphic_Rebuild" style="zoom:50%;">

* UpdateGeometry（更新几何网格），就是确定每一个UI元素Mesh的信息，包括顶点数据、三角形数据、UV数据、顶点色数据。如下代码所示，无论Image还是Text数据都会在OnPopulateMesh函数中进行收集，它是一个虚函数会在各自的类中实现。

<img src="Rebuild_UpdateGeometry.png" alt="Rebuild_UpdateGeometry" style="zoom:50%;">

* 顶点数据准备完毕后会调用canvasRenderer.SetMesh()方法来提交。很遗憾CanvasRenderer.cs并没有开源，我们只能继续反编译看它的实现了。如下代码所示，SetMesh()方法最终在C++中实现，毕竟由于UI的元素很多，同时参与合并顶点的信息也会很多，在C++中实现效率会更好。看到这里，我相信大家应该能明白UGUI为什么效率会被NGUI要高一些了，因为NGUI的网格Mesh合并都是在C#中完成的，而UGUI网格合并都是在C++中底层中完成的。

<img src="CanvasRender_SetMesh.png" alt="CanvasRender_SetMesh" style="zoom:50%;">

* 由于元素的改变可分为`布局变化`、`顶点变化`、`材质变化`，所以分别提供了三个方法`SetLayoutDirty()`、`SetVerticesDirty()`、`SetMaterialDirty()` 供选择。
* 为什么UI发生变化一定要加入待重建队列中呢？其实这个不难想象，一个UI界面同一帧可能有N个对象发生变化，任意一个发生变化都需要重建UI那么肯定会卡死。所以我们先把需要重建的UI加入队列，等待一个统一的时机来合并。

<img src="Graphic_SetDirty.png" alt="Graphic_SetDirty" style="zoom:50%;">

* 举个例子，在UI中如果调整了元素在Hierarchy中的父节点，如下代码所示，在OnTransformParentChanged()方法中监听，通过SetAllDirty();方法将该UI加入“待重建队列”。

<img src="Graphic_OnTransformParentChanged.png" alt="Graphic_OnTransformParentChanged" style="zoom:50%;">

* 再比如需要修改Text文本的字体，由于字体大小的变化只会影响布局信息和顶点信息，那么就调用SetVerticesDirty();SetLayoutDirty();方法即可。

<img src="Text_fontSize.png" alt="Text_fontSize" style="zoom:50%;">

* UI的网格我们都已经合并到了相同Mesh中，还需要保证贴图、材质、Shader相同才能真正合并成一个DrawCall。UGUI开发时使用的是Sprite对象，其实Sprite对象只是在Texture上又封装的一层数据结构，它记录的是Sprite大小以及九宫格的区域，还有Sprite保存在哪个Atals中。如果很多界面Prefab引用了这个Sprite，它只是通过GUID进行了关联，它并不会影响到已经在Prefab中保存的Sprite，这对后期调整图集和DrawCall方便太多了，这也是UGUI比NGUI更方便的一点。

> **回到Graphic/MaskableGraphic这两个类身上**

1. 这两个类是ui显示的核心，后者通过IClippable和IMaskable可实现遮罩效果。这两个类是“被动”类，即我只标记下一帧需要的操作（更新Layout或者Graphic），至于何时更新由管理者CanvasUpdateRegistry去操作
2. IClippable与IMaskable :
IMaskable是实现Mask遮罩的关键，它是通过材质来实现“像素遮罩”的。而IClippable则是实现RectMask2D的遮罩的关键，他是通过CanvasRenderer的EnableRectClipping以及Cull实现的，它只能实现Rect遮罩。这也是Mask和RectMask2D的差别。

> **关于RectMask2D与Mask之间的区别**
>
> > **RectMask2D**
> >
> > > * RectMask2D 是一个类似于__遮罩 (Mask)__ 控件的遮罩控件。遮罩将子元素限制为父元素的矩形。
> > > * RectMask2D 控件的局限性包括：`仅在 2D 空间中有效`、`不能正确掩盖不共面的元素`
> > > * RectMask2D 的优势包括：`不使用模板缓冲区`、`无需额外的绘制调用`、`无需更改材质`、`高速性能`
> >
> > **Mask**
> >
> > > * 应使用 GPU 的模板缓冲区来实现遮罩
> > > * 第一个遮罩元素将 1 写入模板缓冲区。 遮罩下面的所有元素在渲染时进行检查，仅渲染到模板缓冲区中有 1 的区域。嵌套的遮罩会将增量位掩码写入缓冲区，这意味着可渲染的子项需要具有要渲染的逻辑和模板值。

* RectMask2D的遮罩是通过CanvasRenderer的EnableRectClipping以及Cull实现的，它的工作流程比较复杂，流程如下:
  1. 启动时调用MaskUtilities.Notify2DMaskStateChanged方法，通知所有子游戏物体的MaskableGraphic（所有继承IClippable的组件），RectMask2D遮罩产生变化。同时将自己添加的ClipperRegistry的Clipper中
  2. 所有的子MaskableGraphic（所有继承IClippable的组件）根据此通知更新自己遮罩生效的RectMask2D，并加入到RectMask2D的ClipTarget中
  3. 当CanvasUpdateRegistry在更新Canvas时，会调用ClipperRegistry的cull方法，然后依次调用所有RectMask2D的PerformClipping方法。在PerformClipping方法中RectMask2D依次调用ClipTarget元素的SetClipRect和Cull方法，完成剔除
  4. 当以上所有工作完成后，Canvas更新重建，显示我们想要的效果

* 附[Graphic源码](Graphic.cs)
