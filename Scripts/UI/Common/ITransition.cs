using System.Collections;
using Cysharp.Threading.Tasks;

public interface ITransition
{
    UniTask Enter();
    UniTask Exit();
}
