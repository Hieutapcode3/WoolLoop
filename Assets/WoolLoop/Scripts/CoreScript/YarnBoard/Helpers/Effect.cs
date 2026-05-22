using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VTLTools.Effect
{
    public class Effect : MonoBehaviour
    {
        [SerializeField] MainParticleSystem mainParticleSystem;
        [SerializeField] bool isDestroyAfterStop = true;
        [SerializeField] bool isUsePool = false;
        private Coroutine autoStopCoroutine;

        [ShowInInspector]
        public bool IsPlaying
        {
            get
            {
                if (mainParticleSystem != null)
                    return mainParticleSystem.ThisParticleSystem.isPlaying;
                else
                    return false;
            }
        }
        public void OnParticleSystemStoppedListener()
        {
            //DPDebug.Log($"<color=green>Ping</color>");
            if (isDestroyAfterStop)
                Destroy(this.gameObject);
            if (isUsePool)
            {
                ObjectPool.Recycle(this);
            }
        }

        public void Init(Vector3 _pos, Transform _parent = null)
        {
            this.transform.position = _pos;
            this.transform.parent = _parent;
        }

        public void Play()
        {
            if (IsPlaying)
                return;
            EnsureStopActionCallback();
            mainParticleSystem.ThisParticleSystem.Play();
        }

        public void Play(float duration)
        {
            if (duration <= 0f)
            {
                Play();
                return;
            }

            Play();

            if (autoStopCoroutine != null)
            {
                StopCoroutine(autoStopCoroutine);
            }

            autoStopCoroutine = StartCoroutine(AutoStopAfterDuration(duration));
        }

        public void Stop()
        {
            CancelAutoStopCoroutine();
            mainParticleSystem.ThisParticleSystem.Stop();
        }

        public void Pause()
        {
            CancelAutoStopCoroutine();
            mainParticleSystem.ThisParticleSystem.Pause();
        }

        private IEnumerator AutoStopAfterDuration(float duration)
        {
            yield return new WaitForSeconds(duration);

            autoStopCoroutine = null;

            if (mainParticleSystem == null)
            {
                yield break;
            }

            ParticleSystem particleSystem = mainParticleSystem.ThisParticleSystem;
            if (particleSystem == null || !particleSystem.isPlaying)
            {
                yield break;
            }

            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void CancelAutoStopCoroutine()
        {
            if (autoStopCoroutine == null)
            {
                return;
            }

            StopCoroutine(autoStopCoroutine);
            autoStopCoroutine = null;
        }

        private void EnsureStopActionCallback()
        {
            if (mainParticleSystem == null)
            {
                return;
            }

            ParticleSystem particleSystem = mainParticleSystem.ThisParticleSystem;
            if (particleSystem == null)
            {
                return;
            }

            var main = particleSystem.main;
            main.stopAction = ParticleSystemStopAction.Callback;
        }

        public void SetRateOverTime(float _value)
        {
            var emission = mainParticleSystem.ThisParticleSystem.emission;
            emission.rateOverTime = _value;
        }

        public void ChangeColor(Color _color)
        {
            SetParticleColor(_color);
        }

        public void SetParticleColor(Color color, bool includeChildren = true)
        {
            if (mainParticleSystem == null)
            {
                return;
            }

            ParticleSystem particleSystem = mainParticleSystem.ThisParticleSystem;
            if (particleSystem == null)
            {
                return;
            }

            if (!includeChildren)
            {
                SetParticleSystemStartColor(particleSystem, color);
                return;
            }

            ParticleSystem[] systems = particleSystem.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                SetParticleSystemStartColor(systems[i], color);
            }
        }

        public void SetParticleShapeBoxFromWorldPoints(IList<Vector3> worldPoints, bool includeChildren = true)
        {
            if (worldPoints == null || worldPoints.Count == 0)
            {
                return;
            }

            Vector3 center = Vector3.zero;
            for (int i = 0; i < worldPoints.Count; i++)
            {
                center += worldPoints[i];
            }
            center /= worldPoints.Count;

            Vector3 axisX = Vector3.right;
            float width = 0f;
            float height = 0f;

            if (worldPoints.Count >= 2)
            {
                Vector3 edgeX = worldPoints[1] - worldPoints[0];
                width = edgeX.magnitude;
                if (width > 0.0001f)
                {
                    axisX = edgeX / width;
                }
            }

            if (worldPoints.Count >= 3)
            {
                Vector3 edgeY = worldPoints[2] - worldPoints[1];
                height = edgeY.magnitude;
            }

            if (width <= 0.0001f || height <= 0.0001f)
            {
                Vector3 min = worldPoints[0];
                Vector3 max = worldPoints[0];
                for (int i = 1; i < worldPoints.Count; i++)
                {
                    min = Vector3.Min(min, worldPoints[i]);
                    max = Vector3.Max(max, worldPoints[i]);
                }

                width = Mathf.Max(width, max.x - min.x);
                height = Mathf.Max(height, max.y - min.y);
            }

            float rotationZ = Mathf.Atan2(axisX.y, axisX.x) * Mathf.Rad2Deg;
            transform.SetPositionAndRotation(center, Quaternion.Euler(0f, 0f, rotationZ));

            ApplyParticleShapeBox(new Vector3(
                Mathf.Max(0.01f, width),
                Mathf.Max(0.01f, height),
                0f), includeChildren);
        }

        public void SetParticleShapeBox(Vector3 size, bool includeChildren = true)
        {
            ApplyParticleShapeBox(size, includeChildren);
        }

        public void SetParticleShapeBox(Vector2 size, bool includeChildren = true)
        {
            ApplyParticleShapeBox(new Vector3(size.x, size.y, 0f), includeChildren);
        }

        private static void SetParticleSystemStartColor(ParticleSystem particleSystem, Color color)
        {
            if (particleSystem == null)
            {
                return;
            }

            var main = particleSystem.main;
            main.startColor = color;
        }

        private void ApplyParticleShapeBox(Vector3 size, bool includeChildren)
        {
            if (mainParticleSystem == null)
            {
                return;
            }

            ParticleSystem particleSystem = mainParticleSystem.ThisParticleSystem;
            if (particleSystem == null)
            {
                return;
            }

            if (!includeChildren)
            {
                SetParticleSystemShapeBox(particleSystem, size);
                return;
            }

            ParticleSystem[] systems = particleSystem.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                SetParticleSystemShapeBox(systems[i], size);
            }
        }

        private static void SetParticleSystemShapeBox(ParticleSystem particleSystem, Vector3 size)
        {
            if (particleSystem == null)
            {
                return;
            }

            var shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.position = Vector3.zero;
            shape.rotation = Vector3.zero;
            shape.scale = new Vector3(
                Mathf.Max(0.01f, size.x),
                Mathf.Max(0.01f, size.y),
                Mathf.Max(0.01f, size.y));
        }

        //===================================================
        #region [EDITOR]
        [Button]
        public void SetUp()
        {
            ParticleSystem ps = Helpers.GetOrAddComponent<ParticleSystem>(this.gameObject);
            MainParticleSystem mainParticleSystem = Helpers.GetOrAddComponent<MainParticleSystem>(ps.gameObject);
            mainParticleSystem.effect = this;
            this.mainParticleSystem = mainParticleSystem;
        }
        #endregion
        //===================================================
    }
}
