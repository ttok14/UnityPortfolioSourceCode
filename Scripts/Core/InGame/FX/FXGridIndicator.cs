using UnityEngine;

public class FXGridIndicator : FXBase
{
    [SerializeField]
    private Transform _root;

    [SerializeField]
    private SpriteRenderer _gridSpriteRenderer;

    [SerializeField]
    private SpriteRenderer _rangeSpriteRenderer;

    Transform _followTarget;

    float _originalXScale;

    public override bool ActivateLateUpdate => true;

    public override void OnSpawned(ObjectPoolCategory category, string key)
    {
        base.OnSpawned(category, key);
        _originalXScale = _gridSpriteRenderer.transform.localScale.x;
    }

    public override void OnActivated(ulong id)
    {
        base.OnActivated(id);

        _rangeSpriteRenderer.enabled = false;
    }

    public override void OnInactivated()
    {
        base.OnInactivated();

        _followTarget = null;
    }

    protected override void OnUpdated()
    {
        // 수동 active 를 조작하는 일이있어서 현 컴포넌트는
        // 물론 이게 베스트는 아님 . 
        if (gameObject.activeInHierarchy == false)
            return;

        base.OnUpdated();

        if (_followTarget == null)
        {
            gameObject.SetActive(false);
            return;
        }

        var spriteTs = _gridSpriteRenderer.transform;
        if (spriteTs.localScale.x < _originalXScale)
        {
            var curScale = spriteTs.localScale;
            float newScale = curScale.x + 20f * Time.deltaTime;
            if (newScale >= _originalXScale)
                newScale = _originalXScale;
            spriteTs.localScale = new Vector3(newScale, curScale.y, curScale.z);
        }

        var newPos = MapUtils.WorldPosToTileWorldPos(_followTarget.position);
        newPos.y = 0f;
        transform.position = newPos;
    }

    public void SetTarget(Transform followTarget)
    {
        _followTarget = followTarget;

        var scale = _gridSpriteRenderer.transform.localScale;
        scale.x = 0f;
        _gridSpriteRenderer.transform.localScale = scale;
    }

    public void SetGridColor(Color color)
    {
        _gridSpriteRenderer.color = color;
    }

    public void ShowRange(Color rangeColor, float radius)
    {
        _rangeSpriteRenderer.color = rangeColor;
        _rangeSpriteRenderer.transform.localScale = new Vector3(radius, radius, radius);
        _rangeSpriteRenderer.enabled = true;
    }

    public void HideRange()
    {
        _rangeSpriteRenderer.enabled = false;
    }
}
