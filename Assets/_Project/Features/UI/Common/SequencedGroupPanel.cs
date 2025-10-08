// Assets/_Project/Features/UI/Common/SequencedGroupPanel.cs
using UnityEngine;
using System.Collections;
using TypingSurvivor.Features.UI.Common;

[RequireComponent(typeof(AnimationSequencer))]
public class SequencedGroupPanel : ScreenBase
{
    private AnimationSequencer _childSequencer;

    protected override void Awake()
    {
        base.Awake();
        _childSequencer = GetComponent<AnimationSequencer>();
    }

    public override void Show()
    {
        // 1. まず自分自身（親パネル）を表示する
        base.Show();
        // 2. 自分の表示が完了したら、自分の中のシーケンサーを再生する
        // base.Show()のFade完了を待ってから実行
        StartCoroutine(PlayChildSequenceAfterDelay(_fadeDuration));
    }

    private IEnumerator PlayChildSequenceAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (this.gameObject.activeInHierarchy) // 親が非表示にされた場合などに備える
        {
            _childSequencer.Play();
        }
    }

    // このパネルがHideされるときは、子シーケンスも停止する必要があるかもしれないが、
    // 親のCanvasGroup.alpha=0になれば子は見えなくなるので、一旦シンプルな実装に留める
}
