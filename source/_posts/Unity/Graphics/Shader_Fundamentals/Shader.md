---
title: Unity Shader
top_img: '118645167_p2.png'
cover: '116478177_p0.png'
categories: 
    - Unity
      - Shader
tags: 
    - 图形学
    - Shader
# {% asset_link unity_shaders_book.pdf TestTitle %}
# [B站](https://www.bilibili.com/)
---

## Unity Shader 基础

### 渲染管线

在 Unity 中，可以选择不同的渲染管线。渲染管线执行一系列操作来获取场景的内容，并将这些内容显示在屏幕上。概括来说，这些操作如下：

* **剔除**
* **渲染**
* **后期处理**

不同的渲染管线具有不同的功能和性能特征，并且适用于不同的游戏、应用程序和平台。
将项目从一个渲染管线切换到另一个渲染管线可能很困难，因为`不同的渲染管线使用不同的着色器输出`，并且可能没有相同的特性。因此，必须要了解 Unity 提供的不同渲染管线，以便可以在开发早期为项目做出正确决定。

### 选择要使用的渲染管线

Unity 提供以下渲染管线：

* **内置渲染管线**是 Unity 的默认渲染管线。这是通用的渲染管线，其自定义选项有限。
* **通用渲染管线** (URP) 是一种可快速轻松自定义的可编程渲染管线，允许您在`各种平台`上创建优化的图形。
* **高清渲染管线** (HDRP) 是一种可编程渲染管线，可让您在`高端平台`上创建出色的高保真图形。
* 可以使用 Unity 的**可编程渲染管线** API 来创建自定义的可编程渲染管线 (SRP)。这个过程可以从头开始，也可以修改 URP 或 HDRP 来适应具体需求。
  
***

### Shader

在Unity Shader的帮助下，开发者只需要使用ShaderLab来编写Unity Shader文件就可以完成所有工作
{% span blue, 以下是StandardSurfaceShader的标准结构 %}

``` Shader
Shader "Custom/NewSurfaceShader"
{
    Properties
    {
        // 属性
    }

    SubShader
    {
        // 显卡A使用的子着色器
    }

    SubShader
    {
        // 显卡B使用的子着色器
    }

    FallBack "Diffuse"
}
```

<table><tr><td bgcolor=silver>第一行Shader后面的 "Custom/..."是材质选择Shader时的路径（自定义Shader的名称）</td></tr></table>
<img src="CustomNewSurfaceShader.png" alt="其中Shader后面的 'Custom/....'是材质选择Shader时的路径" style="zoom:50%;">

#### Properties

<table><tr><td bgcolor=silver>Properties中的属性可以看作Unity-C#中公开的全局变量（public int num......）, 可以在Inspector窗口改动相对应的属性值，从而达到运行时调试Shader效果的作用</td></tr></table>
<img src="Properties.png" alt="Properties中的属性可以看作Unity-C#中公开的全局变量（public int num......）, 可以在Inspector窗口改动相对应的属性值，从而达到运行时调试Shader效果的作用" style="zoom:50%;">
<img src="Properties_Variable.png" alt="Properties中的属性可以看作Unity-C#中公开的全局变量（public int num......）, 可以在Inspector窗口改动相对应的属性值，从而达到运行时调试Shader效果的作用" style="zoom:50%;">

#### SubShader

当Unity需要加载这个Unity Shader时，Unity会扫描所有的SubShader语义块，然后选择第一个能够在目标平台上运行的SubShader，如果都不支持的话，Unity就会使用Fallback语义指定的Unity Shader
<img src="SubShader.png" alt="当Unity需要加载这个Unity Shader时，Unity会扫描所有的SubShader语义块，然后选择第一个能够在目标平台上运行的SubShader，如果都不支持的话，Unity就会使用Fallback语义指定的Unity Shader" style="zoom:50%;">

##### Pass以及可选状态和标签

SubShader中定义了一系列pass以及可选的状态（[RenderSetUp]）和标签（[Tags]）设置，每个pass定义了一次完整的渲染流程，但如果pass的数目过多，往往会造成渲染性能的下降。
<img src="Tags_Pass_RenderSetUp.png" alt="SubShader中定义了一系列pass以及可选的状态（[RenderSetUp]）和标签（[Tags]）设置，每个pass定义了一次完整的渲染流程，但如果pass的数目过多，往往会造成渲染性能的下降。" style="zoom:50%;">

##### Tags

SubShader的标签(Tags)是一个键值对，它的键和值都是字符串类型
