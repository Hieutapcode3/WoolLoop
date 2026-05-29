using System.Threading;
using Common;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class WoolBallFactory : IFactory<WoolBall>
{
    public UniTask<WoolBall> Create(ICreateParameters parameters, CancellationToken cancellationToken = default)
    {
        throw new System.NotImplementedException();
    }
}
