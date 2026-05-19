using System.Collections;
using UnityEngine;

namespace CrystalMind.MatchMancer
{
    [RequireComponent(typeof(TileVisualController))]
    public class Tile : MonoBehaviour
    {
        #region Variables

        [Header("Selection Settings")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField, Range(1f, 1.5f)] private float selectedScaleMultiplier = 1.15f;

        [Header("Normal Sprites")]
        [SerializeField] private Sprite redTileSprite;
        [SerializeField] private Sprite greenTileSprite;
        [SerializeField] private Sprite blueTileSprite;
        [SerializeField] private Sprite yellowTileSprite;
        [SerializeField] private Sprite purpleTileSprite;

        [Header("Line Horizontal Sprites")]
        [SerializeField] private Sprite redLineHorizontalSprite;
        [SerializeField] private Sprite greenLineHorizontalSprite;
        [SerializeField] private Sprite blueLineHorizontalSprite;
        [SerializeField] private Sprite yellowLineHorizontalSprite;
        [SerializeField] private Sprite purpleLineHorizontalSprite;

        [Header("Line Vertical Sprites")]
        [SerializeField] private Sprite redLineVerticalSprite;
        [SerializeField] private Sprite greenLineVerticalSprite;
        [SerializeField] private Sprite blueLineVerticalSprite;
        [SerializeField] private Sprite yellowLineVerticalSprite;
        [SerializeField] private Sprite purpleLineVerticalSprite;

        [Header("Bomb Sprites")]
        [SerializeField] private Sprite redBombSprite;
        [SerializeField] private Sprite greenBombSprite;
        [SerializeField] private Sprite blueBombSprite;
        [SerializeField] private Sprite yellowBombSprite;
        [SerializeField] private Sprite purpleBombSprite;

        [Header("Debug")]
        [SerializeField] private SpecialTileType specialType;

        [Header("References")]
        [SerializeField] private TileVisualController visualController;

        // Cache
        private static Sprite circleSprite;
        private static Sprite capsuleHorizontalSprite;
        private static Sprite capsuleVerticalSprite;
        private Sprite defaultSprite;
        private BoardManager boardManager;
        private Vector3 defaultScale;
        private bool hasLoggedMissingSpriteRenderer;

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
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (visualController == null)
            {
                visualController = GetComponent<TileVisualController>();
            }

            if (visualController == null)
            {
                visualController = gameObject.AddComponent<TileVisualController>();
            }

            visualController.SetTargetRenderer(spriteRenderer);

            defaultScale = transform.localScale;

            if (spriteRenderer != null)
            {
                defaultSprite = spriteRenderer.sprite;
            }

            EnsureRuntimeSpritesCreated();
        }

        #endregion

        #region Public Methods

        public void Init(int newRow, int newCol, TileType newType)
        {
            row = newRow;
            col = newCol;
            type = newType;
            specialType = SpecialTileType.None;

            visualController?.ResetVisualState();
            UpdateName();
            RefreshVisual();
            visualController?.SetSpecialState(false, false);
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
            bool wasSpecial = IsSpecial;
            specialType = newSpecialType;
            UpdateName();
            RefreshVisual();
            visualController?.SetSpecialState(IsSpecial, !wasSpecial && IsSpecial);
        }

        public void SetSelected(bool isSelected)
        {
            if (visualController != null)
            {
                visualController.SetSelected(isSelected);
                return;
            }

            transform.localScale = isSelected ? defaultScale * selectedScaleMultiplier : defaultScale;
        }

        public void RefreshVisual()
        {
            ApplyVisual();
            visualController?.CaptureCurrentRendererColor();
        }

        public IEnumerator PlayDestroyVisual()
        {
            if (visualController == null)
            {
                yield break;
            }

            yield return visualController.PlayDestroyAnimation();
        }

        public IEnumerator PlayIntroVisual(float duration, float startScale, float overshootScale, float endScale, float startDelay, bool useUnscaledTime)
        {
            if (visualController == null)
            {
                yield break;
            }

            yield return visualController.PlayIntroAnimation(duration, startScale, overshootScale, endScale, startDelay, useUnscaledTime);
        }

        public void SetVisualScaleMultiplier(float scaleMultiplier)
        {
            if (visualController != null)
            {
                visualController.SetScaleMultiplier(scaleMultiplier);
                return;
            }

            transform.localScale = defaultScale * Mathf.Max(0f, scaleMultiplier);
        }

        #endregion

        #region Private Methods

        private void ApplyVisual()
        {
            if (spriteRenderer == null)
            {
                if (!hasLoggedMissingSpriteRenderer)
                {
                    Debug.LogWarning($"{name}: SpriteRenderer is missing.");
                    hasLoggedMissingSpriteRenderer = true;
                }

                return;
            }

            Sprite mappedSprite = GetMappedSprite(type, specialType);

            if (mappedSprite != null)
            {
                spriteRenderer.sprite = mappedSprite;
                spriteRenderer.color = Color.white;
                return;
            }

            spriteRenderer.sprite = GetRuntimeFallbackSpriteBySpecialType(specialType);
            spriteRenderer.color = GetColorByType(type);
        }

        private void UpdateName()
        {
            gameObject.name = specialType == SpecialTileType.None
                ? $"Tile_{row}_{col}_{type}"
                : $"Tile_{row}_{col}_{type}_{specialType}";
        }

        private Sprite GetMappedSprite(TileType tileType, SpecialTileType tileSpecialType)
        {
            switch (tileSpecialType)
            {
                case SpecialTileType.LineHorizontal:
                    return GetLineHorizontalSprite(tileType) ?? GetNormalTileSprite(tileType);

                case SpecialTileType.LineVertical:
                    return GetLineVerticalSprite(tileType) ?? GetNormalTileSprite(tileType);

                case SpecialTileType.Bomb:
                    return GetBombSprite(tileType) ?? GetNormalTileSprite(tileType);

                default:
                    return GetNormalTileSprite(tileType);
            }
        }

        private Sprite GetNormalTileSprite(TileType tileType)
        {
            switch (tileType)
            {
                case TileType.Red:
                    return redTileSprite;

                case TileType.Green:
                    return greenTileSprite;

                case TileType.Blue:
                    return blueTileSprite;

                case TileType.Yellow:
                    return yellowTileSprite;

                case TileType.Purple:
                    return purpleTileSprite;

                default:
                    return null;
            }
        }

        private Sprite GetLineHorizontalSprite(TileType tileType)
        {
            switch (tileType)
            {
                case TileType.Red:
                    return redLineHorizontalSprite;

                case TileType.Green:
                    return greenLineHorizontalSprite;

                case TileType.Blue:
                    return blueLineHorizontalSprite;

                case TileType.Yellow:
                    return yellowLineHorizontalSprite;

                case TileType.Purple:
                    return purpleLineHorizontalSprite;

                default:
                    return null;
            }
        }

        private Sprite GetLineVerticalSprite(TileType tileType)
        {
            switch (tileType)
            {
                case TileType.Red:
                    return redLineVerticalSprite;

                case TileType.Green:
                    return greenLineVerticalSprite;

                case TileType.Blue:
                    return blueLineVerticalSprite;

                case TileType.Yellow:
                    return yellowLineVerticalSprite;

                case TileType.Purple:
                    return purpleLineVerticalSprite;

                default:
                    return null;
            }
        }

        private Sprite GetBombSprite(TileType tileType)
        {
            switch (tileType)
            {
                case TileType.Red:
                    return redBombSprite;

                case TileType.Green:
                    return greenBombSprite;

                case TileType.Blue:
                    return blueBombSprite;

                case TileType.Yellow:
                    return yellowBombSprite;

                case TileType.Purple:
                    return purpleBombSprite;

                default:
                    return null;
            }
        }

        private Sprite GetRuntimeFallbackSpriteBySpecialType(SpecialTileType tileSpecialType)
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
