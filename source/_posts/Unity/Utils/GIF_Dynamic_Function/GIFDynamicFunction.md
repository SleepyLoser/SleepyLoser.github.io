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

<img src="GIF结构.webp" alt="GIF的文件结构" style="zoom:100%;">

* GIF 格式的文件结构整体上分为三部分：文件头、GIF 数据流、文件结尾。其中，GIF 数据流分为全局配置和图像块。

### GIF署名（Signature）和版本号（Version）

* GIF 的前 6 个字节内容是 GIF 的署名和版本号。我们可以通过前 3 个字节判断文件是否为 GIF 格式，后 3 个字节判断 GIF 格式的版本。

<img src="GIFHeader.webp" alt="GIF署名和版本号" style="zoom:100%;">

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
    version = header.Substring(3, 3);
    sb.Clear();
    if (signature.Equals("GIF")) return true;
    return false;
}
```

### 逻辑屏幕标识符（Logical Screen Descriptor）

* 逻辑屏幕标识符配置了 GIF 一些全局属性，我们通过读取解析它，获取 GIF 全局的一些配置。

<img src="LogicalScreenDescriptor.webp" alt="GIF逻辑屏幕标识符" style="zoom:100%;">

* 屏幕逻辑宽度：定义了 GIF 图像的像素宽度，大小为 2 字节（第一个字节是 `基数` ，第二个字节是 `倍数` ，`宽度 = 基数 + 256 * 倍数`）;
* 屏幕逻辑高度：定义了 GIF 图像的像素高度，大小为 2 字节（同上）;
* 接下来是一个压缩字节：
  1. 第一个 `Bit` 为标志位，表示全局颜色列表是否存在。
  2. 接下来三个 `Bit` 表示图像调色板中每个颜色的原色所占用的 `Bit` 数，`011` 表示占用 `4` 个 `Bit` ，`111` 占用 `8` 个 `Bit`（三个 `Bit` 的二进制数值 + `1` ），以此类推。调色板最多只包含256个颜色（实际有很多优化方案能提高颜色分辨率，如加入局部调色板）。
  3. 第五个 `Bit` 为标志位，表示颜色列表排序方式。若为 `1` ，表示颜色列表是按照颜色在图像中出现的频率**降序排列**。
  4. 随后三个 `Bit` 表示全局颜色列表的大小，计算方法是 `2 ^ (N + 1)` ，其中 `N` 为这三个 `Bit` 的二进制数值。
* 背景颜色：背景颜色在全局颜色列表中的索引（PS：是索引而不是 `RGB` 值，所以如果没有全局颜色列表时，该值没有意义）;
* 像素宽高比：全局像素的宽度与高度的比值。大多数时候这个值都是 `0` ，若值为 `N` , 则 `图像的宽高比 = （N + 15）/ 64`。

``` CSharp
/// <summary>
/// GIF的屏幕逻辑宽度（以像素为单位）
/// </summary>
public UInt16 width;

/// <summary>
/// GIF的屏幕逻辑高度（以像素为单位）
/// </summary>
public UInt16 height;

/// <summary>
/// 全局标志
/// </summary>
[Serializable]
public struct GlobalFlag
{
    /// <summary>
    /// 全局颜色列表标志（全局颜色列表是否存在）
    /// </summary>
    public bool globalColorTableFlag;

    /// <summary>
    /// GIF的颜色深度（图像调色板中每个颜色的原色所占用的Bit数）
    /// </summary>
    public UInt16 colorResolution;

    /// <summary>
    /// 颜色列表排序方式（若为0，表示颜色列表是按照颜色在图像中出现的频率升序排列，若为1，降序）
    /// </summary>
    public bool sortFlag;

    /// <summary>
    /// 全局颜色列表大小
    /// </summary>
    public int globalColorTableSize;
}
public GlobalFlag globalFlag;

/// <summary>
/// 背景颜色在全局颜色列表中的索引
/// </summary>
public byte backgroundColorIndex;

/// <summary>
/// 全局像素的宽度与高度的比值
/// </summary>
public byte pixelAspectRatio;

/// <summary>
/// 解析逻辑屏幕标识符
/// </summary>
void AnalyzeLogicalScreenDescriptor()
{
    width = BitConverter.ToUInt16(bytes, index);
    index += 2;
    height = BitConverter.ToUInt16(bytes, index);
    index += 2;

    GlobalFlag globalFlag = new GlobalFlag();
    byte packedByte = bytes[index++];
    globalFlag.globalColorTableFlag = (packedByte & 0x80) != 0;
    globalFlag.colorResolution = (UInt16)((packedByte & 0b01110000) >> 4 + 1);
    globalFlag.sortFlag = (packedByte & 0b00001000) != 0;
    globalFlag.globalColorTableSize = 2 << (packedByte & 7);
    this.globalFlag = globalFlag;

    backgroundColorIndex = bytes[index++];
    pixelAspectRatio = bytes[index++];
}
```

* 颜色深度和颜色列表排序方式，这两者在实际中使用较少。此外 `pixelAspectRatio` 也只是读取，后续的解析中并没有使用到。

### 全局颜色列表(Global Color Table)

* 全局颜色列表，在逻辑屏幕标识之后，每个颜色索引由三字节组成，按 `RGB` 顺序排列（例如：全局颜色列表的大小是 `16` ，每个颜色占 `3` 个字节，按照 `RGB` 排列，所以它占有 `48` 个字节）。

<img src="全局颜色列表.webp" alt="GIF逻辑屏幕标识符" style="zoom:100%;">

* 整个 GIF 在每一帧的画面数组时，是不会出现 `RGB` 值的，画面中所有像素的 `RGB` 值，都是通过从全局 / 局部颜色列表中取得。可以让颜色列表理解为调色板。我需要什么 `RGB` ，我不直接写，而是写我想要 `RGB` 对应颜色列表的索引。这样做的好处，比如我想对 `GIF` 进行调色，如果我每一帧画面直接使用了 `RGB` ，那我每一帧都需要进行图像处理。有了调色盘，我只需要对调色板进行处理，每帧画面都会改变。
* `全局彩色表 (Global Color Table)` 存在与否由 `逻辑屏幕描述块 (LogicalScreen Descriptor)` 中 `字节5` 的 `全局彩色表标志 (GlobalColor Table Flag )` 的值确定。

``` CSharp
/// <summary>
/// GIF的全局颜色列表
/// </summary>
public byte[, ] globalColorTable = null;

/// <summary>
/// 获取GIF的全局颜色列表
/// </summary>
void GetGlobalColorTable()
{
    if (!globalFlag.globalColorTableFlag)
    {
        #if UNITY_EDITOR
        UnityEngine.Debug.LogWarning("该GIF不包含全局颜色列表");
        #endif
        return;
    }

    // int numberOfColorsByte = globalFlag.sizeOfGlobalColorTable * 3;
    globalColorTable = new byte[globalFlag.sizeOfGlobalColorTable, 3];
    int colorTableIndex = 0;
    try
    {
        while (colorTableIndex < globalFlag.sizeOfGlobalColorTable)
        {
            // int r = bytes[index++] & 0xff;
            // int g = bytes[index++] & 0xff;
            // int b = bytes[index++] & 0xff;
            // globalColorTable[colorTableIndex++] = 0xff000000 | (UInt32)(r << 16) | (UInt32)(g << 8) | (UInt32)b;
            globalColorTable[colorTableIndex, 0] = bytes[index++];
            globalColorTable[colorTableIndex, 1] = bytes[index++];
            globalColorTable[colorTableIndex, 2] = bytes[index++];
            ++colorTableIndex;
        }
        #if UNITY_EDITOR
        UnityEngine.Debug.Log("成功解析GIF全局颜色列表");
        #endif
    }
    catch (Exception e)
    {
        #if UNITY_EDITOR
        UnityEngine.Debug.LogError("获取全局颜色列表失败：" + e);
        #endif
        throw;
    }
}
```

* 至此，GIF 文件的全局配置就完成了，接下来是每一帧的配置 or 数据。

### 图像标识符（Image Descriptor）

* 一个GIF文件中可以有**多个图像块**，每个图像块都有图像标识符，描述了当前帧的一些属性。

<img src="ImageDescriptor.png" alt="图像标识符" style="zoom:100%;">

* 图像标识符以 `','(0x2c)` 作为开始标志。接着定义了当前帧的 `偏移量` 和 `宽高` 。
* 最后5个标志的意义分别为：
  1. `m` - 局部颜色列表标志（Local Color Table Flag）：若为1，表示有一个局部彩色表（Local Color Table）将紧跟在这个图像描述块（ImageDescriptor）之后；若为0，表示图像描述块（Image Descriptor）后面没有局部彩色表(Local Color Table)，该图像要使用全局彩色表（GlobalColor Table），忽略 `pixel` 值。
  2. `i` - 交插显示标志(Interlace Flag)：若为0，表示该图像不是交插图像；若为1表示该图像是交插图像。使用该位标志可知道图像数据是如何存放的。
  3. `s` - 局部颜色排序标志：与全局彩色表（GlobalColor Table）中（Sort Flag）域的含义相同。
  4. `r` - 保留（未被使用，必须初始化为0）。
  5. `pixel` - 局部颜色列表大小：用来计算局部彩色表（Global Color Table）中包含的字节数。
* 交插显示标志将在图片的解码时单独解释。

#### 基于颜色列表的图像数据

* 基于颜色列表的图像数据**必须**紧跟在图像标识符后面。数据的第一个字节表示 `LZW` 编码初始表大小的位数。**图像数据（Image Data）由数据子块（Data Sub-blocks）序列组成**。

<img src="基于颜色列表的图像数据.webp" alt="基于颜色列表的图像数据" style="zoom:100%;">

* 数据块的结构：

<img src="数据块的结构.webp" alt="数据块的结构" style="zoom:100%;">

* 每个数据块，第一个字节表示当前块的大小，这个大小不包括第一个字节。这是一个可变长度的数据块，字节数在 `0 ~ 255` 之间。

``` CSharp
/// <summary>
/// 图像标识符
/// </summary>
[Serializable]
public struct ImageDescriptor
{
    /// <summary>
    /// 图像标识符分割符（固定为0x2c，即 ',' ）
    /// </summary>
    public byte imageSeparator;

    /// <summary>
    /// X方向偏移量
    /// </summary>
    public UInt16 xOffset;

    /// <summary>
    /// Y方向偏移量
    /// </summary>
    public UInt16 yOffset;

    /// <summary>
    /// 图像宽度
    /// </summary>
    public UInt16 imageWidth;

    /// <summary>
    /// 图像高度
    /// </summary>
    public UInt16 imageHeight;

    /// <summary>
    /// 局部颜色列表标志（若为1，表示有一个局部彩色表（Local Color Table）将紧跟在这个图像描述块（ImageDescriptor）之后；若为0，表示图像描述块（Image Descriptor）后面没有局部彩色表（Local Color Table），该图像要使用全局彩色表（GlobalColor Table））
    /// </summary>
    public bool localColorTableFlag;

    /// <summary>
    /// 交插显示标志（若为0，表示该图像不是交插图像；若为1表示该图像是交插图像。使用该位标志可知道图像数据是如何存放的）
    /// </summary>
    public bool interlaceFlag;

    /// <summary>
    /// 局部颜色排序标志（与全局彩色表（GlobalColor Table）中（Sort Flag）域的含义相同）
    /// </summary>
    public bool sortFlag;

    /// <summary>
    /// 保留（未被使用，必须初始化为0）
    /// </summary>
    public byte reserved;

    /// <summary>
    /// 局部颜色列表大小（用来计算局部彩色表（Global Color Table）中包含的字节数）
    /// </summary>
    public int localColorTableSize;

    /// <summary>
    /// 用于存储GIF的局部颜色列表（如果存在的话）
    /// </summary>
    public byte[, ] localColorTable;

    /// <summary>
    /// LZW编码初始表大小的位数
    /// </summary>
    public byte lzwMinimumCodeSize;

    /// <summary>
    /// 图像数据（块）
    /// </summary>
    [Serializable]
    public struct ImageData
    {
        /// <summary>
        /// 块大小，不包括blockSize所占的这个字节
        /// </summary>
        public int blockSize;

        /// <summary>
        /// 块数据，8-bit的字符串
        /// </summary>
        public byte[] dataValue;
    }
}

/// <summary>
/// 用于存储GIF的图像标识符（按照帧顺序排列）
/// </summary>
public List<ImageDescriptor> imageDescriptors = new List<ImageDescriptor>();

/// <summary>
/// 用于存储GIF的图像数据块（按照帧顺序排列）
/// </summary>
public List<ImageDescriptor.ImageData> imageDatas = new List<ImageDescriptor.ImageData>();

/// <summary>
/// 获取GIF的图像标识符
/// </summary>
void GetImageDescriptor()
{
    ImageDescriptor imageDescriptor = new ImageDescriptor();
    imageDescriptor.imageSeparator = bytes[index++];

    imageDescriptor.xOffset = BitConverter.ToUInt16(bytes, index);
    index += 2;
    imageDescriptor.yOffset = BitConverter.ToUInt16(bytes, index);
    index += 2;
    imageDescriptor.imageWidth = BitConverter.ToUInt16(bytes, index);
    index += 2;
    imageDescriptor.imageHeight = BitConverter.ToUInt16(bytes, index);
    index += 2;
    
    byte packedByte = bytes[index++];
    imageDescriptor.localColorTableFlag = (packedByte & 0x80) != 0;
    imageDescriptor.interlaceFlag = (packedByte & 0x40) != 0;
    imageDescriptor.sortFlag = (packedByte & 0x20) != 0;
    imageDescriptor.reserved = 0;
    imageDescriptor.localColorTableSize = (int)Math.Pow(2, (packedByte & 0x07) + 1);

    if (imageDescriptor.localColorTableFlag)
    {
        imageDescriptor.localColorTable = new byte[imageDescriptor.localColorTableSize, 3];
        int colorTableIndex = 0;
        while (colorTableIndex < imageDescriptor.localColorTableSize)
        {
            imageDescriptor.localColorTable[colorTableIndex, 0] = bytes[index++];
            imageDescriptor.localColorTable[colorTableIndex, 1] = bytes[index++];
            imageDescriptor.localColorTable[colorTableIndex, 2] = bytes[index++];
            ++colorTableIndex;
        }
    }

    imageDescriptor.lzwMinimumCodeSize = bytes[index++];

    // 数据块，如果需要可重复多次
    while (true)
    {
        int blockSize = bytes[index++];
        if (blockSize.Equals(0x00))
        {
            // #if UNITY_EDITOR
            // UnityEngine.Debug.Log("基于颜色列表的图像数据为空");
            // #endif
            break;
        }

        ImageDescriptor.ImageData imageData = new ImageDescriptor.ImageData();
        imageData.blockSize = blockSize;
        imageData.dataValue = new byte[blockSize];
        for (int i = 0; i < blockSize; ++i)
        {
            imageData.dataValue[i] = bytes[index++];
        }

        imageDatas.Add(imageData);
    }
    
    imageDescriptors.Add(imageDescriptor);
    // #if UNITY_EDITOR
    // UnityEngine.Debug.LogFormat("成功解析图像标识符{0}", imageDescriptors.Count);
    // #endif
}
```

### 图形控制扩展（Graphic Control Extension）
