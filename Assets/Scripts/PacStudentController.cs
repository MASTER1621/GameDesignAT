using UnityEngine;

public class PacStudentController : MonoBehaviour
{
    public enum Dir { None, Up, Down, Left, Right }

    [Header("Grid")]
    public float tileSize = 20f;
    public float cellsPerSecond = 6f;
    public LayerMask wallMask;
    public bool allowGateFromInside = false; //false for player

    [Header("Anim (optional)")]
    public Animator anim; //

    [HideInInspector] public Dir lastInput = Dir.None;
    [HideInInspector] public Dir currentInput = Dir.None;

    Vector2 currentGrid;      // snapper lapper whapper rapper sapper 
    Vector2 targetGrid;
    bool isLerping = false;
    float t = 0f;             // 1

    void Start()
    {
        if (!anim) anim = GetComponent<Animator>();
        SnapToGrid();
        Face(Dir.Right);      // face right
    }

    void Update()
    {
        ReadInput();                  // lastInput on key press

        if (!isLerping)
        {
            if (!TryStartMove(lastInput))  // try last key, chat what
                TryStartMove(currentInput); // else keep going
        }
        else
        {
            t += cellsPerSecond * Time.deltaTime;
            transform.position = Vector2.Lerp(currentGrid, targetGrid, Mathf.Clamp01(t));
            if (t >= 1f)
            {
                transform.position = targetGrid;
                currentGrid = targetGrid;
                isLerping = false;
                t = 0f;
            }
        }

        UpdateAnim();
    }

    void ReadInput()
    {
        if (Input.GetKeyDown(KeyCode.W)) lastInput = Dir.Up;
        if (Input.GetKeyDown(KeyCode.S)) lastInput = Dir.Down;
        if (Input.GetKeyDown(KeyCode.A)) lastInput = Dir.Left;
        if (Input.GetKeyDown(KeyCode.D)) lastInput = Dir.Right;
    }

    bool TryStartMove(Dir d)
    {
        if (d == Dir.None) return false;

        Vector2 dir = ToVec(d) * tileSize;
        Vector2 next = currentGrid + dir;

        if (!IsBlocked(next, d))
        {
            currentInput = d;
            isLerping = true;
            t = 0f;
            targetGrid = next;
            Face(d);
            return true;
        }
        return false;
    }

    bool IsBlocked(Vector2 worldPos, Dir d)
    {
        Vector2 box = Vector2.one * (tileSize * 0.6f);
        var hits = Physics2D.OverlapBoxAll(worldPos, box, 0f, wallMask);
        foreach (var h in hits)
        {
            if (h.CompareTag("GhostExitWall"))
            {
                if (!allowGateFromInside) return true;
                // if allowed from inside, only block when entering from outside
            }
            else
            {
                return true;
            }
        }
        return false;
    }

    void SnapToGrid()
    {
        float x = Mathf.Round(transform.position.x / tileSize) * tileSize;
        float y = Mathf.Round(transform.position.y / tileSize) * tileSize;
        currentGrid = new Vector2(x, y);
        transform.position = currentGrid;
    }

    Vector2 ToVec(Dir d)
    {
        switch (d)
        {
            case Dir.Up: return Vector2.up;
            case Dir.Down: return Vector2.down;
            case Dir.Left: return Vector2.left;
            case Dir.Right: return Vector2.right;
        }
        return Vector2.zero;
    }

    void Face(Dir d)
    {
        if (!anim) return;
        anim.SetBool("FaceUp",    d == Dir.Up);
        anim.SetBool("FaceDown",  d == Dir.Down);
        anim.SetBool("FaceLeft",  d == Dir.Left);
        anim.SetBool("FaceRight", d == Dir.Right);
    }

    void UpdateAnim()
{
    if (!anim) return;
    anim.speed = isLerping ? 1f : 0f;  // kill john lehnon
}

}
