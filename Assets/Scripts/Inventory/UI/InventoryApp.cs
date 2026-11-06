using System.Collections.Generic;
using NoOneCameBack.Tablet;
using UnityEngine;

namespace NoOneCameBack.Inventory
{
    // Single cell-cursor interaction model: the pad/keys step the cursor, the mouse puts it on the hovered cell.
    // Never edits the model directly beyond requesting operations; views redraw from the model's Changed events.
    public class InventoryApp : TabletApp
    {
        private enum Panel
        {
            Grid,
            Locker,
            KeyItems,
        }

        private enum Mode
        {
            Browse,
            Holding,
            Menu,
            Examine,
        }

        private enum ItemAction
        {
            Use,
            Equip,
            Unequip,
            Drop,
            Examine,
        }

        [SerializeField] private PlayerInventoryHolder holder;

        [Header("Panels")]
        [SerializeField] private InventoryGridView gridView;
        [SerializeField] private InventoryGridView lockerView;
        [Tooltip("Shown only while a locker is open.")]
        [SerializeField] private GameObject lockerRoot;
        [Tooltip("Grid area (player grid + locker). Hidden while the key items tab is shown.")]
        [SerializeField] private GameObject gridPanelRoot;
        [SerializeField] private GameObject keyItemsRoot;
        [SerializeField] private KeyItemsView keyItemsView;
        [SerializeField] private ItemInfoPanel infoPanel;
        [SerializeField] private ContextMenuView contextMenu;
        [SerializeField] private StatusStripView statusStrip;

        [Header("Text")]
        [SerializeField] private string useLabel = "Usar";
        [SerializeField] private string equipLabel = "Equipar";
        [SerializeField] private string unequipLabel = "Desequipar";
        [SerializeField] private string dropLabel = "Soltar";
        [SerializeField] private string examineLabel = "Examinar";
        [SerializeField] private string cannotUseMessage = "No lo necesito ahora.";

        private readonly List<ItemAction> menuActions = new List<ItemAction>();
        private readonly List<string> menuLabels = new List<string>();

        private Panel panel;
        private Mode mode;
        private bool lockerSession;
        private Vector2Int gridCursor;
        private Vector2Int lockerCursor;
        private bool pointerActive;
        private bool pointerOverGrid;

        private ItemInstance heldItem;
        private InventoryGrid heldSource;
        private bool heldRotated;
        private Vector2Int heldOffset;
        private bool dragging;
        private Vector2Int dragStartCell;
        private Panel dragStartPanel;

        private ItemInstance menuItem;
        private ItemInstance shownItem;
        private bool infoDirty = true;

        private PlayerInventoryModel Model => holder.Model;
        private InventoryGridView ActiveView => panel == Panel.Locker ? lockerView : gridView;
        private InventoryGrid ActiveGrid => panel == Panel.Locker ? Model.Locker : Model.Grid;
        private Vector2Int HeldPosition => ActiveCursor - heldOffset;

        private Vector2Int ActiveCursor
        {
            get => panel == Panel.Locker ? lockerCursor : gridCursor;
            set
            {
                if (panel == Panel.Locker) lockerCursor = value;
                else gridCursor = value;
            }
        }

        public void OpenWithLocker()
        {
            lockerSession = true;
            if (IsActive)
            {
                lockerView.Bind(Model.Locker, null);
                RefreshLayout();
                return;
            }

            Tablet.Open(this);
            if (!IsActive) lockerSession = false;
        }

        protected override void OnOpen()
        {
            Model.EquippedChanged += OnEquippedChanged;
            Model.Grid.Changed += MarkInfoDirty;
            Model.Locker.Changed += MarkInfoDirty;

            gridView.Bind(Model.Grid, Model.IsEquipped);
            if (lockerSession) lockerView.Bind(Model.Locker, null);
            keyItemsView.Bind(Model.KeyItems);
            statusStrip.Bind(holder.Health, holder.Psychosis, holder.Bleeding);

            contextMenu.Close();
            infoPanel.HideExamine();
            infoPanel.ClearMessage();

            mode = Mode.Browse;
            panel = Panel.Grid;
            pointerActive = false;
            gridCursor = ClampToGrid(gridCursor, Model.Grid);
            lockerCursor = ClampToGrid(lockerCursor, Model.Locker);
            infoDirty = true;

            RefreshLayout();
            RefreshVisuals();
        }

        protected override void OnClose()
        {
            EndHold();
            CloseMenu();
            infoPanel.HideExamine();

            Model.EquippedChanged -= OnEquippedChanged;
            Model.Grid.Changed -= MarkInfoDirty;
            Model.Locker.Changed -= MarkInfoDirty;

            gridView.Unbind();
            lockerView.Unbind();
            keyItemsView.Unbind();
            statusStrip.Unbind();

            lockerSession = false;
            mode = Mode.Browse;
        }

        public override void Tick(float unscaledDeltaTime)
        {
            if (mode == Mode.Holding && !heldSource.Contains(heldItem)) EndHold();

            switch (mode)
            {
                case Mode.Menu:
                    TickMenu();
                    break;
                case Mode.Examine:
                    if (Input.SubmitPressed || Input.ClickPressed) StopExamine();
                    break;
                default:
                    if (panel == Panel.KeyItems) TickKeyItems();
                    else TickGrid();
                    break;
            }

            RefreshVisuals();
        }

        public override bool TryCancel()
        {
            switch (mode)
            {
                case Mode.Holding:
                    // The item never left its slot, so cancelling just releases it.
                    EndHold();
                    return true;
                case Mode.Menu:
                    CloseMenu();
                    return true;
                case Mode.Examine:
                    StopExamine();
                    return true;
                default:
                    return false;
            }
        }

        private void TickGrid()
        {
            if (Input.SwitchPanelPressed)
            {
                CyclePanel();
                return;
            }

            UpdatePointer();

            Vector2Int step = Input.NavigationStep;
            if (step != Vector2Int.zero) MoveCursor(step);

            bool clicked = Input.ClickPressed && pointerOverGrid;

            if (mode == Mode.Browse)
            {
                ItemInstance hovered = ActiveGrid.GetItemAt(ActiveCursor);
                if (hovered == null) return;

                if (Input.SubmitPressed || clicked)
                {
                    BeginHold(hovered);
                    dragging = clicked;
                    dragStartCell = ActiveCursor;
                    dragStartPanel = panel;
                }
                else if (Input.ItemMenuPressed)
                {
                    OpenMenu(hovered);
                }
                return;
            }

            if (Input.RotatePressed) RotateHeld();

            if (Input.SubmitPressed || clicked)
            {
                TryDropHeld();
            }
            else if (dragging && Input.ClickReleased)
            {
                dragging = false;
                // Releasing on the same cell keeps holding (click-to-pick, click-to-place); elsewhere it's a drag-and-drop.
                if (ActiveCursor != dragStartCell || panel != dragStartPanel) TryDropHeld();
            }
        }

        private void TickKeyItems()
        {
            if (Input.SwitchPanelPressed)
            {
                CyclePanel();
                return;
            }

            int confirmed = keyItemsView.HandleInput(Input);
            if ((confirmed >= 0 || Input.ItemMenuPressed) && keyItemsView.Selected != null) OpenMenu(keyItemsView.Selected);
        }

        private void TickMenu()
        {
            MenuListView list = contextMenu.List;
            if (Input.ClickPressed && !list.IsPointerOver(Input.PointerPosition))
            {
                CloseMenu();
                return;
            }

            int index = list.HandleInput(Input);
            if (index < 0) return;

            ItemAction action = menuActions[index];
            ItemInstance item = menuItem;
            CloseMenu();
            Execute(action, item);
        }

        private void UpdatePointer()
        {
            if (Input.PointerMoved || Input.ClickPressed) pointerActive = true;
            if (Input.NavigationStep != Vector2Int.zero) pointerActive = false;

            pointerOverGrid = false;
            if (!pointerActive) return;

            Vector2 pointer = Input.PointerPosition;
            if (gridView.TryGetCellAtScreen(pointer, out Vector2Int cell))
            {
                panel = Panel.Grid;
                gridCursor = cell;
                pointerOverGrid = true;
            }
            else if (lockerSession && lockerView.TryGetCellAtScreen(pointer, out cell))
            {
                panel = Panel.Locker;
                lockerCursor = cell;
                pointerOverGrid = true;
            }
        }

        private void MoveCursor(Vector2Int step)
        {
            InventoryGrid grid = ActiveGrid;
            Vector2Int cursor = ActiveCursor;
            Vector2Int next = cursor + step;

            // While browsing, the cursor hops over whole items instead of walking through their cells.
            ItemInstance under = grid.GetItemAt(cursor);
            if (mode == Mode.Browse && under != null && grid.TryGetPlacement(under, out GridPlacement placement))
            {
                RectInt rect = placement.Rect;
                next = cursor;
                if (step.x > 0) next.x = rect.xMax;
                else if (step.x < 0) next.x = rect.xMin - 1;
                if (step.y > 0) next.y = rect.yMax;
                else if (step.y < 0) next.y = rect.yMin - 1;
            }

            if (!grid.IsInside(next) && TryCrossToOtherGrid(step, next.y)) return;

            ActiveCursor = ClampToGrid(next, grid);
        }

        // Layout assumption: player grid on the left, locker on the right.
        private bool TryCrossToOtherGrid(Vector2Int step, int row)
        {
            if (!lockerSession) return false;

            if (panel == Panel.Grid && step.x > 0)
            {
                panel = Panel.Locker;
                lockerCursor = ClampToGrid(new Vector2Int(0, row), Model.Locker);
                return true;
            }

            if (panel == Panel.Locker && step.x < 0)
            {
                panel = Panel.Grid;
                gridCursor = ClampToGrid(new Vector2Int(Model.Grid.Width - 1, row), Model.Grid);
                return true;
            }

            return false;
        }

        private void CyclePanel()
        {
            if (mode == Mode.Holding)
            {
                if (lockerSession) panel = panel == Panel.Grid ? Panel.Locker : Panel.Grid;
                return;
            }

            panel = panel switch
            {
                Panel.Grid => lockerSession ? Panel.Locker : Panel.KeyItems,
                Panel.Locker => Panel.KeyItems,
                _ => Panel.Grid,
            };
            RefreshLayout();
        }

        private void BeginHold(ItemInstance item)
        {
            if (!ActiveGrid.TryGetPlacement(item, out GridPlacement placement)) return;

            heldItem = item;
            heldSource = ActiveGrid;
            heldRotated = placement.Rotated;
            heldOffset = ActiveCursor - placement.Position;
            mode = Mode.Holding;
            ActiveView.SetDimmedItem(item);
            infoPanel.ClearMessage();
        }

        private void RotateHeld()
        {
            if (!heldItem.Definition.CanRotate) return;

            heldRotated = !heldRotated;
            Vector2Int size = heldItem.Definition.GetSize(heldRotated);
            heldOffset = new Vector2Int(Mathf.Min(heldOffset.y, size.x - 1), Mathf.Min(heldOffset.x, size.y - 1));
        }

        private bool CanDropHeld()
        {
            InventoryGrid target = ActiveGrid;
            ItemInstance occupant = target.GetItemAt(ActiveCursor);
            if (occupant != null && occupant != heldItem && occupant.CanStackWith(heldItem) && occupant.StackSpace > 0) return true;

            return target == heldSource
                ? target.CanPlace(heldItem, HeldPosition, heldRotated)
                : target.CanPlace(heldItem.Definition, HeldPosition, heldRotated);
        }

        private void TryDropHeld()
        {
            InventoryGrid target = ActiveGrid;
            ItemInstance occupant = target.GetItemAt(ActiveCursor);
            if (occupant != null && occupant != heldItem && occupant.CanStackWith(heldItem) && target.MergeInto(heldItem, occupant) > 0)
            {
                EndHold();
                return;
            }

            bool placed = target == heldSource
                ? target.TryMove(heldItem, HeldPosition, heldRotated)
                : InventoryGrid.TryTransfer(heldSource, heldItem, target, HeldPosition, heldRotated);

            if (placed) EndHold();
        }

        private void EndHold()
        {
            gridView.SetDimmedItem(null);
            lockerView.SetDimmedItem(null);
            gridView.HideHeld();
            lockerView.HideHeld();

            heldItem = null;
            heldSource = null;
            dragging = false;
            if (mode == Mode.Holding) mode = Mode.Browse;
            infoDirty = true;
        }

        private void OpenMenu(ItemInstance item)
        {
            menuActions.Clear();
            menuLabels.Clear();

            ItemDefinition definition = item.Definition;
            if (Model.Grid.Contains(item))
            {
                if (definition.HasEffects) AddMenuAction(ItemAction.Use, useLabel);
                if (definition.IsEquippable)
                {
                    if (Model.IsEquipped(item)) AddMenuAction(ItemAction.Unequip, unequipLabel);
                    else AddMenuAction(ItemAction.Equip, equipLabel);
                }
                AddMenuAction(ItemAction.Drop, dropLabel);
            }
            AddMenuAction(ItemAction.Examine, examineLabel);

            Vector3 position = keyItemsView.transform.position;
            if (panel != Panel.KeyItems && ActiveGrid.TryGetPlacement(item, out GridPlacement placement))
                position = ActiveView.GetCellTopRightWorld(placement.Position, placement.Size);

            menuItem = item;
            mode = Mode.Menu;
            contextMenu.Open(menuLabels, position);
        }

        private void AddMenuAction(ItemAction action, string label)
        {
            menuActions.Add(action);
            menuLabels.Add(label);
        }

        private void CloseMenu()
        {
            contextMenu.Close();
            menuItem = null;
            if (mode == Mode.Menu) mode = Mode.Browse;
        }

        private void Execute(ItemAction action, ItemInstance item)
        {
            if (item == null) return;

            switch (action)
            {
                case ItemAction.Use:
                    if (!holder.Use(item)) infoPanel.ShowMessage(cannotUseMessage);
                    break;
                case ItemAction.Equip:
                    Model.Equip(item);
                    break;
                case ItemAction.Unequip:
                    Model.Unequip();
                    break;
                case ItemAction.Drop:
                    holder.Drop(item);
                    break;
                case ItemAction.Examine:
                    mode = Mode.Examine;
                    infoPanel.ShowExamine(item);
                    break;
            }

            infoDirty = true;
        }

        private void StopExamine()
        {
            infoPanel.HideExamine();
            mode = Mode.Browse;
        }

        private void RefreshLayout()
        {
            if (gridPanelRoot != null) gridPanelRoot.SetActive(panel != Panel.KeyItems);
            if (keyItemsRoot != null) keyItemsRoot.SetActive(panel == Panel.KeyItems);
            if (lockerRoot != null) lockerRoot.SetActive(lockerSession);
            infoDirty = true;
        }

        private void RefreshVisuals()
        {
            RefreshGridOverlay(gridView, Panel.Grid);
            if (lockerSession) RefreshGridOverlay(lockerView, Panel.Locker);

            ItemInstance focused = GetFocusedItem();
            if (focused == shownItem && !infoDirty) return;

            if (focused != shownItem) infoPanel.ClearMessage();
            shownItem = focused;
            infoDirty = false;
            infoPanel.Show(focused, Model.IsEquipped(focused));
        }

        private void RefreshGridOverlay(InventoryGridView view, Panel viewPanel)
        {
            if (panel != viewPanel)
            {
                view.HideCursor();
                view.HideHeld();
                return;
            }

            if (mode == Mode.Holding)
            {
                view.HideCursor();
                view.ShowHeld(heldItem, HeldPosition, heldRotated, CanDropHeld());
                return;
            }

            view.HideHeld();
            InventoryGrid grid = view.Grid;
            Vector2Int cursor = panel == Panel.Locker ? lockerCursor : gridCursor;
            ItemInstance under = grid.GetItemAt(cursor);
            if (under != null && grid.TryGetPlacement(under, out GridPlacement placement))
                view.ShowCursor(placement.Position, placement.Size);
            else
                view.ShowCursor(cursor, Vector2Int.one);
        }

        private ItemInstance GetFocusedItem()
        {
            if (mode == Mode.Holding) return heldItem;
            if (mode == Mode.Menu || mode == Mode.Examine) return menuItem ?? shownItem;
            if (panel == Panel.KeyItems) return keyItemsView.Selected;
            return ActiveGrid.GetItemAt(ActiveCursor);
        }

        private static Vector2Int ClampToGrid(Vector2Int cell, InventoryGrid grid) =>
            new Vector2Int(Mathf.Clamp(cell.x, 0, grid.Width - 1), Mathf.Clamp(cell.y, 0, grid.Height - 1));

        private void OnEquippedChanged(ItemInstance item)
        {
            gridView.Redraw();
            infoDirty = true;
        }

        private void MarkInfoDirty() => infoDirty = true;
    }
}
