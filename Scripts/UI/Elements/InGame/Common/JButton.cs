using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[AddComponentMenu("JayceUI/JButton", 1)]
[RequireComponent(typeof(RectTransform))]
public class JButton : Button
{
    public uint ClickSoundID { get; private set; } = 2;

    public void Initiailize()
    {

    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);

        if (AudioManager.HasInstance && ClickSoundID != 0)
        {
            AudioManager.Instance.Play(ClickSoundID, Vector3.zero, AudioTrigger.UI);
        }
    }
}
