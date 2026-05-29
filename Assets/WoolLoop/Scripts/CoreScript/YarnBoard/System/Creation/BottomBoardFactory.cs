using System.Threading;
using Common;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class BottomBoardFactory : IFactory<BottomBoard>
{
    public GlobalYarnBoardSetting globalYarnBoardSetting;

    public UniTask<BottomBoard> Create(ICreateParameters parameters, CancellationToken cancellationToken = default)
    {
        throw new System.NotImplementedException();
    }
}