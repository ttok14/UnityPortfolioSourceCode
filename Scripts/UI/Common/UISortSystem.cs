using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class UISortSystem
{
    Dictionary<UILayer, UILayerGroupEntry> _layerEntryGroup;
    Dictionary<UILayer, List<UIBase>> _registeredUIs = new Dictionary<UILayer, List<UIBase>>();

    public void Initialize(Dictionary<UILayer, UILayerGroupEntry> layerEntryGroup)
    {
        _layerEntryGroup = layerEntryGroup;

        foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
        {
            _registeredUIs.Add(layer, new List<UIBase>(16));
        }
    }

    public void Register(UIBase ui)
    {
        _registeredUIs[ui.SortPolicy.layer].Add(ui);
        Sort(ui);
    }

    public void Unregister(UIBase ui)
    {
        _registeredUIs[ui.SortPolicy.layer].Remove(ui);
    }

    private void Sort(UIBase ui)
    {
        var entry = _layerEntryGroup[ui.SortPolicy.layer];

        switch (ui.SortPolicy.behaviour)
        {
            case UISortPolicy.Behaviour.Default:
                ui.transform.SetParent(entry.canvas.transform, false);

                // AI 는 Position 은 reset 하지 않기를 제안하는데, 일단 문제 생기면 그때 다시 보자.
                // ui.transform.Reset();
                ui.transform.SetAsLastSibling();
                break;
            case UISortPolicy.Behaviour.Manual:
                ui.transform.SetParent(entry.canvas.transform, false);
                ui.transform.localScale = Vector3.one;
                ui.transform.localRotation = Quaternion.identity;
                break;
            default:
                TEMP_Logger.Err($"No implementation : {ui.SortPolicy.behaviour}");
                break;
        }
    }
}
