using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MassSpring : MonoBehaviour
{
    class Spring
    {
        public readonly Transform pointA;
        public readonly Transform pointB;
        public float length;

        public Spring(Transform pointA, Transform pointB)
        {
            this.pointA = pointA;
            this.pointB = pointB;
        }
    }

    [SerializeField] [Range(0, 2)] float elasticity = 0.75f;
    [SerializeField] [Range(0, 0.999f)] float drag = 0.01f;
    [SerializeField] int size = 30;
    [SerializeField] int unit = 1;
    [SerializeField] bool ropeOrCloth;
    [SerializeField] bool useGravity;

    readonly List<Spring> allSprings = new List<Spring>();
    readonly List<Transform> allPoints = new List<Transform>();
    readonly Dictionary<Transform, Vector3> lastPositions = new Dictionary<Transform, Vector3>();

    void Start()
    {
        if (ropeOrCloth) //软绳
        {
            //添加质点
            Transform[] points = new Transform[size];
            for (int i = 0; i < size; i++)
            {
                Vector3 position = new Vector3(i * unit, 0, 0);
                GameObject point = new GameObject();
                allPoints.Add(point.transform);
                lastPositions[point.transform] = position;
                IconManager.SetIcon(point, IconManager.Icon.CircleGreen);
                point.transform.position = position;
                points[i] = point.transform;
            }

            for (int i = 0; i < size - 1; i++)
            {
                allSprings.Add(new Spring(points[i], points[i + 1]));
            }
        }
        else //布料
        {
            //添加质点
            Transform[,] points = new Transform[size, size];
            for (int i = 0, y = 0; i < size; i++, y += unit)
            for (int j = 0, x = 0; j < size; j++, x += unit)
            {
                Vector3 position = new Vector3(x, y, 0);
                GameObject point = new GameObject();
                allPoints.Add(point.transform);
                lastPositions[point.transform] = position;
                IconManager.SetIcon(point, IconManager.Icon.CircleGreen);
                point.transform.position = position;
                points[i, j] = point.transform;
            }

            //添加弹簧
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                //横纵结构
                if (x + 1 < size)
                    allSprings.Add(new Spring(points[y, x], points[y, x + 1]));
                if (y + 1 < size)
                    allSprings.Add(new Spring(points[y, x], points[y + 1, x]));
                //斜向结构
                if (x + 1 < size && y + 1 < size) //右下
                    allSprings.Add(new Spring(points[y, x], points[y + 1, x + 1]));
                if (x + 1 < size && y - 1 >= 0) //右上
                    allSprings.Add(new Spring(points[y, x], points[y - 1, x + 1]));
                //抗弯折弹簧
                if (x + 2 < size)
                    allSprings.Add(new Spring(points[y, x], points[y, x + 2]));
                if (y + 2 < size)
                    allSprings.Add(new Spring(points[y, x], points[y + 2, x]));
            }
        }


        foreach (Spring spring in allSprings)
        {
            spring.length = Vector3.Distance(spring.pointA.position, spring.pointB.position);
        }
    }

    float GetOneMinusDrag(Transform transform)
    {
        return transform == Selection.activeTransform ? 0 : 1 - drag;
    }

    void Update()
    {
        foreach (Transform point in allPoints)
        {
            //Verlet积分法（但消去了对加速度的计算，完全基于位移控制质点）
            //由于直接基于位移修改和计算，后续的每次力计算都会自动考虑力的相互作用，因此弹簧将自带阻力。
            //由于直接修改位置实现速度更改，质点积分便不能放在末尾更新，否则会步进两次。
            Vector3 position = point.position;
            point.position += (position - lastPositions[point]) * GetOneMinusDrag(point);
            lastPositions[point] = position;
        }

        if (useGravity)
            foreach (Transform point in allPoints)
            {
                const float Gravity = 0.02f * 0.02f * -9.81f;
                point.position += new Vector3(0, Gravity, 0) * GetOneMinusDrag(point);
            }

        foreach (Spring spring in allSprings)
        {
            Transform pointA = spring.pointA;
            Transform pointB = spring.pointB;

            //使用质点结算了当前速度和力后的位置信息来计算参数（这里力始终直接施加在位移上），这种类似半隐式欧拉的计算方法可以提高模拟稳定性，
            //因为它能考虑到与当前其他力的相互作用，从而能实现一定的弹簧阻力，否则得额外实现弹簧阻力。
            //但这种阻力也有缺点，它会使质点不再遵循能量守恒（释放的弹力会因其他力变化，而不是基于距离的定值），在单摆实验中无法回到相同的高度，而其他实现阻力的方式没有这种问题。
            Vector3 positionA = pointA.position;
            Vector3 positionB = pointB.position;
            Vector3 vector = positionA - positionB;
            // 下面这条注释使用的质点位置是当前帧尚未结算过速度或其他力的位置，也就是说不会受其他力影响，只基于瞬时距离计算，虽然结果不稳定，需要额外配备阻力功能，但它遵循能量守恒定律，可以用单摆做实验
            // Vector3 vector = particleA.lastPosition - particleB.lastPosition;
            if (vector == Vector3.zero) vector = new Vector3(0, float.Epsilon, 0);
            float distance = vector.magnitude;
            //胡克定律：弹力=弹力系数*距离*方向
            float tendency = distance - spring.length; //距离（可为负数来使方向取反）
            Vector3 direction = vector / distance; //方向
            //弹力系数实际是有上下限的，若当前质点基于约束位置距离为a，则最高不能超过2a，因为超过后每次迭代，离约束点的位置只会越来越远。
            //故此处的弹力系数是基于位移的，范围为[0,2]，代表的就是上述的范围限制，因此此处得到的“弹力”实际是位移。
            //当弹力系数为1时，得到的位移后顶点是最佳约束点，因为它完美按照了弹簧的距离要求进行位移。
            //若非完美约束点，那最远也不能超过最佳约束点1倍距离，因为大于1后每次迭代只会越来越远，最终弹簧将崩溃。
            //考虑添加力后将累计速度，而速度也会产生位移，除非有足以抵消的阻力（包括半隐式欧拉积分法导致的阻力），否则弹力系数不能超过1。
            Vector3 move = elasticity * tendency * direction;

            float moveWeight;
            if (ropeOrCloth && drag > 0.99f)
            {
                //模拟钢绳
                moveWeight = 1;
            }
            else
            {
                //弹簧会使两端质点一起位移来满足约束要求。要考虑两端质点阻力不同，分配不同的位移权重，
                //以便在阻力影响下也能正确达到约束位置，当然最主要的还是为了防止固定点被拉拽。
                float oneMinusDragB = GetOneMinusDrag(pointB);
                float oneMinusDragA = GetOneMinusDrag(pointA);
                moveWeight = Mathf.Approximately(oneMinusDragB + oneMinusDragA, 0.0f) ? 0.5f : oneMinusDragB / (oneMinusDragB + oneMinusDragA);
            }
            pointB.position += moveWeight * move;
            pointA.position += (1 - moveWeight) * -move;
        }
    }

    void OnDrawGizmos()
    {
        if (allSprings != null)
            foreach (Spring spring in allSprings)
            {
                Gizmos.DrawLine(spring.pointA.position, spring.pointB.position);
            }
    }
}
