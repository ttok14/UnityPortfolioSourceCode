using Cysharp.Threading.Tasks;
using GameDB;
using System.Collections.Generic;
using UnityEngine;

public class AnimalEntity : EntityBase
{
    Rigidbody _rigidbody;

    float _lastFootstepSfxAt;

    FXBase _trailFx;

    protected override EntityMovePartBase CreateMovePart()
    {
        return InGameManager.Instance.CacheContainer.GetPartOrCreateParts<EntityStandardMovePart>(new EntityMovePartInitData(this, this.transform));
    }

    public override async UniTask<(EntityBase, IEnumerable<EntityDataBase>)> Initialize(
        E_EntityType entityType,
        IEnumerable<EntityDataBase> entityDatabase,
        EntityObjectData objectData)
    {
        var res = await base.Initialize(entityType, entityDatabase, objectData);

        MovementProcessingListener += OnMoved;

        var data = DBAnimal.Get(TableData.DetailTableID);

        bool ridable = data.IsRidable;
        var rigidbody = GetComponent<Rigidbody>();

        if (ridable)
        {
            if (rigidbody == null)
            {
                _rigidbody = gameObject.AddComponent<Rigidbody>();
                _rigidbody.isKinematic = false;
                _rigidbody.useGravity = false;
                _rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
                _rigidbody.constraints =
                    RigidbodyConstraints.FreezePositionY |
                    RigidbodyConstraints.FreezeRotationX |
                    RigidbodyConstraints.FreezeRotationY |
                    RigidbodyConstraints.FreezeRotationZ;
            }
        }
        else
        {
            if (rigidbody)
            {
                Object.Destroy(_rigidbody);
            }

            _rigidbody = null;
        }

        return res;
    }

    public override void OnInitializeFinished()
    {
        base.OnInitializeFinished();

        var data = DBAnimal.Get(TableData.DetailTableID);

        if (string.IsNullOrEmpty(data.MoveTrailFXKey) == false)
        {
            var trailSocket = ModelPart.GetSocket(EntityModelSocket.Trail);
            if (trailSocket)
            {
                FXSystem.PlayFXCallBack(data.MoveTrailFXKey,
                    parent: trailSocket,
                    onCompleted: (fx) =>
                    {
                        if (EntityHelper.IsValid(this))
                        {
                            _trailFx = fx;
                            fx.transform.localPosition = Vector3.zero;
                        }
                        else
                        {
                            fx.Return();
                        }
                    }).Forget();
            }
            else
            {
                TEMP_Logger.Err($"Dust Trail Exist but Socket does not exist | Name : {TableData.Name} , {gameObject.name}");
            }
        }
    }

    private void OnMoved(EntityBase executor, Vector3 position)
    {
        if (_lastFootstepSfxAt + 0.3f < Time.time)
        {
            _lastFootstepSfxAt = Time.time;
            AudioManager.Instance.Play("Animal_FootStep01", position, AudioTrigger.Default);
        }
    }

    public override void OnInactivated()
    {
        base.OnInactivated();

        //if (_rigidbody)
        //{
        //    Object.Destroy(_rigidbody);
        //    _rigidbody = null;
        //}

        if (_trailFx)
        {
            _trailFx.transform.parent = null;
            _trailFx.Return();
        }

        MovementProcessingListener -= OnMoved;
    }

    public void SetRigidbodyEnable(bool enable)
    {
        MovePart.SetRigidBody(enable ? _rigidbody : null);
    }

    public void OnRidden()
    {
        if (_rigidbody)
            SetRigidbodyEnable(true);
        else TEMP_Logger.Err($"This is maybe not a ridable");
    }

    public void OnUnridden()
    {
        if (_rigidbody)
            SetRigidbodyEnable(false);
        else TEMP_Logger.Err($"This is maybe not a ridable");
    }
}
