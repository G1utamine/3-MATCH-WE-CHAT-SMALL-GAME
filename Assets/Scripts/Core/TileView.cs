using DG.Tweening;
using Match3.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.Core
{
    public class TileView : MonoBehaviour
    {
        public TileData Data { get; private set; }

        [SerializeField] private Image _image;

        private static readonly Color SelectedColor = new Color(1f, 1f, 0.4f, 1f);

        public void Initialize(TileData data, Sprite sprite)
        {
            Data = data;
            _image.sprite = sprite;
            _image.color = Color.white;
        }

        public void SetSelected(bool selected)
        {
            _image.color = selected ? SelectedColor : Color.white;
            if (selected)
                transform.DOPunchScale(Vector3.one * 0.15f, 0.2f, 5, 0.5f);
        }

        public Tween MoveTo(RectTransform targetCell, float duration = 0.25f)
        {
            return transform.DOMove(targetCell.position, duration).SetEase(Ease.OutQuad);
        }

        public Tween PlayClearAnimation()
        {
            return transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack);
        }
    }
}