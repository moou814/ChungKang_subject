using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

enum HitKind
    {
        None,
        Mirror,
        Target,
        Boundary
    }

public class MirrorManager : MonoBehaviour
{
    public bool isClear = false;

    /// <summary>
    /// 광선 충돌 결과를 담는 값 타입입니다.
    ///
    /// Kind: 무엇을 맞았는지
    /// Point: 어디에 맞았는지
    /// Distance: 현재 origin에서 얼마나 떨어져 있는지
    /// Mirror: 맞은 대상이 거울일 때 해당 거울 데이터
    /// </summary>
    private readonly struct RayHit
    {
        public static readonly RayHit None = new RayHit(HitKind.None, Vector2.zero, 0f, null);

        public RayHit(HitKind kind, Vector2 point, float distance, Mirror mirror)
        {
            Kind = kind;
            Point = point;
            Distance = distance;
            Mirror = mirror;
        }

        public HitKind Kind { get; }
        public Vector2 Point { get; }
        public float Distance { get; }
        public Mirror Mirror { get; }
        public bool HasHit => Kind != HitKind.None;
    }

    private const float RayEpsilon = 0.01f;
    private const float ParallelEpsilon = 0.0001f;

    [SerializeField] private int boardWidth = 10;
    [SerializeField] private int boardHeight = 7;
    [SerializeField] private int maxBounces = 10;

    [SerializeField] private Vector2Int sourceCell = new Vector2Int(1, 5);
    [SerializeField] private Vector2 sourceDirection = Vector2.right;
    [SerializeField] private Vector2Int targetCell = new Vector2Int(8, 1);

    [SerializeField] private Vector2Int movableMirrorStartCell = new Vector2Int(3, 5);

    [SerializeField] private Vector2Int rotatableMirrorCell = new Vector2Int(4, 1);
    [SerializeField] private float mirrorLength = 1.1f;
    [SerializeField] private float targetRadius = 0.35f;
    [SerializeField] private float mirrorPickRadius = 0.45f;
    [SerializeField] private float keyboardRotationSpeed = 120f;

    /// <summary>
    /// 현재 빛 경로가 목표 지점에 닿았는지 여부입니다.
    /// 외부 검증 코드나 Unity Context Menu 테스트에서 읽기 쉽게 public getter로 열어둡니다.
    /// </summary>
    public bool IsSolved { get; private set; }

    /// <summary>
    /// 마지막으로 계산된 빛 경로의 점 목록입니다.
    ///
    /// 예시:
    /// (1,5) -> (4,5) -> (4,1) -> (7.65,1)
    ///
    /// 이 점들을 LineRenderer에 순서대로 넣으면 화면에 꺾인 광선이 그려집니다.
    /// </summary>
    public IReadOnlyList<Vector2> LastPath => lastPath;

    /// <summary>
    /// 현재 스테이지에 존재하는 논리적 거울 목록입니다.
    /// GameObject 목록이 아니라, 계산에 필요한 중심점/각도/끝점/법선 정보를 가진 데이터 객체입니다.
    /// </summary>
    private readonly List<Mirror> mirrors = new List<Mirror>();

    /// <summary>
    /// RecalculateLightPath가 계산한 빛 경로 점 목록입니다.
    /// 매번 새 List를 만들지 않고 Clear 후 재사용해서 불필요한 할당을 줄입니다.
    /// </summary>
    private readonly List<Vector2> lastPath = new List<Vector2>();

    private Camera mainCamera;
    private Transform runtimeRoot;
    private Transform rayRoot;
    private Material lineMaterial;
    private Sprite squareSprite;
    private LineRenderer lightLine;
    private TextMesh statusText;
    private TextMesh clearText;
    private Mirror selectedMirror;
    private Vector2 dragOffset;

    /// <summary>
    /// Unity가 GameObject를 활성화한 뒤 첫 프레임 전에 호출합니다.
    /// 여기서 프로토타입에 필요한 모든 시각 오브젝트를 런타임으로 만들고,
    /// 시작 상태로 퍼즐을 리셋합니다.
    /// </summary>
    private void Start()
    {
        BuildPrototype();
        ResetPuzzle();
    }

    /// <summary>
    /// 매 프레임 호출됩니다.
    /// 입력은 매 프레임 변하기 때문에 Update에서 처리합니다.
    /// </summary>
    private void Update()
    {
        HandleInput();
    }

    /// <summary>
    /// Unity Inspector의 Context Menu 또는 검증 코드에서 정답 상태를 즉시 적용하는 함수입니다.
    ///
    /// 135도 거울은 아래로 내려온 빛을 오른쪽으로 반사합니다.
    /// </summary>
    public void ApplyPlannedSolution()
    {
        Mirror mirrorA = FindMirror("A");
        Mirror mirrorB = FindMirror("B");

        if (mirrorB != null)
        {
            mirrorB.SetSurfaceAngle(135f);
        }

        selectedMirror = mirrorB;
        RefreshMirrorSelection();
        RecalculateLightPath();
    }

    /// <summary>
    /// 퍼즐을 처음 시작 상태로 되돌립니다.
    /// Backspace 입력과 Context Menu 테스트에서 사용합니다.
    /// </summary>
    public void ResetPuzzle()
    {
        Mirror mirrorA = FindMirror("A");
        Mirror mirrorB = FindMirror("B");

        if (mirrorA != null)
        {
            mirrorA.SetSurfaceAngle(135f);
        }

        if (mirrorB != null)
        {
            mirrorB.SetSurfaceAngle(45f);
        }

        selectedMirror = null;
        RefreshMirrorSelection();
        RecalculateLightPath();
    }

    /// <summary>
    /// 자동 검증용 함수입니다.
    /// 정답을 적용한 뒤 실제로 IsSolved가 true가 되는지 반환합니다.
    /// </summary>
    public bool VerifyPlannedSolution()
    {
        ApplyPlannedSolution();
        return IsSolved;
    }

    /// <summary>
    /// 현재 거울 배치와 회전값을 기준으로 빛의 전체 경로를 다시 계산합니다.
    ///
    /// 이 게임의 핵심 알고리즘입니다.
    /// 플레이어가 거울을 움직이거나 회전할 때마다 이 함수가 호출됩니다.
    ///
    /// 처리 순서:
    /// 1. 광원 위치와 시작 방향을 준비한다.
    /// 2. 현재 광선에서 가장 가까운 충돌 대상을 찾는다.
    /// 3. 목표에 닿으면 클리어 처리한다.
    /// 4. 거울에 닿으면 반사 방향을 계산하고 다음 광선을 이어간다.
    /// 5. 벽에 닿거나 충돌 대상이 없으면 경로 계산을 끝낸다.
    /// 6. maxBounces로 무한 반사를 방지한다.
    /// </summary>
    public void RecalculateLightPath()
    {
        Vector2 origin = CellToWorld(sourceCell);
        Vector2 direction = sourceDirection.normalized;
        bool solved = false;

        lastPath.Clear();
        lastPath.Add(origin);

        for (int bounce = 0; bounce <= maxBounces; bounce++)
        {
            RayHit nearestHit = FindNearestHit(origin, direction);

            if (!nearestHit.HasHit)
            {
                // 이론상 보드 경계가 항상 잡히므로 거의 오지 않는 분기입니다.
                // 그래도 방어 코드로 광선을 충분히 멀리 뻗어 화면에 표시합니다.
                lastPath.Add(origin + direction * 20f);
                break;
            }

            lastPath.Add(nearestHit.Point);

            if (nearestHit.Kind == HitKind.Target)
            {
                solved = true;
                break;
            }

            if (nearestHit.Kind == HitKind.Mirror && nearestHit.Mirror != null)
            {
                Vector2 normal = nearestHit.Mirror.Normal;
                direction = ReflectDirection(direction, normal);

                // 방금 맞은 거울을 다시 맞는 자기 충돌을 피하기 위해 살짝 앞으로 이동합니다.
                origin = nearestHit.Point + direction * RayEpsilon;
                continue;
            }

            // Boundary에 닿으면 빛은 사라집니다.
            break;
        }

        bool changed = IsSolved != solved;
        IsSolved = solved;
        UpdateLightLine();
        UpdateStatusText();

        if (changed)
        {
            Debug.Log(IsSolved ? "MirrorPuzzleGame: Stage Clear." : "MirrorPuzzleGame: Puzzle is no longer solved.");
        }
    }

    /// <summary>
    /// 입사 방향과 거울 법선으로 반사 방향을 계산합니다.
    ///
    /// 사용 공식:
    /// R = D - 2 * dot(D, N) * N
    ///
    /// D: 들어오는 빛 방향
    /// N: 거울 표면의 법선 방향
    /// R: 반사되어 나가는 빛 방향
    ///
    /// dot(D, N)은 D가 N 방향으로 얼마나 들어가 있는지를 의미합니다.
    /// 그 성분을 2배 빼면 법선 기준으로 방향이 뒤집히면서 반사 벡터가 됩니다.
    /// </summary>
    public static Vector2 ReflectDirection(Vector2 direction, Vector2 normal)
    {
        Vector2 d = direction.normalized;
        Vector2 n = normal.normalized;
        return (d - 2f * Vector2.Dot(d, n) * n).normalized;
    }

    /// <summary>
    /// 2D 벡터를 원점 기준으로 degrees만큼 회전합니다.
    ///
    /// 회전 행렬:
    /// x' = x cos(theta) - y sin(theta)
    /// y' = x sin(theta) + y cos(theta)
    ///
    /// 거울의 표면 방향과 법선 방향을 각도로부터 계산할 때 사용합니다.
    /// </summary>
    public static Vector2 RotateVector(Vector2 vector, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);
        return new Vector2(vector.x * cos - vector.y * sin, vector.x * sin + vector.y * cos);
    }

    /// <summary>
    /// 현재 광선에서 가장 먼저 만나는 대상을 찾습니다.
    ///
    /// 같은 방향에 목표, 여러 거울, 보드 경계가 동시에 있을 수 있습니다.
    /// 실제 빛은 가장 가까운 대상에 먼저 닿기 때문에 모든 후보를 검사한 뒤
    /// distance가 가장 작은 RayHit만 선택합니다.
    /// </summary>
    private RayHit FindNearestHit(Vector2 origin, Vector2 direction)
    {
        RayHit nearest = RayHit.None;

        RayHit targetHit = IntersectTarget(origin, direction);
        if (targetHit.HasHit)
        {
            nearest = targetHit;
        }

        for (int i = 0; i < mirrors.Count; i++)
        {
            RayHit mirrorHit = IntersectMirror(origin, direction, mirrors[i]);
            if (mirrorHit.HasHit && (!nearest.HasHit || mirrorHit.Distance < nearest.Distance))
            {
                nearest = mirrorHit;
            }
        }

        RayHit boundaryHit = IntersectBoardBoundary(origin, direction);
        if (boundaryHit.HasHit && (!nearest.HasHit || boundaryHit.Distance < nearest.Distance))
        {
            nearest = boundaryHit;
        }

        return nearest;
    }

    /// <summary>
    /// 광선과 원형 목표 지점의 교차를 검사합니다.
    ///
    /// 목표는 화면에서는 사각형으로 보이지만, 판정은 반지름 targetRadius를 가진 원으로 처리합니다.
    /// 원 판정이 사각형 판정보다 설명하기 쉽고, 광선이 살짝 빗겨도 목표에 닿은 것으로 보이기 때문입니다.
    ///
    /// 핵심 수학:
    /// 1. 광선 시작점에서 목표 중심까지의 벡터를 구한다.
    /// 2. 그 벡터를 광선 방향에 투영한다. 투영값이 광선 위의 가장 가까운 지점 거리입니다.
    /// 3. 목표 중심과 광선 사이의 최단 거리 제곱을 구한다.
    /// 4. 그 값이 반지름 제곱보다 작거나 같으면 교차합니다.
    /// </summary>
    private RayHit IntersectTarget(Vector2 origin, Vector2 direction)
    {
        Vector2 center = CellToWorld(targetCell);
        Vector2 toCenter = center - origin;
        float projection = Vector2.Dot(toCenter, direction);

        if (projection <= RayEpsilon)
        {
            return RayHit.None;
        }

        float closestSqr = toCenter.sqrMagnitude - projection * projection;
        float radiusSqr = targetRadius * targetRadius;
        if (closestSqr > radiusSqr)
        {
            return RayHit.None;
        }

        float offset = Mathf.Sqrt(Mathf.Max(0f, radiusSqr - closestSqr));
        float distance = projection - offset;

        if (distance <= RayEpsilon)
        {
            distance = projection + offset;
        }
        if (distance <= RayEpsilon)
        {
            return RayHit.None;
        }

        return new RayHit(HitKind.Target, origin + direction * distance, distance, null);
    }

    /// <summary>
    /// 광선과 거울 선분의 교차를 검사합니다.
    ///
    /// 광선: origin + direction * t, t >= 0
    /// 선분: a + (b - a) * u, 0 <= u <= 1
    ///
    /// t가 양수이고 u가 0~1 사이면 광선 앞쪽에서 선분과 만난 것입니다.
    /// 여기서는 2D 외적을 이용해 t와 u를 계산합니다.
    /// </summary>
    private RayHit IntersectMirror(Vector2 origin, Vector2 direction, Mirror Mirror)
    {
        Vector2 a = Mirror.EndpointA;
        Vector2 b = Mirror.EndpointB;
        Vector2 segment = b - a;
        float denominator = Cross(direction, segment);

        if (Mathf.Abs(denominator) < ParallelEpsilon)
        {
            return RayHit.None;
        }

        Vector2 delta = a - origin;
        float rayDistance = Cross(delta, segment) / denominator;
        float segmentFactor = Cross(delta, direction) / denominator;

        if (rayDistance <= RayEpsilon || segmentFactor < -ParallelEpsilon || segmentFactor > 1f + ParallelEpsilon)
        {
            return RayHit.None;
        }

        return new RayHit(HitKind.Mirror, origin + direction * rayDistance, rayDistance, Mirror);
    }

    /// <summary>
    /// 광선이 보드 외곽 경계와 만나는 지점을 계산합니다.
    ///
    /// 보드는 왼쪽 -0.5, 오른쪽 boardWidth - 0.5, 아래 -0.5, 위 boardHeight - 0.5로 잡습니다.
    /// 격자 칸 중심이 정수 좌표이기 때문에 외곽선을 칸 중심에서 반 칸 바깥에 둔 것입니다.
    /// </summary>
    private RayHit IntersectBoardBoundary(Vector2 origin, Vector2 direction)
    {
        float left = -0.5f;
        float right = boardWidth - 0.5f;
        float bottom = -0.5f;
        float top = boardHeight - 0.5f;
        float nearestDistance = float.PositiveInfinity;

        if (direction.x > ParallelEpsilon)
        {
            nearestDistance = Mathf.Min(nearestDistance, (right - origin.x) / direction.x);
        }
        else if (direction.x < -ParallelEpsilon)
        {
            nearestDistance = Mathf.Min(nearestDistance, (left - origin.x) / direction.x);
        }

        if (direction.y > ParallelEpsilon)
        {
            nearestDistance = Mathf.Min(nearestDistance, (top - origin.y) / direction.y);
        }
        else if (direction.y < -ParallelEpsilon)
        {
            nearestDistance = Mathf.Min(nearestDistance, (bottom - origin.y) / direction.y);
        }

        if (float.IsInfinity(nearestDistance) || nearestDistance <= RayEpsilon)
        {
            return RayHit.None;
        }

        return new RayHit(HitKind.Boundary, origin + direction * nearestDistance, nearestDistance, null);
    }

    /// <summary>
    /// 씬에 직접 배치된 아트 리소스 없이 프로토타입을 런타임으로 생성합니다.
    ///
    /// 장점:
    /// - 빈 씬에서도 스크립트 하나만 붙이면 게임이 보입니다.
    /// - 과제 제출 시 오브젝트 구조가 코드로 설명됩니다.
    /// - 아트 리소스 의존성이 거의 없습니다.
    /// </summary>
    private void BuildPrototype()
    {
        DestroyRuntimeRootIfNeeded();
        EnsureRuntimeAssets();
        ConfigureCamera();

        runtimeRoot = new GameObject("Mirror Puzzle Runtime").transform;
        runtimeRoot.SetParent(transform, false);

        rayRoot = new GameObject("Light Path").transform;
        rayRoot.SetParent(runtimeRoot, false);

        CreateBoardVisuals();
        CreateStageObjects();
        CreateInstructionText();
    }

    /// <summary>
    /// 플레이 모드 도중 스크립트가 다시 시작될 때 기존 런타임 오브젝트가 중복 생성되는 것을 방지합니다.
    /// </summary>
    private void DestroyRuntimeRootIfNeeded()
    {
        Transform existing = transform.Find("Mirror Puzzle Runtime");
        if (existing != null)
        {
            Destroy(existing.gameObject);
        }
    }

    /// <summary>
    /// 라인과 사각형을 그리기 위한 최소 런타임 리소스를 생성합니다.
    ///
    /// lineMaterial: LineRenderer가 사용할 머티리얼입니다.
    /// squareSprite: 1x1 흰색 텍스처로 만든 Sprite입니다. 색과 크기는 SpriteRenderer에서 바꿉니다.
    /// </summary>
    private void EnsureRuntimeAssets()
    {
        if (lineMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            }
            if (shader == null)
            {
                shader = Shader.Find("Hidden/Internal-Colored");
            }

            lineMaterial = new Material(shader);
        }

        if (squareSprite == null)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            squareSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
    }

    /// <summary>
    /// 카메라가 전체 10x7 보드를 한 화면에 볼 수 있게 설정합니다.
    /// orthographic 카메라이므로 원근 왜곡 없이 격자 퍼즐처럼 보입니다.
    /// </summary>
    private void ConfigureCamera()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            mainCamera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
        }

        mainCamera.orthographic = true;
        mainCamera.orthographicSize = 4.7f;
        mainCamera.transform.position = new Vector3((boardWidth - 1f) * 0.5f, (boardHeight - 1f) * 0.5f, -10f);
        mainCamera.backgroundColor = new Color(0.04f, 0.05f, 0.07f, 1f);
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
    }

    /// <summary>
    /// 보드 배경, 격자선, 외곽선을 생성합니다.
    /// 격자선은 LineRenderer로 만들고, 배경은 SpriteRenderer 사각형으로 만듭니다.
    /// </summary>
    private void CreateBoardVisuals()
    {
        Vector2 boardCenter = new Vector2((boardWidth - 1f) * 0.5f, (boardHeight - 1f) * 0.5f);
        CreateRect("Board Background", boardCenter, new Vector2(boardWidth, boardHeight), new Color(0.1f, 0.12f, 0.16f, 1f), -20, runtimeRoot);

        for (int x = 0; x <= boardWidth; x++)
        {
            float worldX = x - 0.5f;
            CreateLine("Grid Vertical " + x, new Vector2(worldX, -0.5f), new Vector2(worldX, boardHeight - 0.5f), 0.025f, new Color(0.28f, 0.31f, 0.38f, 0.65f), -10, runtimeRoot);
        }

        for (int y = 0; y <= boardHeight; y++)
        {
            float worldY = y - 0.5f;
            CreateLine("Grid Horizontal " + y, new Vector2(-0.5f, worldY), new Vector2(boardWidth - 0.5f, worldY), 0.025f, new Color(0.28f, 0.31f, 0.38f, 0.65f), -10, runtimeRoot);
        }

        CreateLine("Board Bottom", new Vector2(-0.5f, -0.5f), new Vector2(boardWidth - 0.5f, -0.5f), 0.07f, new Color(0.75f, 0.77f, 0.82f, 1f), -5, runtimeRoot);
        CreateLine("Board Top", new Vector2(-0.5f, boardHeight - 0.5f), new Vector2(boardWidth - 0.5f, boardHeight - 0.5f), 0.07f, new Color(0.75f, 0.77f, 0.82f, 1f), -5, runtimeRoot);
        CreateLine("Board Left", new Vector2(-0.5f, -0.5f), new Vector2(-0.5f, boardHeight - 0.5f), 0.07f, new Color(0.75f, 0.77f, 0.82f, 1f), -5, runtimeRoot);
        CreateLine("Board Right", new Vector2(boardWidth - 0.5f, -0.5f), new Vector2(boardWidth - 0.5f, boardHeight - 0.5f), 0.07f, new Color(0.75f, 0.77f, 0.82f, 1f), -5, runtimeRoot);
    }

    /// <summary>
    /// 광원, 목표, 광선 LineRenderer, 거울 A/B를 생성합니다.
    /// </summary>
    private void CreateStageObjects()
    {
        Vector2 sourcePosition = CellToWorld(sourceCell);
        Vector2 targetPosition = CellToWorld(targetCell);

        CreateRect("Light Source", sourcePosition, new Vector2(0.62f, 0.62f), new Color(1f, 0.75f, 0.16f, 1f), 3, runtimeRoot);
        CreateLine("Source Direction", sourcePosition + new Vector2(0.15f, 0f), sourcePosition + new Vector2(0.85f, 0f), 0.08f, new Color(1f, 0.83f, 0.2f, 1f), 6, runtimeRoot);
        CreateText("Source Label", "SOURCE", sourcePosition + new Vector2(0f, 0.48f), 0.11f, new Color(1f, 0.86f, 0.35f, 1f), 10, runtimeRoot);

        CreateRect("Target", targetPosition, new Vector2(0.72f, 0.72f), new Color(0.1f, 0.78f, 0.35f, 1f), 3, runtimeRoot);
        CreateText("Target Label", "TARGET", targetPosition + new Vector2(0f, 0.5f), 0.11f, new Color(0.6f, 1f, 0.72f, 1f), 10, runtimeRoot);

        lightLine = CreateLine("Light Ray", sourcePosition, sourcePosition + Vector2.right, 0.09f, new Color(1f, 0.95f, 0.25f, 1f), 8, rayRoot);
        lightLine.numCapVertices = 8;
        lightLine.numCornerVertices = 8;

        mirrors.Clear();
        mirrors.Add(CreateMirror("A", "Q/W A", movableMirrorStartCell, 135f, new Color(0.35f, 0.78f, 1f, 1f)));
        mirrors.Add(CreateMirror("B", "Q/W B", rotatableMirrorCell, 45f, new Color(0.72f, 0.48f, 1f, 1f)));
    }

    /// <summary>
    /// 제목, 조작 안내, 클리어 텍스트를 생성합니다.
    /// </summary>
    private void CreateInstructionText()
    {
        statusText = CreateText("Status", "", new Vector2(4.5f, -0.95f), 0.11f, new Color(0.78f, 0.84f, 0.92f, 1f), 20, runtimeRoot);
        clearText = CreateText("Stage Clear Text", "STAGE CLEAR", new Vector2(4.5f, 3.2f), 0.24f, new Color(0.45f, 1f, 0.54f, 1f), 25, runtimeRoot);
        clearText.gameObject.SetActive(false);
    }

    /// <summary>
    /// 논리 거울 데이터와 시각 라인을 같이 생성합니다.
    /// </summary>
    private Mirror CreateMirror(string id, string label, Vector2Int cell, float angle, Color color)
    {
        GameObject root = new GameObject("Mirror " + id);
        root.transform.SetParent(runtimeRoot, false);

        LineRenderer surface = CreateLine("Mirror " + id + " Surface", Vector2.zero, Vector2.right, 0.13f, color, 5, root.transform);
        surface.numCapVertices = 8;

        TextMesh caption = CreateText("Mirror " + id + " Label", label, CellToWorld(cell) + new Vector2(0f, -0.5f), 0.1f, color, 10, root.transform);

        Mirror Mirror = new Mirror(id, label, cell, angle, mirrorLength, color, surface, caption, this);
        Mirror.Refresh(false);
        return Mirror;
    }

    /// <summary>
    /// 마우스와 키보드 입력을 처리합니다.
    ///
    /// 입력 규칙:
    /// - 왼쪽 클릭: 거울 선택
    /// - Q 누르고 있기: 회전 가능한 거울 B를 반시계 방향으로 회전
    /// - W 누르고 있기: 회전 가능한 거울 B를 시계 방향으로 회전
    /// </summary>
    private void HandleInput()
    {
        if (Mouse.current == null || Keyboard.current == null || mainCamera == null)
        {
            return;
        }

        Vector2 mouseWorld = ScreenToWorld(Mouse.current.position.ReadValue());

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            selectedMirror = PickMirror(mouseWorld);
            dragOffset = selectedMirror != null ? selectedMirror.Center - mouseWorld : Vector2.zero;
            RefreshMirrorSelection();
        }

        float rotationInput = 0f;
        if (Keyboard.current.qKey.isPressed)
        {
            rotationInput += 1f;
        }
        if (Keyboard.current.wKey.isPressed)
        {
            rotationInput -= 1f;
        }

        if (!Mathf.Approximately(rotationInput, 0f))
        {
            if (selectedMirror == null)
            {
                selectedMirror = mirrors[0];
                RefreshMirrorSelection();
            }
            else
            {
                selectedMirror.RotateBy(rotationInput * keyboardRotationSpeed * Time.deltaTime);
                RecalculateLightPath();
            }
        }

    }

    /// <summary>
    /// 마우스 위치와 가장 가까운 거울을 찾습니다.
    /// 중심점이 아니라 거울 선분까지의 거리를 사용하므로, 긴 거울을 더 자연스럽게 선택할 수 있습니다.
    /// </summary>
    private Mirror PickMirror(Vector2 worldPosition)
    {
        Mirror best = null;
        float bestDistance = float.PositiveInfinity;

        for (int i = 0; i < mirrors.Count; i++)
        {
            float distance = mirrors[i].DistanceToSurface(worldPosition);
            if (distance <= mirrorPickRadius && distance < bestDistance)
            {
                bestDistance = distance;
                best = mirrors[i];
            }
        }

        return best;
    }

    /// <summary>
    /// 선택된 거울은 흰색과 더 두꺼운 선으로 표시합니다.
    /// </summary>
    private void RefreshMirrorSelection()
    {
        for (int i = 0; i < mirrors.Count; i++)
        {
            mirrors[i].Refresh(mirrors[i] == selectedMirror);
        }
    }

    /// <summary>
    /// lastPath에 저장된 점 목록을 LineRenderer에 적용합니다.
    /// 클리어 상태면 녹색, 아니면 노란색으로 표시합니다.
    /// </summary>
    private void UpdateLightLine()
    {
        if (lightLine == null)
        {
            return;
        }

        lightLine.positionCount = lastPath.Count;
        for (int i = 0; i < lastPath.Count; i++)
        {
            lightLine.SetPosition(i, ToVector3(lastPath[i], -0.1f));
        }

        Color lightColor = IsSolved ? new Color(0.38f, 1f, 0.45f, 1f) : new Color(1f, 0.9f, 0.22f, 1f);
        lightLine.startColor = lightColor;
        lightLine.endColor = lightColor;
    }

    /// <summary>
    /// 하단 안내 텍스트와 클리어 텍스트를 현재 상태에 맞게 갱신합니다.
    /// </summary>
    private void UpdateStatusText()
    {
        if (statusText != null)
        {
            string selectedText = selectedMirror == null ? "No Mirror selected" : "Selected: Mirror " + selectedMirror.Id;
            statusText.text = selectedText + "\nDrag A. Hold Q/W to rotate B. Space solves. Backspace resets.";
        }

        if (clearText != null)
        {
            clearText.gameObject.SetActive(IsSolved);
        }
    }

    /// <summary>
    /// 2D 선을 생성하는 공통 함수입니다.
    /// 격자, 외곽선, 광선, 거울 표면을 모두 LineRenderer로 그립니다.
    /// </summary>
    private LineRenderer CreateLine(string name, Vector2 start, Vector2 end, float width, Color color, int sortingOrder, Transform parent)
    {
        GameObject lineObject = new GameObject(name);
        lineObject.transform.SetParent(parent, false);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.material = lineMaterial;
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.startWidth = width;
        line.endWidth = width;
        line.startColor = color;
        line.endColor = color;
        line.numCapVertices = 2;
        line.SetPosition(0, ToVector3(start, -0.05f));
        line.SetPosition(1, ToVector3(end, -0.05f));
        line.sortingOrder = sortingOrder;
        return line;
    }

    /// <summary>
    /// 색이 있는 사각형을 생성하는 공통 함수입니다.
    /// 1x1 흰색 스프라이트를 원하는 크기로 스케일해서 사용합니다.
    /// </summary>
    private GameObject CreateRect(string name, Vector2 position, Vector2 size, Color color, int sortingOrder, Transform parent)
    {
        GameObject rect = new GameObject(name);
        rect.transform.SetParent(parent, false);
        rect.transform.position = ToVector3(position, 0f);
        rect.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer spriteRenderer = rect.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = squareSprite;
        spriteRenderer.color = color;
        spriteRenderer.sortingOrder = sortingOrder;
        return rect;
    }

    /// <summary>
    /// TextMesh 기반 월드 공간 텍스트를 생성합니다.
    /// UI Canvas를 만들지 않고도 과제 프로토타입에 필요한 안내 문구를 표시할 수 있습니다.
    /// </summary>
    private TextMesh CreateText(string name, string text, Vector2 position, float characterSize, Color color, int sortingOrder, Transform parent)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        textObject.transform.position = ToVector3(position, -0.2f);

        TextMesh textMesh = textObject.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.characterSize = characterSize;
        textMesh.fontSize = 24;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = color;

        MeshRenderer meshRenderer = textObject.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.sortingOrder = sortingOrder;
        }

        return textMesh;
    }

    /// <summary>
    /// 화면 픽셀 좌표를 Unity 월드 좌표로 변환합니다.
    /// 마우스 입력은 픽셀 좌표이지만, 거울과 광선 계산은 월드 좌표로 처리합니다.
    /// </summary>
    private Vector2 ScreenToWorld(Vector2 screenPosition)
    {
        Vector3 world = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -mainCamera.transform.position.z));
        return new Vector2(world.x, world.y);
    }

    /// <summary>
    /// 격자 좌표를 월드 좌표로 변환합니다.
    /// 현재 프로토타입에서는 한 칸이 1 Unity unit이므로 그대로 변환합니다.
    /// </summary>
    public Vector2 CellToWorld(Vector2Int cell)
    {
        return new Vector2(cell.x, cell.y);
    }

    /// <summary>
    /// id로 거울을 찾습니다. 현재는 A, B 두 개만 존재합니다.
    /// </summary>
    private Mirror FindMirror(string id)
    {
        for (int i = 0; i < mirrors.Count; i++)
        {
            if (mirrors[i].Id == id)
            {
                return mirrors[i];
            }
        }

        return null;
    }


    /// <summary>
    /// 2D 계산 좌표를 화면 그리기용 3D 좌표로 바꿉니다.
    /// z값은 렌더링 순서를 안정적으로 잡기 위해 호출부에서 지정합니다.
    /// </summary>
    private static Vector3 ToVector3(Vector2 vector, float z)
    {
        return new Vector3(vector.x, vector.y, z);
    }

    /// <summary>
    /// 2D 외적의 z 성분을 계산합니다.
    ///
    /// Cross(a, b) = a.x * b.y - a.y * b.x
    ///
    /// 값의 의미:
    /// - 양수: b가 a 기준 반시계 방향 쪽에 있음
    /// - 음수: b가 a 기준 시계 방향 쪽에 있음
    /// - 0 근처: 두 벡터가 평행함
    /// </summary>
    private static float Cross(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }

    /// <summary>
    /// 점과 선분 사이의 최단 거리를 구합니다.
    /// 마우스가 거울 선분 근처에 있는지 판정할 때 사용합니다.
    /// </summary>
    public static float DistancePointToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float denominator = Vector2.Dot(ab, ab);
        if (denominator <= ParallelEpsilon)
        {
            return Vector2.Distance(point, a);
        }

        float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / denominator);
        return Vector2.Distance(point, a + ab * t);
    }
}
