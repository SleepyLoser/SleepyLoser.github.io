---
title: UGUI渲染机制
top_img: '121108388_p2.jpg'
cover: '109479258_p0.jpg'
categories: 
    - Unity
      - UI
tags: 
    - UGUI
---

## 浅谈UGUI的渲染机制

### 渲染层级

* **相机的Layer和Depth**：Culling Layer可以决定相机能看到什么Layer，Depth越高的相机，其视野内能看到的所有物体渲染层级越高
* **Canvas的Layer和Order**:
  1. Screen Space - Overlay: UI元素置于屏幕上方，画布自动适应屏幕尺寸改变。Sort Order越大显示越前面
  2. Screen Spacce - Camera: 画布自动适应屏幕尺寸改变，需要设置Render Camera。如果Scene中的GameObject比UI平面更靠近camera，就会遮挡到UI平面。( 其中Order Layer越大显示越前面；Sorting Layer中越在下方的Layer显示越前面 )
  3. World Space: 当UI为场景的一部分，即UI为场景的一部分，需要以3D形式展示。

* **物体的层级（Hierarchy）关系**：如下图, 物体越在Hierarchy窗口里的下方，显示越在前面 (Text > RawImage > Image)

<img src="Sort.png" alt="物体的层级（Hierarchy）关系" style="zoom:50%;">

### 渲染器的对比

* UGUI的渲染器是Canvas Render, 同样渲染2D物体的是Sprite Render
* 相同点:
  1. 都有一个渲染队列来处理透明物体，从后往前渲染
  2. 都可以通过图集并合并渲染批次，减少drawcall

* 不同点：
  1. Canvas Render要与Rect Transform配合，必须在Canvas里使用，常用于UI。Sprite Render与transform配合，常用于gameplay
  2. Canvas Render基于矩形分隔的三角形网络，一张网格里最少有两个三角形（不同的image type, 三角形的个数也会不同），透明部分也占空间。Sprite Render的三角网络较为复杂，能剔除透明部分

<img src="Image.png" alt="Sprite会根据显示内容，裁剪掉元素中的大部分透明区域，最终生成的几何体可能会有比较复杂的顶点结构" style="zoom:50%;">
<img src="RawImage.png" alt="Image会老老实实地为一个矩形的Sprite生成两个三角形拼成的矩形几何体" style="zoom:50%;">

### 一个DrawCall的渲染流程

1. CPU发送Draw Call指令给GPU
2. GPU读取必要的数据到自己的显存
3. GPU通过顶点着色器（vertex shader）等步骤将输入的几何体信息转化为像素点数据
4. 每个像素都通过片段着色器（fragment shader）处理后写入帧缓存
5. 当全部计算完成后，GPU将帧缓存内容显示在屏幕上

* 从上面的步骤可知，因为Sprite的顶点数据更复杂，在第一步和第二步的效率会比Image低，Image会有更多的Fragment Shader的计算因为是针对每个像素的计算，Sprite会裁剪掉透明的部分，从而减少了大量的片段着色器运算，并降低了Overdraw，Sprite会有更多的Vertex Shader的计算
