using System;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    public event Action OnInteractIntent;

    void Update()
    {
        // このコンポーネントが有効な時だけUpdateが呼ばれるので、
        // 自分がローカルプレイヤーかどうかを気にする必要がない。
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            OnInteractIntent?.Invoke();
        }
    }
}