using System;
using System.Collections.Generic;
using NoOneCameBack.Tablet;
using UnityEngine;
using UnityEngine.UI;

namespace NoOneCameBack.Inventory
{
    // Draws one InventoryGrid. Read-only: it redraws on grid.Changed and never edits the model.
    public class InventoryGridView : MonoBehaviour
    {
        [Tooltip("Top-left anchored container; cells and items are positioned manually inside it.")]
        [SerializeField] private RectTransform content;
        [SerializeField] private RectTransform cellLayer;
        [SerializeField] private RectTransform itemLayer;
        [SerializeField] private InventoryCellView cellPrefab;
        [SerializeField] private InventoryItemView itemPrefab;

        [Header("Overlays")]
        [SerializeField] private RectTransform cursor;
        [SerializeField] private Image placementPreview;
        [Tooltip("Item view that follows the cursor while holding an item.")]
        [SerializeField] private InventoryItemView heldGhost;

        [Header("Layout")]
        // TODO(diseño): cell size and spacing in canvas units.
        [SerializeField, Min(1f)] private float cellSize = 96f;
        [SerializeField, Min(0f)] private float spacing = 4f;
        [SerializeField] private Color validColor = new Color(0.3f, 1f, 0.3f, 0.45f);
        [SerializeField] private Color invalidColor = new Color(1f, 0.3f, 0.3f, 0.45f);

        private readonly List<InventoryCellView> cells = new List<InventoryCellView>();
        private readonly List<InventoryItemView> itemViews = new List<InventoryItemView>();

        private InventoryGrid grid;
        private Func<ItemInstance, bool> isEquipped;
        private ItemInstance dimmedItem;
        private Camera eventCamera;

        private bool cursorShown;
        private RectInt cursorRect;
        private bool heldShown;
        private ItemInstance heldItem;
        private Vector2Int heldPosition;
        private bool heldRotated;
        private bool heldValid;

        public InventoryGrid Grid => grid;

        private void OnDestroy() => Unbind();

        public void Bind(InventoryGrid target, Func<ItemInstance, bool> equippedCheck)
        {
            if (grid == target)
            {
                Redraw();
                return;
            }

            Unbind();
            grid = target;
            isEquipped = equippedCheck;
            eventCamera = TabletUIUtility.GetEventCamera(this);

            if (grid == null) return;

            grid.Changed += Redraw;
            BuildCells();
            Redraw();
        }

        public void Unbind()
        {
            if (grid != null) grid.Changed -= Redraw;
            grid = null;
            dimmedItem = null;
        }

        public void Redraw()
        {
            if (grid == null) return;

            int index = 0;
            foreach (GridPlacement placement in grid.Placements)
            {
                if (index == itemViews.Count) itemViews.Add(Instantiate(itemPrefab, itemLayer));

                InventoryItemView view = itemViews[index++];
                view.gameObject.SetActive(true);
                view.Show(placement.Item, CellToLocal(placement.Position), SizeFor(placement.Size), placement.Rotated,
                    isEquipped != null && isEquipped(placement.Item), placement.Item == dimmedItem);
            }

            for (int i = index; i < itemViews.Count; i++) itemViews[i].gameObject.SetActive(false);
        }

        public void SetDimmedItem(ItemInstance item)
        {
            if (dimmedItem == item) return;

            dimmedItem = item;
            Redraw();
        }

        public void ShowCursor(Vector2Int position, Vector2Int size)
        {
            if (cursor == null) return;
            if (cursorShown && cursorRect.Equals(new RectInt(position, size))) return;

            cursorShown = true;
            cursorRect = new RectInt(position, size);
            cursor.gameObject.SetActive(true);
            PlaceOverlay(cursor, position, size);
        }

        public void HideCursor()
        {
            cursorShown = false;
            if (cursor != null) cursor.gameObject.SetActive(false);
        }

        public void ShowHeld(ItemInstance item, Vector2Int position, bool rotated, bool valid)
        {
            if (heldShown && item == heldItem && position == heldPosition && rotated == heldRotated && valid == heldValid) return;

            heldShown = true;
            heldItem = item;
            heldPosition = position;
            heldRotated = rotated;
            heldValid = valid;

            Vector2Int size = item.Definition.GetSize(rotated);

            if (placementPreview != null)
            {
                placementPreview.gameObject.SetActive(true);
                placementPreview.color = valid ? validColor : invalidColor;
                PlaceOverlay(placementPreview.rectTransform, position, size);
            }

            if (heldGhost != null)
            {
                heldGhost.gameObject.SetActive(true);
                heldGhost.Show(item, CellToLocal(position), SizeFor(size), rotated, false, false);
                heldGhost.transform.SetAsLastSibling();
            }
        }

        public void HideHeld()
        {
            heldShown = false;
            heldItem = null;
            if (placementPreview != null) placementPreview.gameObject.SetActive(false);
            if (heldGhost != null) heldGhost.gameObject.SetActive(false);
        }

        public bool TryGetCellAtScreen(Vector2 screenPosition, out Vector2Int cell)
        {
            cell = default;
            if (grid == null) return false;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(content, screenPosition, eventCamera, out Vector2 local)) return false;

            Rect rect = content.rect;
            float x = local.x - rect.xMin;
            float y = rect.yMax - local.y;
            float pitch = cellSize + spacing;
            if (x < 0f || y < 0f) return false;

            cell = new Vector2Int(Mathf.FloorToInt(x / pitch), Mathf.FloorToInt(y / pitch));
            return grid.IsInside(cell);
        }

        // World position of a cell's top-right corner; used to place the context menu next to an item.
        public Vector3 GetCellTopRightWorld(Vector2Int position, Vector2Int size)
        {
            Vector2 local = CellToLocal(position) + new Vector2(SizeFor(size).x, 0f);
            return content.TransformPoint(new Vector3(local.x + content.rect.xMin, local.y + content.rect.yMax, 0f));
        }

        private void BuildCells()
        {
            if (cellLayer == null) cellLayer = content;
            if (itemLayer == null) itemLayer = content;

            TabletUIUtility.ForceTopLeftAnchors(content);
            content.sizeDelta = SizeFor(new Vector2Int(grid.Width, grid.Height));

            foreach (InventoryCellView cell in cells) Destroy(cell.gameObject);
            cells.Clear();

            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    InventoryCellView cell = Instantiate(cellPrefab, cellLayer);
                    cell.Place(CellToLocal(new Vector2Int(x, y)), SizeFor(Vector2Int.one));
                    cells.Add(cell);
                }
            }

            HideHeld();
            HideCursor();
        }

        private void PlaceOverlay(RectTransform overlay, Vector2Int position, Vector2Int size)
        {
            TabletUIUtility.ForceTopLeftAnchors(overlay);
            overlay.anchoredPosition = CellToLocal(position);
            overlay.sizeDelta = SizeFor(size);
            overlay.SetAsLastSibling();
        }

        private Vector2 CellToLocal(Vector2Int cell) => new Vector2(cell.x * (cellSize + spacing), -cell.y * (cellSize + spacing));

        private Vector2 SizeFor(Vector2Int cellCount) =>
            new Vector2(cellCount.x * cellSize + (cellCount.x - 1) * spacing, cellCount.y * cellSize + (cellCount.y - 1) * spacing);
    }
}
