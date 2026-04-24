using System;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// A rectangular ground area that can be painted by two teams.
/// The area is divided into a 2D grid on the local XZ plane.
/// </summary>
[DisallowMultipleComponent]
public class PaintableArea : MonoBehaviour
{
    [Header("Area Settings")]
    [SerializeField] private Vector2 areaSize = new Vector2(20f, 20f);
    [SerializeField, Min(1)] private int gridWidth = 50;
    [SerializeField, Min(1)] private int gridHeight = 50;
    [SerializeField] private bool resetOnAwake = true;

    [Header("Debug Colors")]
    [SerializeField] private Color unpaintedGizmoColor = new Color(1f, 1f, 1f, 0.08f);
    [SerializeField] private Color teamAGizmoColor = new Color(0.1f, 0.45f, 1f, 0.55f);
    [SerializeField] private Color teamBGizmoColor = new Color(1f, 0.45f, 0.05f, 0.55f);

    [Header("Gizmos")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private bool drawOnlyWhenSelected = true;
    [SerializeField] private bool drawPaintedCells = true;
    [SerializeField] private bool drawUnpaintedCells = false;
    [SerializeField] private bool drawGridLines = false;
    [SerializeField, Range(0.1f, 1f)] private float cellFillScale = 0.92f;
    [SerializeField] private float gizmoYOffset = 0.03f;

    [Header("Runtime Overlay")]
    [SerializeField] private bool showRuntimeOverlay = true;
    [SerializeField] private float runtimeOverlayYOffset = 0.02f;
    [SerializeField] private Color teamAOverlayColor = new Color(0.1f, 0.45f, 1f, 0.9f);
    [SerializeField] private Color teamBOverlayColor = new Color(1f, 0.45f, 0.05f, 0.9f);
    [SerializeField] private Color unpaintedOverlayColor = new Color(0f, 0f, 0f, 0f);

    [Header("Context Menu Test")]
    [SerializeField, Min(0.1f)] private float debugPaintRadius = 2f;

    [SerializeField, HideInInspector] private PaintGridCell[] cells;
    [SerializeField, HideInInspector] private int teamACellCount;
    [SerializeField, HideInInspector] private int teamBCellCount;

    public event Action<PaintableArea> PaintChanged;

    public Vector2 AreaSize => areaSize;
    public int GridWidth => gridWidth;
    public int GridHeight => gridHeight;
    public int TotalCellCount => gridWidth * gridHeight;

    private float CellWidth => areaSize.x / gridWidth;
    private float CellHeight => areaSize.y / gridHeight;

    private const string RuntimeOverlayName = "RuntimePaintOverlay";

    private Texture2D runtimeOverlayTexture;
    private Material runtimeOverlayMaterial;
    private MeshRenderer runtimeOverlayRenderer;
    private Transform runtimeOverlayTransform;
    private static Mesh runtimeOverlayMesh;
    private bool runtimeOverlayReady;

    private void Awake()
    {
        if (resetOnAwake)
        {
            InitializeGrid();
        }
        else
        {
            EnsureGrid();
            RecalculateCounts();
        }
    }

    private void Start()
    {
        SetupRuntimeOverlay();
        runtimeOverlayReady = runtimeOverlayRenderer != null;
        RefreshRuntimeOverlay();
    }

    private void OnValidate()
    {
        ClampSettings();
        EnsureGrid();
        RecalculateCounts();

        if (Application.isPlaying && runtimeOverlayReady)
        {
            UpdateOverlayTransform();
            RefreshRuntimeOverlay();
        }
    }

    private void OnDestroy()
    {
        if (runtimeOverlayTexture != null)
        {
            Destroy(runtimeOverlayTexture);
        }

        if (runtimeOverlayMaterial != null)
        {
            Destroy(runtimeOverlayMaterial);
        }

        runtimeOverlayReady = false;
    }

    /// <summary>
    /// Paints all cells inside radius from the given world position.
    /// Returns how many cells changed owner.
    /// </summary>
    public int PaintAtWorldPosition(Vector3 worldPosition, float radius, Team team)
    {
        if (team == Team.None || radius <= 0f)
        {
            return 0;
        }

        EnsureGrid();

        if (!ContainsWorldPosition(worldPosition))
        {
            return 0;
        }

        Vector3 localHit = transform.InverseTransformPoint(worldPosition);
        float scaleX = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.x));
        float scaleZ = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.z));
        float localRadiusX = radius / scaleX;
        float localRadiusZ = radius / scaleZ;

        int minX = LocalXToCellIndexClamped(localHit.x - localRadiusX);
        int maxX = LocalXToCellIndexClamped(localHit.x + localRadiusX);
        int minY = LocalZToCellIndexClamped(localHit.z - localRadiusZ);
        int maxY = LocalZToCellIndexClamped(localHit.z + localRadiusZ);

        float radiusSqr = radius * radius;
        int changedCount = 0;

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                Vector3 localCellCenter = GetCellCenterLocal(x, y);
                float dxWorld = (localCellCenter.x - localHit.x) * scaleX;
                float dzWorld = (localCellCenter.z - localHit.z) * scaleZ;

                if (dxWorld * dxWorld + dzWorld * dzWorld > radiusSqr)
                {
                    continue;
                }

                if (SetCellOwner(x, y, team))
                {
                    changedCount++;
                }
            }
        }

        if (changedCount > 0)
        {
            RefreshRuntimeOverlay();
            PaintChanged?.Invoke(this);
        }

        return changedCount;
    }

    /// <summary>
    /// Returns the current coverage percentage for a team.
    /// The denominator includes unpainted cells, which matches territory-control scoring.
    /// </summary>
    public float GetCoveragePercent(Team team)
    {
        int total = TotalCellCount;

        if (total <= 0)
        {
            return 0f;
        }

        return GetCellCount(team) * 100f / total;
    }

    public int GetCellCount(Team team)
    {
        switch (team)
        {
            case Team.TeamA:
                return teamACellCount;
            case Team.TeamB:
                return teamBCellCount;
            case Team.None:
                return TotalCellCount - teamACellCount - teamBCellCount;
            default:
                return 0;
        }
    }

    public Team GetCellOwner(int x, int y)
    {
        if (!IsValidCell(x, y))
        {
            return Team.None;
        }

        EnsureGrid();
        return cells[ToIndex(x, y)].Owner;
    }

    public Vector3 GetCellCenterWorld(int x, int y)
    {
        return transform.TransformPoint(GetCellCenterLocal(x, y));
    }

    public bool TryGetCellAtWorldPosition(Vector3 worldPosition, out int x, out int y)
    {
        x = -1;
        y = -1;

        if (!ContainsWorldPosition(worldPosition))
        {
            return false;
        }

        Vector3 local = transform.InverseTransformPoint(worldPosition);
        x = LocalXToCellIndexClamped(local.x);
        y = LocalZToCellIndexClamped(local.z);
        return true;
    }

    public bool ContainsWorldPosition(Vector3 worldPosition)
    {
        Vector3 local = transform.InverseTransformPoint(worldPosition);
        float halfX = areaSize.x * 0.5f;
        float halfZ = areaSize.y * 0.5f;

        return local.x >= -halfX
            && local.x <= halfX
            && local.z >= -halfZ
            && local.z <= halfZ;
    }

    public void ClearPaint()
    {
        EnsureGrid();

        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].Clear();
        }

        teamACellCount = 0;
        teamBCellCount = 0;
        RefreshRuntimeOverlay();
        PaintChanged?.Invoke(this);
    }

    public void InitializeGrid()
    {
        ClampSettings();

        cells = new PaintGridCell[TotalCellCount];

        for (int i = 0; i < cells.Length; i++)
        {
            cells[i] = new PaintGridCell();
        }

        teamACellCount = 0;
        teamBCellCount = 0;
        RefreshRuntimeOverlay();
        PaintChanged?.Invoke(this);
    }

    private bool SetCellOwner(int x, int y, Team newOwner)
    {
        int index = ToIndex(x, y);
        Team oldOwner = cells[index].Owner;

        if (oldOwner == newOwner)
        {
            return false;
        }

        DecreaseCount(oldOwner);
        cells[index].SetOwner(newOwner);
        IncreaseCount(newOwner);
        return true;
    }

    private void IncreaseCount(Team team)
    {
        if (team == Team.TeamA)
        {
            teamACellCount++;
        }
        else if (team == Team.TeamB)
        {
            teamBCellCount++;
        }
    }

    private void DecreaseCount(Team team)
    {
        if (team == Team.TeamA)
        {
            teamACellCount = Mathf.Max(0, teamACellCount - 1);
        }
        else if (team == Team.TeamB)
        {
            teamBCellCount = Mathf.Max(0, teamBCellCount - 1);
        }
    }

    private void RecalculateCounts()
    {
        teamACellCount = 0;
        teamBCellCount = 0;

        if (cells == null)
        {
            return;
        }

        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i] == null)
            {
                continue;
            }

            IncreaseCount(cells[i].Owner);
        }
    }

    private void EnsureGrid()
    {
        ClampSettings();
        int expectedCellCount = TotalCellCount;

        if (cells == null || cells.Length != expectedCellCount)
        {
            cells = new PaintGridCell[expectedCellCount];
        }

        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i] == null)
            {
                cells[i] = new PaintGridCell();
            }
        }
    }

    private void ClampSettings()
    {
        areaSize.x = Mathf.Max(0.1f, areaSize.x);
        areaSize.y = Mathf.Max(0.1f, areaSize.y);
        gridWidth = Mathf.Max(1, gridWidth);
        gridHeight = Mathf.Max(1, gridHeight);
    }

    private bool IsValidCell(int x, int y)
    {
        return x >= 0 && x < gridWidth && y >= 0 && y < gridHeight;
    }

    private int ToIndex(int x, int y)
    {
        return y * gridWidth + x;
    }

    private Vector3 GetCellCenterLocal(int x, int y)
    {
        float halfX = areaSize.x * 0.5f;
        float halfZ = areaSize.y * 0.5f;
        float centerX = -halfX + (x + 0.5f) * CellWidth;
        float centerZ = -halfZ + (y + 0.5f) * CellHeight;

        return new Vector3(centerX, 0f, centerZ);
    }

    private Vector3 GetCellCenterLocalForGizmo(int x, int y)
    {
        Vector3 center = GetCellCenterLocal(x, y);
        center.y += gizmoYOffset;
        return center;
    }

    private int LocalXToCellIndexClamped(float localX)
    {
        float halfX = areaSize.x * 0.5f;
        float normalized = (localX + halfX) / areaSize.x;
        int index = Mathf.FloorToInt(normalized * gridWidth);
        return Mathf.Clamp(index, 0, gridWidth - 1);
    }

    private int LocalZToCellIndexClamped(float localZ)
    {
        float halfZ = areaSize.y * 0.5f;
        float normalized = (localZ + halfZ) / areaSize.y;
        int index = Mathf.FloorToInt(normalized * gridHeight);
        return Mathf.Clamp(index, 0, gridHeight - 1);
    }

    private void SetupRuntimeOverlay()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        runtimeOverlayTransform = transform.Find(RuntimeOverlayName);

        if (runtimeOverlayTransform == null)
        {
            GameObject overlayObject = new GameObject(RuntimeOverlayName);
            overlayObject.name = RuntimeOverlayName;
            overlayObject.transform.SetParent(transform, false);
            overlayObject.layer = gameObject.layer;

            MeshFilter meshFilter = overlayObject.AddComponent<MeshFilter>();
            runtimeOverlayRenderer = overlayObject.AddComponent<MeshRenderer>();
            meshFilter.sharedMesh = GetOrCreateRuntimeOverlayMesh();
            runtimeOverlayTransform = overlayObject.transform;
        }
        else
        {
            runtimeOverlayRenderer = runtimeOverlayTransform.GetComponent<MeshRenderer>();

            MeshFilter meshFilter = runtimeOverlayTransform.GetComponent<MeshFilter>();

            if (meshFilter == null)
            {
                meshFilter = runtimeOverlayTransform.gameObject.AddComponent<MeshFilter>();
            }

            meshFilter.sharedMesh = GetOrCreateRuntimeOverlayMesh();
        }

        UpdateOverlayTransform();

        if (runtimeOverlayRenderer == null)
        {
            return;
        }

        runtimeOverlayRenderer.shadowCastingMode = ShadowCastingMode.Off;
        runtimeOverlayRenderer.receiveShadows = false;
        runtimeOverlayRenderer.lightProbeUsage = LightProbeUsage.Off;
        runtimeOverlayRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

        if (runtimeOverlayMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");

            if (shader == null)
            {
                shader = Shader.Find("Unlit/Transparent");
            }

            runtimeOverlayMaterial = new Material(shader);
            runtimeOverlayMaterial.name = "MAT_RuntimePaintOverlay_Instance";

            if (runtimeOverlayMaterial.HasProperty("_Cull"))
            {
                runtimeOverlayMaterial.SetFloat("_Cull", (float)CullMode.Off);
            }
        }

        runtimeOverlayRenderer.sharedMaterial = runtimeOverlayMaterial;
        runtimeOverlayRenderer.enabled = showRuntimeOverlay;
    }

    private void UpdateOverlayTransform()
    {
        if (runtimeOverlayTransform == null)
        {
            return;
        }

        runtimeOverlayTransform.localPosition = new Vector3(0f, runtimeOverlayYOffset, 0f);
        runtimeOverlayTransform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        runtimeOverlayTransform.localScale = new Vector3(areaSize.x, areaSize.y, 1f);
    }

    private void RefreshRuntimeOverlay()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (!runtimeOverlayReady)
        {
            return;
        }

        if (!showRuntimeOverlay)
        {
            if (runtimeOverlayRenderer != null)
            {
                runtimeOverlayRenderer.enabled = false;
            }

            return;
        }

        EnsureGrid();
        runtimeOverlayRenderer.enabled = true;

        if (runtimeOverlayTexture == null || runtimeOverlayTexture.width != gridWidth || runtimeOverlayTexture.height != gridHeight)
        {
            if (runtimeOverlayTexture != null)
            {
                Destroy(runtimeOverlayTexture);
            }

            runtimeOverlayTexture = new Texture2D(gridWidth, gridHeight, TextureFormat.RGBA32, false);
            runtimeOverlayTexture.name = "RuntimePaintOverlayTexture";
            runtimeOverlayTexture.filterMode = FilterMode.Point;
            runtimeOverlayTexture.wrapMode = TextureWrapMode.Clamp;
        }

        Color32[] pixels = new Color32[TotalCellCount];

        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                Team owner = cells[ToIndex(x, y)].Owner;
                pixels[ToIndex(x, y)] = GetOverlayColor(owner);
            }
        }

        runtimeOverlayTexture.SetPixels32(pixels);
        runtimeOverlayTexture.Apply(false, false);
        runtimeOverlayMaterial.mainTexture = runtimeOverlayTexture;
        runtimeOverlayMaterial.color = Color.white;
    }

    private static Mesh GetOrCreateRuntimeOverlayMesh()
    {
        if (runtimeOverlayMesh != null)
        {
            return runtimeOverlayMesh;
        }

        runtimeOverlayMesh = new Mesh();
        runtimeOverlayMesh.name = "RuntimeOverlayQuad";

        runtimeOverlayMesh.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3(0.5f, -0.5f, 0f),
            new Vector3(-0.5f, 0.5f, 0f),
            new Vector3(0.5f, 0.5f, 0f)
        };

        runtimeOverlayMesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        };

        runtimeOverlayMesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        runtimeOverlayMesh.normals = new[]
        {
            Vector3.forward,
            Vector3.forward,
            Vector3.forward,
            Vector3.forward
        };

        runtimeOverlayMesh.RecalculateBounds();
        return runtimeOverlayMesh;
    }

    private Color32 GetOverlayColor(Team team)
    {
        switch (team)
        {
            case Team.TeamA:
                return teamAOverlayColor;
            case Team.TeamB:
                return teamBOverlayColor;
            default:
                return unpaintedOverlayColor;
        }
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos || drawOnlyWhenSelected)
        {
            return;
        }

        DrawAreaGizmos();
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos || !drawOnlyWhenSelected)
        {
            return;
        }

        DrawAreaGizmos();
    }

    private void DrawAreaGizmos()
    {
        EnsureGrid();

        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;

        DrawAreaBounds();

        if (drawGridLines)
        {
            DrawGridLines();
        }

        if (drawPaintedCells || drawUnpaintedCells)
        {
            DrawCellFills();
        }

        Gizmos.matrix = previousMatrix;
    }

    private void DrawAreaBounds()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3(0f, gizmoYOffset, 0f);
        Vector3 size = new Vector3(areaSize.x, 0.01f, areaSize.y);
        Gizmos.DrawWireCube(center, size);
    }

    private void DrawGridLines()
    {
        Gizmos.color = new Color(1f, 1f, 1f, 0.18f);

        float halfX = areaSize.x * 0.5f;
        float halfZ = areaSize.y * 0.5f;

        for (int x = 0; x <= gridWidth; x++)
        {
            float lineX = -halfX + x * CellWidth;
            Gizmos.DrawLine(
                new Vector3(lineX, gizmoYOffset, -halfZ),
                new Vector3(lineX, gizmoYOffset, halfZ));
        }

        for (int y = 0; y <= gridHeight; y++)
        {
            float lineZ = -halfZ + y * CellHeight;
            Gizmos.DrawLine(
                new Vector3(-halfX, gizmoYOffset, lineZ),
                new Vector3(halfX, gizmoYOffset, lineZ));
        }
    }

    private void DrawCellFills()
    {
        Vector3 cellSize = new Vector3(CellWidth * cellFillScale, 0.01f, CellHeight * cellFillScale);

        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                Team owner = cells[ToIndex(x, y)].Owner;

                if (owner == Team.None && !drawUnpaintedCells)
                {
                    continue;
                }

                if (owner != Team.None && !drawPaintedCells)
                {
                    continue;
                }

                Gizmos.color = GetGizmoColor(owner);
                Gizmos.DrawCube(GetCellCenterLocalForGizmo(x, y), cellSize);
            }
        }
    }

    private Color GetGizmoColor(Team team)
    {
        switch (team)
        {
            case Team.TeamA:
                return teamAGizmoColor;
            case Team.TeamB:
                return teamBGizmoColor;
            default:
                return unpaintedGizmoColor;
        }
    }

    [ContextMenu("Debug Paint Center / Team A")]
    private void DebugPaintCenterTeamA()
    {
        PaintAtWorldPosition(transform.position, debugPaintRadius, Team.TeamA);
    }

    [ContextMenu("Debug Paint Center / Team B")]
    private void DebugPaintCenterTeamB()
    {
        PaintAtWorldPosition(transform.position, debugPaintRadius, Team.TeamB);
    }

    [ContextMenu("Debug Clear Paint")]
    private void DebugClearPaint()
    {
        ClearPaint();
    }
}
