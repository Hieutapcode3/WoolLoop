using Common;
using BoardSpline.Runtime;
using UnityEngine;

public class WoolBall : MonoBehaviour, IRuntimeCreatable
{
    public WoolBallData Data { get; private set; }
    public BoardSplineDataAdapterInfo Adapter { get; private set; }
    public WoolBallVisual Visual { get; private set; }

    public void OnCreated(ICreateParameters parameters)
    {
        if (parameters is not WoolBallCreateParameters createParameters)
            throw new System.ArgumentException($"Expected {nameof(WoolBallCreateParameters)}.", nameof(parameters));

        Data = createParameters.Data;
        Adapter = createParameters.Adapter;
        transform.localPosition = Adapter.IndexToWorld(Data.tileId);

        Visual = GetComponentInChildren<WoolBallVisual>(true);
        if (Visual == null)
            Visual = EnsureVisualChild().AddComponent<WoolBallVisual>();

        Visual.Render(Data, Adapter);
    }

    private GameObject EnsureVisualChild()
    {
        var visualTransform = transform.Find("Visual");
        if (visualTransform != null)
            return visualTransform.gameObject;

        var visual = new GameObject("Visual");
        visual.transform.SetParent(transform, false);
        return visual;
    }
}
