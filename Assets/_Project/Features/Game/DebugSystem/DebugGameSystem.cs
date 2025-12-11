using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem; // Input System用
using TypingSurvivor.Features.Game.Gameplay;
using TypingSurvivor.Features.Game.Level.Data;
using TypingSurvivor.Features.Game.Camera; // CameraManagerの名前空間
using System.Linq;

namespace TypingSurvivor.Features.DebugSystem
{
    public class DebugGameSystem : NetworkBehaviour
    {
        [SerializeField] private bool _showDebugMenu = true;

        private GameManager _gameManager;
        private LevelManager _levelManager;
        private Grid _grid;
        private ItemRegistry _itemRegistry;
        private CameraManager _cameraManager; // 追加: カメラマネージャーへの参照

        // 状態
        private bool _isSpawnMode = false;
        private int _selectedItemIndex = 0;
        private float _currentOxygenMultiplier = 1.0f;

        // 初期化メソッドの引数に CameraManager を追加
        public void Initialize(
            GameManager gameManager, 
            LevelManager levelManager, 
            Grid grid, 
            ItemRegistry itemRegistry,
            CameraManager cameraManager) 
        {
            _gameManager = gameManager;
            _levelManager = levelManager;
            _grid = grid;
            _itemRegistry = itemRegistry;
            _cameraManager = cameraManager;
        }

        private void Update()
        {
            // F3キーの判定
            if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame)
            {
                _showDebugMenu = !_showDebugMenu;
            }

            // マウス左クリックの判定
            if (_showDebugMenu && _isSpawnMode && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                HandleMapClick();
            }
        }

        private void HandleMapClick()
        {
            // マウス位置の取得
            Vector2 mousePos2D = Mouse.current.position.ReadValue();
            
            // 適切なカメラを探す
            UnityEngine.Camera targetCam = null;

            // CameraManager があれば、マウス位置が含まれるビューポートを持つカメラを探す（画面分割対応）
            if (_cameraManager != null)
            {
                foreach (var cam in _cameraManager.GetAssignedCameras().Values)
                {
                    if (cam.pixelRect.Contains(mousePos2D))
                    {
                        targetCam = cam;
                        break;
                    }
                }
            }

            // 見つからなければ Camera.main を試す（フォールバック）
            if (targetCam == null) targetCam = UnityEngine.Camera.main;

            // それでもカメラがなければ処理を中断（これでNullReferenceExceptionを防ぐ）
            if (targetCam == null) 
            {
                UnityEngine.Debug.LogWarning("[DebugGameSystem] No active camera found to raycast from.");
                return;
            }

            // ワールド座標に変換
            Vector3 mousePosData = new Vector3(mousePos2D.x, mousePos2D.y, 0f);
            Vector3 mouseWorldPos = targetCam.ScreenToWorldPoint(mousePosData);
            Vector3Int gridPos = _grid.WorldToCell(mouseWorldPos);
            gridPos.z = 0;

            SpawnDebugItemServerRpc(gridPos, _selectedItemIndex);
        }

        [ServerRpc(RequireOwnership = false)]
        private void SpawnDebugItemServerRpc(Vector3Int gridPos, int itemIndex)
        {
            if (_itemRegistry == null || _itemRegistry.AllItems.Count == 0) return;
            if (itemIndex < 0 || itemIndex >= _itemRegistry.AllItems.Count) return;

            var itemData = _itemRegistry.AllItems[itemIndex];
            _levelManager.PlaceItem(gridPos, itemData.itemTile);
            
            UnityEngine.Debug.Log($"[Debug] Spawned {itemData.itemName} at {gridPos}");
        }

        [ServerRpc(RequireOwnership = false)]
        private void SetOxygenMultiplierServerRpc(float multiplier)
        {
            _gameManager.SetOxygenDepletionMultiplier(multiplier);
        }

        private void OnGUI()
        {
            if (!IsSpawned) return; // 接続前は表示しない
            if (!_showDebugMenu) return;

            GUILayout.BeginArea(new Rect(10, 10, 250, 400), "Debug Menu", GUI.skin.window);

            GUILayout.Label($"Oxygen Drain: x{_currentOxygenMultiplier:F1}");
            float newMultiplier = GUILayout.HorizontalSlider(_currentOxygenMultiplier, 0.0f, 5.0f);
            if (Mathf.Abs(newMultiplier - _currentOxygenMultiplier) > 0.01f)
            {
                _currentOxygenMultiplier = newMultiplier;
                SetOxygenMultiplierServerRpc(_currentOxygenMultiplier);
            }

            GUILayout.Space(10);
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
            GUILayout.Space(10);

            _isSpawnMode = GUILayout.Toggle(_isSpawnMode, "Enable Item Click Spawn");

            if (_isSpawnMode && _itemRegistry != null)
            {
                GUILayout.Label("Select Item to Spawn:");
                var itemNames = _itemRegistry.AllItems.Select(x => x.itemName).ToArray();
                _selectedItemIndex = GUILayout.SelectionGrid(_selectedItemIndex, itemNames, 1);
            }

            GUILayout.EndArea();
        }
    }
}