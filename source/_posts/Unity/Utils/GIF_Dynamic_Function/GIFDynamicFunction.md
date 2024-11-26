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
    /// <summary>
    /// GIF的解码器，可获取解码内容
    /// </summary>
    [HideInInspector] public GifDecoder gifDecoder;
}
```

* 然后在导入 GIF 图的时候，把GIF图的数据存入 `gifData` 里的二进制数组中。麻烦的是， Unity 虽然不支持 GIF 图，但是会识别 `.gif` 文件。此处我的做法是给 GIF 图增加一个后缀 `.bytes` 。 `bytes` 文件默认会导入成文本资产`（TextAsset）`。通过 `textAsset.bytes` 就可以轻松获取到 GIF 图的二进制数据。具体代码如下。

``` CSharp
using System.IO;
using UnityEditor;
using UnityEngine;

public class GifPostprocessor : AssetPostprocessor
{
    /// <summary>
    /// 用于对导入的资源进行预处理
    /// </summary>
    /// <param name="importedAssets">所有导入的文件的路径</param>
    /// <param name="deletedAssets">未使用</param>
    /// <param name="movedAssets">未使用</param>
    /// <param name="movedFromAssetPaths">未使用</param>
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
                    GifDecoder gifDecoder = new GifDecoder(gifData, textAsset.bytes);

                    string gifDataAssetPath = Path.ChangeExtension(assetPath, ".asset");
                    AssetDatabase.CreateAsset(gifData, gifDataAssetPath);
                    // 自动为.gif.bytes文件创建一个TextAsset , 删除原始的TextAsset
                    AssetDatabase.DeleteAsset(assetPath);
                }
            }
        }
    }
}

// 附 GifDecoder 的构造函数 与 解码入口

/// <summary>
/// GifDecoder的构造函数
/// </summary>
/// <param name="gifData">GifData类型的数据</param>
/// <param name="bytes">GIF的二进制数据</param>
public GifDecoder(GifData gifData, byte[] bytes)
{
    this.bytes = bytes;
    GifDecode();
    gifData.gifDecoder = this;
}

/// <summary>
/// GIF解码入口
/// </summary>
void GifDecode()
{
    GetHeader();
    AnalyzeLogicalScreenDescriptor();
    GetGlobalColorTable();
    // -------------------- 完成全局配置
    GetGifBlock();
}
// 函数详情见下文
```

* 上述代码的作用是后处理的过程中，识别出所有的 `.gif.bytes` 文件，创建一个 `GifData` 资产，并且删掉原有的文本资产。此时，第一步导入 GIF 图已经完成。

## 解码 GIF（以文章末的源码为标准，文章中的代码仅供参考，不保证正确性（其实是懒得改））

* GIF 的文件结构

<img src="GIF结构.webp" alt="GIF的文件结构" style="zoom:100%;">

* GIF 格式的文件结构整体上分为三部分：文件头、GIF 数据流、文件结尾。其中，GIF 数据流分为全局配置和图像块。
* GIF 使用**大端**存储字节

### GIF署名（Signature）和版本号（Version）

* GIF 的前 6 个字节内容是 GIF 的署名和版本号。我们可以通过前 3 个字节判断文件是否为 GIF 格式，后 3 个字节判断 GIF 格式的版本。

<img src="GIFHeader.webp" alt="GIF署名和版本号" style="zoom:100%;">

``` CSharp
/// <summary>
/// 遍历字节数组时的下标
/// </summary>
private int index = 0;

/// <summary>
/// GIF的二进制数据
/// </summary>
private readonly byte[] bytes;

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

### 图像块（Image Block）

* 一个GIF文件中可以有**多个图像块**，每个图像块都有**图像标识符（Image Descriptor）**，描述了当前帧的一些属性。

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
/// 图像块
/// </summary>
[Serializable]
public struct ImageBlock
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
    /// 用于存储图像块中的数据块
    /// </summary>
    public List<ImageDataBlock> imageDataBlocks;

    /// <summary>
    /// 图像数据（块）
    /// </summary>
    [Serializable]
    public struct ImageDataBlock
    {
        /// <summary>
        /// 块大小，不包括blockSize所占的这个字节
        /// </summary>
        public UInt16 blockSize;

        /// <summary>
        /// 块数据，8-bit的字符串
        /// </summary>
        public byte[] data;
    }
}

/// <summary>
/// 用于存储GIF的图像块（按照帧顺序排列）
/// </summary>
public List<ImageBlock> imageBlocks = null;

/// <summary>
/// 获取GIF的图像块
/// </summary>
void GetImageBlock()
{
    ImageBlock imageBlock = new ImageBlock();
    imageBlock.imageSeparator = bytes[index++];

    imageBlock.xOffset = BitConverter.ToUInt16(bytes, index);
    index += 2;
    imageBlock.yOffset = BitConverter.ToUInt16(bytes, index);
    index += 2;
    imageBlock.imageWidth = BitConverter.ToUInt16(bytes, index);
    index += 2;
    imageBlock.imageHeight = BitConverter.ToUInt16(bytes, index);
    index += 2;
    
    byte packedByte = bytes[index++];
    imageBlock.localColorTableFlag = (packedByte & 0x80) != 0;
    imageBlock.interlaceFlag = (packedByte & 0x40) != 0;
    imageBlock.sortFlag = (packedByte & 0x20) != 0;
    imageBlock.reserved = 0;
    imageBlock.localColorTableSize = (int)Math.Pow(2, (packedByte & 0x07) + 1);

    if (imageBlock.localColorTableFlag)
    {
        imageBlock.localColorTable = new byte[imageBlock.localColorTableSize, 3];
        int colorTableIndex = 0;
        while (colorTableIndex < imageBlock.localColorTableSize)
        {
            imageBlock.localColorTable[colorTableIndex, 0] = bytes[index++];
            imageBlock.localColorTable[colorTableIndex, 1] = bytes[index++];
            imageBlock.localColorTable[colorTableIndex, 2] = bytes[index++];
            ++colorTableIndex;
        }
    }

    imageBlock.lzwMinimumCodeSize = bytes[index++];

    // 图像数据块，如果需要可重复多次
    while (true)
    {
        UInt16 blockSize = bytes[index++];
        if (blockSize.Equals(0x00))
        {
            // #if UNITY_EDITOR
            // UnityEngine.Debug.Log("基于颜色列表的图像数据为空");
            // #endif
            break;
        }

        ImageBlock.ImageDataBlock imageDataBlock = new ImageBlock.ImageDataBlock();
        imageDataBlock.blockSize = blockSize;
        imageDataBlock.data = new byte[blockSize];
        for (int i = 0; i < blockSize; ++i)
        {
            imageDataBlock.data[i] = bytes[index++];
        }

        if (imageDatas == null)
        {
            imageDatas = new List<ImageBlock.ImageDataBlock>();
        }
        imageDatas.Add(imageDataBlock);
    }
    
    if (imageBlocks == null)
    {
        imageBlocks = new List<ImageBlock>();
    }
    imageBlocks.Add(imageBlock);
}
```

### 图形控制扩展（Graphic Control Extension）

* 在 `89a` 版本，GIF 添加了图形控制扩展块。放在一个图像块(图像标识符)的**前面**，用来控制紧跟在它后面的第一个图像的显示。

<img src="图形控制拓展.webp" alt="图形控制拓展" style="zoom:100%;">

* 对于上图中的第四个字节的八位：
  * 前三位（7~5）：保留（未使用）。
  * 中间三位（4~2）**处置方法（Disposal Method）**：指出处置图形的方法，当值为：
    1. `0` - 不使用处置方法。
    2. `1` - 不处置图形，把图形从当前位置移去。
    3. `2` - 恢复到背景色。
    4. `3` - 恢复到先前状态。
    5. `4 ~ 7` - 保留（未使用 / 未定义）
  * 倒数第二位（1）- **自定义用户输入标志（User Input Flag）**：指出是否期待用户有输入之后才继续进行下去，值为真表示期待，值为否表示不期待。用户输入可以是按回车键、鼠标点击等，可以和延迟时间一起使用。在设置的延迟时间内用户有输入则马上继续进行，没有输入则等待延迟时间结束再继续。利用延迟时间，我们可以展示出速度不均匀的 GIF 。
  * 倒数第一位（0）**透明颜色标志（Transparent Color Flag）**：值为真表示使用透明颜色。解码器会通过透明色索引在颜色列表中找到改颜色，标记为透明，当渲染图像时，标记为透明色的颜色将不会绘制，显示下面的背景。

``` CSharp
/// <summary>
/// 图形控制扩展
/// </summary>
[Serializable]
public struct GraphicControlExtension
{
    /// <summary>
    /// 标识这是一个扩展块，固定值 0x21
    /// </summary>
    public byte extensionIntroducer;

    /// <summary>
    /// 标识这是一个图形控制扩展块，固定值 0xF9
    /// </summary>
    public byte GraphicControlLabel;

    /// <summary>
    /// 图形控制扩展块大小，不包括块终结器，固定值 4
    /// </summary>
    public byte blockSize;

    /// <summary>
    /// `0` - 不使用处置方法；
    /// `1` - 不处置图形，把图形从当前位置移去；
    /// `2` - 恢复到背景色；
    /// `3` - 恢复到先前状态；
    /// </summary>
    public UInt16 disposalMethod;

    /// <summary>
    /// 值为 1 表示使用透明颜色
    /// </summary>
    public bool transparentColorFlag;

    /// <summary>
    /// 单位 1/100 秒，如果值不为 1 ，表示暂停规定的时间后再继续往下处理数据流
    /// </summary>
    public UInt16 delayTime;

    /// <summary>
    /// 透明色索引值
    /// </summary>
    public byte transparentColorIndex;

    /// <summary>
    /// 标识块终结，固定值 0
    /// </summary>
    public byte blockTerminator;
}
/// <summary>
/// 用于存储GIF的图形控制扩展（按照帧顺序排列）
/// </summary>
public List<GraphicControlExtension> graphicControlExtensions = null;

/// <summary>
/// 获取图形控制扩展，标识符固定值为0xf9
/// </summary>
void GetGraphicControlExtension()
{
    GraphicControlExtension graphicControlExtension = new GraphicControlExtension();

    graphicControlExtension.extensionIntroducer = 0x21;
    graphicControlExtension.GraphicControlLabel = bytes[index++];
    graphicControlExtension.blockSize = bytes[index++];

    switch (bytes[index] & 28) // 28 : 0b00011100
    {
        case 4: // 4 : 0b00000100
            graphicControlExtension.disposalMethod = 1;
            break;
        case 8: // 8 : 0b00001000
            graphicControlExtension.disposalMethod = 2;
            break;
        case 12: // 12 : 0b00001100
            graphicControlExtension.disposalMethod = 3;
            break;
        default:
            graphicControlExtension.disposalMethod = 0;
            break;
    }
    graphicControlExtension.transparentColorFlag = (bytes[index++] & 1) != 0;

    graphicControlExtension.delayTime = BitConverter.ToUInt16(bytes, index);
    index += 2;

    graphicControlExtension.transparentColorIndex = bytes[index++];

    graphicControlExtension.blockTerminator = bytes[index++];

    if (graphicControlExtensions == null)
    {
        graphicControlExtensions = new List<GraphicControlExtension>();
    }
    graphicControlExtensions.Add(graphicControlExtension);
}
```

### 注释扩展块（Comment Extension）

* 注释扩展块的内容用来说明图形、作者或者其他任何非图形数据和控制信息的文本信息。结构如下图，其中的注释数据是序列数据子块（Data Sub-blocks），每块最多 255 个字节，最少 1 个字节。

<img src="注释扩展块.png" alt="注释扩展块" style="zoom:100%;">

``` CSharp
/// <summary>
/// 注释扩展块
/// </summary>
[Serializable]
public struct CommentExtension
{
    /// <summary>
    /// 标识符，固定值 0x21
    /// </summary>
    public byte extensionIntroducer;

    /// <summary>
    /// 注释标签，固定值 0xfe
    /// </summary>
    public byte commentLabel;

    /// <summary>
    /// 块结束符，固定值 0x00
    /// </summary>
    public byte blockTerminator;

    /// <summary>
    /// 注释数据块列表
    /// </summary>
    public List<CommentDataBlock> commentDataBlocksList;

    /// <summary>
    /// 注释扩展块中的注释数据块
    /// </summary>
    [Serializable]
    public struct CommentDataBlock
    {
        /// <summary>
        /// 注释数据大小
        /// </summary>
        public byte blockSize;
        /// <summary>
        /// 注释数据
        /// </summary>
        public byte[] commentData;
    }
}

/// <summary>
/// 用于存储GIF的注释扩展块（按照帧顺序排列）
/// </summary>
public List<CommentExtension> commentExtensions = null;

/// <summary>
/// 获取注释扩展块
/// </summary>
void GetCommentExtension()
{
    CommentExtension commentExtension = new CommentExtension();

    commentExtension.extensionIntroducer = 0x21;
    commentExtension.commentLabel = bytes[index++];

    while (true)
    {
        // 块结束符 0
        if (bytes[index].Equals(0x00))
        {
            commentExtension.blockTerminator = 0;
            ++index;
            break;
        }

        CommentExtension.CommentDataBlock commentDataBlock = new CommentExtension.CommentDataBlock();
        commentDataBlock.blockSize = bytes[index++];

        commentDataBlock.commentData = new byte[commentDataBlock.blockSize];
        for (int i = 0; i < commentDataBlock.blockSize; ++i)
        {
            commentDataBlock.commentData[i] = bytes[index++];
        }
    }

    if (commentExtensions == null)
    {
        commentExtensions = new List<CommentExtension>();
    }
    commentExtensions.Add(commentExtension);
}
```

### 无格式文本扩展块（PlainText Extension）（图像说明扩充块）

* 无格式文本扩展块（PlainText Extension）包含 `文本数据` 和 `描绘文本` 所须的参数。文本数据用 `7` 位的 ASCII 字符编码并以图形形式显示。扩展块的结构如下图。

<img src="无格式文本扩展块.png" alt="无格式文本扩展块" style="zoom:100%;">

* `BlockSize`用来指定该图像扩充块的长度，其取值固定为 `13` 。
* `Text Grid Left Position` 用来指定文字显示方格相对于逻辑屏幕左上角的 `X` 坐标（以像素为单位）。
* `Text Grid Top Position` 用来指定文字显示方格相对于逻辑屏幕左上角的 `Y` 坐标。
* `Text Grid Width` 用来指定文字显示方格的宽度。
* `Text Grid Height` 用来指定文字显示方格的高度。
* `Character Cell Width` 用来指定字符的宽度。
* `Character Cell Height` 用来指定字符的高度。
* `Text Foreground Color Index` 用来指定字符的前景色。
* `Text Background Color Index` 用来指定字符的背景色。

``` CSharp
/// <summary>
/// 无格式文本扩展块（图像说明扩充块）
/// </summary>
[Serializable]
public struct PlainTextExtension
{
    /// <summary>
    /// 扩展标识符，固定值 0x21
    /// </summary>
    public byte extensionIntroducer;
    
    /// <summary>
    /// 无格式文本标识符，固定值 0x01
    /// </summary>
    public byte plainTextLabel;
    
    /// <summary>
    /// 块大小
    /// </summary>
    public byte blockSize;

    /// <summary>
    /// 块结束符，固定值 0x00
    /// </summary>
    public byte blockTerminator;
    
    /// <summary>
    /// 无格式文本数据块列表
    /// </summary>
    public List<PlainTextDataBlock> plainTextDataBlocks;

    /// <summary>
    /// 无格式文本数据块
    /// </summary>
    [Serializable]
    public struct PlainTextDataBlock
    {
        /// <summary>
        /// 块大小
        /// </summary>
        public byte blockSize;
        
        /// <summary>
        /// 无格式文本数据
        /// </summary>
        public byte[] plainTextData;
    }
}

/// <summary>
/// 无格式文本扩展块列表
/// </summary>
public List<PlainTextExtension> plainTextExtensions = null;

/// <summary>
/// 获取无格式文本扩展块（图像说明扩充块）
/// </summary>
void GetPlainTextExtension()
{
    PlainTextExtension plainTextExtension = new PlainTextExtension();

    plainTextExtension.extensionIntroducer = 0x21;
    plainTextExtension.plainTextLabel = bytes[index++];
    plainTextExtension.blockSize = bytes[index++];

    // Text Grid Left Position(2 Bytes) 不支持
    index += 2;
    // Text Grid Top Position(2 Bytes) 不支持
    index += 2;
    // Text Grid Width(2 Bytes) 不支持
    index += 2;
    // Text Grid Height(2 Bytes) 不支持
    index += 2;
    // Character Cell Width(1 Bytes) 不支持
    ++index;
    // Character Cell Height(1 Bytes) 不支持
    ++index;
    // Text Foreground Color Index(1 Bytes) 不支持
    ++index;
    // Text Background Color Index(1 Bytes) 不支持
    ++index;

    while (true)
    {
        if (bytes[index].Equals(0x00))
        {
            plainTextExtension.blockTerminator = 0;
            ++index;
            break;
        }
        PlainTextExtension.PlainTextDataBlock plainTextDataBlock = new PlainTextExtension.PlainTextDataBlock();
        plainTextDataBlock.blockSize = bytes[index++];

        plainTextDataBlock.plainTextData = new byte[plainTextDataBlock.blockSize];
        for (int i = 0; i < plainTextDataBlock.blockSize; ++i)
        {
            plainTextDataBlock.plainTextData[i] = bytes[index++];
        }

        if (plainTextExtension.plainTextDataBlocks == null)
        {
            plainTextExtension.plainTextDataBlocks = new List<PlainTextExtension.PlainTextDataBlock>();
        }
        plainTextExtension.plainTextDataBlocks.Add(plainTextDataBlock);
    }

    if (plainTextExtensions == null)
    {
        plainTextExtensions = new List<PlainTextExtension>();
    }
    plainTextExtensions.Add(plainTextExtension);
}
```

### 应用扩展块（Application Extension）

* 应用扩展块（ApplicationExtension）包含制作该图像文件的应用程序的相关信息，它的结构如下图

<img src="应用扩展块.png" alt="应用扩展块" style="zoom:100%;">

* `Block Size` 用来指定该应用程序扩充块的长度，其取值固定为 `12` 。
* `Identifier` 用来指定应用程序名称。
* `Authentication` 用来指定应用程序的识别码。

``` CSharp
/// <summary>
/// 应用扩展块
/// </summary>
[Serializable]
public struct ApplicationExtension
{
    /// <summary>
    /// 扩展标识符，固定值 0x21
    /// </summary>
    public byte extensionIntroducer;
    
    /// <summary>
    /// 应用扩展块标识符，固定值 0xFF
    /// </summary>
    public byte extensionLabel;
    
    /// <summary>
    /// 块大小，固定值 0x0b（12）
    /// </summary>
    public byte blockSize;
    
    /// <summary>
    /// 应用程序标识符
    /// </summary>
    public string applicationIdentifier;
    
    /// <summary>
    /// 应用程序识别码
    /// </summary>
    public string applicationAuthenticationCode;

    /// <summary>
    /// 块结束符，固定值 0x00
    /// </summary>
    public byte blockTerminator;
    
    /// <summary>
    /// 应用程序数据块列表
    /// </summary>
    public List<ApplicationDataBlock> applicationDataBlocks;

    /// <summary>
    /// GIF 循环次数（0 表示无限）
    /// </summary>
    public int loopCount;

    /// <summary>
    /// 应用程序数据块
    /// </summary>
    [Serializable]
    public struct ApplicationDataBlock
    {
        /// <summary>
        /// 块大小
        /// </summary>
        public byte blockSize;
        
        /// <summary>
        /// 应用程序数据
        /// </summary>
        public byte[] applicationData;
    }
}
public ApplicationExtension applicationExtension = new ApplicationExtension();

/// <summary>
/// 获取应用扩展块
/// </summary>
void GetApplicationExtension()
{
    StringBuilder sb = new StringBuilder();

    applicationExtension.extensionIntroducer = 0x21;
    applicationExtension.extensionLabel = bytes[index++];
    applicationExtension.blockSize = bytes[index++];

    for (int i = 0; i < 8; ++i)
    {
        sb.Append(bytes[index++]);
    }
    applicationExtension.applicationIdentifier = sb.ToString();
    sb.Clear();

    for (int i = 0; i < 3; ++i)
    {
        sb.Append(bytes[index++]);
    }
    applicationExtension.applicationAuthenticationCode = sb.ToString();

    while (true)
    {
        if (bytes[index].Equals(0x00))
        {
            applicationExtension.blockTerminator = 0;
            ++index;
            break;
        }
        
        ApplicationExtension.ApplicationDataBlock applicationDataBlock = new ApplicationExtension.ApplicationDataBlock();
        applicationDataBlock.blockSize = bytes[index++];

        applicationDataBlock.applicationData = new byte[applicationDataBlock.blockSize];
        for (int i = 0; i < applicationDataBlock.blockSize; ++i)
        {
            applicationDataBlock.applicationData[i] = bytes[index++];
        }

        if (applicationExtension.applicationDataBlocks == null)
        {
            applicationExtension.applicationDataBlocks = new List<ApplicationExtension.ApplicationDataBlock>();
        }
        applicationExtension.applicationDataBlocks.Add(applicationDataBlock);
    }

    if (applicationExtension.applicationDataBlocks == null || applicationExtension.applicationDataBlocks.Count < 1 ||
        applicationExtension.applicationDataBlocks[0].applicationData.Length < 3 ||
        applicationExtension.applicationDataBlocks[0].applicationData[0] != 0x01)
    {
        applicationExtension.loopCount = 0;
    }
    else
    {
        applicationExtension.loopCount = BitConverter.ToUInt16(applicationExtension.applicationDataBlocks[0].applicationData, 1);
    }
}
```

### 结束块（GIF Trailer）

* 结束块（GIF Trailer）表示 GIF 文件的结尾，它包含一个固定的数值：`0x3B` 。

<img src="文件终结.webp" alt="文件终结" style="zoom:100%;">

``` CSharp
/// <summary>
/// 标识 GIF 文件结束，固定值 0x3b
/// </summary>
public byte trailer;
```

## GIF 转纹理

* 待补充（内容有点多，想偷懒）

## 播放 GIF
