using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathLineRenderController : PoolableObjectBase
{
    [SerializeField]
    private LineRenderer _lineRenderer;

    [SerializeField]
    private float _scrollSpeed = 1.1f;

    static readonly int ID_BaseMapST = Shader.PropertyToID("_BaseMap_ST");
    static readonly int ID_BaseColor = Shader.PropertyToID("_BaseColor");

    private MaterialPropertyBlock _propBlock;
    private Vector4 _currentST;

    public override void OnSpawned(ObjectPoolCategory category, string key)
    {
        base.OnSpawned(category, key);

        transform.localPosition = new Vector3(0, 0.15f, 0);
        if (_propBlock == null)
        {
            _propBlock = new MaterialPropertyBlock();
        }

        _propBlock.Clear();

        if (_lineRenderer.sharedMaterial != null)
        {
            _currentST = _lineRenderer.sharedMaterial.GetVector(ID_BaseMapST);
            _currentST.x = 0.5f;
        }
        else
        {
            _currentST = new Vector4(1, 1, 0, 0);
        }

        _propBlock.SetColor(ID_BaseColor, _lineRenderer.startColor);
    }

    public void ChangeSettings(Vector3[] positions)
    {
        _lineRenderer.positionCount = positions.Length;
        _lineRenderer.SetPositions(positions);
    }

    private void Update()
    {
        _currentST.z += _scrollSpeed * -1 * Time.deltaTime;
        // _currentST.z %= 1.0f;

        _propBlock.SetVector(ID_BaseMapST, _currentST);

        _lineRenderer.SetPropertyBlock(_propBlock);
    }
}
