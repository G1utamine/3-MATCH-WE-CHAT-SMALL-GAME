using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Match3.Core;
using Match3.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.Systems
{
    public class ItemManager : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private BoardView _boardView;
        [SerializeField] private RectTransform _effectCanvas;
        [SerializeField] private Canvas _canvas;

        [Header("Bomb")]
        [SerializeField] private Button _bombButton;
        [SerializeField] private TMP_Text _bombNumberText;
        [SerializeField] private GameObject _bombIconPrefab;

        [Header("Lightning")]
        [SerializeField] private Button _lightningButton;
        [SerializeField] private TMP_Text _lightningNumberText;
        [SerializeField] private GameObject _lightningIconPrefab;

        [Header("Potion")]
        [SerializeField] private Button _potionButton;
        [SerializeField] private TMP_Text _potionNumberText;
        [SerializeField] private GameObject _potionIconPrefab;

        [Header("Ad Reward Panel")]
        [SerializeField] private AdRewardPanel adRewardPanel;

        public event System.Action<List<Vector2Int>> OnTilesDestroyed;
        public event System.Action<List<Vector2Int>, TileType> OnTilesModified;

        private Camera UICamera => _canvas.worldCamera;
        private Sprite _squareSprite;

        // Particle pool
        private readonly Queue<(GameObject go, Image img, RectTransform rt)> _particlePool
            = new Queue<(GameObject, Image, RectTransform)>();
        private const int PARTICLE_POOL_SIZE = 80;

        // Line pool and shockwave pool
        private Queue<GameObject> _linePool = new Queue<GameObject>();
        private Queue<GameObject> _shockwavePool = new Queue<GameObject>();
        private const int LINE_POOL_SIZE = 25;
        private const int SHOCKWAVE_POOL_SIZE = 15;

        private void Start()
        {
            if (_boardView == null) _boardView = BoardView.Instance;

            _bombButton.onClick.AddListener(() => TryUseBomb());
            _lightningButton.onClick.AddListener(() => TryUseLightning());
            _potionButton.onClick.AddListener(() => TryUsePotion());

            GenerateSquareSprite();
            WarmParticlePool();
            WarmLinePool();
            WarmShockwavePool();
            RefreshItemUI();
        }

        private void GenerateSquareSprite()
        {
            var tex = new Texture2D(8, 8);
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                    tex.SetPixel(x, y, Color.white);
            tex.Apply();
            _squareSprite = Sprite.Create(tex, new Rect(0, 0, 8, 8), Vector2.one * 0.5f);
        }

        // ========== Particle Pool ==========
        private void WarmParticlePool()
        {
            for (int i = 0; i < PARTICLE_POOL_SIZE; i++)
            {
                var item = CreateParticle();
                item.go.SetActive(false);
                _particlePool.Enqueue(item);
            }
        }

        private (GameObject go, Image img, RectTransform rt) CreateParticle()
        {
            var go = new GameObject("CubeParticle");
            go.transform.SetParent(_effectCanvas, false);
            var rt = go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.sprite = _squareSprite;
            img.raycastTarget = false;
            return (go, img, rt);
        }

        private (GameObject go, Image img, RectTransform rt) GetParticle()
        {
            if (_particlePool.Count > 0)
            {
                var item = _particlePool.Dequeue();
                item.go.SetActive(true);
                return item;
            }
            return CreateParticle();
        }

        private void ReleaseParticle(GameObject go, Image img, RectTransform rt)
        {
            if (go == null) return;
            go.SetActive(false);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            img.color = Color.white;
            go.transform.SetParent(_effectCanvas, false);
            _particlePool.Enqueue((go, img, rt));
        }

        // ========== Line Pool ==========
        private void WarmLinePool()
        {
            for (int i = 0; i < LINE_POOL_SIZE; i++)
            {
                var go = CreateLineObject();
                go.SetActive(false);
                _linePool.Enqueue(go);
            }
        }

        private GameObject CreateLineObject()
        {
            var go = new GameObject("Line");
            go.transform.SetParent(_effectCanvas, false);
            var rt = go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.sprite = _squareSprite;
            img.raycastTarget = false;
            return go;
        }

        private GameObject GetLineObject()
        {
            if (_linePool.Count > 0)
            {
                var go = _linePool.Dequeue();
                go.SetActive(true);
                return go;
            }
            return CreateLineObject();
        }

        private void ReleaseLineObject(GameObject go)
        {
            if (go == null) return;
            go.SetActive(false);
            var rt = go.GetComponent<RectTransform>();
            var img = go.GetComponent<Image>();
            if (rt != null)
            {
                rt.localScale = Vector3.one;
                rt.rotation = Quaternion.identity;
            }
            if (img != null) img.color = Color.white;
            _linePool.Enqueue(go);
        }

        // ========== Shockwave Pool ==========
        private void WarmShockwavePool()
        {
            for (int i = 0; i < SHOCKWAVE_POOL_SIZE; i++)
            {
                var go = CreateShockwaveObject();
                go.SetActive(false);
                _shockwavePool.Enqueue(go);
            }
        }

        private GameObject CreateShockwaveObject()
        {
            var go = new GameObject("Shockwave");
            go.transform.SetParent(_effectCanvas, false);
            var rt = go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.sprite = TileClearEffect.GetCircleSpriteStatic();
            img.raycastTarget = false;
            return go;
        }

        private GameObject GetShockwaveObject()
        {
            if (_shockwavePool.Count > 0)
            {
                var go = _shockwavePool.Dequeue();
                go.SetActive(true);
                return go;
            }
            return CreateShockwaveObject();
        }

        private void ReleaseShockwaveObject(GameObject go)
        {
            if (go == null) return;
            go.SetActive(false);
            var rt = go.GetComponent<RectTransform>();
            var img = go.GetComponent<Image>();
            if (rt != null)
            {
                rt.localScale = Vector3.one;
                rt.sizeDelta = new Vector2(30f, 30f);
            }
            if (img != null) img.color = Color.white;
            _shockwavePool.Enqueue(go);
        }

        // ========== Coordinate Conversion ==========
        private Vector2 WorldToUI(Vector3 worldPos)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _effectCanvas,
                RectTransformUtility.WorldToScreenPoint(UICamera, worldPos),
                UICamera,
                out Vector2 uiPos);
            return uiPos;
        }

        public void RefreshItemUI()
        {
            _bombNumberText.text = DataManager.Instance.BombCount.ToString();
            _lightningNumberText.text = DataManager.Instance.LightningCount.ToString();
            _potionNumberText.text = DataManager.Instance.PotionCount.ToString();

            // Buttons always clickable (for ad panel when count = 0)
            _bombButton.interactable = true;
            _lightningButton.interactable = true;
            _potionButton.interactable = true;
        }

        // ========== Bomb ==========
        private void TryUseBomb()
        {
            if (!GameFlowController.Instance.CanAcceptInput) return;

            if (DataManager.Instance.BombCount > 0)
            {
                ExecuteBomb();
                return;
            }

            // No bomb, show ad panel
            if (adRewardPanel != null)
            {
                adRewardPanel.Show("Bomb", () =>
                {
                    DataManager.Instance.BombCount += 10;
                    RefreshItemUI();
                    // 不自动执行，让用户下次点击时使用
                });
            }
        }

        public void ExecuteBomb()
        {
            if (DataManager.Instance.BombCount <= 0) return;

            AudioManager.Instance?.PlayItem();
            GameFlowController.Instance.SetState(GameState.Swapping);
            DataManager.Instance.BombCount--;
            RefreshItemUI();

            int bombCount = Random.Range(3, 7);
            var allClearedTiles = new List<Vector2Int>();
            int finishedBombs = 0;

            for (int i = 0; i < bombCount; i++)
            {
                int r = Random.Range(0, _boardView.Rows);
                int c = Random.Range(0, _boardView.Cols);
                Vector2 uiTarget = WorldToUI(_boardView.GridToWorld(r, c));

                GameObject fx = Instantiate(_bombIconPrefab, _effectCanvas);
                RectTransform fxRt = fx.GetComponent<RectTransform>();
                fxRt.anchoredPosition = new Vector2(uiTarget.x, 1050f);
                fxRt.localScale = Vector3.one * 1.5f;

                StartCoroutine(ParticleTailCoroutine(fxRt, new Color(0.98f, 0.15f, 0.15f), 0.06f));

                int capturedR = r, capturedC = c;

                var flySeq = DOTween.Sequence();
                flySeq.Append(fxRt.DOAnchorPos(uiTarget, 0.85f).SetEase(Ease.InQuad));
                flySeq.Join(fxRt.DOScale(1.2f, 0.85f));

                flySeq.OnComplete(() =>
                {
                    var explodeSeq = DOTween.Sequence();
                    explodeSeq.Append(fxRt.DOScale(1.8f, 0.15f).SetEase(Ease.OutBack));

                    Image bombImg = fx.GetComponent<Image>();
                    if (bombImg != null)
                    {
                        var flashSeq = DOTween.Sequence();
                        for (int f = 0; f < 8; f++)
                        {
                            bool red = f % 2 == 0;
                            flashSeq.Append(bombImg.DOColor(red ? Color.red : Color.white, 0.04f));
                            flashSeq.Join(fxRt.DOScale(red ? 2.2f : 1.6f, 0.04f));
                        }
                        explodeSeq.Append(flashSeq);
                    }

                    explodeSeq.AppendInterval(0.05f);
                    explodeSeq.OnComplete(() =>
                    {
                        AudioManager.Instance?.PlayExplosion();
                        Color bombColor = new Color(0.98f, 0.15f, 0.15f);
                        SpawnCubeParticleInternal(uiTarget, bombColor, true, 12);
                        SpawnShockwave(uiTarget, bombColor, 300f);

                        _boardView.transform.DOShakePosition(0.5f, 35f, 18);

                        for (int cr = capturedR - 1; cr <= capturedR + 1; cr++)
                            for (int cc = capturedC - 1; cc <= capturedC + 1; cc++)
                            {
                                if (cr >= 0 && cr < _boardView.Rows && cc >= 0 && cc < _boardView.Cols)
                                {
                                    var pos = new Vector2Int(cr, cc);
                                    if (!allClearedTiles.Contains(pos))
                                    {
                                        _boardView.ClearTileAtDirectly(cr, cc);
                                        allClearedTiles.Add(pos);
                                    }
                                }
                            }

                        Destroy(fx);
                        finishedBombs++;
                        if (finishedBombs >= bombCount)
                            OnTilesDestroyed?.Invoke(allClearedTiles);
                    });
                });
            }
        }

        // ========== Lightning ==========
        private void TryUseLightning()
        {
            if (!GameFlowController.Instance.CanAcceptInput) return;

            if (DataManager.Instance.LightningCount > 0)
            {
                ExecuteLightning();
                return;
            }

            if (adRewardPanel != null)
            {
                adRewardPanel.Show("Lightning", () =>
                {
                    DataManager.Instance.LightningCount += 10;
                    RefreshItemUI();
                });
            }
        }

        public void ExecuteLightning()
        {
            if (DataManager.Instance.LightningCount <= 0) return;

            AudioManager.Instance?.PlayItem();
            GameFlowController.Instance.SetState(GameState.Swapping);
            DataManager.Instance.LightningCount--;
            RefreshItemUI();

            TileType targetType = (TileType)Random.Range(0, 4);
            List<TileView> views = _boardView.GetAllViewsOfType(targetType);

            if (views.Count == 0)
            {
                OnTilesDestroyed?.Invoke(new List<Vector2Int>());
                return;
            }

            Vector3 boardTopWorld = _boardView.GridToWorld(_boardView.Rows - 1, _boardView.Cols / 2);
            Vector2 boardTopUI = WorldToUI(boardTopWorld);
            boardTopUI.y += 150f;

            GameObject lightningFx = Instantiate(_lightningIconPrefab, _effectCanvas);
            RectTransform ltRt = lightningFx.GetComponent<RectTransform>();
            ltRt.anchoredPosition = new Vector2(boardTopUI.x, 1050f);
            ltRt.localScale = Vector3.one;

            Color arcColor = new Color(1.0f, 0.95f, 0.1f);

            var enterSeq = DOTween.Sequence();
            enterSeq.Append(ltRt.DOAnchorPos(boardTopUI, 0.35f).SetEase(Ease.OutQuad));
            enterSeq.Join(ltRt.DOScale(2.5f, 0.35f));

            enterSeq.OnComplete(() =>
            {
                ltRt.DOShakePosition(0.4f, 15f, 20);

                bool charging = true;
                StartCoroutine(LightningParticleEmitter(ltRt, () => charging, 6));

                DOVirtual.DelayedCall(0.4f, () =>
                {
                    charging = false;

                    var positions = new List<Vector2Int>();
                    var strikeSeq = DOTween.Sequence();
                    float delay = 0f;

                    foreach (var v in views)
                    {
                        var capturedView = v;
                        Vector2 targetUI = WorldToUI(_boardView.GridToWorld(v.Data.Row, v.Data.Col));
                        float capturedDelay = delay;

                        strikeSeq.InsertCallback(capturedDelay, () =>
                        {
                            AudioManager.Instance?.PlayLightningStrike();
                            Vector2 mid1 = Vector2.Lerp(boardTopUI, targetUI, 0.33f) + new Vector2(Random.Range(-50f, 50f), Random.Range(-30f, 30f));
                            Vector2 mid2 = Vector2.Lerp(boardTopUI, targetUI, 0.66f) + new Vector2(Random.Range(-50f, 50f), Random.Range(-30f, 30f));
                            SpawnUILine(boardTopUI, mid1, arcColor, 0.25f, 10f, 16f);
                            SpawnUILine(mid1, mid2, arcColor, 0.25f, 10f, 16f);
                            SpawnUILine(mid2, targetUI, arcColor, 0.25f, 10f, 16f);
                            SpawnCubeParticleInternal(targetUI, arcColor, false, 6);
                            SpawnShockwave(targetUI, arcColor, 80f);
                        });

                        strikeSeq.Insert(capturedDelay + 0.1f, capturedView.transform.DOScale(1.3f, 0.07f));
                        strikeSeq.Insert(capturedDelay + 0.17f, capturedView.transform.DOScale(0f, 0.1f));

                        positions.Add(new Vector2Int(v.Data.Row, v.Data.Col));
                        delay += 0.12f;
                    }

                    strikeSeq.OnComplete(() =>
                    {
                        ltRt.DOScale(0f, 0.2f).OnComplete(() =>
                        {
                            Destroy(lightningFx);
                            foreach (var p in positions)
                                _boardView.ClearTileAtDirectly(p.x, p.y);
                            OnTilesDestroyed?.Invoke(positions);
                        });
                    });
                });
            });
        }

        // ========== Potion ==========
        private void TryUsePotion()
        {
            if (!GameFlowController.Instance.CanAcceptInput) return;

            if (DataManager.Instance.PotionCount > 0)
            {
                ExecutePotion();
                return;
            }

            if (adRewardPanel != null)
            {
                adRewardPanel.Show("Potion", () =>
                {
                    DataManager.Instance.PotionCount += 10;
                    RefreshItemUI();
                });
            }
        }

        public void ExecutePotion()
        {
            if (DataManager.Instance.PotionCount <= 0) return;

            AudioManager.Instance?.PlayItem();
            GameFlowController.Instance.SetState(GameState.Swapping);
            DataManager.Instance.PotionCount--;
            RefreshItemUI();

            int r = Random.Range(0, _boardView.Rows);
            int c = Random.Range(0, _boardView.Cols);
            Vector2 uiTarget = WorldToUI(_boardView.GridToWorld(r, c));

            GameObject fx = Instantiate(_potionIconPrefab, _effectCanvas);
            RectTransform fxRt = fx.GetComponent<RectTransform>();
            fxRt.anchoredPosition = new Vector2(uiTarget.x - 60f, 1050f);
            fxRt.localScale = Vector3.one * 1.5f;

            Color acidColor = new Color(0.15f, 0.98f, 0.25f);
            StartCoroutine(ParticleTailCoroutine(fxRt, acidColor, 0.06f));

            int capturedR = r, capturedC = c;

            var flySeq = DOTween.Sequence();
            flySeq.Append(fxRt.DOAnchorPos(uiTarget, 0.85f).SetEase(Ease.InCubic));
            flySeq.Join(fxRt.DOScale(1.2f, 0.85f));
            flySeq.Join(fxRt.DORotate(new Vector3(0, 0, 450), 0.85f, RotateMode.FastBeyond360));

            flySeq.OnComplete(() =>
            {
                var breakSeq = DOTween.Sequence();
                breakSeq.Append(fxRt.DOAnchorPosY(uiTarget.y + 40f, 0.12f).SetEase(Ease.OutQuad));
                breakSeq.Append(fxRt.DOAnchorPosY(uiTarget.y, 0.1f).SetEase(Ease.InQuad));
                breakSeq.Join(fxRt.DOScale(1.6f, 0.12f));

                breakSeq.AppendCallback(() =>
                {
                    AudioManager.Instance?.PlayPotionBreak();
                    SpawnCubeParticleInternal(uiTarget, acidColor, true, 15);
                    SpawnShockwave(uiTarget, acidColor, 180f);
                    _boardView.transform.DOShakePosition(0.4f, 25f, 15);
                    Destroy(fx);
                });

                breakSeq.AppendInterval(0.1f);
                breakSeq.OnComplete(() =>
                {
                    var clearedTiles = new List<Vector2Int>();

                    var rowTiles = new List<(Vector2Int pos, Vector2 ui)>();
                    var colTiles = new List<(Vector2Int pos, Vector2 ui)>();

                    for (int cc = 0; cc < _boardView.Cols; cc++)
                        rowTiles.Add((new Vector2Int(capturedR, cc), WorldToUI(_boardView.GridToWorld(capturedR, cc))));

                    for (int cr = 0; cr < _boardView.Rows; cr++)
                    {
                        if (cr == capturedR) continue;
                        colTiles.Add((new Vector2Int(cr, capturedC), WorldToUI(_boardView.GridToWorld(cr, capturedC))));
                    }

                    var rowSeq = DOTween.Sequence();
                    foreach (var (pos, uiPos) in rowTiles)
                    {
                        var capturedPos = pos;
                        float dist = Mathf.Abs(uiPos.x - uiTarget.x);
                        float t = dist / 600f * 0.3f;
                        rowSeq.InsertCallback(t, () =>
                        {
                            Color transColor = new Color(acidColor.r, acidColor.g, acidColor.b, 0.4f);
                            SpawnUILine(uiTarget, uiPos, transColor, 0.35f, 35f, 60f);
                            SpawnCubeParticleInternal(uiPos, acidColor, false, 2);
                            _boardView.ClearTileAtDirectly(capturedPos.x, capturedPos.y);
                            clearedTiles.Add(capturedPos);
                        });
                    }

                    var colSeq = DOTween.Sequence();
                    colSeq.AppendInterval(0.05f);
                    foreach (var (pos, uiPos) in colTiles)
                    {
                        var capturedPos = pos;
                        float dist = Mathf.Abs(uiPos.y - uiTarget.y);
                        float t = dist / 800f * 0.3f;
                        colSeq.InsertCallback(t, () =>
                        {
                            Color transColor = new Color(acidColor.r, acidColor.g, acidColor.b, 0.4f);
                            SpawnUILine(uiTarget, uiPos, transColor, 0.35f, 35f, 60f);
                            SpawnCubeParticleInternal(uiPos, acidColor, false, 2);
                            _boardView.ClearTileAtDirectly(capturedPos.x, capturedPos.y);
                            if (!clearedTiles.Contains(capturedPos))
                                clearedTiles.Add(capturedPos);
                        });
                    }

                    DOVirtual.DelayedCall(0.45f, () => OnTilesDestroyed?.Invoke(clearedTiles));
                });
            });
        }

        // ========== Internal Effect Methods ==========
        private IEnumerator LightningParticleEmitter(RectTransform lightningRt, System.Func<bool> checkCondition, int particleCount = 6)
        {
            while (checkCondition())
            {
                if (lightningRt == null) yield break;
                SpawnCubeParticleInternal(lightningRt.anchoredPosition, new Color(1.0f, 0.95f, 0.1f), true, particleCount);
                yield return new WaitForSeconds(0.04f);
            }
        }

        private IEnumerator ParticleTailCoroutine(RectTransform propRt, Color particleColor, float interval = 0.06f)
        {
            while (propRt != null)
            {
                SpawnCubeParticleInternal(propRt.anchoredPosition, particleColor, false, 1);
                yield return new WaitForSeconds(interval);
            }
        }

        private void SpawnCubeParticleInternal(Vector2 uiCenter, Color particleColor, bool isExplosion, int baseCount = 12)
        {
            int spawnCount = isExplosion ? (int)(baseCount * Random.Range(1.2f, 1.6f)) : baseCount;

            for (int i = 0; i < spawnCount; i++)
            {
                var (go, img, rt) = GetParticle();

                rt.anchoredPosition = uiCenter;
                float size = isExplosion ? Random.Range(20f, 35f) : Random.Range(15f, 25f);
                rt.sizeDelta = new Vector2(size, size);
                rt.localScale = Vector3.one;
                rt.localRotation = Quaternion.identity;
                img.color = particleColor;

                Vector2 targetPos;
                float duration;

                if (isExplosion)
                {
                    float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                    float dist = Random.Range(120f, 280f);
                    targetPos = uiCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
                    duration = Random.Range(0.4f, 0.65f);
                }
                else
                {
                    float angle = Random.Range(-25f, 25f) * Mathf.Deg2Rad;
                    float xOffset = Mathf.Sin(angle) * Random.Range(35f, 70f);
                    float yOffset = Mathf.Cos(angle) * Random.Range(80f, 150f);
                    rt.anchoredPosition += new Vector2(Random.Range(-20f, 20f), 15f);
                    targetPos = rt.anchoredPosition + new Vector2(xOffset, yOffset);
                    duration = Random.Range(0.2f, 0.4f);
                }

                float spin = Random.Range(360f, 720f) * (Random.value > 0.5f ? 1 : -1);
                var capturedGo = go;
                var capturedImg = img;
                var capturedRt = rt;

                rt.DOKill();

                var seq = DOTween.Sequence();
                seq.Join(rt.DOAnchorPos(targetPos, duration).SetEase(isExplosion ? Ease.OutCubic : Ease.OutQuad));
                seq.Join(rt.DORotate(new Vector3(0, 0, spin), duration, RotateMode.FastBeyond360));
                seq.Join(rt.DOScale(0f, duration * 0.7f).SetDelay(duration * 0.3f).SetEase(Ease.InQuad));
                seq.Join(img.DOColor(new Color(particleColor.r, particleColor.g, particleColor.b, 0f), duration).SetEase(Ease.InQuad));
                seq.OnComplete(() => ReleaseParticle(capturedGo, capturedImg, capturedRt));
            }
        }

        private void SpawnUILine(Vector2 start, Vector2 end, Color color, float duration, float minWidth = 4f, float maxWidth = 8f)
        {
            var go = GetLineObject();
            var rt = go.GetComponent<RectTransform>();
            var img = go.GetComponent<Image>();

            Vector2 dir = end - start;
            float length = dir.magnitude;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            rt.anchoredPosition = start + dir * 0.5f;
            rt.sizeDelta = new Vector2(length, Random.Range(minWidth, maxWidth));
            rt.localRotation = Quaternion.Euler(0, 0, angle);
            img.color = color;

            var seq = DOTween.Sequence();
            seq.AppendInterval(duration * 0.3f);
            seq.Append(img.DOColor(new Color(color.r, color.g, color.b, 0f), duration * 0.7f));
            seq.Join(rt.DOScaleX(0f, duration * 0.7f).SetEase(Ease.InQuad));
            seq.OnComplete(() => ReleaseLineObject(go));
        }

        private void SpawnShockwave(Vector2 center, Color color, float maxSize = 400f)
        {
            var go = GetShockwaveObject();
            var rt = go.GetComponent<RectTransform>();
            var img = go.GetComponent<Image>();

            img.sprite = TileClearEffect.GetCircleSpriteStatic();
            img.color = new Color(color.r, color.g, color.b, 0.75f);
            rt.anchoredPosition = center;
            rt.sizeDelta = new Vector2(30f, 30f);
            rt.localScale = Vector3.one;

            var seq = DOTween.Sequence();
            seq.Join(rt.DOSizeDelta(new Vector2(maxSize, maxSize), 0.4f).SetEase(Ease.OutQuad));
            seq.Join(img.DOColor(new Color(color.r, color.g, color.b, 0f), 0.4f).SetEase(Ease.OutQuad));
            seq.OnComplete(() => ReleaseShockwaveObject(go));
        }
    }
}