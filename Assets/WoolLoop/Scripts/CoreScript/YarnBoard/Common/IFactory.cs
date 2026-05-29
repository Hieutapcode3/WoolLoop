using System.Threading;
using Cysharp.Threading.Tasks;

namespace Common
{
    public interface IFactory<TEntity>
    {
        UniTask<TEntity> Create(ICreateParameters parameters, CancellationToken cancellationToken = default);
    }
}