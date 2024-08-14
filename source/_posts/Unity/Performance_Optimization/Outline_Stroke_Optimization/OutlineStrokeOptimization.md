---
title: 一种UGUI的Outline描边优化方案
top_img: '118435175_p0.jpg'
cover: '98639154_p0.png'
categories: 
    - Unity
      - 性能优化
tags: 
    - UGUI
    - 性能优化
---

## 性能消耗原因(例如顶点数量的大幅增加)分析

* 描边的文字与美术同学出的效果图有不小的差异。效果图的描边效果连贯而且均匀，而UGUI的Outline组件的效果仅仅只是解决了“温饱问题”，Better Than Not而已，并且这种实现方式带来了其他问题，比如顶点数量的大幅增加。

<img src="效果图差异.png" alt="美术同学的效果图与实际实现的对比（放大至300%）" style="zoom:50%;">

## 原因分析

* 通过研究Outline的源码可以看到，Outline组件的原理是在原顶点的基础上，在effectDistance.(x,y), (x, -y), (-x,y), (-x,-y)这4个偏移上ApplyShadowZeroAlloc实现的。亦即Outline组件是在Shadow的基础上实现的，Outline相当于4个不同偏移方向上的Shadow。

<img src="Outline组件核心源码.png" alt="UGUI Outline组件的核心源码" style="zoom:50%;">

* 进一步查看Shadow的源码，发现Shadow的原理实际上是将原始的顶点数据复制一份, 根据设置的偏移量计算复制后的新顶点的位置，并为新的顶点颜色赋值为设置的颜色值。

<img src="Shadow组件核心源码.png" alt="Shadow组件的核心源码" style="zoom:50%;">

* 我们可以简单地将Outline的参数EffectDistance调整到一个较大的值即可观察到4次复制。实际上Outline在处理1像素以上的描边时就会出现较大瑕疵，超过1.5像素就已经不可用了。

## 业界的一般做法

### 基于Shadow

* Outline的实现是在原始顶点的4个方向ApplyShadow，为了描边效果更好，可以在8个方向甚至16个方向ApplyShadow来实现饱满的描边效果。实现原理与Outline类似。

### TextMeshPro

* 注意TextMeshPro使用时需要制作字体文件，即FontAsset。对于英文及数字来说，只需要针对ASCII字符集制作FontAsset即可，但对于中文需要动态生成的文字来说，需要生成的字体文件相对较大，对于手游项目来说，对包量及内存的影响在目前来看都是不能接受的。
* 但TextMeshPro用在可确定的中文字符及ASCII字符上的效果还是不错的，比如用在登录、主界面这类文字不需要动态生成的界面上。

### 基于Mesh

* 实现思路如下：
    1. 提取文字原始UV区域，扩大文字绘图区域
    2. 对文字纹理的每个像素点周围多次采样，应用描边RGBA作为新的点的颜色
    3. 将原始纹理及采样后的点进行融合

## 第一次优化

* 采用基于Shadow的实现方式，多次ApplyShadow以使描边效果饱和。
* Outline的实现方式是在(x,-y),(x,y),(-x,y),(-x,-y)4个方向ApplyShadow。模仿Outline的实现有了新的BoldOutline，在原有的4个方向之外增加(x,0),(-x,0),(0,y),(0,-y)4个方向共计8个方向的描边。描边效果较Outline有了一定提升。

<img src="Outline和BoldOutline效果对比.png" alt="Outline和BoldOutline效果对比" style="zoom:50%;">

* 但，顶点总数爆炸了

<img src="理论计算.png" alt="理论计算的各效果数值表" style="zoom:50%;">

* 从理论上计算，Outline对于1个字符是绘制了5次，BoldOutline是绘制了9次，顶点总数及三角形总数相对于无效果的字符分别应该是5倍和9倍的关系。

<img src="实际运行.png" alt="实际运行时的各效果数值表" style="zoom:50%;">

* 原因分析：
    1. 在UGUI中Text是由TextGenerator产生顶点数据，通过顶点数据与字体贴图渲染到屏幕上的。
    2. Text组件继承自MaskableGraphic，MaskableGraphic又继承自Graphic，Text组件实现了protected virtual void OnPopulateMesh(VertexHelper vh)方法为所需的数据赋值。
    3. 实现这些文字效果的基础是Unity为外部提供的接口IMeshModifier，如果Text组件所在的GameObject中存在实现了IMeshModifier接口的组件，就会调用对应组件的ModifyMesh方法。这样就可以在外部修改Text的顶点数据了。

<img src="ModifyMesh.png" alt="典型的顶点修改组件结构" style="zoom:100%;">

* 了解了各种文字效果实现的原理，继续来看关键函数ModifyMesh(VertexHelper vh)的具体实现。Text组件生成的顶点数据通过参数类型VertexHelper传入后，需要通过`public void GetUIVertexStream(List<UIVertex> stream)`这个方法获取具体数据。Verts/Tris比值的变化就发生在这个帮助函数中。这个函数将原本共享的三角形顶点做了拆分，按照1个三角形对应3个顶点的数据输出了。
* 引用一位国外Unity开发者的话讲：`GetUIVertexStream takes a nice optimized mesh with shared verts and completely ruins it. Dont call it. Ever.`

## 第二次优化

> 简单的基于Shadow的实现方式会引起顶点数量的暴涨，第二次优化将方向调整为了**基于Mesh**。实现思路如下：

**在字符原UV边界uvOrigin的基础上，通过描边大小fSize计算描边后的UV边界uvAdd。**

**将原UV边界uvOrigin,扩展后的UV边界uvAdd,描边大小fSize以及描边颜色color传入Shader。将顶点信息传入Shader时需要注意，UIVertex结构体的成员如下：**

<img src="UIVertex.png" alt="UIVertex结构体" style="zoom:100%;">

* 没有被默认Shader使用的参数是uv1,normal及tangent。这些需要传给Shader的值就塞进这些参数里就可以了，在Shader中只要逆向解出一一对应即可。

**对字符贴图像素处理时，从当前像素向四周做8次采样以确定当前像素点的值。采样时需要判断当前点是否是在uvOrigin的范围内，否则的话可能会出现采样到隔壁字符的情况。**

* 判断当前点是否在uvOrigin的方法如下：在范围内返回1，不在范围内返回0

<img src="判断方法.png" alt="使用Step方法判断范围" style="zoom:100%;">

* 沿当前像素采样的8个方向偏移，数组已经被解开，避免需要在Shader中用循环来实现。

<img src="采样偏移矩阵.png" alt="采样偏移矩阵" style="zoom:100%;">

* 将所有采样点采样的像素值求和，然后除以采样的数量。

<img src="偏移采样.png" alt="对（-1，-1）偏移采样" style="zoom:100%;">

**在接下来的pass中将字符的原始贴图信息与刚刚计算得到的数据进行融合。**

* 需要注意的是，不在uvOrigin中的像素点的color值直接置0.避免uvOrigin之外，uvAdd之内的脏点混进来。
* 于是我们就得到了这样的结果：

<img src="效果图.png" alt="描边2像素、15像素效果及对应数据" style="zoom:100%;">
<img src="MeshOutline.png" alt="MeshOutline与其他效果对比数据" style="zoom:50%;">

**使用MeshOutline后Batches数量为2，而Outline因为是复制的原顶点信息，所以无论复制多少份Batches数量都为1（如果不理解的话可参考[UGUI的优化方案(1)](https://sleepyloser.github.io/2024/07/24/Unity/Performance_Optimization/Optimize_UI_One/OptimizeUI_One/)和[UGUI的优化方案(2)](https://sleepyloser.github.io/2024/08/05/Unity/Performance_Optimization/Optimize_UI_Two/OptimizeUI_Two/)）.总的来说，这种优化方式带来的是采样次数的增加及顶点数量的相对减少。**
