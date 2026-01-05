using UnityEngine;

public class PeaceBattleTimer
{
    public float StartBattleTimeAt { get; private set; }
    public float WarningAt { get; private set; }

    bool _showedWarning;

    bool _isEnabled;

    public void SetTimer(float startInSeconds, float warningInSeconds)
    {
        // 경고를 시작전에 띄어야 하는데,
        // 경고 보여줄 시간이 시작시간 보다 뒤인건 데이터 에러
        if (warningInSeconds >= startInSeconds)
        {
            TEMP_Logger.Err($"Battle Warning Time Seconds Data Error");
            warningInSeconds = startInSeconds - 1;
        }

        StartBattleTimeAt = Time.time + startInSeconds;
        WarningAt = Time.time + warningInSeconds;

        _isEnabled = true;

        InGameManager.Instance.PublishEvent(InGameEvent.StartRunBattleTimer, new StartRunBattleTimer()
        {
            StartBattleTimeAt = StartBattleTimeAt
        });
    }

    public void Update()
    {
        if (_isEnabled == false)
            return;

        if (_showedWarning == false && Time.time >= WarningAt)
        {
            _showedWarning = true;

            LightManager.Instance.ToNightTime();
            AudioManager.Instance.Play("SFX_BattleWarning");
            UIToastSystem.ShowToast(UIToastSystem.ToastType.Center_StaticWarning, "해가 저물고 사방에 위험이 도사립니다.");
        }

        if (Time.time >= StartBattleTimeAt)
        {
            _isEnabled = false;
            _showedWarning = false;

            var arg = new EnterBattlePhaseEventArg(
                InGameManager.Instance.CurrentPhaseType,
                InGameBattleMode.Defense);

            InGameManager.Instance.RequestChangePhase(InGamePhase.Battle, arg);
        }
    }
}
