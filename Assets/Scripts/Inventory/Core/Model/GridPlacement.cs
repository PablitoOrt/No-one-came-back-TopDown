using UnityEngine;

namespace NoOneCameBack.Inventory
{
    public readonly struct GridPlacement
    {
        public GridPlacement(ItemInstance item, Vector2Int position, bool rotated)
        {
            Item = item;
            Position = position;
            Rotated = rotated;
        }

        public ItemInstance Item { get; }
        public Vector2Int Position { get; }
        public bool Rotated { get; }

        public Vector2Int Size => Item.Definition.GetSize(Rotated);
        public RectInt Rect => new RectInt(Position, Size);

        public bool Contains(Vector2Int cell) => Rect.Contains(cell);
    }
}
