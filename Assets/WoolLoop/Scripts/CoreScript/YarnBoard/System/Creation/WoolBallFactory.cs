using System.Threading;
using Common;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class WoolBallFactory : IFactory<WoolBall>
{
    public UniTask<WoolBall> Create(ICreateParameters parameters, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (parameters is not WoolBallCreateParameters createParameters)
            throw new System.ArgumentException($"Expected {nameof(WoolBallCreateParameters)}.", nameof(parameters));

        GameObject instance;
        if (PrefabProfile.WoolBallPrefab != null)
        {
            instance = Object.Instantiate(PrefabProfile.WoolBallPrefab, createParameters.Parent, false);
        }
        else
        {
            instance = new GameObject("WoolBall");
            instance.transform.SetParent(createParameters.Parent, false);
        }

        instance.name = $"WoolBall_{createParameters.Data.BallId}";

        if (!instance.TryGetComponent(out WoolBall woolBall))
            woolBall = instance.AddComponent<WoolBall>();

        woolBall.OnCreated(createParameters);
        return UniTask.FromResult(woolBall);
    }
}
