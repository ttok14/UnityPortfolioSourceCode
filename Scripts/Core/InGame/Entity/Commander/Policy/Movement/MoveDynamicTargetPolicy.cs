//using System;
//using System.Collections.Generic;
//using System.Threading;
//using UnityEngine;

//public class MoveDynamicTargetPolicy : MovePolicyBase
//{
//    EntityBase _lastTarget;

//    PathListPoolable _cachedPath;

//    bool _isRequestingPath;

//    uint _pathVersion;

//    CancellationTokenSource _ctkSrc;

//    public MoveDynamicTargetPolicy(EntityBase owner, IPathProvider pathProvider)
//        : base(owner, pathProvider)
//    {
//    }

//    public override MoveCommand GetCommand(EntityBase mover, EntityBase target)
//    {
//        if (target == null || _isRequestingPath)
//        {
//            return MoveCommand.Stop;
//        }
//        // static target 전용이니까 굳이 '거리' 체크해서 갱신할 필요 없음
//        else if (_lastTarget == target)
//        {
//            return new MoveCommand()
//            {
//                result = MoveCommandResult.Path,
//                path = _cachedPath,
//                pathVersion = _pathVersion
//            };
//        }

//        _lastTarget = target;

//        RequestPath(target);

//        return MoveCommand.Stop;
//    }

//    void RequestPath(EntityBase target)
//    {
//        if (_ctkSrc != null)
//        {
//            _ctkSrc.Cancel();
//            _ctkSrc.Dispose();
//            _ctkSrc = null;
//        }

//        _ctkSrc = new CancellationTokenSource();

//        _isRequestingPath = true;

//        // 타겟이 죽었을때도 Cancel 을 해야할지? 아니면
//        // 죽으면 자동으로 리타게팅되니까 현 시점에서 캔슬될거같기도? 
//        _pathProvider.FetchPath(_owner, target.ID, _ctkSrc.Token, OnPathReceived);
//    }

//    void OnPathReceived(PathListPoolable path)
//    {
//        if (_ctkSrc != null)
//        {
//            _ctkSrc.Dispose();
//            _ctkSrc = null;
//        }

//        _isRequestingPath = false;
//        _cachedPath = path;
//        _pathVersion++;
//    }
//}
