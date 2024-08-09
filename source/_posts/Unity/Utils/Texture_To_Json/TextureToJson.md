---
title: 将纹理批量转换成Json文件
top_img: '120498462_p0.jpg'
cover: '109951168016315868.jpg'
categories: 
    - Unity
      - 工具类
tags: 
    - 纹理
    - 实用工具
---

## TextureToJson

* 主要涉及以下知识点:

1. EditorWindow中如何显示例如List这样的数据结构;
2. 如何正确的使用JsonUtility.ToJson(), 即如何解决输出的Json文件内容为空;
3. 获取纹理的字节数据(Texture => byte[])。

### 1. EditorWindow中如何显示例如List这样的数据结构

``` C#
public sealed class TextureToJson : EditorWindow
{
    public List<Texture2D> textures2D = new List<Texture2D>();
    SerializedProperty texturesProperty;
    SerializedObject texturesObject;

    void OnEnable()
    {
        texturesObject = new SerializedObject(this);
        texturesProperty = texturesObject.FindProperty("textures2D");
    }

    private void OnGUI()
    {
        GUILayout.Label("将需要转换的纹理放入下面的列表中");
        EditorGUILayout.PropertyField(texturesProperty);
    }
}
```

* 本质上是将该类序列化, 再通过该类序列化它的属性, 得到序列化的List

### 2. 如何正确的使用JsonUtility.ToJson(), 即如何解决输出的Json文件内容为空

* 关于JsonUtility.ToJson():

1. 在内部, 此方法使用 Unity 序列化器, 因此**传入的对象必须受序列化器支持**;
2. 传入的对象**必须是** MonoBehaviour、ScriptableObject 或应用了 Serializable 属性的普通类/结构。要包含的字段的类型必须受序列化器支持；不受支持的字段以及私有字段、静态字段和应用了 NonSerialized 属性的字段会被忽略;
3. 支持任何普通类或结构, 以及派生自 MonoBehaviour 或 ScriptableObject 的类, 不支持其他引擎类型。( **只能在 Editor 中使用 EditorJsonUtility.ToJson 将其他引擎类型序列化为 JSON** );
4. 请注意, 虽然可以将原始类型传递到此方法, 但是结果可能不同于预期; 此方法不会直接序列化, 而是尝试序列化其公共实例字段, 从而生成**空对象**作为结果。同样，将**数组**传递到此方法**不会**生成包含每个元素的 JSON 数组，而是**生成一个对象**，其中包含数组对象本身的公共字段 (**都无值**) 。若要序列化数组或原始类型的实际内容，**需要将它放入类或结构中**。
5. 此方法**可以从后台线程进行调用**。在此函数仍在执行期间，不应更改传递给它的对象。

* 故有以下解决方案:

``` C#
[Serializable]
struct TexturesData
{
    public byte[] data;
    public TexturesData(byte[] data)
    {
        this.data = data;
    }
    public readonly string ToJson()
    {
        return JsonUtility.ToJson(this);
    }
}
public sealed class TextureToJson : EditorWindow
{
    private void OnGUI()
    {
        // 将纹理数据转换为字节
        byte[] data = DeCompress(textures2D[i]).EncodeToPNG();

        // 实例化结构体并序列化(ToJson)它
        TexturesData texturesData = new TexturesData(data);
        data = Encoding.UTF8.GetBytes(texturesData.ToJson());
        // 将JSON数据保存到文件
        File.WriteAllBytes(path, data);
    }
}
```

### 3. 获取纹理的字节数据(Texture => byte[])

``` C#
// 将纹理数据转换为字节
byte[] data = DeCompress(textures2D[i]).EncodeToPNG();

// 对纹理做预处理
public Texture2D DeCompress(Texture2D source)
{
    RenderTexture renderTex = RenderTexture.GetTemporary(
                source.width,
                source.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear);

    Graphics.Blit(source, renderTex);
    RenderTexture previous = RenderTexture.active;
    RenderTexture.active = renderTex;
    Texture2D readableText = new Texture2D(source.width, source.height);
    readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
    readableText.Apply();
    RenderTexture.active = previous;
    RenderTexture.ReleaseTemporary(renderTex);
    return readableText;
}
```

## 附完整工具代码(顶部菜单栏"HaruoYaguchi/TextureToJson")

``` C#
namespace HaruoYaguchi.Editor
{
    [Serializable]
    struct TexturesData
    {
        public byte[] data;
        public TexturesData(byte[] data)
        {
            this.data = data;
        }
        public readonly string ToJson()
        {
            return JsonUtility.ToJson(this);
        }
    }

    public sealed class TextureToJson : EditorWindow
    {
        public List<Texture2D> textures2D = new List<Texture2D>();
        SerializedProperty texturesProperty;
        SerializedObject texturesObject;
        StringBuilder pathBuilder = new StringBuilder();
        string _jsonStoragePath;


        void OnEnable()
        {
            texturesObject = new SerializedObject(this);
            texturesProperty = texturesObject.FindProperty("textures2D");
        }


        [MenuItem("HaruoYaguchi/TextureToJson", false, 2)]
        public static void Internal_TextureToJson()
        {
            if (EditorApplication.isCompiling || EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("警告", "不允许在游戏运行时或代码编译时进行转换", "确定");
                return;
            }
            TextureToJson textureToJson = GetWindow<TextureToJson>();
            textureToJson.Show();
        }
        private void OnGUI() 
        {
            GUILayout.Label("将需要转换的纹理放入下面的列表中");
            EditorGUILayout.PropertyField(texturesProperty);

            GUILayout.Label("转换后Json文件存储的位置(不填写则在列表中第一个纹理的父级目录下创建一个新的文件夹, 文件夹名为'TextureToJsonFolder'), 以该文件夹为存储位置");
            GUILayout.Label("路径填写标准: './Assets/.../' 或 'C:/.../' (相对路径或绝对路径, 需要保证该路径存在)");
            _jsonStoragePath = EditorGUILayout.TextField("存储路径: ", _jsonStoragePath);

            if (GUILayout.Button("将纹理转换为Json文件"))
            {
                // 更新
                // texturesObject.Update();
    
                // //开始检查是否有修改
                // EditorGUI.BeginChangeCheck();
                try
                {
                    //结束检查是否有修改
                    if (EditorGUI.EndChangeCheck())
                    {
                        //提交修改
                        texturesObject.ApplyModifiedProperties();
                    }

                    if (_jsonStoragePath == null || _jsonStoragePath == string.Empty)
                    {
                        _jsonStoragePath = AssetDatabase.GetAssetPath(textures2D[0]);
                        _jsonStoragePath = Directory.GetParent(_jsonStoragePath).Parent.FullName;
                        pathBuilder.Append(_jsonStoragePath + "/TextureToJsonFolder/");
                        Directory.CreateDirectory(pathBuilder.ToString());
                    }
                    else
                    {
                        pathBuilder = new StringBuilder(_jsonStoragePath);
                    }

                    for (int i = 0; i < textures2D.Count; ++i)
                    {

                        // 将纹理数据转换为字节
                        byte[] data = DeCompress(textures2D[i]).EncodeToPNG();

                        pathBuilder.Append(textures2D[i].name + ".json");

                        // 将JSON数据保存到文件
                        TexturesData texturesData = new TexturesData(data);
                        data = Encoding.UTF8.GetBytes(texturesData.ToJson());
                        File.WriteAllBytes(pathBuilder.ToString(), data);

                        pathBuilder.Remove(pathBuilder.Length - textures2D[i].name.Length - 5, textures2D[i].name.Length + 5);
                    }

                    _jsonStoragePath = null;
                    pathBuilder.Clear();
                    Debug.Log("纹理转换Json成功!!!!!!");
                }
                catch (System.Exception e)
                {
                    _jsonStoragePath = null;
                    pathBuilder.Clear();
                    Debug.LogError(e);
                }
            }
        }

        public Texture2D DeCompress(Texture2D source)
        {
            RenderTexture renderTex = RenderTexture.GetTemporary(
                        source.width,
                        source.height,
                        0,
                        RenderTextureFormat.Default,
                        RenderTextureReadWrite.Linear);

            Graphics.Blit(source, renderTex);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTex;
            Texture2D readableText = new Texture2D(source.width, source.height);
            readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
            readableText.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTex);
            return readableText;
        }

    }    
}
```
