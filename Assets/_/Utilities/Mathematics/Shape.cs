using System;
using Unity.Collections;
using UnityEngine;

[Serializable]
public struct Line
{
    public Line(Vector3 origin, Vector3 normal)
    {
        if (Mathf.Abs(normal.magnitude - 1) > 0.0001f)
            throw new Exception("法线不是单位长度" + normal.magnitude);

        this.origin = origin;
        this.normal = normal;
    }

    public Vector3 Origin => origin;
    public Vector3 Normal => normal;

    /// <summary>
    /// 返回直线上与指定点最近的位置，从指定点向直线做垂线段，返回该线段的根
    /// </summary>
    public bool ProjectPoint(Vector3 point, out Vector3 result)
    {
        //直线方程参数
        double la = Normal.x;
        double lai = Origin.x;
        double lb = Normal.y;
        double lbi = Origin.y;
        double lc = Normal.z;
        double lci = Origin.z;
        //以point为原点，lineNormal为法线的平面
        double fa = Normal.x;
        double fb = Normal.y;
        double fc = Normal.z;
        double fd = fa * -point.x + fb * -point.y + fc * -point.z;
        //将直线参数带入平面方程求出交点的t参数
        double left = fa * la + fb * lb + fc * lc;
        double right = -(fd + fa * lai + fb * lbi + fc * lci);
        double t = right / left;
        //将t带入直线方程求出交点
        result = new Vector3((float)(la * t + lai), (float)(lb * t + lbi), (float)(lc * t + lci));
        return true;
    }

    public float DistanceFromPoint(Vector3 point, out Vector3 result)
    {
        if (ProjectPoint(point, out result) == false)
            throw new Exception("不可能好吧");
        return Vector3.Distance(point, result);
    }

    [SerializeField] Vector3 origin;
    [SerializeField] Vector3 normal;
}
[Serializable]
public struct LineSegment
{
    public LineSegment(Line line, float length)
    {
        this.line = line;
        this.length = length;
    }

    public LineSegment(Vector3 vertexA, Vector3 vertexB)
    {
        if (vertexA == vertexB)
            throw new Exception("线段两端点相同");

        Vector3 normal = vertexB - vertexA;
        line = new Line(vertexA, normal.normalized);
        length = normal.magnitude;
    }

    public Line Line => line;
    public float Length => length;
    public Vector3 PointA => Line.Origin;
    public Vector3 PointB => Line.Origin + Line.Normal * Length;

    public bool ProjectPoint(Vector3 point, out Vector3 result)
    {
        Line.ProjectPoint(point, out result);
        Vector3 direction = result - Line.Origin;

        if (Vector3.Dot(direction, Line.Normal) < 0 || direction.magnitude > Length)
            return false;
        return true;
    }

    public float DistanceFromPoint(Vector3 point, out Vector3 result)
    {
        return DistanceFromPoint(point, out _, out result);
    }

    public float DistanceFromPoint(Vector3 point, out Vector3 project, out Vector3 result)
    {
        if (ProjectPoint(point, out project) == false)
            result = Vector3.Dot(project - PointA, Line.Normal) > 0 ? PointB : PointA;
        else
            result = project;

        return Vector3.Distance(point, result);
    }

    public void DrawGizmos()
    {
        Gizmos.DrawRay(Line.Origin, Line.Normal * Length);
    }

    public void DebugGizmos(Color color)
    {
        Debug.DrawLine(PointA, PointB, color);
    }

    [SerializeField] Line line;
    [SerializeField] float length;
}
[Serializable]
public struct Capsule
{
    public Capsule(Vector3 origin, Vector3 normal, float length, float radius)
    {
        lineSegment = new LineSegment(new Line(origin, normal), length);
        this.radius = radius;
    }
    public Capsule(CapsuleCollider capsuleCollider, Transform capsuleTransform, float extend = 0)
    {
        float scale = Mathf.Max(Mathf.Max(capsuleTransform.lossyScale.x, capsuleTransform.lossyScale.y), capsuleTransform.lossyScale.z);

        radius = scale * capsuleCollider.radius + extend;

        float length = Mathf.Max(capsuleCollider.height * scale + extend * 2 - radius * 2, 0);
        Vector3 normal = capsuleCollider.direction switch {
            0 => capsuleTransform.right,
            1 => capsuleTransform.up,
            2 => capsuleTransform.forward,
            _ => throw new ArgumentOutOfRangeException()
        };

        Vector3 position = capsuleTransform.TransformPoint(capsuleCollider.center) - normal * length / 2;

        lineSegment = new LineSegment(new Line(position, normal), length);
    }
    public Capsule(CapsuleCollider capsuleCollider) : this(capsuleCollider, capsuleCollider.transform) { }

    public LineSegment LineSegment => lineSegment;
    public float Radius => radius;
    public Vector3 Origin => lineSegment.Line.Origin;
    public Vector3 Normal => lineSegment.Line.Normal;
    public float Length => lineSegment.Length;

    public Vector3 Extrude(Vector3 point)
    {
        lineSegment.DistanceFromPoint(point, out Vector3 pointOnLine);
        Vector3 vector3 = point - pointOnLine;
        return pointOnLine + vector3.normalized * Mathf.Max(vector3.magnitude, radius);
    }
    public bool IsContained(Vector3 point)
    {
        lineSegment.DistanceFromPoint(point, out Vector3 pointOnLine);
        return Vector3.Distance(point, pointOnLine) < radius;
    }

    [SerializeField] LineSegment lineSegment;
    [SerializeField] float radius;
}

[Serializable]
public struct Plane
{
    public Plane(Vector3 origin, Vector3 normal)
    {
        // if (Mathf.Abs(normal.magnitude - 1) > MathTools._Epsilon)
        //     throw new Exception("法线不是单位长度" + normal.magnitude);

        this.origin = origin;
        this.normal = normal;
    }

    public Vector3 Origin => origin;
    public Vector3 Normal => normal;

    public bool IsFrontPoint(Vector3 point)
    {
        return Vector3.Dot(point - Origin, Normal) > 0;
    }
    public bool ProjectPoint(Vector3 point, out Vector3 result)
    {
        return IntersectLine(new Line(point, Normal), out result);
    }
    public float DistanceFromPoint(Vector3 point, out Vector3 result)
    {
        if (ProjectPoint(point, out result) == false)
            throw new Exception("不可能好吧");
        return Vector3.Distance(point, result);
    }
    public bool IntersectLine(Line line, out Vector3 intersection)
    {
        //不相交
        if (Vector3.Dot(line.Normal, Normal) == 0)
        {
            intersection = default;
            return false;
        }

        //平面方程（Ax+Bx+Cx+D=0）的参数
        double planeA = Normal.x;
        double planeB = Normal.y;
        double planeC = Normal.z;
        double planeD = -(Normal.x * Origin.x +
                          Normal.y * Origin.y +
                          Normal.z * Origin.z);
        // 带入直线参数方程至平面方程
        // 解方程：A(Ox+Nx*t)+B(Oy+Ny*t)+C(Oz+Nz*t)+D=0
        // (A*Ox)+(B*Oy)+(C*Oz)+D+(A*Nx+B*Ny+C*Nz)*t=0
        // (aOx+bOy+cOz+D)+(aNx+bNy+cNz)*t=0
        double aOx = planeA * line.Origin.x;
        double bOy = planeB * line.Origin.y;
        double cOz = planeC * line.Origin.z;

        double aNx = planeA * line.Normal.x;
        double bNy = planeB * line.Normal.y;
        double cNz = planeC * line.Normal.z;

        // t=-(aOx+bOy+cOz+D)/(aNx+bNy+cNz)
        double t = -(aOx + bOy + cOz + planeD) / (aNx + bNy + cNz);

        intersection = new Vector3(
            (float)(line.Origin.x + line.Normal.x * t),
            (float)(line.Origin.y + line.Normal.y * t),
            (float)(line.Origin.z + line.Normal.z * t));

        return true;
    }
    public bool IntersectLineSegment(LineSegment lineSegment, out Vector3 intersection)
    {
        Line line = lineSegment.Line;

        intersection = default;

        if (IntersectLine(line, out intersection) == false) //不相交
            return false;

        Vector3 normal = intersection - line.Origin;

        if (Vector3.Dot(normal, line.Normal) < 0 || normal.magnitude > lineSegment.Length) //超出范围
            return false;

        return true;
    }

    [SerializeField] Vector3 origin;
    [SerializeField] Vector3 normal;
}

[Serializable]
public struct Triangle
{
    public Triangle(Vector3 point0, Vector3 point1, Vector3 point2)
    {
        this.point0 = point0;
        this.point1 = point1;
        this.point2 = point2;

        Vector3 normal = Vector3.Cross(point1 - point0, point2 - point0).normalized;
        // if (Mathf.Abs(normal.magnitude - 1) > MathTools._Epsilon)
        //     throw new Exception($"存在相同点或共线，Normal{normal:F6}\n[\n{_Point0:F6}\n{_Point1:F6}\n{_Point2:F6}\n]");

        plane = new Plane((point0 + point1 + point2) / 3, normal);
    }

    public Plane Plane => plane;
    public Vector3 Point0 => point0;
    public Vector3 Point1 => point1;
    public Vector3 Point2 => point2;
    public float Radius
    {
        get
        {
            NativeArray<float> radius = new NativeArray<float>(3, Allocator.Temp) {
                [0] = Vector3.Distance(Plane.Origin, Point0),
                [1] = Vector3.Distance(Plane.Origin, Point1),
                [2] = Vector3.Distance(Plane.Origin, Point2),
            };

            return radius[radius.MaxIndex()];
        }
    }
    public float Area
    {
        get
        {
            Vector3 v01 = point1 - point0;
            Vector3 v02 = point2 - point0;
            float l01 = v01.magnitude;
            float l02 = v02.magnitude;

            float cos0 = Vector3.Dot(v01, v02) / (l01 * l02);
            float sin0 = Mathf.Sqrt(1 - cos0 * cos0);
            float h = sin0 * l01;
            float s = 0.5f * l02 * h;
            return s;
        }
    }

    public bool ContainPoint(Vector3 point)
    {
        Vector3 vector0 = Point0 - point;
        float magnitude0 = vector0.magnitude;
        if (magnitude0 < float.Epsilon)
            return true;

        Vector3 vector1 = Point1 - point;
        float magnitude1 = vector1.magnitude;
        if (magnitude1 < float.Epsilon)
            return true;

        Vector3 vector2 = Point2 - point;
        float magnitude2 = vector2.magnitude;
        if (magnitude2 < float.Epsilon)
            return true;

        Vector3 direction0 = vector0 / magnitude0;
        Vector3 direction1 = vector1 / magnitude1;
        Vector3 direction2 = vector2 / magnitude2;
        if ((direction0 + direction1).sqrMagnitude < float.Epsilon ||
            (direction1 + direction2).sqrMagnitude < float.Epsilon ||
            (direction2 + direction0).sqrMagnitude < float.Epsilon)
            return true;


        Vector3 normal0 = Vector3.Cross(vector0, vector1);
        Vector3 normal1 = Vector3.Cross(vector1, vector2);
        Vector3 normal2 = Vector3.Cross(vector2, vector0);
        if (Vector3.Dot(normal0, normal1) > 0 && Vector3.Dot(normal1, normal2) > 0)
            return true;

        return false;
    }

    public bool ProjectPoint(Vector3 point, out Vector3 result)
    {
        Plane.ProjectPoint(point, out result);
        return ContainPoint(result);
    }

    public float DistanceFromPoint(Vector3 point, out Vector3 result)
    {
        return DistanceFromPoint(point, out _, out result);
    }

    public float DistanceFromPoint(Vector3 point, out Vector3 project, out Vector3 result)
    {
        if (ProjectPoint(point, out project) == false)
        {
            NativeArray<Vector3> origins = new NativeArray<Vector3>(3, Allocator.Temp);
            NativeArray<float> distance = new NativeArray<float>(3, Allocator.Temp);
            for (int i = 0; i < 3; i++)
            {
                GetLineSegment(i).ProjectPoint(point, out result);
                origins[i] = result;
                distance[i] = result.sqrMagnitude;
            }

            result = origins[distance.MinIndex()];
        }
        else
        {
            result = project;
        }

        return Vector3.Distance(point, result);
    }

    public bool IntersectLineSegment(LineSegment lineSegment, out Vector3 intersection)
    {
        if (Plane.IntersectLineSegment(lineSegment, out intersection) == false)
            return false;

        if (ContainPoint(intersection) == false) //不在三角形内
            return false;

        return true;
    }

    public LineSegment GetLineSegment(int index)
    {
        return index switch {
            0 => new LineSegment(Point0, Point1),
            1 => new LineSegment(Point1, Point2),
            2 => new LineSegment(Point2, Point0),
            _ => throw new ArgumentOutOfRangeException(nameof(index), index, null)
        };
    }
    public NativeArray<LineSegment> GetLineSegments(Allocator allocator = Allocator.Temp)
    {
        NativeArray<LineSegment> lineSegments = new NativeArray<LineSegment>(3, allocator) {
            [0] = new LineSegment(Point0, Point1),
            [1] = new LineSegment(Point1, Point2),
            [2] = new LineSegment(Point2, Point0),
        };

        return lineSegments;
    }

    public void DrawGizmos()
    {
        Gizmos.DrawLine(Point0, Point1);
        Gizmos.DrawLine(Point1, Point2);
        Gizmos.DrawLine(Point2, Point0);
    }

    public void DebugGizmos(Color color)
    {
        Debug.DrawLine(Point0, Point1, color);
        Debug.DrawLine(Point1, Point2, color);
        Debug.DrawLine(Point2, Point0, color);
        Debug.DrawRay(Plane.Origin, Plane.Normal, color);
    }

    public override string ToString()
    {
        return $"[{Point0}\t{Point1}\t{Point2}]";
    }

    [SerializeField] Plane plane;
    [SerializeField] Vector3 point0;
    [SerializeField] Vector3 point1;
    [SerializeField] Vector3 point2;
}

[Serializable]
public struct Tetrahedron
{
    public Tetrahedron(Vector3 point0, Vector3 point1, Vector3 point2, Vector3 point3)
    {
        this.point0 = point0;
        this.point1 = point1;
        this.point2 = point2;
        this.point3 = point3;
    }

    public Vector3 Point0 => point0;
    public Vector3 Point1 => point1;
    public Vector3 Point2 => point2;
    public Vector3 Point3 => point3;

    public void DrawGizmos()
    {
        Gizmos.DrawLine(Point0, point1);
        Gizmos.DrawLine(Point0, point2);
        Gizmos.DrawLine(Point0, point3);
        Gizmos.DrawLine(Point1, point2);
        Gizmos.DrawLine(Point1, point3);
        Gizmos.DrawLine(Point2, point3);
    }
    public void DebugGizmos(Color color)
    {
        Debug.DrawLine(Point0, point1, color);
        Debug.DrawLine(Point0, point2, color);
        Debug.DrawLine(Point0, point3, color);
        Debug.DrawLine(Point1, point2, color);
        Debug.DrawLine(Point1, point3, color);
        Debug.DrawLine(Point2, point3, color);
    }

    [SerializeField] Vector3 point0;
    [SerializeField] Vector3 point1;
    [SerializeField] Vector3 point2;
    [SerializeField] Vector3 point3;
}
