using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// 파이프 퍼즐의 블록 하나. 클릭하면 시계방향으로 90도씩 회전한다.
/// 블록 종류(BlockKind)가 가진 기본 통로 방향에 회전 횟수(angle)를 더해
/// 현재 열려 있는 통로 방향을 계산한다.
/// </summary>
public class PipePuzzleBlock : MonoBehaviour
{
    /// <summary>시계방향 90도 회전 횟수 (0~3)</summary>
    public int angle;
    public BlockKind kind;

    /// <summary>시작/도착/고정 블록은 회전할 수 없다.</summary>
    [FormerlySerializedAs("itFixed")]
    public bool isFixed;

    private Image img;

    // GetOpenDirections()가 매번 새 배열을 만들지 않도록 재사용하는 버퍼 (GC 부하 방지)
    private readonly int[] openDirs = new int[4];

    private void Awake()
    {
        img = GetComponent<Image>();
    }

    /// <summary>UI Button onClick에 연결된다. 회전 후 매니저에게 연결 상태 재검사를 요청한다.</summary>
    public void Turn()
    {   
        if (isFixed) return;

        SoundManager.Instance.soundEffect(0);

        angle = (angle + 1) % 4;
        transform.Rotate(0, 0, -90);

        PipePuzzleManager.Instance.CheckClear();
    }

    /// <summary>시작점과 연결된 블록인지 여부를 색으로 표시한다.</summary>
    public void SetRoadLit(bool isOn)
    {
        if (img != null)
        {
            img.color = isOn ? Color.white : Color.gray;
        }
    }

    /// <summary>
    /// 현재 회전 상태에서 열려 있는 통로 방향들을 반환한다.
    /// 값은 방향 인덱스(0:오른쪽, 1:아래, 2:왼쪽, 3:위)이고, 닫힌 슬롯은 -1.
    /// 기본 통로 방향에 회전 횟수를 더한 뒤 4로 나눈 나머지가 실제 방향이 된다.
    /// </summary>
    public int[] GetOpenDirections()
    {
        for (int i = 0; i < 4; i++)
        {
            int baseDir = PipePuzzleManager.ConnectedDir[(int)kind, i];
            openDirs[i] = baseDir == -1 ? -1 : (baseDir + angle) % 4;
        }

        return openDirs;
    }
}
