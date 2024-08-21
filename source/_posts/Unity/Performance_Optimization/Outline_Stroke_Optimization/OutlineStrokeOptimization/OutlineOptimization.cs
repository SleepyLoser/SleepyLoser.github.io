using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace HaruoYaguchi.UI
{
    public class OutlineOptimization : BaseMeshEffect
    {
        // 描边颜色
        public Color OutlineColor = Color.white;

        // 描边距离
        [Range(0, 6)]
        public int OutlineWidth = 0;

        // UI各三角形网格的顶点列表
        private static List<UIVertex> m_VertexList = new List<UIVertex>();

        protected override void Start() 
        {
            base.Start();

            Shader shader = Shader.Find("HaruoYaguchi/UI/OutlineOptimization");
            // 注意，如果着色器没有引用方，着色器可能未包含在播放器版本中！这种情况下，Shader.Find 仅在 编辑器中起作用，在播放器版本中将生成粉红色“缺失着色器”材质。
            // 因此，建议使用 着色器引用，而不是按名称查找它们。要确保着色器包含在游戏版本中，则执行以下操作之一： 
            // 1 从场景中使用的某些材质引用着色器。
            // 2 将其添加在 ProjectSettings/Graphics 中的“Always Included Shaders”列表下。
            // 3 将着色器或其引用方（例如某个材质）放置到“Resources”文件夹中。
            base.graphic.material = new Material(shader);

            AdditionalCanvasShaderChannels v1 = base.graphic.canvas.additionalShaderChannels;
            AdditionalCanvasShaderChannels v2 = AdditionalCanvasShaderChannels.TexCoord1;
            if ((v1 & v2) != v2)
            {
                base.graphic.canvas.additionalShaderChannels |= v2;   
            }
            v2 = AdditionalCanvasShaderChannels.TexCoord2;
            if ((v1 & v2) != v2)
            {
                base.graphic.canvas.additionalShaderChannels |= v2;   
            }

            OutlineRefresh();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
 
            if (base.graphic.material != null)
            {
                OutlineRefresh();
            }
        }
#endif

        /// <summary>
        /// 更新Outline颜色和宽度
        /// </summary>
        private void OutlineRefresh()
        {
            base.graphic.material.SetColor("_OutlineColor", OutlineColor);
            base.graphic.material.SetInt("_OutlineWidth", OutlineWidth);
            base.graphic.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            vh.GetUIVertexStream(m_VertexList);
   
            this.ProcessVertices();
   
            vh.Clear();
            vh.AddUIVertexTriangleStream(m_VertexList);
        }

        private void ProcessVertices()
        {
            for (int i = 0, count = m_VertexList.Count - 3; i <= count; i += 3)
            {
                UIVertex v1 = m_VertexList[i];
                UIVertex v2 = m_VertexList[i + 1];
                UIVertex v3 = m_VertexList[i + 2];

                // 计算原顶点坐标中心点
                float minX = Min(v1.position.x, v2.position.x, v3.position.x);
                float minY = Min(v1.position.y, v2.position.y, v3.position.y);
                float maxX = Max(v1.position.x, v2.position.x, v3.position.x);
                float maxY = Max(v1.position.y, v2.position.y, v3.position.y);
                Vector2 posCenter = new Vector2(minX + maxX, minY + maxY) * 0.5f;

                // 计算原始顶点坐标和UV的方向
                Vector2 triX, triY, uvX, uvY;
                Vector2 pos1 = v1.position;
                Vector2 pos2 = v2.position;
                Vector2 pos3 = v3.position;
                if (Mathf.Abs(Vector2.Dot((pos2 - pos1).normalized, Vector2.right))
                    > Mathf.Abs(Vector2.Dot((pos3 - pos2).normalized, Vector2.right)))
                {
                    triX = pos2 - pos1;
                    triY = pos3 - pos2;
                    uvX = v2.uv0 - v1.uv0;
                    uvY = v3.uv0 - v2.uv0;
                }
                else
                {
                    triX = pos3 - pos2;
                    triY = pos2 - pos1;
                    uvX = v3.uv0 - v2.uv0;
                    uvY = v2.uv0 - v1.uv0;
                }

                // 计算原始UV框
                Vector2 uvMin = Min(v1.uv0, v2.uv0, v3.uv0);
                Vector2 uvMax = Max(v1.uv0, v2.uv0, v3.uv0);
                Vector4 uvOrigin = new Vector4(uvMin.x, uvMin.y, uvMax.x, uvMax.y);

                // 为每个顶点设置新的Position和UV，并传入原始UV框
                v1 = SetNewPosAndUV(v1, this.OutlineWidth, posCenter, triX, triY, uvX, uvY, uvOrigin);
                v2 = SetNewPosAndUV(v2, this.OutlineWidth, posCenter, triX, triY, uvX, uvY, uvOrigin);
                v3 = SetNewPosAndUV(v3, this.OutlineWidth, posCenter, triX, triY, uvX, uvY, uvOrigin);

                // 应用设置后的UIVertex
                m_VertexList[i] = v1;
                m_VertexList[i + 1] = v2;
                m_VertexList[i + 2] = v3;
            }
        }

        private static UIVertex SetNewPosAndUV(UIVertex pVertex, int pOutlineWidth,
            Vector2 pPosCenter,
            Vector2 pTriangleX, Vector2 pTriangleY,
            Vector2 pUVX, Vector2 pUVY,
            Vector4 pUVOrigin)
        {
            // Position
            Vector3 pos = pVertex.position;
            int posXOffset = pos.x > pPosCenter.x ? pOutlineWidth : -pOutlineWidth;
            int posYOffset = pos.y > pPosCenter.y ? pOutlineWidth : -pOutlineWidth;
            pos.x += posXOffset;
            pos.y += posYOffset;
            pVertex.position = pos;

            // UV
            Vector4 uv = pVertex.uv0;
            uv += (Vector4)pUVX / pTriangleX.magnitude * posXOffset * (Vector2.Dot(pTriangleX, Vector2.right) > 0 ? 1 : -1);
            uv += (Vector4)pUVY / pTriangleY.magnitude * posYOffset * (Vector2.Dot(pTriangleY, Vector2.up) > 0 ? 1 : -1);
            pVertex.uv0 = uv;

            // 原始UV框
            pVertex.uv1 = new Vector2(pUVOrigin.x, pUVOrigin.y);
            pVertex.uv2 = new Vector2(pUVOrigin.z, pUVOrigin.w);
 
            return pVertex;
        }

        private static float Min(float pA, float pB, float pC)
        {
            return Mathf.Min(Mathf.Min(pA, pB), pC);
        }
  
  
        private static float Max(float pA, float pB, float pC)
        {
            return Mathf.Max(Mathf.Max(pA, pB), pC);
        }
  
  
        private static Vector2 Min(Vector2 pA, Vector2 pB, Vector2 pC)
        {
            return new Vector2(Min(pA.x, pB.x, pC.x), Min(pA.y, pB.y, pC.y));
        }
  
  
        private static Vector2 Max(Vector2 pA, Vector2 pB, Vector2 pC)
        {
            return new Vector2(Max(pA.x, pB.x, pC.x), Max(pA.y, pB.y, pC.y));
        }
    }
}

