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

### 详情可参考 [渲染管线](https://sleepyloser.github.io/2024/08/05/Unity/Graphics/Rendering_Pipeline/RenderingPipeline/)
  
***

### Shader基本结构

在Unity Shader的帮助下，开发者只需要使用ShaderLab来编写Unity Shader文件就可以完成所有工作
{% span blue, 以下是StandardSurfaceShader的标准结构 %}

``` ShaderLab
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

* SubShader的标签(Tags)是一个键值对，它的键和值都是字符串类型（见上图），**用于确定子着色器的渲染顺序和其他参数**。**请注意**，以下由 Unity 识别的标签**必须**位于 SubShader 部分中，**不能**在 Pass 中！

#### Fallback

* 在所有子着色器的后面可定义 Fallback。其意义在于：如果没有任何子着色器能够在此硬件上运行，则尝试使用另一个着色器中的子着色器。
* **基本语法如下**，其意义在于回退到具有给定_名称 (name)_ 的着色器。

``` ShaderLab
Fallback "name"
```

* 或者如下代码显式说明即使没有子着色器可以在此硬件上运行，也不会进行回退并且不会显示警告。

``` ShaderLab
Fallback Off
```

* Fallback 语句的效果等同于插入其他着色器中的所有子着色器。

### Shader语法（内置管线）

#### CG语言

``` ShaderLab
Shader "Custom/TestShader"
{
    SubShader
    {
        pass
        {
            CGPROGRAM // 开始CG语言
                
            ENDCG // 结束CG语言
        }
    }
}
```

#### 语义

``` ShaderLab
//声明变量float4  //变量名称v  //POSITION就是语义，你只要:POSITION就能获取到模型的顶点
float4 v : POSITION
```

#### 引用

* 像C#里的using UnityEngine，在shader里，引入是#pragma。不同点是shader引入后需要给引入的内容起个名字，下面案例起名叫vert

``` ShaderLab
 #pragma vertex vert
```

* 和C#一样，引用之后，就可以用引入内容里有的方法，在这里当你引入了顶点着色器并起了名，你就可以用**顶点着色器**的方法了。
* 顶点着色器的方法代码如下：

``` ShaderLab
#pragma vertex vert

float4 vert()
{
    return //一个float4
}
```

#### 对材质颜色进行干预

##### 获取位置信息

* 我们可以在顶点着色器中干预上色的位置
    1. :POSITION 获取到模型的顶点坐标
    2. :SV_POSITION 输出给像素着色器的屏幕坐标
    3. :SV_TARGET 输出值直接用于渲染了

* 引入顶点位置信息代码
* 获取模型顶点位置
* 坐标转换, 即将世界坐标下的顶点位置，转换成屏幕坐标下的位置
* 把转换好的坐标输出给像素着色器的屏幕坐标

``` ShaderLab
Shader "Custom/TestShader"
{
    SubShader
    {
        pass
        {
            CGPROGRAM
            //引入vertex //起名叫vert
            #pragma vertex vert
            // float4 v : POSITION 引入模型顶点坐标
            // SV_POSITION : return的值直接给到片元着色器的屏幕坐标
            float4 vert(float4 v : POSITION) : SV_POSITION
            {
                // 模型顶点的世界坐标 => 屏幕坐标
                return UnityObjectToClipPos(v);
            }
            ENDCG
        }
    }
}
```

##### 处理颜色

* 引入片元着色器信息代码
* 修改颜色代码
* 片元着色器输出白色代码
* **注意**：这里return 的数据，如果是都在0-1里面，默认0是黑色，1是白色。如果是在0-255里，默认0是黑色，255是白色。

``` ShaderLab
// 片元着色器
#pragma fragment frag
// 输出渲染
float4 frag() : SV_TARGET
{
    // return 的数据，如果是都在0-1里面，默认0是黑色，1是白色。如果是在0-255里，默认0是黑色，255是白色。
    return float4(1, 1, 1, 1);
}
```

* **整体代码如下**

``` ShaderLab
Shader "Custom/TestShader"
{
    SubShader
    {
        pass
        {
            CGPROGRAM
            // 顶点着色器
            #pragma vertex vert
            // float4 v : POSITION 引入模型顶点坐标
            // SV_POSITION : return的值直接给到片元着色器的屏幕坐标
            float4 vert(float4 v : POSITION) : SV_POSITION
            {
                // 模型顶点的世界坐标 => 屏幕坐标
                return UnityObjectToClipPos(v);
            }
            // 片元着色器
            #pragma fragment frag
            // 输出渲染
            float4 frag() : SV_TARGET
            {
                // return 的数据，如果是都在0-1里面，默认0是黑色，1是白色。如果是在0-255里，默认0是黑色，255是白色。
                return float4(1, 1, 1, 1);
            }
            ENDCG
        }
    }
}
```

#### 结构体的需求

* 我们现在有3个语义想用：
    1. :POSITION 顶点坐标
    2. :NORMAL 法线坐标
    3. :TEXCOORD0 第一套纹理坐标 // 纹理坐标即UV坐标。可参考 [Unity Texture 基础](https://sleepyloser.github.io/2024/07/15/Unity/Graphics/Texture_Fundamentals/Texture/)

* 用结构体把这些数据保存起来

``` ShaderLab
//这里结构体的名字是可以自己起的
struct DataRequiredForUse
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float4 texcoord : TEXCOORD0;
}
```

* 结合处理颜色部分的代码

``` ShaderLab
Shader "Custom/TestShader"
{
    SubShader
    {
        pass
        {
            CGPROGRAM

            struct DataRequiredForUse
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 texcoord : TEXCOORD0;
            }; // 记得打分号
            
            // 顶点着色器
            #pragma vertex vert

            // float4 v : POSITION 引入模型顶点坐标
            // SV_POSITION : return的值直接给到片元着色器的屏幕坐标
            float4 vert(float4 v : POSITION) : SV_POSITION
            {
                // 模型顶点的世界坐标 => 屏幕坐标
                return UnityObjectToClipPos(v);
            }

            // 片元着色器
            #pragma fragment frag

            // 输出渲染
            float4 frag() : SV_TARGET
            {
                // return 的数据，如果是都在0-1里面，默认0是黑色，1是白色。如果是在0-255里，默认0是黑色，255是白色。
                return float4(1, 1, 1, 1);
            }

            ENDCG
        }
    }
}
```

#### 使用封装好的结构体

* Unity封装好了很多结构体，例如：

``` ShaderLab
struct appdata_base 
{
    float4 vertex : POSITION;       //顶点坐标
    float3 normal : NORMAL;         //法线
    float4 texcoord : TEXCOORD0;    //第一纹理坐标
    UNITY_VERTEX_INPUT_INSTANCE_ID  //ID信息
};
 
struct appdata_tan 
{
    float4 vertex : POSITION;       //顶点坐标
    float4 tangent : TANGENT;       //切线
    float3 normal : NORMAL;         //法线
    float4 texcoord : TEXCOORD0;    //第一纹理坐标
    UNITY_VERTEX_INPUT_INSTANCE_ID  //ID信息
};
 
struct appdata_full 
{
    float4 vertex : POSITION;
    float4 tangent : TANGENT;
    float3 normal : NORMAL;
    float4 texcoord : TEXCOORD0;
    float4 texcoord1 : TEXCOORD1;   //第二纹理坐标
    float4 texcoord2 : TEXCOORD2;   //第三纹理坐标
    float4 texcoord3 : TEXCOORD3;   //第四纹理坐标
    fixed4 color : COLOR;           //顶点颜色
    UNITY_VERTEX_INPUT_INSTANCE_ID  //ID信息
};
```

* 提前引用就可以使用, 例：

``` ShaderLab
//之前学的CG引用
#pragma vertex vert
#pragma fragment frag

//Unity封装好的部分结构体引用
#include "UnityCG.cginc"
```

* 案例：

``` ShaderLab
Shader "Custom/TestShader"
{
    SubShader
    {
        pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
 
            #include"UnityCG.cginc"
 
            // 假设我们要传入 appdata_base
            // 传入上述结构体
            float4 vert(appdata_base v) : SV_POSITION
            {
                // 调用结构体的vertex
                return UnityObjectToClipPos(v.vertex);
            }
 
            float4 frag() : SV_TARGET
            {
                return float4(1, 1, 1, 1);
            }
 
            ENDCG
        }
    }
}
```

### 案例：利用Shader制作 “ 彩球 ”

* 结构体 a2v（application to vertex）

``` ShaderLab
struct a2v
{
    float4 vertex : POSITION;       // 顶点坐标
    float3 normal : NORMAL;         // 法线
    float4 texcoord : TEXCOORD0;    // 第一纹理坐标
    UNITY_VERTEX_INPUT_INSTANCE_ID  // ID信息
}; // 记得打分号
```

* 如果我们声明了这个结构体，这个结构体会自带值，这个值就是后面的语义里所带的值。

``` ShaderLab
a2v vert(a2v data)
{
}
```

* 如果我们改变了这个结构体，在把它return出去，改变的值就会自己输出到后面的语义里，进行下一轮计算。

``` ShaderLab
a2v vert(a2v data)
{
    // 模型顶点的世界坐标 => 屏幕坐标
    data.vertex = UnityObjectToClipPos(data.vertex);
    return data;
}
```

* 到此，我们的顶点计算就完成了，也输出给了POSITION。
* 因为我们在顶点着色器里修改的值，**实际上是储存在语义里了**，我们再次声明也是从语义里接收数据，所以我们只需要再次声明a2v，我们就可以接收到顶点着色器中修改过的数据。
* 根据数学知识，把法线**映射**成 [0, 1] 的数据。

> 1. 球体上的法线刚好是连续的从向量（-1，-1，-1）到（1，1，1）的值
> 2. 我们输出的颜色的值，是0到1之间，那我们只需要让-1到1，等比变成0到1就可以了。
> 3. 例：如果是-1，输出 0, 如果是0，输出0.5, 如果是1，输出1。以此类推。

``` ShaderLab
// 输出渲染
float4 frag(a2v data) : SV_TARGET
{
    float3 mapping = data.normal / 2 + float3(0.5, 0.5, 0.5);
    // return 的数据，如果是都在0-1里面，默认0是黑色，1是白色。如果是在0-255里，默认0是黑色，255是白色。
    return float4(mapping, 1);
}
```

* **以下是整体代码**

``` ShaderLab
Shader "Custom/TestShader"
{
    SubShader
    {
        pass
        {
            CGPROGRAM

            struct a2v
            {
                float4 vertex : POSITION;       // 顶点坐标
                float3 normal : NORMAL;         // 法线
                float4 texcoord : TEXCOORD0;    // 第一纹理坐标
                // UNITY_VERTEX_INPUT_INSTANCE_ID  // ID信息
            }; // 记得打分号

            // 顶点着色器
            #pragma vertex vert


            a2v vert(a2v data)
            {
                // 模型顶点的世界坐标 => 屏幕坐标
                data.vertex = UnityObjectToClipPos(data.vertex);
                return data;
            }

            // 片元着色器
            #pragma fragment frag

            // 输出渲染
            float4 frag(a2v data) : SV_TARGET
            {
                float3 mapping = data.normal / 2 + float3(0.5, 0.5, 0.5);
                // return 的数据，如果是都在0-1里面，默认0是黑色，1是白色。如果是在0-255里，默认0是黑色，255是白色。
                return float4(mapping, 1);
            }

            ENDCG
        }
    }
    FallBack "Diffuse"
}
```
