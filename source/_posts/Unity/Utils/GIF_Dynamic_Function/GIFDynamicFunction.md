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
