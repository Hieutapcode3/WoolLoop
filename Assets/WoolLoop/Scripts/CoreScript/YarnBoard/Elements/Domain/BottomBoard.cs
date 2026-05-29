using Common;
using BoardSpline.Runtime;
using UnityEngine;

public class BottomBoard : MonoBehaviour, IRuntimeCreatable
{
    public LevelData Level { get; private set; }
    public BoardSplineDataAdapterInfo Adapter { get; private set; }
    public BottomBoardVisual Visual { get; private set; }

    public void OnCreated(ICreateParameters parameters)
    {
        if (parameters is not BottomBoardCreateParameters createParameters)
            throw new System.ArgumentException($"Expected {nameof(BottomBoardCreateParameters)}.", nameof(parameters));

        Level = createParameters.Level;
        Adapter = createParameters.Adapter;
        Visual = GetComponent<BottomBoardVisual>();
        if (Visual == null)
            Visual = gameObject.AddComponent<BottomBoardVisual>();

        Visual.Render(Level, Adapter);
    }
}
