---
title: 纹理工具 
top_img: '121250572_p0.png'
cover: '74451735_p0.png'
permalink: /Unity/Utils/TextureUtils/
categories: 
    - Unity
      - 工具类 
tags: 
    - 实用工具
    - 纹理
---

## 纹理工具

### 前言

* [Unity Texture 基础](/Unity/Graphics/Texture_Fundamentals/)
* [如何正确序列化Texture](/Unity/Utils/Texture_To_Json/)

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

### 存储纹理的结构体

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
/// <summary>
/// 从Image组件中获取纹理数据
/// </summary>
/// <param name="image">想要提取纹理数据的Image组件</param>
/// <param name="atlasTexture">Image组件中精灵图的纹理</param>
/// <returns>Image组件中的纹理数据</returns>
public static TextureData GetTexture2DFromImage (Image image, Texture2D atlasTexture)
{
    float textureWidth;
    float textureHeight;
    float textureX = 0;
    float textureY = 0;
    // 如果Image的SourceImage使用的是原图
    if (image.mainTexture == null || (image.sprite.rect.width == atlasTexture.width &&
        image.sprite.rect.height == atlasTexture.height))
    {
        textureWidth = image.sprite.texture.width;
        textureHeight = image.sprite.texture.height;
    }
    // 如果Image的SourceImage使用的是图集
    else
    {
        textureWidth = image.sprite.textureRect.width;
        textureHeight = image.sprite.textureRect.height;
        textureY = image.sprite.textureRect.y;
        textureX = image.sprite.textureRect.x;
    }

    TextureData textureData = new TextureData()
    {
        rectX = textureX.Equals(0) ? 0 : textureX / propertyScaleImage,
        rectY = textureY.Equals(0) ? 0 : textureY / propertyScaleImage,
        rectWidth = textureWidth.Equals(0) ? 0 : textureWidth / propertyScaleImage,
        rectHeight = textureHeight.Equals(0) ? 0 : textureHeight / propertyScaleImage,
        rectOffsetX = image.sprite.textureRectOffset.x.Equals(0) ? 0 : image.sprite.textureRectOffset.x / propertyScaleImage,
        rectOffsetY = image.sprite.textureRectOffset.y.Equals(0) ? 0 : image.sprite.textureRectOffset.y / propertyScaleImage,
        width = image.sprite.rect.width.Equals(0) ? 0 : (int)image.sprite.rect.width / propertyScaleImage,
        height = image.sprite.rect.height.Equals(0) ? 0 : (int)image.sprite.rect.height / propertyScaleImage,
        name = image.sprite.name.Equals("") ? image.GetHashCode().ToString() : image.sprite.name,
    };

    if (atlasTexture.name.Equals(""))
    {
        textureData.atlasName = image.name + "_" + atlasTexture.GetHashCode();
    }
    else
    {
        textureData.atlasName = atlasTexture.name;
    }
    // textureData.atlasPath = ;
    textureData.creationDate = DateTimeOffset.Now.ToUnixTimeSeconds() + textureData.atlasName;

    return textureData;
}
```

### 从RawImage组件中获取纹理数据

``` C#

```

### 获取Texture2D的渲染数据

``` C#
// 精灵图缩放比例
public static int propertyScaleImage = 1;

/// <summary>
/// 获取Texture2D的渲染数据
/// </summary>
/// <param name="source">源纹理</param>
/// <param name="renderTex">指定的渲染纹理</param>
/// <param name="sourceMaterial">要使用的材质。如果您不提供材质，将使用默认材质。</param>
/// <returns>Texture2D的渲染数据</returns>
public static Texture2D CopyTextureByBlit(Texture2D source, RenderTexture renderTex = null, Material sourceMaterial = null)
{

    Material material = new Material(sourceMaterial);
    material.SetVector("_ClipRect", new Vector4(0, 0, 0, 1));

    // 如果使用的是内置渲染管线，则当 dest 为 null 时，Unity 将屏幕后备缓冲区用作 blit 目标。
    // 但是，如果将主摄像机设置为渲染到 RenderTexture（即 Camera.main 具有非 null 的 targetTexture 属性）则 blit 使用主摄像机的渲染目标作为目标。
    // 为确保 blit 确实写入到屏幕后备缓冲区，在调用 Blit 前请务必将 Camera.main.targetTexture 设置为 null。

    RenderTexture currentCameraRenderTexture = null;
    if (renderTex == null)
    {
        
        if (Camera.main != null)
        {
            currentCameraRenderTexture = Camera.main.targetTexture;
        }
        if (currentCameraRenderTexture != null)
        {
            Camera.main.targetTexture = null;
        }
    }

    Graphics.Blit(source, renderTex, material);

    RenderTexture previous = RenderTexture.active;
    RenderTexture.active = renderTex;

    Texture2D readableTexture = new Texture2D(source.width, source.height);
    readableTexture.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);

    RenderTexture.active = previous;
    if (Camera.main != null && currentCameraRenderTexture != null)
    {
        Camera.main.targetTexture = currentCameraRenderTexture;
    }

    return readableTexture;
}
```

### 获取Texture的渲染数据

``` C#
/// <summary>
/// 获取Texture的渲染数据（复制纹理最快的方法，但，只能复制纹理，无法复制材质信息，源纹理要可读且无法复制压缩纹理）
/// </summary>
/// <param name="sourceTexture">源纹理</param>
/// <returns>Texture的渲染数据</returns>
public static Texture2D CopyTextureByCT(Texture sourceTexture)
{
    if (!sourceTexture.isReadable || sourceTexture == null)
    {
        return null;
    }
    Texture2D myTexture2D = new Texture2D(sourceTexture.width, sourceTexture.height, TextureFormat.ARGB32, false);
    Graphics.CopyTexture(sourceTexture, myTexture2D);
    myTexture2D.Apply();
    return myTexture2D;
}
```
