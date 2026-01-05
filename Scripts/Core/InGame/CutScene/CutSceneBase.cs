using System.Collections;
using UnityEngine;
using Cysharp.Threading.Tasks;

public abstract class CutSceneBase
{
    public abstract UniTask BeginCutScene(CutSceneArgs arg);
}
