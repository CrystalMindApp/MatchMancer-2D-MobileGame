using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CrystalMind.MatchMancer;
using UnityEditor;
using UnityEngine;

public class Match3SpecialTileQATool : EditorWindow
{
    #region Variables

    private const BindingFlags InstancePrivateFlags = BindingFlags.Instance | BindingFlags.NonPublic;

    [SerializeField] private BoardManager sceneBoardManager;
    [SerializeField] private bool useDiagnosticMode = true;

    private int passCount;
    private int failCount;

    #endregion

    #region Unity Methods

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Week 4 Special Tile QA", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        sceneBoardManager = (BoardManager)EditorGUILayout.ObjectField(
            "Scene Board Manager",
            sceneBoardManager,
            typeof(BoardManager),
            true);

        useDiagnosticMode = EditorGUILayout.ToggleLeft("Use non-destructive diagnostic board", useDiagnosticMode);

        EditorGUILayout.HelpBox(
            "Diagnostic mode creates hidden temporary boards and tiles, runs tests, then destroys them. " +
            "The scene BoardManager assignment is optional unless you want Test J to inspect that instance.",
            MessageType.Info);

        if (GUILayout.Button("Run Special Tile QA"))
        {
            RunAllTests();
        }
    }

    #endregion

    #region Public Methods

    [MenuItem("Tools/Match-3 QA/Special Tile QA")]
    public static void OpenWindow()
    {
        GetWindow<Match3SpecialTileQATool>("Special Tile QA");
    }

    #endregion

    #region Private Methods

    private void RunAllTests()
    {
        passCount = 0;
        failCount = 0;

        Debug.Log("=== Match-3 Special Tile QA: Start ===");

        RunTest("A. Special tile matching uses TileType, not SpecialTileType", TestSpecialTileMatchingUsesTileType);
        RunTest("B. Horizontal match 4 classification -> LineHorizontal", TestHorizontalMatch4Classification);
        RunTest("C. Vertical match 4 classification -> LineVertical", TestVerticalMatch4Classification);
        RunTest("D. Match 5 classification -> Bomb", TestMatch5Classification);
        RunTest("E. T/L shape classification -> Bomb", TestCornerShapeClassification);
        RunTest("F. Line horizontal/vertical clear target collection", TestLineClearTargetCollection);
        RunTest("G. Bomb 3x3 target collection", TestBombAreaTargetCollection);
        RunTest("H. Chain reaction target expansion", TestChainReactionTargetExpansion);
        RunTest("I. Duplicate clear prevention through HashSet<Tile>", TestDuplicateClearPrevention);
        RunTest("J. Resolve safety guard exists and is positive", TestResolveSafetyGuard);

        Debug.Log($"=== Match-3 Special Tile QA: Complete. Passed {passCount}, Failed {failCount}. ===");
    }

    private void RunTest(string testName, Func<TestResult> test)
    {
        TestResult result;

        try
        {
            result = test();
        }
        catch (Exception exception)
        {
            result = TestResult.Fail("No exception during test.", exception.ToString());
        }

        if (result.Passed)
        {
            passCount++;
            Debug.Log($"[PASS] {testName}\nExpected: {result.Expected}\nActual: {result.Actual}");
            return;
        }

        failCount++;
        Debug.LogError($"[FAIL] {testName}\nExpected: {result.Expected}\nActual: {result.Actual}");
    }

    private TestResult TestSpecialTileMatchingUsesTileType()
    {
        using (DiagnosticBoard board = CreateBoard(3, 3))
        {
            FillBoard(board, TileType.Blue);
            SetRowTypes(board, 1, TileType.Red, TileType.Red, TileType.Red);
            board.Tiles[1, 1].SetSpecialType(SpecialTileType.Bomb);

            List<MatchGroup> groups = new MatchFinder(board.BoardManager).FindMatchGroups();
            MatchGroup redGroup = groups.FirstOrDefault(group =>
                group.Count == 3 &&
                group.Tiles.All(tile => tile.Type == TileType.Red) &&
                group.Tiles.Contains(board.Tiles[1, 1]));

            return redGroup != null
                ? TestResult.Pass("One 3-tile Red match containing the Bomb tile.", DescribeGroups(groups))
                : TestResult.Fail("One 3-tile Red match containing the Bomb tile.", DescribeGroups(groups));
        }
    }

    private TestResult TestHorizontalMatch4Classification()
    {
        using (DiagnosticBoard board = CreateBoard(5, 5))
        {
            FillBoard(board, TileType.Blue);
            SetRowTypes(board, 2, TileType.Red, TileType.Red, TileType.Red, TileType.Red, TileType.Green);

            MatchGroup group = FindOnlyRelevantGroup(board, 4, true, false);
            SpecialTileType actual = ClassifyGroup(board.BoardManager, group);

            return actual == SpecialTileType.LineHorizontal
                ? TestResult.Pass("LineHorizontal.", actual.ToString())
                : TestResult.Fail("LineHorizontal.", actual.ToString());
        }
    }

    private TestResult TestVerticalMatch4Classification()
    {
        using (DiagnosticBoard board = CreateBoard(5, 5))
        {
            FillBoard(board, TileType.Blue);
            SetColumnTypes(board, 2, TileType.Red, TileType.Red, TileType.Red, TileType.Red, TileType.Green);

            MatchGroup group = FindOnlyRelevantGroup(board, 4, false, true);
            SpecialTileType actual = ClassifyGroup(board.BoardManager, group);

            return actual == SpecialTileType.LineVertical
                ? TestResult.Pass("LineVertical.", actual.ToString())
                : TestResult.Fail("LineVertical.", actual.ToString());
        }
    }

    private TestResult TestMatch5Classification()
    {
        using (DiagnosticBoard board = CreateBoard(5, 5))
        {
            FillBoard(board, TileType.Blue);
            SetRowTypes(board, 2, TileType.Red, TileType.Red, TileType.Red, TileType.Red, TileType.Red);

            MatchGroup group = FindOnlyRelevantGroup(board, 5, true, false);
            SpecialTileType actual = ClassifyGroup(board.BoardManager, group);

            return actual == SpecialTileType.Bomb
                ? TestResult.Pass("Bomb.", actual.ToString())
                : TestResult.Fail("Bomb.", actual.ToString());
        }
    }

    private TestResult TestCornerShapeClassification()
    {
        using (DiagnosticBoard board = CreateBoard(5, 5))
        {
            FillBoard(board, TileType.Blue);
            SetTileType(board, 2, 1, TileType.Red);
            SetTileType(board, 2, 2, TileType.Red);
            SetTileType(board, 2, 3, TileType.Red);
            SetTileType(board, 1, 2, TileType.Red);
            SetTileType(board, 0, 2, TileType.Red);

            MatchGroup group = FindOnlyRelevantGroup(board, 5, true, true);
            SpecialTileType actual = ClassifyGroup(board.BoardManager, group);

            return actual == SpecialTileType.Bomb
                ? TestResult.Pass("Bomb for combined horizontal and vertical group.", actual.ToString())
                : TestResult.Fail("Bomb for combined horizontal and vertical group.", actual.ToString());
        }
    }

    private TestResult TestLineClearTargetCollection()
    {
        using (DiagnosticBoard board = CreateBoard(5, 5))
        {
            FillBoard(board, TileType.Red);

            HashSet<Tile> horizontalTargets = new HashSet<Tile>();
            HashSet<Tile> horizontalActivated = new HashSet<Tile>();
            InvokePrivate(board.BoardManager, "AddRowToClear", 2, horizontalTargets, horizontalActivated);

            HashSet<Tile> verticalTargets = new HashSet<Tile>();
            HashSet<Tile> verticalActivated = new HashSet<Tile>();
            InvokePrivate(board.BoardManager, "AddColumnToClear", 3, verticalTargets, verticalActivated);

            bool passed = horizontalTargets.Count == board.Cols &&
                verticalTargets.Count == board.Rows &&
                horizontalTargets.All(tile => tile.Row == 2) &&
                verticalTargets.All(tile => tile.Col == 3);

            string actual = $"Horizontal row targets: {horizontalTargets.Count}; Vertical column targets: {verticalTargets.Count}.";

            return passed
                ? TestResult.Pass("Horizontal line clears 5 row tiles; vertical line clears 5 column tiles.", actual)
                : TestResult.Fail("Horizontal line clears 5 row tiles; vertical line clears 5 column tiles.", actual);
        }
    }

    private TestResult TestBombAreaTargetCollection()
    {
        using (DiagnosticBoard board = CreateBoard(5, 5))
        {
            FillBoard(board, TileType.Red);

            HashSet<Tile> targets = new HashSet<Tile>();
            HashSet<Tile> activated = new HashSet<Tile>();
            InvokePrivate(board.BoardManager, "AddAreaToClear", 2, 2, 1, targets, activated);

            bool passed = targets.Count == 9 &&
                targets.All(tile => Mathf.Abs(tile.Row - 2) <= 1 && Mathf.Abs(tile.Col - 2) <= 1);

            return passed
                ? TestResult.Pass("Nine unique tiles in a 3x3 area around [2,2].", DescribeTiles(targets))
                : TestResult.Fail("Nine unique tiles in a 3x3 area around [2,2].", DescribeTiles(targets));
        }
    }

    private TestResult TestChainReactionTargetExpansion()
    {
        using (DiagnosticBoard board = CreateBoard(5, 5))
        {
            FillBoard(board, TileType.Red);
            board.Tiles[2, 1].SetSpecialType(SpecialTileType.LineHorizontal);
            board.Tiles[2, 3].SetSpecialType(SpecialTileType.LineVertical);

            HashSet<Tile> targets = new HashSet<Tile>();
            HashSet<Tile> activated = new HashSet<Tile>();
            InvokePrivate(board.BoardManager, "ActivateSpecialTileIfNeeded", board.Tiles[2, 1], targets, activated);

            bool hasFullRow = Enumerable.Range(0, board.Cols).All(col => targets.Contains(board.Tiles[2, col]));
            bool hasFullColumn = Enumerable.Range(0, board.Rows).All(row => targets.Contains(board.Tiles[row, 3]));
            bool passed = hasFullRow && hasFullColumn && activated.Count == 2;

            string actual = $"Targets: {targets.Count}; Activated specials: {activated.Count}; Full row: {hasFullRow}; Full column: {hasFullColumn}.";

            return passed
                ? TestResult.Pass("LineHorizontal activates LineVertical when it is included in the row clear.", actual)
                : TestResult.Fail("LineHorizontal activates LineVertical when it is included in the row clear.", actual);
        }
    }

    private TestResult TestDuplicateClearPrevention()
    {
        using (DiagnosticBoard board = CreateBoard(5, 5))
        {
            FillBoard(board, TileType.Red);

            HashSet<Tile> targets = new HashSet<Tile>();
            HashSet<Tile> activated = new HashSet<Tile>();
            InvokePrivate(board.BoardManager, "AddRowToClear", 2, targets, activated);
            InvokePrivate(board.BoardManager, "AddColumnToClear", 2, targets, activated);

            bool passed = targets.Count == 9;

            return passed
                ? TestResult.Pass("Row plus column crossing on a 5x5 board produces 9 unique tiles, not 10.", DescribeTiles(targets))
                : TestResult.Fail("Row plus column crossing on a 5x5 board produces 9 unique tiles, not 10.", DescribeTiles(targets));
        }
    }

    private TestResult TestResolveSafetyGuard()
    {
        BoardManager managerToInspect = sceneBoardManager;
        bool usedSceneManager = managerToInspect != null && !useDiagnosticMode;

        using (DiagnosticBoard board = usedSceneManager ? null : CreateBoard(3, 3))
        {
            if (!usedSceneManager)
            {
                managerToInspect = board.BoardManager;
            }

            FieldInfo field = GetPrivateField("maxResolveLoops");
            int value = (int)field.GetValue(managerToInspect);

            return value > 0
                ? TestResult.Pass("maxResolveLoops exists and is greater than zero.", $"maxResolveLoops = {value}.")
                : TestResult.Fail("maxResolveLoops exists and is greater than zero.", $"maxResolveLoops = {value}.");
        }
    }

    private DiagnosticBoard CreateBoard(int rows, int cols)
    {
        GameObject boardObject = new GameObject("Match3_SpecialTileQA_Board");
        boardObject.hideFlags = HideFlags.HideAndDontSave;

        BoardManager boardManager = boardObject.AddComponent<BoardManager>();
        SetPrivateField(boardManager, "rows", rows);
        SetPrivateField(boardManager, "cols", cols);

        Tile[,] tiles = new Tile[rows, cols];
        SetPrivateField(boardManager, "boardTiles", tiles);

        DiagnosticBoard board = new DiagnosticBoard(boardObject, boardManager, tiles, rows, cols);

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                GameObject tileObject = new GameObject($"QA_Tile_{row}_{col}");
                tileObject.hideFlags = HideFlags.HideAndDontSave;
                tileObject.transform.SetParent(boardObject.transform);
                tileObject.AddComponent<SpriteRenderer>();

                Tile tile = tileObject.AddComponent<Tile>();
                tile.Init(row, col, TileType.Red);
                tiles[row, col] = tile;
            }
        }

        return board;
    }

    private void FillBoard(DiagnosticBoard board, TileType defaultType)
    {
        TileType[] pattern =
        {
            TileType.Red,
            TileType.Blue,
            TileType.Green,
            TileType.Yellow,
            TileType.Purple
        };

        for (int row = 0; row < board.Rows; row++)
        {
            for (int col = 0; col < board.Cols; col++)
            {
                TileType type = pattern[(row * 2 + col * 3) % pattern.Length];
                board.Tiles[row, col].Init(row, col, type == defaultType ? TileType.Purple : type);
            }
        }
    }

    private void SetRowTypes(DiagnosticBoard board, int row, params TileType[] types)
    {
        for (int col = 0; col < types.Length; col++)
        {
            SetTileType(board, row, col, types[col]);
        }
    }

    private void SetColumnTypes(DiagnosticBoard board, int col, params TileType[] types)
    {
        for (int row = 0; row < types.Length; row++)
        {
            SetTileType(board, row, col, types[row]);
        }
    }

    private void SetTileType(DiagnosticBoard board, int row, int col, TileType type)
    {
        board.Tiles[row, col].Init(row, col, type);
    }

    private MatchGroup FindOnlyRelevantGroup(DiagnosticBoard board, int expectedCount, bool expectedHorizontal, bool expectedVertical)
    {
        List<MatchGroup> groups = new MatchFinder(board.BoardManager).FindMatchGroups();

        return groups.FirstOrDefault(group =>
            group.Count == expectedCount &&
            group.HasHorizontalMatch == expectedHorizontal &&
            group.HasVerticalMatch == expectedVertical);
    }

    private SpecialTileType ClassifyGroup(BoardManager boardManager, MatchGroup group)
    {
        if (group == null)
        {
            return SpecialTileType.None;
        }

        return (SpecialTileType)InvokePrivate(boardManager, "ClassifySpecialTileType", group);
    }

    private object InvokePrivate(BoardManager boardManager, string methodName, params object[] parameters)
    {
        MethodInfo method = typeof(BoardManager).GetMethod(methodName, InstancePrivateFlags);

        if (method == null)
        {
            throw new MissingMethodException(typeof(BoardManager).Name, methodName);
        }

        return method.Invoke(boardManager, parameters);
    }

    private void SetPrivateField(BoardManager boardManager, string fieldName, object value)
    {
        GetPrivateField(fieldName).SetValue(boardManager, value);
    }

    private FieldInfo GetPrivateField(string fieldName)
    {
        FieldInfo field = typeof(BoardManager).GetField(fieldName, InstancePrivateFlags);

        if (field == null)
        {
            throw new MissingFieldException(typeof(BoardManager).Name, fieldName);
        }

        return field;
    }

    private string DescribeGroups(List<MatchGroup> groups)
    {
        if (groups == null || groups.Count == 0)
        {
            return "No groups found.";
        }

        return string.Join("; ", groups.Select(group =>
            $"Count={group.Count}, H={group.LongestHorizontalLength}, V={group.LongestVerticalLength}, Tiles={DescribeTiles(group.Tiles)}"));
    }

    private string DescribeTiles(IEnumerable<Tile> tiles)
    {
        return string.Join(", ", tiles
            .OrderBy(tile => tile.Row)
            .ThenBy(tile => tile.Col)
            .Select(tile => $"[{tile.Row},{tile.Col}:{tile.Type}/{tile.SpecialType}]"));
    }

    private readonly struct TestResult
    {
        public readonly bool Passed;
        public readonly string Expected;
        public readonly string Actual;

        private TestResult(bool passed, string expected, string actual)
        {
            Passed = passed;
            Expected = expected;
            Actual = actual;
        }

        public static TestResult Pass(string expected, string actual)
        {
            return new TestResult(true, expected, actual);
        }

        public static TestResult Fail(string expected, string actual)
        {
            return new TestResult(false, expected, actual);
        }
    }

    private sealed class DiagnosticBoard : IDisposable
    {
        public readonly GameObject Root;
        public readonly BoardManager BoardManager;
        public readonly Tile[,] Tiles;
        public readonly int Rows;
        public readonly int Cols;

        public DiagnosticBoard(GameObject root, BoardManager boardManager, Tile[,] tiles, int rows, int cols)
        {
            Root = root;
            BoardManager = boardManager;
            Tiles = tiles;
            Rows = rows;
            Cols = cols;
        }

        public void Dispose()
        {
            if (Root != null)
            {
                DestroyImmediate(Root);
            }
        }
    }

    #endregion
}
