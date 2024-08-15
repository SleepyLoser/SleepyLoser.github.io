---
title: 纹理工具 
top_img: '121250572_p0.png'
cover: '121409992_p0.png'
categories: 
    - Unity
      - 工具类 
tags: 
    - 实用工具
    - 纹理
---

## 纹理工具

### 前言

* [Unity Texture 基础](https://sleepyloser.github.io/2024/07/15/Unity/Graphics/Texture_Fundamentals/Texture/)
* [如何正确序列化Texture](https://sleepyloser.github.io/2024/07/29/Unity/Utils/Texture_To_Json/TextureToJson/)

#### Texture（UI）和Sprite（UI）的区别

* Texture是一张独立的图片，不依托于任何图集，这张Texture有自己的材质和Shader，每一个Texture都将消耗一个DrawCall去渲染，每一个Texture都将独立进行加载。
* Texture作为低层次的图像数据结构，需要考虑的是如何高效地存储和访问像素数据。它需要支持各种图像处理操作和着色器操作，因此其设计更为通用。
* Sprite是为2D图形和UI设计的图像类型，提供了丰富的2D图形处理功能，如九宫格切片和填充模式。**是Unity为拓展Texture的功能而创造的一种格式**。
* Sprite作为高层次的2D图形结构，需要支持一系列2D图形特性，如九宫格切片（9-slicing）、动画、多帧处理等。这些特性是为2D渲染和UI优化设计的，使用Sprite可以更高效地进行2D渲染和批处理。

##### 什么时候使用Texture

1. 当图片过大，不适合合成图集的时候，可以使用Texture，此时要**尽量的保证图片的宽高是2的N次方**（1. 底层图形学只识别2的N次方的图片，OpenGL**仅支持**分辨率为2^m * 2^n的纹理，非2的N次方的图片会转化为2的N次方的图片，这个转化的过程十分耗时; 2. ios pvrtc的原因，有些GPU不支持NPOT，遇到NPOT会有一个转换POT的过程，浪费性能）。
2. 当图片为2的N次方，但出现的频率不高时，可以使用Texture。例如游戏的背景和Logo。
3. 修改更改特别频繁的图片，为了减少每次更新维护的麻烦，可以考了使用Texture。
4. 如果图片很小且多处会使用到（例如按钮）尽量将图片放入图集，通过精灵的方式使用。

* 总结：Texture的功能是在屏幕上显示一张图片，在这一点上它和Sprite有着相似的功能，但是UITexture会消耗单独的DrawCall去渲染，并会单独加载进内存，所以会增大程序性能的开销。

##### 什么时候使用Sprite

1. 如果要显示一张图片，它的形状不规则，长宽不是2的N次方，那么一定要使用Sprite。因为unity对非2的N次方的图片处理要慢很多。
2. 如果这个UI元件频繁的出现，那么最好使用UISprite，因为这样它就可以和图集一起被载入内存，并不用新增一个DrawCall去渲染它。
3. 对于一些展示型的图片，不会变化，只是起一个展示的作用，例如弹框上的花纹装饰，顶部的花圈, 一般都以Sprite的方式来制作和展示。

### 存储纹理的数据结构

``` C#
[Serializable]
public struct TextureData
{
    // 纹理名称
    public string name;

    // 图集名称（如果存在）
    public string atlasName;
    
    // 图集路径（如果存在）
    public string atlasPath;
    
    // public string md5;   //hash code
    
    // 矩形
    public float rectX;
    
    // 矩形
    public float rectY;
    
    // 矩形
    public float rectWidth;

    // 矩形
    public float rectHeight;

    // 矩形
    public float rectOffsetX;

    // 矩形
    public float rectOffsetY;
    
    // 资源实际宽度，与矩形宽度有可能不一样，因为在图集中会被剪切透明区域
    public int width;

    // 实际高度
    public int height;

    // 缩放比例
    public int propertyScale;

    // 创建日期
    public string creationDate;

    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }
}
```

### 从Image组件中获取纹理数据

``` C#

```
