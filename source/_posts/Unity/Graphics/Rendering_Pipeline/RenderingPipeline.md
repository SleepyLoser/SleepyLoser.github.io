---
title: 渲染管线
top_img: '118645167_p0.png'
cover: '116535516_p0.png'
categories: 
    - Unity
      - 渲染管线
tags: 
    - 渲染管线
    - 图形学
---

## 渲染管线

* 渲染的三个阶段一般分为：`应用阶段`、`几何阶段`和`光栅化阶段`, 如下图

<img src="渲染管线.png" alt="渲染管线" style="zoom:50%;">

* 三个阶段的操作对象的流程图可参考下图

<img src="渲染管线流程图.png" alt="渲染管线流程图" style="zoom:50%;">

### 1、应用阶段

* 这一阶段由CPU处理，主要任务是为接下来GPU的渲染操作**提供所需要的几何信息**，即输出`渲染图元（rending primitives）`以供后续阶段的使用。渲染图元就是由若干个顶点构成的几何形状，点，线，三角形，多边形面都可以是一个图元

#### 1、数据的准备

* **第一步**应先将不需要的数据剔除出去，如以包围盒为单位的视锥体（粗粒度）剔除，遮挡剔除，层级剔除等等
* **第二步**根据UI对象在Herachy面板深度值的顺序（DFS深度优先搜索）设置渲染的顺序，其余物体大体可以按照离摄像机先近后远的规则为后续循环绘制所有对象制定排队顺序
* **第三步**先将所有需要的渲染数据从硬盘读取到主存中，再把GPU渲染需要用到的数据打包发给显存（GPU一般没有对主存的访问权限，且与显存进行交换速度较快）
* 打包的数据详情见下图

<img src="打包数据.png" alt="打包数据" style="zoom:50%;">

#### 2、设置渲染状态

* 渲染状态包括着色器（Shader），纹理，材质，灯光等等。
* 设置渲染状态实质上就是，告诉GPU该使用哪个Shader，纹理，材质等去渲染模型网格体，这个过程也就是SetPassCall。当使用不同的材质或者相同材质下不同的Pass时就需要设置切换多个渲染状态，就会增加SetPassCall 所以SetPassCall的次数也能反映性能的优劣。

#### 3、发送DrawCall

* 当收到一个DrawCall时，GPU会按照命令，根据**渲染状态**和输入的顶点信息对**指定**的模（网格）进行计算渲染。
* CPU通过调用图形API接口（ glDrawElements (OpenGl中的图元渲染函数) 或者 DrawIndexedPrimitive (DirectX中的顶点绘制方法) ）命令GPU对指定物体进行一次渲染的操作即为DrawCall。此过程实质上就是在告诉GPU该使用哪个模型的数据（图形API函数的功能就是将CPU计算出的顶点数据渲染出来）。

> 在应用阶段有三个衡量性能指标非常重要的名词
> > DrawCall
> > > CPU每次调用图形API接口命令GPU进行渲染的操作称为一次DrawCall
> >
> > SetPassCall
> > > 设置/切换一次渲染状态
> >
> > Batch
> > > 把数据加载到显存，设置渲染状态，CPU调用GPU渲染的过程称之为一个Batch
>
> **注意：一个Batch包含至少一个DrawCall**
> **详情见 [UGUI的优化方案(2)](https://sleepyloser.github.io/2024/08/05/Unity/Performance%20Optimization/Optimize_UI_Two/OptimizeUI_Two/)**

### 几何阶段
