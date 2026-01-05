using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using Cysharp.Threading.Tasks;
using System.Threading;
using System.Text;
using GameDB;

public class EntityStandardMovePart : EntityMovePartBase
{
    public enum DirectionalOption
    {
        None = 0,

        OnlyRotate,
        MoveAfterRotateDone,
        Simulatenous,
    }

    class PathMove
    {
        public bool isMoving;
        // 기존에는 new 할당했는데 제거 . 
        public PathListPoolable path;
        public E_EntityType entityTypeContext;
        public int currentPathIndex;

        public TweenValue speedTween = new TweenValue(); // = new TweenValue(0f, 1f, 1.5f, DG.Tweening.Ease.Linear, null, null);
        // public Coroutine moveCo;
        public CancellationTokenSource manualCancellationToken;
        public CancellationTokenSource linkedCancellationToken;

        public void RefreshCancellationToken(GameObject ownerGameObject)
        {
            manualCancellationToken = new CancellationTokenSource();
            linkedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(
                manualCancellationToken.Token, ownerGameObject.GetCancellationTokenOnDestroy());
        }

        public void Stop()
        {
            isMoving = false;

            if (path != null)
            {
                path.ReturnToPool();
                path = null;
            }

            entityTypeContext = E_EntityType.None;

            speedTween.StartTween(speedTween.CurrentValue, 0f, 0.6f, DG.Tweening.Ease.OutQuad);

            if (manualCancellationToken != null)
            {
                manualCancellationToken.Cancel();

                linkedCancellationToken.Dispose();
                linkedCancellationToken = null;

                manualCancellationToken.Dispose();
                manualCancellationToken = null;
            }

            //if (moveCo != null)
            //{
            //    CoroutineRunner.Instance.StopCoroutine(moveCo);
            //    moveCo = null;
            //}
        }
    }

    class Directional
    {
        public DirectionalOption option;

        public bool rotEnabled;
        public Vector3 dirToTarget;

        public bool enableDistanceAutoStop;
        public float sqrAutoStopDistance;

        public void Stop()
        {
            option = DirectionalOption.None;
            rotEnabled = false;
            dirToTarget = default;
            enableDistanceAutoStop = false;
            sqrAutoStopDistance = 0;
        }
    }

    EntityStatData _stat;

    PathMove _pathMoveProps = new PathMove();
    Directional _directionalProps = new Directional();

    public override bool IsMoving => _pathMoveProps.isMoving || _directionalProps.option != DirectionalOption.None;

    public override void OnPoolActivated(IInstancePoolInitData initData)
    {
        base.OnPoolActivated(initData);
        _stat = Owner.GetData(EntityDataCategory.Stat) as EntityStatData;
    }

    public override void OnPoolReturned()
    {
        _stat = null;
        // 풀에 반납될땐 바로 그냥 Tween 정리
        _pathMoveProps.speedTween.Release();
        base.OnPoolReturned();
    }

    public override void MoveToPathFinding(Vector3 destination)
    {
        Stop();

        _pathMoveProps.path = InGameManager.Instance.CacheContainer.PathBufferCache.GetEmtpyPath();
        _pathMoveProps.isMoving = true;

        _pathMoveProps.RefreshCancellationToken(Owner.gameObject);
        DoPathFindingAsync(destination, _pathMoveProps.linkedCancellationToken.Token).Forget();
        //_pathMoveProps.moveCo = CoroutineRunner.Instance.RunCoroutine(DoPathFindingCo(destination));
    }

    public override void MoveAlongPaths(PathListPoolable newPaths, MoveContext? context)
    {
        Stop();

        _pathMoveProps.isMoving = true;
        _pathMoveProps.path = newPaths;

        if (context.HasValue)
        {
            _pathMoveProps.entityTypeContext = context.Value.DestEntityType;
        }
        else
        {
            _pathMoveProps.entityTypeContext = E_EntityType.None;
        }

        if (newPaths != null)
        {
            _pathMoveProps.RefreshCancellationToken(Owner.gameObject);
            MoveCurrentPathsAsync(_pathMoveProps.linkedCancellationToken.Token).Forget();
        }

    }

    public override void MoveDirectional(Vector3 direction, float normalizedAmount)
    {
        StopPathMovement(false);

        Owner.MovementBeginListener?.Invoke(Owner, false, direction, null);

        var prevOption = _directionalProps.option;

        if (prevOption == DirectionalOption.None)
        {
            _stat.SetCurrentRotationSpeed(999/*_stat.TableRotationSpeed*/);
        }

        _stat.SetCurrentMoveSpeed(_stat.TableMoveSpeed * normalizedAmount);

        _directionalProps.option = DirectionalOption.Simulatenous;

        _directionalProps.dirToTarget = direction;

        // 자동 스톱 없음 
        _directionalProps.enableDistanceAutoStop = false;
        _directionalProps.sqrAutoStopDistance = 0;

        _directionalProps.rotEnabled = true;
    }

    public override void MoveToDestDirectional(Vector3 destPos, float normalizedAmount)
    {
        StopPathMovement(false);

        var moverPos = Owner.ApproxPosition.FlatHeight();

        Vector3 direction = (destPos - moverPos).normalized;

        Owner.MovementBeginListener?.Invoke(Owner, false, direction, null);

        var prevOption = _directionalProps.option;

        if (prevOption == DirectionalOption.None)
        {
            _stat.SetCurrentRotationSpeed(999/*_stat.TableRotationSpeed*/);
        }

        _stat.SetCurrentMoveSpeed(_stat.TableMoveSpeed * normalizedAmount);

        _directionalProps.option = DirectionalOption.Simulatenous;
        _directionalProps.dirToTarget = direction;
        _directionalProps.enableDistanceAutoStop = true;
        _directionalProps.sqrAutoStopDistance = Vector3.SqrMagnitude(destPos - moverPos);
        _directionalProps.rotEnabled = true;
    }

    public override void RotateToDirection(Vector3 direction)
    {
        Stop();

        var prevOption = _directionalProps.option;

        if (prevOption == DirectionalOption.None)
        {
            _stat.SetCurrentRotationSpeed(999/*_stat.TableRotationSpeed*/);
        }

        direction.y = 0;
        _directionalProps.option = DirectionalOption.OnlyRotate;
        _directionalProps.dirToTarget = direction;
        _directionalProps.rotEnabled = true;
    }

    public override void Stop()
    {
        StopPathMovement(false);

        if (_directionalProps.option != DirectionalOption.None)
        {
            Owner.MovementEndListener?.Invoke(Owner, true);
        }

        _directionalProps.Stop();

        if (_stat != null)
        {
            _stat.SetCurrentMoveSpeed(0);
            _stat.SetCurrentRotationSpeed(0);
        }
    }

    public override void DoFixedUpdate()
    {
        base.DoFixedUpdate();

        if (_directionalProps.option != DirectionalOption.None)
        {
            if (_directionalProps.rotEnabled)
            {
                bool isIdentical = RotateToward(_stat.CurrentRotationSpeed * Time.fixedDeltaTime, _directionalProps.dirToTarget);

                if (isIdentical)
                {
                    _directionalProps.rotEnabled = false;
                }
            }

            if (_directionalProps.option == DirectionalOption.OnlyRotate && _directionalProps.rotEnabled == false)
            {
                Stop();
                return;
            }
            else if (_directionalProps.option == DirectionalOption.MoveAfterRotateDone && _directionalProps.rotEnabled)
            {
                return;
            }

            if (_directionalProps.option == DirectionalOption.Simulatenous)
            {
                var movement = Mover.forward * _stat.CurrentMoveSpeed * Time.fixedDeltaTime;

                if (_rigidbody)
                {
                    RigidBodyMove(_rigidbody.position + movement);
                }
                else
                {
                    Move(movement);
                }

                Owner.MovementProcessingListener?.Invoke(Owner, Owner.ApproxPosition);

                if (_directionalProps.enableDistanceAutoStop)
                {
                    _directionalProps.sqrAutoStopDistance = Mathf.Max(_directionalProps.sqrAutoStopDistance - movement.sqrMagnitude, 0f);

                    // 도착 간주
                    if (_directionalProps.sqrAutoStopDistance <= 0.01f)
                    {
                        Stop();
                        return;
                    }
                }
            }
        }
    }

    //----------------------------------------------------------------//

    private async UniTaskVoid DoPathFindingAsync(Vector3 destination, CancellationToken token)
    {
        bool success = await PathFindingManager.Instance.PathFinder.FindPath(
            Owner.ApproxPosition.FlatHeight(),
            destination,
            _pathMoveProps.path.Instance,
            DBEntity.Get(Owner.EntityTID).EntityFlags,
            token,
            new PathFinding.Config(trimPaths: true));

        if (success == false)
            return;

        await MoveCurrentPathsAsync(token);
    }

    //private IEnumerator DoPathFindingCo(Vector3 destination)
    //{
    //    yield return PathFindingManager.Instance.PathFinder.FindPath(
    //        Mover.position,
    //        destination,
    //        _pathMoveProps.paths,
    //        DBEntity.Get(_owner.EntityTID).EntityFlags,
    //        new PathFinding.Config(trimPaths: true));

    //    yield return MoveCurrentPathsCo();
    //}

    void StopPathMovement(bool reachedDest)
    {
        bool isMoving = _pathMoveProps.isMoving;

        if (_pathMoveProps.path != null)
        {
            // 유닛을 상대로 움직일때도 잔여 패스를 기록해두면
            // 너무 비대해짐. 때문에 건물일때만 등록함 , 캐시히트율 그나마 높을것임.
            if (_pathMoveProps.entityTypeContext == E_EntityType.Structure)
            {
                TryRegisterRemainedPathToCache();
            }
        }

        _pathMoveProps.Stop();

        if (isMoving)
        {
            Owner.MovementEndListener?.Invoke(Owner, reachedDest);
        }
    }

    async UniTask MoveCurrentPathsAsync(CancellationToken token = default)
    {
        _pathMoveProps.speedTween.StartTween(_pathMoveProps.speedTween.CurrentValue,
            1f,
            1.5f,
            DG.Tweening.Ease.InQuad);

        //_stat.SetCurrentMoveSpeed(_stat.TableMoveSpeed);
        _stat.SetCurrentRotationSpeed(_stat.TableRotationSpeed);

        Owner.MovementBeginListener?.Invoke(Owner, true, null, _pathMoveProps.path.Instance);

        if (_pathMoveProps == null)
        {
            TEMP_Logger.Err($"Prop Null?");
        }
        else if (_pathMoveProps.path == null)
        {
            TEMP_Logger.Err("Path Null?");
        }

        if (_pathMoveProps.path.Count == 0)
            return;

        // TODO : 킹샷처럼 initial rotation 처리는 처음에 하는게 좋겠지
        // 이거를 별도 상태머신으로 빼는게 좋을까? 그러겠쟈?
        // 더 나아가서 추상화 설계를 해보자 . 이동에 관한 .
        _pathMoveProps.currentPathIndex = 0;

        bool rotateEnabled = true;
        var myPos = Owner.ApproxPosition.FlatHeight();

        if (_pathMoveProps.path.Instance.Count > 1)
        {
            // 시작위치 넘 가까우면 걍 뻄 
            if (Vector3.SqrMagnitude(_pathMoveProps.path.Instance[0] - myPos) <= 1f /* 1임 어차피 * 1f*/)
            {
                _pathMoveProps.currentPathIndex++;
            }
        }

        var dirToDest = _pathMoveProps.path.Instance[_pathMoveProps.currentPathIndex] - myPos;
        dirToDest.y = 0;
        dirToDest.Normalize();

        while (_pathMoveProps.currentPathIndex < _pathMoveProps.path.Count)
        {
            // var destWorldPos = MapUtils.TilePosToWorldPos(result[curDestIdx].Position);
            var curDest = _pathMoveProps.path.Instance[_pathMoveProps.currentPathIndex];

            // 1. 지금 방식은 매번 이동 시작전 회전이 끝난 후에 이동을 시작하기때문에
            // 뚝뚝 끊김. 그렇다고 forward 로 이동을 시키게되면 지금 이동 로직의 종료
            // 조건으로서는 회전이 덜된 상태에서 dest에 도착하게되면 dest 에 충분히
            // 가깝게 가지못해 종료되지 않고 뻉뻉이 도는 현상이 발현됨
            // 그래서, 추후 이 뚝 끊기는 느낌과 뺑뻉이를 동시에 해결하려면
            // 이동 경로를 한번 베지어나 spline 방식으로 곡선 포인트를 찍어줘야할거같음. 그러면 이때는
            // 시작할때만 rotation 하고 , 중간에 이동해서 멈추는 포인트에서는 그냥 forward 로만가도되지않을까?
            // 2. 중간 이동중 회전은 조금더 속도를 빠르게 해줄까?
            // 3. 아니면 이동 종료 조건을 개선할까. 추후작업할것 

            if (rotateEnabled)
            {
                float rotationAmount = _stat.CurrentRotationSpeed * Time.deltaTime;
                bool isIdentical = RotateToward(rotationAmount, dirToDest);
                if (isIdentical)
                {
                    rotateEnabled = false;
                }
            }

            bool translate = rotateEnabled == false;

            if (translate)
            {
                Vector3 movement = Mover.forward * _stat.CurrentMoveSpeed * Time.deltaTime;
                var postDirToDest = (curDest - Owner.ApproxPosition + movement).normalized;
                bool isArrived = Vector3.Dot(dirToDest, postDirToDest) < 0;

                if (isArrived == false)
                {
                    Move(movement);
                }
                else
                {
                    SetPos(curDest);

                    _pathMoveProps.currentPathIndex++;
                    if (_pathMoveProps.currentPathIndex < _pathMoveProps.path.Count)
                    {
                        curDest = _pathMoveProps.path.Instance[_pathMoveProps.currentPathIndex] - Owner.ApproxPosition;

                        dirToDest = curDest;
                        dirToDest.y = 0;
                        dirToDest.Normalize();
                    }

                    rotateEnabled = true;
                }
            }

            float moveSpeed = _stat.TableMoveSpeed * _pathMoveProps.speedTween.CurrentValue;

            _stat.SetCurrentMoveSpeed(moveSpeed);

            Owner.MovementProcessingListener?.Invoke(Owner, Owner.ApproxPosition);

            await UniTask.Yield();

            if (token.IsCancellationRequested)
                return;
        }

        if (token.IsCancellationRequested)
            return;

        _stat.SetCurrentMoveSpeed(0);
        _stat.SetCurrentRotationSpeed(0);

        StopPathMovement(true);
    }

    void TryRegisterRemainedPathToCache()
    {
        // 가야할 path 가 있을때만 캐시
        if (_pathMoveProps == null || _pathMoveProps.currentPathIndex >= _pathMoveProps.path.Count)
            return;

        var path = _pathMoveProps.path.Instance;

        // 여기서는 할당 새로하는거 의도임
        var remainedPath = path.GetRange(_pathMoveProps.currentPathIndex, path.Count - _pathMoveProps.currentPathIndex);

        InGameManager.Instance.CacheContainer.PathBufferCache.RegisterManually(
            MapUtils.WorldPosToTilePos(Owner.ApproxPosition.FlatHeight()),
            MapUtils.WorldPosToTilePos(path[path.Count - 1]),
            Owner.TableData.EntityFlags,
            remainedPath);
    }

    //IEnumerator MoveCurrentPathsCo()
    //{
    //    _pathMoveProps.speedTween.StartTween(_pathMoveProps.speedTween.CurrentValue,
    //        1f,
    //        1.5f,
    //        DG.Tweening.Ease.InQuad);

    //    //_stat.SetCurrentMoveSpeed(_stat.TableMoveSpeed);
    //    _stat.SetCurrentRotationSpeed(_stat.TableRotationSpeed);

    //    _owner.MovementBeginListener?.Invoke(_owner, true, null, _pathMoveProps.paths);

    //    if (_pathMoveProps == null)
    //    {
    //        TEMP_Logger.Err($"Prop Null?");
    //    }
    //    else if (_pathMoveProps.paths == null)
    //    {
    //        TEMP_Logger.Err("Path Null?");
    //    }

    //    if (_pathMoveProps.paths.Count == 0)
    //        yield break;

    //    // TODO : 킹샷처럼 initial rotation 처리는 처음에 하는게 좋겠지
    //    // 이거를 별도 상태머신으로 빼는게 좋을까? 그러겠쟈?
    //    // 더 나아가서 추상화 설계를 해보자 . 이동에 관한 .
    //    int curDestIdx = 0;
    //    bool rotateEnabled = true;

    //    if (_pathMoveProps.paths.Count > 1)
    //    {
    //        // 시작위치 넘 가까우면 걍 뻄 
    //        if (Vector3.SqrMagnitude(_pathMoveProps.paths[0] - Mover.position) <= 1f * 1f)
    //        {
    //            // _pathMoveProps.paths.RemoveAt(0);
    //            curDestIdx++;
    //        }
    //        // _pathMoveProps.paths.RemoveAt(0);
    //    }

    //    var dirToDest = _pathMoveProps.paths[curDestIdx] - Mover.position;
    //    dirToDest.y = 0;
    //    dirToDest.Normalize();

    //    while (curDestIdx < _pathMoveProps.paths.Count)
    //    {
    //        // var destWorldPos = MapUtils.TilePosToWorldPos(result[curDestIdx].Position);
    //        var curDest = _pathMoveProps.paths[curDestIdx];

    //        // 1. 지금 방식은 매번 이동 시작전 회전이 끝난 후에 이동을 시작하기때문에
    //        // 뚝뚝 끊김. 그렇다고 forward 로 이동을 시키게되면 지금 이동 로직의 종료
    //        // 조건으로서는 회전이 덜된 상태에서 dest에 도착하게되면 dest 에 충분히
    //        // 가깝게 가지못해 종료되지 않고 뻉뻉이 도는 현상이 발현됨
    //        // 그래서, 추후 이 뚝 끊기는 느낌과 뺑뻉이를 동시에 해결하려면
    //        // 이동 경로를 한번 베지어나 spline 방식으로 곡선 포인트를 찍어줘야할거같음. 그러면 이때는
    //        // 시작할때만 rotation 하고 , 중간에 이동해서 멈추는 포인트에서는 그냥 forward 로만가도되지않을까?
    //        // 2. 중간 이동중 회전은 조금더 속도를 빠르게 해줄까?
    //        // 3. 아니면 이동 종료 조건을 개선할까. 추후작업할것 

    //        if (rotateEnabled)
    //        {
    //            float rotationAmount = _stat.CurrentRotationSpeed * Time.deltaTime;
    //            bool isIdentical = RotateToward(rotationAmount, dirToDest);
    //            if (isIdentical)
    //            {
    //                rotateEnabled = false;
    //            }
    //        }

    //        bool translate = rotateEnabled == false;

    //        if (translate)
    //        {
    //            Vector3 movement = Mover.forward * _stat.CurrentMoveSpeed * Time.deltaTime;
    //            var postDirToDest = (curDest - Mover.position + movement).normalized;
    //            bool isArrived = Vector3.Dot(dirToDest, postDirToDest) < 0;

    //            if (isArrived == false)
    //            {
    //                Move(movement);
    //            }
    //            else
    //            {
    //                SetPos(curDest);

    //                curDestIdx++;
    //                if (curDestIdx < _pathMoveProps.paths.Count)
    //                {
    //                    curDest = _pathMoveProps.paths[curDestIdx] - Mover.position;
    //                    dirToDest = curDest;
    //                    dirToDest.y = 0;
    //                    dirToDest.Normalize();
    //                }

    //                rotateEnabled = true;
    //            }
    //        }

    //        float moveSpeed = _stat.TableMoveSpeed * _pathMoveProps.speedTween.CurrentValue;

    //        _stat.SetCurrentMoveSpeed(moveSpeed);

    //        _owner.MovementProcessingListener?.Invoke(_owner, Mover.position);

    //        yield return null;
    //    }

    //    _stat.SetCurrentMoveSpeed(0);
    //    _stat.SetCurrentRotationSpeed(0);

    //    StopPathMovement(true);
    //}

    public override void ReturnToPool()
    {
        InGameManager.Instance.CacheContainer.EntityPartsPool.Return(this);
    }
}
