---
title: 在 Unity 中实现 GIF 动态功能 
top_img: '121400138_p0.png'
cover: '106999447_p0.png'
categories: 
    - Unity
      - 工具类 
tags: 
    - 实用工具
---

## 引言

* Unity 本身是不支持 GIF 图的直接播放的，不过有多种方法可以间接实现，
  1. 通过 `.net` 的 `Drawing库` 来实现 GIF 图片解析，不过**只能在 windows 平台使用**。
  2. 使用 `AE` 等动画软件先将 GIF 转成序列帧，然后再在 Unity 中作为动画调用。
  3. 在脚本中手动解码 GIF 文件，然后将 GIF 的帧信息缓存在数组中，进行轮播。
* 本文主要介绍第三种方法。目的是将 GIF 图变为和普通图片一样，可以导入到Unity中，并挂载在脚本中运行。具体分为三个步骤，导入、解码和播放。

## 导入 GIF 图

* Unity默认会将 `.gif` 文件导入成 `TextureAsset` ，这显然不是我们需要的，解码 GIF 图需要文件的所有二进制信息，因此此处需要自定义一种 Asset ，代码如下。

``` CSharp
using UnityEngine;

public class GifData : ScriptableObject
{
    public byte[] gifBytes;
    public void SetGifBytes(byte[] gifBytes)
    {
        this.gifBytes = gifBytes;
    }
}
```

* 然后在导入 GIF 图的时候，把GIF图的数据存入 `gifData` 里的二进制数组中。麻烦的是， Unity 虽然不支持 GIF 图，但是会识别 `.gif` 文件。此处我的做法是给 GIF 图增加一个后缀 `.bytes` 。 `bytes` 文件默认会导入成文本资产`（TextAsset）`。通过 `textAsset.bytes` 就可以轻松获取到 GIF 图的二进制数据。具体代码如下。

``` CSharp
using System.IO;
using UnityEditor;
using UnityEngine;

public class GifPostprocessor : AssetPostprocessor
{
    public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach (string assetPath in importedAssets)
        {
            if (assetPath.EndsWith(".gif.bytes"))
            {
                TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
                if (textAsset != null)
                {
                    GifData gifData = ScriptableObject.CreateInstance<GifData>();
                    gifData.SetGifBytes(textAsset.bytes);
                    string gifDataAssetPath = Path.ChangeExtension(assetPath, ".asset");
                    AssetDatabase.CreateAsset(gifData, gifDataAssetPath);
                    // 自动为 .gif.bytes 文件创建一个 TextAsset , 删除原始的 TextAsset
                    AssetDatabase.DeleteAsset(assetPath);
                }
            }
        }
    }
}
```

* 上述代码的作用是后处理的过程中，识别出所有的 `.gif.bytes` 文件，创建一个 `GifData` 资产，并且删掉原有的文本资产。此时，第一步导入 GIF 图已经完成。

## 解码 GIF 图

* GIF 的文件结构

<img src="GIF结构.webp" alt="GIF的文件结构" style="zoom:50%;">

* GIF 格式的文件结构整体上分为三部分：文件头、GIF 数据流、文件结尾。其中，GIF 数据流分为全局配置和图像块。

### GIF署名（Signature）和版本号（Version）

* GIF 的前 6 个字节内容是 GIF 的署名和版本号。我们可以通过前 3 个字节判断文件是否为 GIF 格式，后 3 个字节判断 GIF 格式的版本。

<img src="GIFHeader.webp" alt="GIF署名和版本号" style="zoom:50%;">

``` CSharp
/// <summary>
/// 遍历字节数组时的下标
/// </summary>
int index = 0;

/// <summary>
/// GIF的二进制数据
/// </summary>
readonly byte[] bytes;

readonly StringBuilder sb = new StringBuilder();

/// <summary>
/// GIF署名
/// </summary>
public string signature;

/// <summary>
/// GIF版本号（87a或89a）
/// </summary>
public string version;

/// <summary>
/// 获取GIF署名（Signature）和版本号（Version）
/// </summary>
/// <returns>该二进制数据是否为GIF的二进制数据</returns>
bool GetHeader()
{
    for (; index < 6; ++index)
    {
        sb.Append(Convert.ToChar(bytes[index]));
    }
    string header = sb.ToString();
    signature = header.Substring(0, 3);
    version = header.Substring(3, 6);
    sb.Clear();
    if (signature.Equals("GIF")) return true;
    return false;
}
```

### 逻辑屏幕标识符（Logical Screen Descriptor）

* 逻辑屏幕标识符配置了 GIF 一些全局属性，我们通过读取解析它，获取 GIF 全局的一些配置。

<img src="LogicalScreenDescriptor.webp" alt="GIF逻辑屏幕标识符" style="zoom:50%;">

* 屏幕逻辑宽度：定义了 GIF 图像的像素宽度，大小为 2 字节（第一个字节是 `基数` ，第二个字节是 `倍数` ，`宽度 = 基数 + 256 * 倍数`）;
* 屏幕逻辑高度：定义了 GIF 图像的像素高度，大小为 2 字节（同上）;
* 接下来是一个压缩字节：
  1. 第一个 `Bit` 为标志位，表示全局颜色列表是否存在。
  2. 接下来三个 `Bit` 表示图像调色板中每个颜色的原色所占用的 `Bit` 数，`011` 表示占用 `4` 个 `Bit` ，`111` 占用 `8` 个 `Bit`（三个 `Bit` 的二进制数值 + `1` ），以此类推。调色板最多只包含256个颜色（实际有很多优化方案能提高颜色分辨率，如加入局部调色板）。
  3. 第五个 `Bit` 为标志位，表示颜色列表排序方式。若为 `1` ，表示颜色列表是按照颜色在图像中出现的频率**降序排列**。
  4. 随后三个 `Bit` 表示全局颜色列表的大小，计算方法是 `2 ^ (N + 1)` ，其中 `N` 为这三个 `Bit` 的二进制数值。
* 背景颜色：背景颜色在全局颜色列表中的索引（PS：是索引而不是 `RGB` 值，所以如果没有全局颜色列表时，该值没有意义）;
* 像素宽高比：全局像素的宽度与高度的比值。

``` CSharp
/// <summary>
/// GIF的宽度（以像素为单位）
/// </summary>
public UInt16 width;

/// <summary>
/// GIF的高度（以像素为单位）
/// </summary>
public UInt16 height;

/// <summary>
/// 全局标志
/// </summary>
public struct GlobalFlag
{
    /// <summary>
    /// 全局颜色列表标志（全局颜色列表是否存在）
    /// </summary>
    public static bool globalColorTableFlag;

    /// <summary>
    /// GIF的颜色深度（图像调色板中每个颜色的原色所占用的Bit数）
    /// </summary>
    public static UInt16 colorResolution;

    /// <summary>
    /// 颜色列表排序方式（若为0，表示颜色列表是按照颜色在图像中出现的频率升序排列，若为1，降序）
    /// </summary>
    public static bool sortFlag;

    /// <summary>
    /// 全局颜色列表大小
    /// </summary>
    public static int globalColorTableSize;
}

/// <summary>
/// 背景颜色在全局颜色列表中的索引
/// </summary>
public int backgroundColorIndex;

/// <summary>
/// 全局像素的宽度与高度的比值
/// </summary>
public int pixelAspectRatio;

/// <summary>
/// 解析逻辑屏幕标识符
/// </summary>
void AnalyzeLogicalScreenDescriptor()
{
    width = BitConverter.ToUInt16(bytes, index);
    index += 2;
    height = BitConverter.ToUInt16(bytes, index);
    index += 2;

    byte packedByte = bytes[index++];
    GlobalFlag.globalColorTableFlag = (packedByte & 0x80) != 0;
    GlobalFlag.colorResolution = (UInt16)((packedByte & 0b01110000) >> 4 + 1);
    GlobalFlag.sortFlag = (packedByte & 0b00001000) != 0;
    GlobalFlag.globalColorTableSize = 2 << (packedByte & 7);

    backgroundColorIndex = Convert.ToInt32(bytes[index++]);
    pixelAspectRatio = Convert.ToInt32(bytes[index++]);
}
```

* 颜色深度和颜色列表排序方式，这两者在实际中使用较少。此外 `pixelAspectRatio` 也只是读取，后续的解析中并没有使用到。
