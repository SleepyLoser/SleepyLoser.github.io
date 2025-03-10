---
title: UGUI的优化方案(2)
top_img: '121106160_p0.jpg'
cover: '109951165836194238.jpg'
permalink: /Unity/Performance_Optimization/Optimize_UI_Two/
categories: 
    - Unity
      - 性能优化
tags: 
    - UGUI
    - 性能优化
---

## DrawCall

* CPU每次调用图像编程接口 glDrawElements（OpenGl中的图元渲染函数）或者 DrawIndexedPrimitive（DirectX中的顶点绘制方法）命令GPU渲染的操作称为一次DrawCall。
* DrawCall就是一次渲染命令的调用，它指向一个需要被渲染的图元（primitive）列表，不包含任何材质信息，glDrawElements 或者 DrawIndexedPrimitive 函数的作用是将CPU准备好的顶点数据渲染出来。

## Batch

* 把数据加载到显存，设置渲染状态，CPU调用GPU渲染的过程称之为一个Batch。这其实就是渲染流程的运用阶段，最终输出一个渲染图元（点、线、面等），再传递给GPU进行几何阶段和光栅化阶段的渲染显示。一个Batch必然会触发一次或多次DrawCall，且包含了该对象的所有的网格和顶点数据以及材质信息。把数据加载到显存是指把渲染所需的数据从硬盘加载到内存（RAM），再将网格和纹理等加载到显卡（VRAM），这一步比较耗时。设置渲染状态就是设置场景中的网格的顶点（Vertex）/片元（Fragment）着色器，光源属性，材质等。Unity提供的动态合批（Dynamic Batching ）合并的就是这一过程，将渲染状态相同的对象合并成一个Batch，减少DrawCall。

## SetPassCall

* Shader脚本中一个Pass语义块就是一个完整的渲染流程，一个着色器可以包含多个Pass语义块，每当GPU运行一个Pass之前，就会产生一个SetPassCall，所以可以理解为一次完整的渲染流程次数。

## 总结

* 一个Batch包含一个或多个DrawCall，都是产生是在CPU阶段，而目前普遍渲染的瓶颈恰恰就是CPU，GPU的处理速度比CPU快多了，Draw Call太高，CPU会把大量时间花费在处理Draw Call调用上。如果Batch太大，CPU需要频繁的从硬盘加载数据，切换渲染状态，这个消耗要比DrawCall大，所以后面Unity才逐渐弱化了DrawCall的显示
