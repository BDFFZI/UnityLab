using UnityEngine;

public class PhysicsCurveGenerator : MonoBehaviour
{
    [SerializeField] LineRenderer lineRenderer;
    [SerializeField] Transform origin;
    [SerializeField] Transform destination;
    [SerializeField] AnimationCurve speedCurve;
    [SerializeField] float speedPower;
    [SerializeField] int precision = 30;

    Vector3[] points;

    void Awake()
    {
        points = new Vector3[precision + 1];
    }

    void OnEnable()
    {
        Application.onBeforeRender += OnBeforeRender;

        for (int i = 0; i < points.Length; i++)
            points[i] = origin.position;
        lineRenderer.positionCount = points.Length;
        lineRenderer.SetPositions(points);
    }

    void OnDisable()
    {
        Application.onBeforeRender -= OnBeforeRender;
    }

    void OnBeforeRender()
    {
        Vector3 originPosition = origin.position;
        Vector3 destinationPosition = destination.position;
        for (int i = 0; i <= precision; i++)
            points[i] = Vector3.Lerp(originPosition, destinationPosition, (float)i / precision);


        lineRenderer.positionCount = points.Length;
        for (int i = 0; i < points.Length; i++)
        {
            float x = (float)i / precision;
            lineRenderer.SetPosition(i, Vector3.Lerp(lineRenderer.GetPosition(i), points[i], Mathf.Pow(speedCurve.Evaluate(x), speedPower)));
        }
    }
}
