using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.Core
{
    public class TileClearEffect : MonoBehaviour
    {
        [SerializeField] private int _fragmentCount = 8;
        [SerializeField] private float _fragmentSize = 18f;
        [SerializeField] private float _burstRadius = 60f;
        [SerializeField] private float _burstDuration = 0.45f;
        [SerializeField] private float _flashScale = 1.8f;
        [SerializeField] private float _flashDuration = 0.25f;

        private Image _flashImage;
        private Image[] _fragments;
        private bool _built;

        private RectTransform _rectTransform;

        // ── 构建 UI 子物体（只建一次）────────────────────────────────────
        private void EnsureBuilt()
        {
            if (_built) return;
            _built = true;

            // 确保自身有 RectTransform
            _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform == null) _rectTransform = gameObject.AddComponent<RectTransform>();

            // 1. 闪光圆
            var flashGO = new GameObject("Flash");
            flashGO.transform.SetParent(transform, false);
            _flashImage = flashGO.AddComponent<Image>();
            _flashImage.sprite = GetCircleSpriteStatic();
            _flashImage.raycastTarget = false;

            // 2. 碎片
            _fragments = new Image[_fragmentCount];
            for (int i = 0; i < _fragmentCount; i++)
            {
                var go = new GameObject($"Frag_{i}");
                go.transform.SetParent(transform, false);
                var img = go.AddComponent<Image>();
                img.sprite = GetSquareSprite();
                img.raycastTarget = false;

                var rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(_fragmentSize, _fragmentSize);
                _fragments[i] = img;
            }
        }

        // ── 对外接口 ─────────────────────────────────────────────────
        public void Play(Vector3 worldPos, Color color, System.Action onComplete)
        {
            EnsureBuilt();

            // 1. 强力清理所有旧动画，杜绝残留
            DOTween.Kill(transform);
            _flashImage.transform.DOKill(true);
            _flashImage.DOKill(true);
            foreach (var f in _fragments)
            {
                f.transform.DOKill(true);
                f.DOKill(true);
            }

            // 2. 🔥【核心修正】彻底洗白自身的 UI 空间属性，防止二次复用时变形、缩水或飞走
            _rectTransform.position = worldPos; // 设定世界坐标
            _rectTransform.localScale = Vector3.one; // 👈 必须确保父节点缩放是 1
            // 强行把局部 Z 轴归零，防止它飞到 UI 摄像机的剪裁平面后面
            Vector3 localPos = _rectTransform.localPosition;
            localPos.z = 0f;
            _rectTransform.localPosition = localPos;

            // 3. 强力洗白【闪光】的状态
            _flashImage.gameObject.SetActive(true);
            var rtFlash = _flashImage.GetComponent<RectTransform>();
            rtFlash.localScale = Vector3.one * 0.3f;
            rtFlash.localPosition = Vector3.zero; // 居中
            rtFlash.sizeDelta = new Vector2(_burstRadius * 2, _burstRadius * 2);
            _flashImage.color = new Color(color.r, color.g, color.b, 0.85f);

            // 4. 强力洗白【碎片】的状态
            for (int i = 0; i < _fragmentCount; i++)
            {
                var img = _fragments[i];
                var rt = img.GetComponent<RectTransform>();

                img.gameObject.SetActive(true);
                rt.localPosition = Vector3.zero;         // 回归起点
                rt.localScale = Vector3.one;             // 尺寸回弹
                rt.localRotation = Quaternion.identity;  // 角度回正
                rt.sizeDelta = new Vector2(_fragmentSize, _fragmentSize); // 确保长宽不为0
                img.color = new Color(color.r, color.g, color.b, 1f);
            }

            StartCoroutine(PlayRoutine(color, onComplete));
        }

        private IEnumerator PlayRoutine(Color color, System.Action onComplete)
        {
            PlayFlash(color);
            PlayFragments(color);

            float total = Mathf.Max(_flashDuration, _burstDuration) + 0.05f;
            yield return new WaitForSeconds(total);

            // 🔥【新增优化】在彻底退场进池子前，把自身和子物体的所有 Tween 彻底抹杀
            // 这样 DOTween 后台就不会在下一帧找不到目标而报 Safe Mode 警告了
            DOTween.Kill(transform);
            if (_flashImage != null) _flashImage.transform.DOKill();
            if (_fragments != null)
            {
                foreach (var f in _fragments)
                {
                    if (f != null) f.transform.DOKill();
                }
            }

            onComplete?.Invoke();
        }

        private void PlayFlash(Color color)
        {
            Color c1 = new Color(color.r, color.g, color.b, 0f);
            var seq = DOTween.Sequence();
            seq.Join(_flashImage.transform.DOScale(_flashScale, _flashDuration).SetEase(Ease.OutQuad));
            seq.Join(_flashImage.DOColor(c1, _flashDuration).SetEase(Ease.InQuad));
            seq.OnComplete(() => _flashImage.gameObject.SetActive(false));
        }

        private void PlayFragments(Color color)
        {
            float angleStep = 360f / _fragmentCount;

            for (int i = 0; i < _fragmentCount; i++)
            {
                var img = _fragments[i];
                var rt = img.GetComponent<RectTransform>();

                float angle = angleStep * i + Random.Range(-10f, 10f);
                float rad = angle * Mathf.Deg2Rad;

                Vector3 localDir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
                float dist = _burstRadius * Random.Range(0.7f, 1.3f);
                Vector3 localDest = localDir * dist;

                float spin = Random.Range(180f, 360f) * (Random.value > 0.5f ? 1 : -1);
                float delay = Random.Range(0f, 0.04f);
                Color fade = new Color(color.r, color.g, color.b, 0f);

                var seq = DOTween.Sequence();
                seq.AppendInterval(delay);
                seq.Join(rt.DOLocalMove(localDest, _burstDuration).SetEase(Ease.OutCubic));
                seq.Join(rt.DORotate(new Vector3(0, 0, spin), _burstDuration, RotateMode.FastBeyond360));
                seq.Join(rt.DOScale(0f, _burstDuration * 0.8f).SetDelay(_burstDuration * 0.2f).SetEase(Ease.InQuad));
                seq.Join(img.DOColor(fade, _burstDuration).SetEase(Ease.InQuad));
                seq.OnComplete(() => img.gameObject.SetActive(false));
            }
        }

        // ── Texture & Sprite 生成保持不变 ─────────────────────────────────
        private static Sprite _circle;
        private static Sprite _square;

        // 把原来的 private static 改成 public static
        public static Sprite GetCircleSpriteStatic()
        {
            if (_circle != null) return _circle;
            int size = 64; float c = size / 2f, rad = c - 1f;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                    tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(1f - (d - (rad - 2f)) / 2f)));
                }
            tex.Apply();
            _circle = Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, 100f);
            return _circle;
        }

        private static Sprite GetSquareSprite()
        {
            if (_square != null) return _square;
            var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                    tex.SetPixel(x, y, Color.white);
            tex.Apply();
            _square = Sprite.Create(tex, new Rect(0, 0, 16, 16), Vector2.one * 0.5f, 100f);
            return _square;
        }
    }
}