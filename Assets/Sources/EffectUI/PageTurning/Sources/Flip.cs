using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 基本思路是在物体空间下构建一个圆柱空间，然后将模型绕着贴到圆柱上。在圆柱空间下实现卷起效果是非常简单，然后再将卷起后的顶点转回到物体空间即可。
/// </summary>
public class Flip : BaseMeshEffect
{
    [SerializeField] int tess = 5;
    [SerializeField] Vector2 origin = new Vector3(0.5f, -0.5f);
    [SerializeField] Vector2 direction = new Vector3(1f, -1f);
    [SerializeField] float radius = 50;
    [SerializeField] float depth;
    [SerializeField] bool rollup;
    [SerializeField] bool sort;
    [SerializeField] bool isMeshDirty = true;

    struct Triangle : IComparable<Triangle>
    {
        public Triangle(List<UIVertex> vertices, Vector3Int indices)
        {
            depth = vertices[indices.x].position.z +
                    vertices[indices.y].position.z +
                    vertices[indices.z].position.z;
            this.indices = indices;
        }

        public float depth;
        public Vector3Int indices;

        public int CompareTo(Triangle other)
        {
            return -depth.CompareTo(other.depth);
        }
    }

    List<UIVertex> tempVertices = new List<UIVertex>();
    List<UIVertex> vertices = new List<UIVertex>();
    List<int> indices = new List<int>();
    List<Triangle> triangleInfos = new List<Triangle>();

    protected override void Start()
    {
        base.Start();
        tempVertices = new List<UIVertex>();
        vertices = new List<UIVertex>();
        indices = new List<int>();
        triangleInfos = new List<Triangle>();
        isMeshDirty = true;
    }

    Vector3 GetCylinderPositionWS()
    {
        RectTransform transform = graphic.rectTransform;
        return transform.TransformPoint((Vector3)(origin * transform.sizeDelta) + Vector3.back * (radius - depth));
    }
    void SetCylinderPositionWS(Vector3 pos)
    {
        RectTransform transform = graphic.rectTransform;
        Vector2 positionLS = transform.InverseTransformPoint(pos);
        origin.x = positionLS.x / transform.sizeDelta.x;
        origin.y = positionLS.y / transform.sizeDelta.y;
    }

    UIVertex Lerp(UIVertex a, UIVertex b)
    {
        UIVertex c;
        c.position = Vector3.Lerp(a.position, b.position, 0.5f);
        c.normal = Vector3.Lerp(a.normal, b.normal, 0.5f);
        c.tangent = Vector4.Lerp(a.tangent, b.tangent, 0.5f);
        c.uv0 = Vector4.Lerp(a.uv0, b.uv0, 0.5f);
        c.uv1 = Vector4.Lerp(a.uv1, b.uv1, 0.5f);
        c.uv2 = Vector4.Lerp(a.uv2, b.uv2, 0.5f);
        c.uv3 = Vector4.Lerp(a.uv3, b.uv3, 0.5f);
        c.color = Color.Lerp(a.color, b.color, 0.5f);
        return c;
    }

    /// <summary>
    /// 将一个三角形拆成四个更小的三角形
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="c"></param>
    /// <param name="output"></param>
    void Tessellate(UIVertex a, UIVertex b, UIVertex c, List<UIVertex> output)
    {
        UIVertex d = Lerp(a, b);
        UIVertex e = Lerp(b, c);
        UIVertex f = Lerp(c, a);

        output.Add(a);
        output.Add(d);
        output.Add(f);

        output.Add(d);
        output.Add(b);
        output.Add(e);

        output.Add(f);
        output.Add(e);
        output.Add(c);

        output.Add(f);
        output.Add(d);
        output.Add(e);
    }

    void Merge(List<UIVertex> vertexList, ref List<UIVertex> vertices, ref List<int> indices)
    {
        Dictionary<UIVertex, int> vertexToIndex = new Dictionary<UIVertex, int>();
        int AddOrGetVertex(UIVertex vertex)
        {
            if (vertexToIndex.TryGetValue(vertex, out int index))
                return index;

            vertexToIndex.Add(vertex, vertexToIndex.Count);
            return vertexToIndex.Count - 1;
        }

        indices.Clear();
        for (int i = 0; i < vertexList.Count; i += 3)
        {
            int i0 = AddOrGetVertex(vertexList[i]);
            int i1 = AddOrGetVertex(vertexList[i + 1]);
            int i2 = AddOrGetVertex(vertexList[i + 2]);
            indices.Add(i0);
            indices.Add(i1);
            indices.Add(i2);
        }

        vertices.Clear();
        foreach (UIVertex vertex in vertexToIndex.Keys)
            vertices.Add(vertex);
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        RectTransform transform = graphic.rectTransform;

        if (isMeshDirty)
        {
            vh.GetUIVertexStream(tempVertices);
            //细分网格，使有足够的顶点可以细分
            for (int i = 0; i < tess; i++)
            {
                vertices.Clear();
                for (int vi = 0; vi < tempVertices.Count; vi += 3)
                {
                    Tessellate(
                        tempVertices[vi],
                        tempVertices[vi + 1],
                        tempVertices[vi + 2],
                        vertices
                    );
                }
                if (i != tess - 1)
                {
                    tempVertices.Clear();
                    tempVertices.AddRange(vertices);
                }
            }
            //合并重复点
            Merge(vertices, ref tempVertices, ref indices);
            vertices.Clear();
            vertices.AddRange(tempVertices);

            isMeshDirty = false;
        }

        //通过柱面映射偏移顶点来实现翻页效果
        Profiler.BeginSample("顶点偏移");
        //计算圆柱空间信息（该圆柱是基于物体空间的，以便和网格顶点兼容）
        Vector3 position = transform.InverseTransformPoint(GetCylinderPositionWS());
        Vector3 forward = Vector3.forward;
        Vector3 right = direction.normalized;
        Vector3 up = Vector3.Cross(forward, right);
        Matrix4x4 cylinder = new Matrix4x4(right, up, forward, new Vector4(position.x, position.y, position.z, 1));
        //将顶点映射到圆柱空间
        for (int i = 0; i < vertices.Count; i++)
        {
            UIVertex MapVertex(UIVertex vertex)
            {
                Vector3 cylinderToVertex = vertex.position - position;
                float length = Vector3.Dot(cylinderToVertex, right); //通过投影获得顶点离柱面起点的长度，该长度之后将被绕在圆柱上，故其表示弧长
                if (length < 0) //左侧的书面不需要翻动
                    return vertex;

                float rad = length / radius; //将弧长转为夹角弧度
                float height = Vector3.Dot(cylinderToVertex, up); //通过投影获得顶点在圆柱上的高

                //计算顶点被环绕贴到柱面后的新位置（圆柱空间位置）
                Vector3 positionCS;
                if (rollup || rad < Mathf.PI)
                {
                    positionCS = new Vector3(
                        Mathf.Sin(rad) * radius,
                        height,
                        Mathf.Cos(rad) * radius
                    );
                }
                else //如果不需要持续将顶点绕在圆柱上，可以在绕到180度时，使后续顶点直接向后伸展而不是继续环绕
                {
                    positionCS = new Vector3(
                        Mathf.Sin(Mathf.PI) * radius - (length - Mathf.PI * radius),
                        height,
                        Mathf.Cos(Mathf.PI) * radius
                    );
                }

                //将圆柱坐标系下的顶点转到物体坐标系
                vertex.position = cylinder.MultiplyPoint(positionCS);

                return vertex;
            }

            vertices[i] = MapVertex(tempVertices[i]); //计算结果不能累加，故始终用最原始的顶点计算
        }
        Profiler.EndSample();

        //基元（三角面）顺序是会影响渲染顺序的，所以可以根据顶点被卷起后的深度值排序基元，来实现不依靠深度测试的遮挡效果。
        //这对Overly模式下的画布非常有用，因为该模式下无法使用深度测试
        if (sort)
        {
            Profiler.BeginSample("三角面排序");
            //统计三角形信息
            triangleInfos.Clear();
            for (int i = 0; i < indices.Count; i += 3)
                triangleInfos.Add(new Triangle(vertices, new Vector3Int(indices[i], indices[i + 1], indices[i + 2])));
            //排序
            triangleInfos.Sort();
            //写回新的三角形顺序
            indices.Clear();
            foreach (Triangle triangle in triangleInfos)
            {
                indices.Add(triangle.indices.x);
                indices.Add(triangle.indices.y);
                indices.Add(triangle.indices.z);
            }
            Profiler.EndSample();
        }


        //处理因深度偏移导致书面凹陷的情况，这些顶点应该是未被卷起状态
        Profiler.BeginSample("消除顶点凹陷");
        for (int i = 0; i < vertices.Count; i++)
        {
            UIVertex vertex = vertices[i];
            vertex.position.z = Mathf.Min(vertex.position.z, 0);
            vertices[i] = vertex;
        }
        Profiler.EndSample();

        vh.Clear();
        vh.AddUIVertexStream(vertices, indices);
    }

    void OnDrawGizmosSelected()
    {
        Vector3 positionWS = GetCylinderPositionWS();
        Vector3 boundaryWS = transform.TransformVector(direction.normalized * radius);

        Gizmos.color = Color.red;
        Gizmos.DrawRay(positionWS, boundaryWS);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(positionWS, boundaryWS.magnitude);
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(Flip))]
    public class FlipEditor : Editor
    {
        bool enableEditorUI;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Separator();
            enableEditorUI = GUILayout.Toggle(enableEditorUI, "FlipEditor");
        }

        void OnSceneGUI()
        {
            if (enableEditorUI == false)
                return;

            Flip flip = (Flip)target;
            Transform transform = flip.transform;

            Vector3 positionWS = flip.GetCylinderPositionWS();
            Vector3 boundaryWS = transform.TransformVector(flip.direction.normalized * flip.radius);

            Vector3 newPositionWS = Handles.PositionHandle(positionWS, transform.rotation);
            Vector3 newBoundaryWS = Handles.PositionHandle(newPositionWS + boundaryWS, transform.rotation) - newPositionWS;

            if (positionWS != newPositionWS || boundaryWS != newBoundaryWS)
            {
                Undo.RecordObject(flip, "Flip");
                flip.SetCylinderPositionWS(newPositionWS);
                Vector3 newBoundaryCS = transform.InverseTransformVector(newBoundaryWS);
                flip.direction = newBoundaryCS;
                flip.radius = newBoundaryCS.magnitude;
                flip.graphic.SetVerticesDirty();
            }
        }
    }
#endif
}
