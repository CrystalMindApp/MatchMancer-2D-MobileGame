using UnityEngine;

namespace CrystalMind.MatchMancer
{
    public class Tile : MonoBehaviour
    {
        #region Variables

        [Header("Selection Settings")]
        [SerializeField, Range(1f, 1.5f)] private float selectedScaleMultiplier = 1.15f;

        [Header("Debug")]
        [SerializeField] private SpecialTileType specialType;

        // Cache
        private static Sprite circleSprite;
        private static Sprite capsuleHorizontalSprite;
        private static Sprite capsuleVerticalSprite;
        private SpriteRenderer spriteRenderer;
        private Sprite defaultSprite;
        private BoardManager boardManager;
        private Vector3 defaultScale;

        // State
        private int row;
        private int col;
        private TileType type;

        #endregion

        #region Properties

        public int Row => row;
        public int Col => col;
        public TileType Type => type;
        public SpecialTileType SpecialType => specialType;
        public bool IsSpecial => specialType != SpecialTileType.None;

        #endregion

        #region Unity Methods

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            defaultScale = transform.localScale;

            if (spriteRenderer != null)
            {
                defaultSprite = spriteRenderer.sprite;
            }

            EnsureRuntimeSpritesCreated();
        }

        private void Start()
        {
        }

        private void Update()
        {
        }

        #endregion

        #region Public Methods

        public void Init(int newRow, int newCol, TileType newType)
        {
            row = newRow;
            col = newCol;
            type = newType;
            specialType = SpecialTileType.None;

            UpdateName();
            ApplyVisual();
            SetSelected(false);
        }

        public void SetBoardManager(BoardManager manager)
        {
            boardManager = manager;
        }

        public void SetCoordinate(int newRow, int newCol)
        {
            row = newRow;
            col = newCol;
            UpdateName();
        }

        public void SetSpecialType(SpecialTileType newSpecialType)
        {
            specialType = newSpecialType;
            UpdateName();
            ApplyVisual();
        }

        public void SetSelected(bool isSelected)
        {
            transform.localScale = isSelected ? defaultScale * selectedScaleMultiplier : defaultScale;
        }

        #endregion

        #region Protected Methods

        #endregion

        #region Private Methods

        private void ApplyVisual()
        {
            if (spriteRenderer == null)
            {
                Debug.LogError($"{name}: SpriteRenderer is missing.");
                return;
            }

            spriteRenderer.sprite = GetSpriteBySpecialType(specialType);
            spriteRenderer.color = GetColorByType(type);
        }

        private void UpdateName()
        {
            gameObject.name = specialType == SpecialTileType.None
                ? $"Tile_{row}_{col}_{type}"
                : $"Tile_{row}_{col}_{type}_{specialType}";
        }

        private Sprite GetSpriteBySpecialType(SpecialTileType tileSpecialType)
        {
            switch (tileSpecialType)
            {
                case SpecialTileType.LineHorizontal:
                    return capsuleHorizontalSprite != null ? capsuleHorizontalSprite : defaultSprite;

                case SpecialTileType.LineVertical:
                    return capsuleVerticalSprite != null ? capsuleVerticalSprite : defaultSprite;

                case SpecialTileType.Bomb:
                    return circleSprite != null ? circleSprite : defaultSprite;

                default:
                    return defaultSprite;
            }
        }

        private Color GetColorByType(TileType tileType)
        {
            switch (tileType)
            {
                case TileType.Red:
                    return Color.red;

                case TileType.Blue:
                    return Color.blue;

                case TileType.Green:
                    return Color.green;

                case TileType.Yellow:
                    return Color.yellow;

                case TileType.Purple:
                    return Color.magenta;

                default:
                    return Color.white;
            }
        }

        private void EnsureRuntimeSpritesCreated()
        {
            if (circleSprite == null)
            {
                circleSprite = CreateCircleSprite(64, "Runtime_Circle_Sprite");
            }

            if (capsuleHorizontalSprite == null)
            {
                capsuleHorizontalSprite = CreateCapsuleSprite(64, 64, true, "Runtime_Capsule_Horizontal_Sprite");
            }

            if (capsuleVerticalSprite == null)
            {
                capsuleVerticalSprite = CreateCapsuleSprite(64, 64, false, "Runtime_Capsule_Vertical_Sprite");
            }
        }

        private Sprite CreateCircleSprite(int size, string spriteName)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
            texture.name = spriteName;
            texture.filterMode = FilterMode.Point;

            float radius = size * 0.45f;
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    texture.SetPixel(x, y, distance <= radius ? Color.white : Color.clear);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private Sprite CreateCapsuleSprite(int width, int height, bool horizontal, string spriteName)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
            texture.name = spriteName;
            texture.filterMode = FilterMode.Point;

            float halfThickness = horizontal ? height * 0.18f : width * 0.18f;
            float radius = halfThickness;
            Vector2 firstCenter = horizontal
                ? new Vector2(width * 0.28f, height * 0.5f)
                : new Vector2(width * 0.5f, height * 0.28f);
            Vector2 secondCenter = horizontal
                ? new Vector2(width * 0.72f, height * 0.5f)
                : new Vector2(width * 0.5f, height * 0.72f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 point = new Vector2(x, y);
                    bool inMiddle = horizontal
                        ? x >= firstCenter.x && x <= secondCenter.x && Mathf.Abs(y - height * 0.5f) <= halfThickness
                        : y >= firstCenter.y && y <= secondCenter.y && Mathf.Abs(x - width * 0.5f) <= halfThickness;
                    bool inCaps = Vector2.Distance(point, firstCenter) <= radius || Vector2.Distance(point, secondCenter) <= radius;

                    texture.SetPixel(x, y, inMiddle || inCaps ? Color.white : Color.clear);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), width);
        }

        #endregion
    }
}
