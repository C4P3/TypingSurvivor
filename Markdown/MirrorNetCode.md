| Mirror API | Netcode for GameObjects API | 意味 |
| ------------- |:-------------:| -----:|
isLocalPlayer | IsOwner | このオブジェクトを操作する権限があるクライアントか？ |
| OnStartAuthority() | OnNetworkSpawn() 内で if (IsOwner) | 操作権限を持ってネットワークに出現した時の処理 |
| [Command] | [ServerRpc] | クライアントからサーバーへ処理を要求する |
| [ClientRpc] | [ClientRpc] | サーバーから全クライアントへ処理を命令する |
| [SyncVar] | NetworkVariable<T> | サーバーからクライアントへ自動的に同期される変数 |