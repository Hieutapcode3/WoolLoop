using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;
using AntiStress;
using System.Linq;
using UnityEditor;
using UnityEngine.UI;

namespace VTLTools
{
    /// <summary>
    /// A static class for general helpful methods
    /// </summary> 

    public class Helpers : MonoBehaviour
    {

        public static void DestroyAllChilds(Transform go)
        {
            TransformExtensions.DestroyAllChildren(go);
        }

        public static void DestroyImmediateAllChilds(Transform go)
        {
            TransformExtensions.DestroyAllChildren(go, true);
        }

        public static List<T> GetAllChildsComponent<T>(Transform _parent)
        {
            return _parent.GetAllChildrenComponent<T>();
        }

        public static string FormatTime(float _time)
        {
            int _minutes = Mathf.FloorToInt(_time / 60f);
            int _seconds = Mathf.FloorToInt(_time % 60f);
            return string.Format("{0:00}:{1:00}", _minutes, _seconds);
        }

        public static bool IsLongScreen()
        {
            return (float)Screen.currentResolution.width / (float)Screen.currentResolution.height < 9f / 16f;
        }

        public static void Shuffle<T>(List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = Random.Range(0, n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static List<int> ShuffleNumber(int start, int end)
        {
            List<int> _list = new List<int>();
            for (int i = start; i <= end; i++)
            {
                _list.Add(i);
            }
            Helpers.Shuffle(_list);
            return _list;
        }

        public static T GetRandomEnum<T>(T _min, T _max)
        {
            System.Array A = System.Enum.GetValues(typeof(T));
            T V = (T)A.GetValue(UnityEngine.Random.Range((int)(object)_min, (int)(object)_max));
            return V;
        }

        public static List<T> GetAllEnum<T>()
        {

            List<T> enumValues = new();
            foreach (T value in System.Enum.GetValues(typeof(T)))
            {
                enumValues.Add(value);
            }
            return enumValues;
        }

        public static bool GetMousePostionOnCollider(Ray _ray, LayerMask _mask, out Vector3 _hitPoint)
        {
            if (Physics.Raycast(_ray, out RaycastHit _raycastHitPoint, Mathf.Infinity, _mask))
            {
                _hitPoint = _raycastHitPoint.point;
                return true;
            }
            else
            {
                _hitPoint = Vector3.zero;
                return false;
            }
        }

        public static bool GetMousePostionOnCollider(Ray _ray, LayerMask _mask, out RaycastHit _raycastHit)
        {
            if (Physics.Raycast(_ray, out RaycastHit _raycastHitPoint, Mathf.Infinity, _mask))
            {
                _raycastHit = _raycastHitPoint;
                return true;
            }
            else
            {
                _raycastHit = new RaycastHit();
                return false;
            }
        }

        public static bool PhysicRaycast2D(Camera _cam, out RaycastHit2D _hit)
        {
            // Cast a ray from the mouse position
            _hit = Physics2D.Raycast(_cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);

            if (_hit.collider != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static float ParseFloatWithPoint(string _value)
        {
            if (_value.Contains(","))
                _value = _value.Replace(",", ".");

            if (float.TryParse(_value, out float _result))
            {
                return _result;
            }
            else
            {
                Debug.LogError("Fail to parse: " + _value);
                return 0;
            }
        }

        public static IEnumerator IEGetRequest(string _uri, System.Action<bool, string> _onCompleted)
        {
            UnityWebRequest _webRequest = UnityWebRequest.Get(_uri);
            yield return _webRequest.SendWebRequest();
            //
            if (_webRequest.result == UnityWebRequest.Result.ConnectionError || _webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                _onCompleted.Invoke(false, _webRequest.error);
            }
            else
            {
                string _jsonResponse = _webRequest.downloadHandler.text;
                _onCompleted.Invoke(true, _jsonResponse);
            }
        }


        public static List<T> GetDistinctElements<T>(List<T> list, int n, T _exclude = default)
        {
            var distinctElements = new HashSet<T>();
            var random = new System.Random();

            // Filter out the excluded element if provided and get distinct elements
            var filteredList = new List<T>(list);
            if (!EqualityComparer<T>.Default.Equals(_exclude, default))
            {
                filteredList.RemoveAll(item => item.Equals(_exclude));
            }

            while (distinctElements.Count < n && filteredList.Count > 0)
            {
                var index = random.Next(filteredList.Count);
                var element = filteredList[index];
                distinctElements.Add(element);
            }

            return new List<T>(distinctElements);
        }

        public static Vector3? GetMouseClickPointOnPlane(Plane plane)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                if (plane.Raycast(ray, out float enter))
                {
                    return ray.GetPoint(enter);
                }
            }
            return null;
        }

        public static Vector3 GetRandomPositionXZ(Vector3 _position, float _distance)
        {
            float x = Random.Range(-_distance, _distance);
            float z = Random.Range(-_distance, _distance);
            return new Vector3(_position.x + x, _position.y, _position.z + z);
        }

        public static Vector3 GetRandomPositionXY(Vector3 _position, float _distance)
        {
            float x = Random.Range(-_distance, _distance);
            float y = Random.Range(-_distance, _distance);
            return new Vector3(_position.x + x, _position.y + y, _position.z);
        }

        public static Vector3 GetRandomPosInRadius(Vector3 center, float radius)
        {
            float x = Random.Range(center.x - radius, center.x + radius);
            float y = Random.Range(center.y - radius, center.y + radius);
            float z = Random.Range(center.z - radius, center.z + radius);
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Thêm phần tử vào list tại index mong muốn. Nếu phần tử đã tồn tại, di chuyển nó đến index mới.
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu của list</typeparam>
        /// <param name="list">List cần thao tác</param>
        /// <param name="item">Phần tử cần thêm/di chuyển</param>
        /// <param name="targetIndex">Vị trí mong muốn (0-based)</param>
        /// <returns>True nếu thêm mới, False nếu di chuyển phần tử đã tồn tại</returns>
        public static bool InsertOrMoveToIndex<T>(List<T> list, T item, int targetIndex)
        {
            if (list == null)
            {
                Debug.LogError("List is null!");
                return false;
            }

            // Clamp targetIndex trong khoảng hợp lệ
            targetIndex = Mathf.Clamp(targetIndex, 0, list.Count);

            // Kiểm tra phần tử đã tồn tại chưa
            int existingIndex = list.IndexOf(item);

            if (existingIndex >= 0)
            {
                // Phần tử đã tồn tại - di chuyển đến vị trí mới
                if (existingIndex == targetIndex)
                {
                    // Đã ở đúng vị trí rồi
                    return false;
                }

                // Xóa khỏi vị trí cũ
                list.RemoveAt(existingIndex);

                // Điều chỉnh targetIndex nếu phần tử cũ ở trước vị trí target
                if (existingIndex < targetIndex)
                {
                    targetIndex--;
                }

                // Chèn vào vị trí mới
                list.Insert(targetIndex, item);
                return false;
            }
            else
            {
                // Phần tử chưa tồn tại - thêm mới
                list.Insert(targetIndex, item);
                return true;
            }
        }

        /// <summary>
        /// Đẩy phần tử lên đầu list (index 0). Nếu chưa tồn tại thì thêm vào đầu.
        /// </summary>
        public static bool MoveToFirst<T>(List<T> list, T item)
        {
            return InsertOrMoveToIndex(list, item, 0);
        }

        /// <summary>
        /// Đẩy phần tử xuống cuối list. Nếu chưa tồn tại thì thêm vào cuối.
        /// </summary>
        public static bool MoveToLast<T>(List<T> list, T item)
        {
            if (list == null) return false;
            return InsertOrMoveToIndex(list, item, list.Count);
        }

        //===================================================
        #region [DEBUG]
        //debug draw sphere
        public static void DrawWireSphere(Vector3 center, float radius, Color color, float duration = -1, bool depthTest = true)
        {
            int segments = 24;
            float step = 360f / segments;

            // Vòng ngang XY
            for (int i = 0; i < segments; i++)
            {
                Vector3 p1 = center + Quaternion.Euler(0, step * i, 0) * Vector3.forward * radius;
                Vector3 p2 = center + Quaternion.Euler(0, step * (i + 1), 0) * Vector3.forward * radius;
                Debug.DrawLine(p1, p2, color, duration, depthTest);
            }

            // Vòng dọc XZ
            for (int i = 0; i < segments; i++)
            {
                Vector3 p1 = center + Quaternion.Euler(step * i, 0, 0) * Vector3.up * radius;
                Vector3 p2 = center + Quaternion.Euler(step * (i + 1), 0, 0) * Vector3.up * radius;
                Debug.DrawLine(p1, p2, color, duration, depthTest);
            }

            // Vòng dọc YZ
            for (int i = 0; i < segments; i++)
            {
                Vector3 p1 = center + Quaternion.Euler(0, 0, step * i) * Vector3.up * radius;
                Vector3 p2 = center + Quaternion.Euler(0, 0, step * (i + 1)) * Vector3.up * radius;
                Debug.DrawLine(p1, p2, color, duration, depthTest);
            }
        }

        //draw cube wireframe
        public static void DrawWireCube(Vector3 center, Vector3 size, Color color, float time = -1, bool depthTest = true)
        {
            float duration = time == -1 ? Time.deltaTime : time;
            Vector3 halfSize = size / 2;

            Vector3 p1 = center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z);
            Vector3 p2 = center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z);
            Vector3 p3 = center + new Vector3(halfSize.x, -halfSize.y, halfSize.z);
            Vector3 p4 = center + new Vector3(-halfSize.x, -halfSize.y, halfSize.z);

            Vector3 p5 = center + new Vector3(-halfSize.x, halfSize.y, -halfSize.z);
            Vector3 p6 = center + new Vector3(halfSize.x, halfSize.y, -halfSize.z);
            Vector3 p7 = center + new Vector3(halfSize.x, halfSize.y, halfSize.z);
            Vector3 p8 = center + new Vector3(-halfSize.x, halfSize.y, halfSize.z);

            // Bottom face
            Debug.DrawLine(p1, p2, color, duration, depthTest);
            Debug.DrawLine(p2, p3, color, duration, depthTest);
            Debug.DrawLine(p3, p4, color, duration, depthTest);
            Debug.DrawLine(p4, p1, color, duration, depthTest);

            // Top face
            Debug.DrawLine(p5, p6, color, duration, depthTest);
            Debug.DrawLine(p6, p7, color, duration, depthTest);
            Debug.DrawLine(p7, p8, color, duration, depthTest);
            Debug.DrawLine(p8, p5, color, duration, depthTest);

            // Vertical edges
            Debug.DrawLine(p1, p5, color, duration, depthTest);
            Debug.DrawLine(p2, p6, color, duration, depthTest);
            Debug.DrawLine(p3, p7, color, duration, depthTest);
            Debug.DrawLine(p4, p8, color, duration, depthTest);
        }

        public static void ClearUnityConsole()
        {
#if UNITY_EDITOR
            var assembly = System.Reflection.Assembly.GetAssembly(typeof(SceneView));
            var type = assembly.GetType("UnityEditor.LogEntries");
            var method = type.GetMethod("Clear");
            method.Invoke(null, null);
#endif
        }


        //===================================================
        #region [Mathf 2D]
        public static bool IsPointOnLineSegment(Vector2Int point, Vector2Int lineStart, Vector2Int lineEnd)
        {
            // Kiểm tra xem 3 điểm có thẳng hàng không
            Vector2Int v1 = point - lineStart;
            Vector2Int v2 = lineEnd - lineStart;

            // Cross product = 0 nghĩa là thẳng hàng
            int crossProduct = v1.x * v2.y - v1.y * v2.x;
            if (crossProduct != 0) return false;

            // Kiểm tra point có nằm trong bounds của line segment không
            int minX = Mathf.Min(lineStart.x, lineEnd.x);
            int maxX = Mathf.Max(lineStart.x, lineEnd.x);
            int minY = Mathf.Min(lineStart.y, lineEnd.y);
            int maxY = Mathf.Max(lineStart.y, lineEnd.y);

            return point.x >= minX && point.x <= maxX && point.y >= minY && point.y <= maxY;
        }
        #endregion
        //===================================================
        #endregion
        //===================================================

        public enum AspectRatioType
        {
            FitWidth,
            FitHeight,
            Fill,
            Custom
        }

        public static void SetImageAspect(Image img, AspectRatioType type, float targetWidth = -1, float targetHeight = -1, bool isFollowHighestResolution = false)
        {
            if (img == null) return;
            img.SetNativeSize();

            float nativeWidth = img.rectTransform.rect.width;
            float nativeHeight = img.rectTransform.rect.height;
            float aspect = nativeWidth / nativeHeight;
            if (isFollowHighestResolution)
            {
                type = nativeWidth > nativeHeight ? AspectRatioType.FitWidth : AspectRatioType.FitHeight;
            }
            switch (type)
            {
                case AspectRatioType.FitWidth:
                    img.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
                    img.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetWidth / aspect);
                    break;
                case AspectRatioType.FitHeight:
                    img.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
                    img.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetHeight * aspect);
                    break;
                case AspectRatioType.Fill:
                    img.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
                    img.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
                    break;
                case AspectRatioType.Custom:
                    img.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
                    img.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
                    break;
            }
        }

        public static Color FromHex(string hex)
        {
            Color c;
            ColorUtility.TryParseHtmlString(hex, out c);
            return c;
        }

        //find and add component if not exist
        public static T GetOrAddComponent<T>(GameObject _go) where T : Component
        {
            T _comp = _go.GetComponentInChildren<T>();
            if (_comp == null)
            {
                _comp = _go.AddComponent<T>();
            }
            return _comp;
        }
    }


}