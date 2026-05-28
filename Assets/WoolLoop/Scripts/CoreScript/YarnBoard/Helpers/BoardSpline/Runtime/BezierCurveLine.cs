using UnityEngine;

namespace NgoUyenNguyen.Line
{
    [RequireComponent(typeof(LineRenderer)), ExecuteInEditMode]
    public class BezierCurveLine : MonoBehaviour
    {

        [Header("Curve Settings")]
        [SerializeField, Range(0,1)] private float curveTightness = 0.5f;
        [SerializeField] private float curveHeight = 2.0f;
        [SerializeField, Range(1, 100)] private int resolution = 100;
        [SerializeField] private Transform target;


        private LineRenderer line;







        private void Awake()
        {
            line = GetComponent<LineRenderer>();
        }

        private void Update()
        {
            if (target != null)
            {
                Draw(target.position);
            }
        }









        public void Draw(Vector3 target)
        {
            if (target == null) return;

            Vector3 p0 = transform.position;
            Vector3 p1 = target;

            Vector3 mid = (p0 + p1) * 0.5f;
            Vector3 up = transform.right.normalized * curveHeight;

            // Tính 2 điểm điều khiển lệch lên để tạo chiều cao
            Vector3 c1 = Vector3.Lerp(p0, mid, curveTightness) + up;
            Vector3 c2 = Vector3.Lerp(p1, mid, curveTightness) + up;

            line.positionCount = resolution + 1;

            for (int i = 0; i <= resolution; i++)
            {
                float t = i / (float)resolution;
                Vector3 point = CalculateCubicBezierPoint(t, p0, c1, c2, p1);
                line.SetPosition(i, point);
            }
        }

        public void TurnOffCurve()
        {
            line.enabled = false;
        }








        private Vector3 CalculateCubicBezierPoint(float t, Vector3 p0, Vector3 c1, Vector3 c2, Vector3 p1)
        {
            float u = 1 - t;
            return
                u * u * u * p0 +
                3 * u * u * t * c1 +
                3 * u * t * t * c2 +
                t * t * t * p1;
        }
    }
}
