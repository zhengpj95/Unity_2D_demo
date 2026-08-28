using System.Collections.Generic;
using UnityEngine;

namespace VampireSurvivorsLike
{
    /// <summary>
    /// Reuses a small group of SpriteRenderers around an orthographic camera to
    /// present a visually infinite ground made from regularly sliced sprites.
    /// </summary>
    public sealed class InfiniteGroundTilemap : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera _targetCamera;
        [SerializeField] private Sprite[] _groundTiles;

        [Header("Source layout")]
        [Min(1)]
        [SerializeField] private int _sourceColumns = 4;
        [Min(1)]
        [SerializeField] private int _sourceRows = 4;
        [Tooltip("Enable this when the Sprite Editor lists slices from the top-left corner.")]
        [SerializeField] private bool _tilesStartAtTopLeft = true;

        [Header("Rendering")]
        [Min(0)]
        [SerializeField] private int _viewPadding = 1;
        [SerializeField] private string _sortingLayerName = "Default";
        [SerializeField] private int _sortingOrder = -10;

        private readonly List<SpriteRenderer> _renderers = new List<SpriteRenderer>();
        private Vector2 _tileWorldSize;
        private Vector2Int _minVisibleCell;
        private Vector2Int _maxVisibleCell;
        private bool _hasVisibleRange;

        private void Awake()
        {
            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
            }

            if (!TryInitialize())
            {
                enabled = false;
            }
        }

        private void LateUpdate()
        {
            RefreshVisibleTiles();
        }

        private bool TryInitialize()
        {
            if (_targetCamera == null || !_targetCamera.orthographic)
            {
                Debug.LogError("[InfiniteGroundTilemap] An orthographic target camera is required.", this);
                return false;
            }

            int expectedTileCount = _sourceColumns * _sourceRows;
            if (_groundTiles == null || _groundTiles.Length != expectedTileCount)
            {
                Debug.LogError($"[InfiniteGroundTilemap] Expected {expectedTileCount} ground tiles, " +
                               $"but received {_groundTiles?.Length ?? 0}.", this);
                return false;
            }

            Sprite firstTile = _groundTiles[0];
            if (firstTile == null || firstTile.pixelsPerUnit <= 0f)
            {
                Debug.LogError("[InfiniteGroundTilemap] Ground tile 0 is missing or has invalid Pixels Per Unit.", this);
                return false;
            }

            _tileWorldSize = firstTile.rect.size / firstTile.pixelsPerUnit;
            if (_tileWorldSize.x <= 0f || _tileWorldSize.y <= 0f)
            {
                Debug.LogError("[InfiniteGroundTilemap] Ground tile world size must be positive.", this);
                return false;
            }

            for (int i = 0; i < _groundTiles.Length; i++)
            {
                Sprite tile = _groundTiles[i];
                if (tile == null || tile.rect.size != firstTile.rect.size ||
                    !Mathf.Approximately(tile.pixelsPerUnit, firstTile.pixelsPerUnit))
                {
                    Debug.LogError("[InfiniteGroundTilemap] All ground tiles must use the same size and Pixels Per Unit.", this);
                    return false;
                }
            }

            RefreshVisibleTiles(true);
            return true;
        }

        private void RefreshVisibleTiles(bool force = false)
        {
            Vector3 viewportBottomLeft = _targetCamera.ViewportToWorldPoint(new Vector3(0f, 0f, 0f));
            Vector3 viewportTopRight = _targetCamera.ViewportToWorldPoint(new Vector3(1f, 1f, 0f));
            Vector3 localBottomLeft = transform.InverseTransformPoint(viewportBottomLeft);
            Vector3 localTopRight = transform.InverseTransformPoint(viewportTopRight);

            Vector2Int minCell = new Vector2Int(
                Mathf.FloorToInt(localBottomLeft.x / _tileWorldSize.x) - _viewPadding,
                Mathf.FloorToInt(localBottomLeft.y / _tileWorldSize.y) - _viewPadding);
            Vector2Int maxCell = new Vector2Int(
                Mathf.FloorToInt(localTopRight.x / _tileWorldSize.x) + _viewPadding,
                Mathf.FloorToInt(localTopRight.y / _tileWorldSize.y) + _viewPadding);

            if (!force && _hasVisibleRange && minCell == _minVisibleCell && maxCell == _maxVisibleCell)
            {
                return;
            }

            _minVisibleCell = minCell;
            _maxVisibleCell = maxCell;
            _hasVisibleRange = true;

            int requiredCount = (maxCell.x - minCell.x + 1) * (maxCell.y - minCell.y + 1);
            EnsureRendererCount(requiredCount);

            int rendererIndex = 0;
            for (int y = minCell.y; y <= maxCell.y; y++)
            {
                for (int x = minCell.x; x <= maxCell.x; x++)
                {
                    SpriteRenderer tileRenderer = _renderers[rendererIndex++];
                    Sprite tile = GetTileForCell(x, y);
                    Vector2 pivotOffset = tile.pivot / tile.pixelsPerUnit;

                    tileRenderer.sprite = tile;
                    tileRenderer.transform.localPosition = new Vector3(
                        x * _tileWorldSize.x + pivotOffset.x,
                        y * _tileWorldSize.y + pivotOffset.y,
                        0f);
                    tileRenderer.gameObject.SetActive(true);
                }
            }

            for (; rendererIndex < _renderers.Count; rendererIndex++)
            {
                _renderers[rendererIndex].gameObject.SetActive(false);
            }
        }

        private void EnsureRendererCount(int requiredCount)
        {
            while (_renderers.Count < requiredCount)
            {
                GameObject tileObject = new GameObject($"GroundTile_{_renderers.Count}");
                tileObject.transform.SetParent(transform, false);

                SpriteRenderer tileRenderer = tileObject.AddComponent<SpriteRenderer>();
                tileRenderer.sortingLayerName = _sortingLayerName;
                tileRenderer.sortingOrder = _sortingOrder;
                _renderers.Add(tileRenderer);
            }
        }

        private Sprite GetTileForCell(int cellX, int cellY)
        {
            int sourceX = PositiveModulo(cellX, _sourceColumns);
            int sourceY = PositiveModulo(cellY, _sourceRows);
            int arrayY = _tilesStartAtTopLeft ? _sourceRows - 1 - sourceY : sourceY;
            return _groundTiles[arrayY * _sourceColumns + sourceX];
        }

        private static int PositiveModulo(int value, int modulus)
        {
            int result = value % modulus;
            return result < 0 ? result + modulus : result;
        }
    }
}
