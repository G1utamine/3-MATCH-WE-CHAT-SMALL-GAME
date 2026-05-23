using System.Collections.Generic;
using Match3.Core;
using Match3.Data;
using UnityEngine;

namespace Match3.Systems
{
    /// <summary>
    /// 特效管理器（只负责 Tile 消除特效，粒子等仍由 ItemManager 自行管理）
    /// </summary>
    public class EffectManager : MonoBehaviour
    {
        public static EffectManager Instance { get; private set; }

        [Header("层级设置")]
        [SerializeField] private Transform _bgEffectContainer;

        private TileClearEffect _effectPrefabTemplate;

        private static readonly Color[] TileColors =
        {
            new Color(0.95f, 0.25f, 0.25f), // Red
            new Color(0.25f, 0.55f, 0.95f), // Blue
            new Color(0.25f, 0.85f, 0.35f), // Green
            new Color(0.98f, 0.85f, 0.15f), // Yellow
            new Color(0.65f, 0.25f, 0.95f), // Purple
            new Color(0.98f, 0.55f, 0.10f), // Orange
        };

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            // 创建隐藏模板
            var go = new GameObject("[Template_TileClearEffect]");
            go.transform.SetParent(transform, false);
            _effectPrefabTemplate = go.AddComponent<TileClearEffect>();
            go.SetActive(false);
        }

        public void PlayClearEffect(Vector3 worldPos, TileType tileType)
        {
            Transform parentContainer = _bgEffectContainer != null ? _bgEffectContainer : transform;
            var effect = GameObjectPool<TileClearEffect>.Instance.Get(_effectPrefabTemplate, parentContainer);
            Color color = GetColor(tileType);
            effect.gameObject.SetActive(true);
            effect.Play(worldPos, color, () =>
            {
                GameObjectPool<TileClearEffect>.Instance.Release(_effectPrefabTemplate, effect);
            });
        }

        public void PlayClearEffects(IEnumerable<(Vector3 pos, TileType type)> items)
        {
            foreach (var (pos, type) in items)
                PlayClearEffect(pos, type);
        }

        private static Color GetColor(TileType type)
        {
            int index = (int)type;
            return index >= 0 && index < TileColors.Length ? TileColors[index] : Color.white;
        }
    }
}