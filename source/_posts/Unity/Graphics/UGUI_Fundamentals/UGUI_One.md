---
title: UGUI运作原理(1)
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

1. 移除队列中的非法元素，比如null或者destroyed
2. 进行Layout更新，会调用`m_LayoutRebuildQueue`里元素的Rebuild(继承自ICanvasElement)进行重构
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
