// Assets/_Project/Features/UI/Common/AnimationSequencer.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TypingSurvivor.Features.UI.Common;

public class AnimationSequencer : MonoBehaviour
{
    [System.Serializable]
    public class SequenceStep
    {
        [Tooltip("Inspectorでの識別名。SetStepEnabledで操作するために使います")]
        public string stepName;
        [Tooltip("アニメーション対象のパネル")]
        public ScreenBase panel;
        [Tooltip("表示前の待機時間")]
        public float delayBeforeShow = 0f;
        [Tooltip("表示させておく時間")]
        public float holdDuration = 2.0f;
        [Tooltip("表示後に非表示にするか")]
        public bool hideAfterHold = true;
        [Tooltip("trueの場合、holdDurationを無視してResume()が呼ばれるまで待機します")]
        public bool waitForManualResume = false;

        [HideInInspector] public bool isEnabled = true;
    }

    [Header("Animation Settings")]
    [SerializeField]
    private List<SequenceStep> _sequence;

    [Header("Options")]
    [SerializeField]
    private InteractiveButton _skipButton;

    private Dictionary<string, SequenceStep> _stepMap;
    private bool _isSkipped = false;
    private bool _isWaitingForResume = false;
    private Coroutine _sequenceCoroutine;

    private void Awake()
    {
        _skipButton?.onClick.AddListener(Skip);

        _stepMap = new Dictionary<string, SequenceStep>();
        foreach (var step in _sequence)
        {
            if (!string.IsNullOrEmpty(step.stepName) && !_stepMap.ContainsKey(step.stepName))
            {
                _stepMap[step.stepName] = step;
            }
        }
    }

    public void SetStepEnabled(string stepName, bool isEnabled)
    {
        // ステップが存在すれば有効/無効をセットする。存在しない場合は何もしない。
        if (_stepMap.TryGetValue(stepName, out var step))
        {
            step.isEnabled = isEnabled;
        }
    }

    public void Play()
    {
        if (_sequenceCoroutine != null) StopCoroutine(_sequenceCoroutine);
        _sequenceCoroutine = StartCoroutine(ExecuteSequence());
    }

    public void Resume()
    {
        _isWaitingForResume = false;
    }

    private void Skip() => _isSkipped = true;

    private IEnumerator ExecuteSequence()
    {
        _isSkipped = false;
        if (_skipButton) _skipButton.gameObject.SetActive(true);

        // 開始前に全パネルを初期状態（非表示）にリセット
        foreach (var step in _sequence)
        {
            if (step.panel != null)
            {
                // Hide()は内部でCanvasGroupを操作するので、GameObjectがアクティブである必要がある
                step.panel.gameObject.SetActive(true);
                step.panel.Hide();
            }
        }
        // ScreenBaseのFadeは0.2sなので、それより少し長く待つ
        yield return new WaitForSeconds(0.3f);

        // シーケンス実行
        foreach (var step in _sequence)
        {
            if (!step.isEnabled || step.panel == null || !step.panel.gameObject.activeInHierarchy) continue;

            if (step.delayBeforeShow > 0) yield return new WaitForSeconds(step.delayBeforeShow);

            step.panel.Show();
            // ScreenBaseのFadeDurationを取得できるようにする必要があるが、一旦固定値で待つ
            yield return new WaitForSeconds(0.2f);

            // 待機処理
            if (step.waitForManualResume)
            {
                _isWaitingForResume = true;
                while (_isWaitingForResume)
                {
                    yield return null;
                }
            }
            else
            {
                float timer = 0f;
                while (timer < step.holdDuration && !_isSkipped)
                {
                    timer += Time.deltaTime;
                    yield return null;
                }
                _isSkipped = false;
            }

            if (step.hideAfterHold)
            {
                step.panel.Hide();
                yield return new WaitForSeconds(0.2f);
            }
        }

        if (_skipButton) _skipButton.gameObject.SetActive(false);
    }
}
