using System.Collections;
using UnityEngine;


[DisallowMultipleComponent]
[RequireComponent(typeof(ParticleSystem))]
public class RecycleOnParticleNoOut : MonoBehaviour, IRecycleHandle
{
	private ParticleSystem _particleSystem;
	private GameObject _go;

	private void Awake()
	{
		_particleSystem = GetComponent<ParticleSystem>();
		_go = gameObject;
	}

	public void SetRecycle(float delayTime)
	{
		Invoke(nameof(StopOnTime), delayTime);
	}

#if UNITY_EDITOR
	[ContextMenu("Stop And Wait Particle")]
#endif
	private void StopOnTime()
	{
		_particleSystem.Stop();

		if (_go)
			StartCoroutine(WaitForRecycle());
	}

	IEnumerator WaitForRecycle()
	{
		while (_go && _particleSystem.IsAlive(true))
		{
			yield return null;
		}

		if (_go)
			_go.Recycle();
	}
}
