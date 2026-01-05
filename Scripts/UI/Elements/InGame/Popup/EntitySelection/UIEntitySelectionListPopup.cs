using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[UIAttribute("UIEntitySelectionListPopup")]
public class UIEntitySelectionListPopup : PopupBase
{
    public enum Result
    {
        Confirm,
        Cancel
    }

    public class Arg : PopupShowArgBase
    {
        public UIEntitySelectionTask task;
        public Predicate<uint> selectionPredicate;

        public Arg(UIEntitySelectionTask task, Predicate<uint> predicate, Action<PopupResultBase> resultReceiver) : base(resultReceiver)
        {
            this.task = task;
            selectionPredicate = predicate;
        }
    }

    public class ResultArg : PopupResultBase
    {
        public Result result;

        public uint selectedEntityTid;

        public ResultArg(Result result, uint selectedEntityTid)
        {
            this.result = result;
            this.selectedEntityTid = selectedEntityTid;
        }
    }

    [SerializeField]
    private UIEntitySelectionElement _elementSrc;

    [SerializeField]
    private GridLayoutGroup _grid;

    List<UIEntitySelectionElement> _elements;

    private void Awake()
    {
        _elementSrc.gameObject.SetActive(false);
    }

    public override void OnSpawned(ObjectPoolCategory category, string key)
    {
        base.OnSpawned(category, key);
        _elements = new List<UIEntitySelectionElement>();
    }

    public override void OnShow(UITrigger trigger, UIArgBase arg)
    {
        base.OnShow(trigger, arg);
        var popupArg = arg as Arg;

        int showIdx = 0;
        foreach (var tableData in GameDBManager.Instance.Container.EntityTable_data)
        {
            bool show = popupArg.selectionPredicate(tableData.Key);
            if (show)
            {
                // 외부에서 얘를 '보여줘라' 라고 하는 기준은
                // 절대적 기준이라기 보다 '건물 타입이면' .. '캐릭터면 ...' 이런식이
                // 일단 대부분일거라 , 우선순위를 먼저 데이터가 있는지로 체크.
                // 데이터가 없다면 그냥 noShow 로 처리함. (에러 처리 X)
                var costData = DBPurchaseCost.GetByEntityID(tableData.Key);
                if (costData == null)
                    continue;

                UIEntitySelectionElement element;

                if (_elements.Count > showIdx)
                {
                    element = _elements[showIdx];
                }
                else
                {
                    element = GameObject.Instantiate(_elementSrc, _grid.transform);
                    _elements.Add(element);
                }

                element.ShowPurchaseItem(tableData.Key, DBEntity.GetIconKey(tableData.Key), costData.CostCurrencyType, (int)costData.CostPrice, showIdx, OnClickElement);
                element.gameObject.SetActive(true);

                showIdx++;
            }
        }

        if (_elements.Count >= showIdx)
        {
            int disableCnt = 0;

            for (int i = 0; i < disableCnt; i++)
            {
                _elements[i].Release();
                _elements[i].gameObject.SetActive(false);
            }
        }
    }

    public override void OnHide(UIArgBase arg)
    {
        foreach (var element in _elements)
        {
            element.Release();
            element.gameObject.SetActive(false);
        }

        base.OnHide(arg);
    }

    public void OnClickXButton()
    {
        SendResult(new ResultArg(Result.Cancel, 0));
        Hide();
    }

    void OnClickElement(UIEntitySelectionElement element)
    {
        SendResult(new ResultArg(Result.Confirm, element.EntityTID));
        Hide();
    }
}
