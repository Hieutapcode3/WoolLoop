using System.Threading;
using Common;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class BottomBoardFactory : IFactory<BottomBoard>
{
    public UniTask<BottomBoard> Create(ICreateParameters parameters, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (parameters is not BottomBoardCreateParameters createParameters)
            throw new System.ArgumentException($"Expected {nameof(BottomBoardCreateParameters)}.", nameof(parameters));

        var boardObject = new GameObject("BottomBoard");
        boardObject.transform.SetParent(createParameters.Parent, false);

        var board = boardObject.AddComponent<BottomBoard>();
        board.OnCreated(createParameters);
        return UniTask.FromResult(board);
    }
}
