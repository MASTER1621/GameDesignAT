using UnityEngine;

public class PacStudentController : MonoBehaviour
{
    public enum Dir { None, Up, Down, Left, Right }

    [Header("Grid")]
    public float tileSize = 20f;
    public float cellsPerSecond = 6f;
    public LayerMask wallMask;     // set to Wall in Inspector

    [Header("Anim")]
    public Animator anim;

    [Header("Audio")]
    public AudioSource audioSrc;
    public AudioClip sfxMove;
    public AudioClip sfxMoveEat;

    [HideInInspector] public Dir lastInput = Dir.None;
    [HideInInspector] public Dir currentInput = Dir.None;

    Vector2 currentGrid, targetGrid;
    bool isLerping = false;
    float t = 0f;

    void Start()
    {
        if (!anim) anim = GetComponent<Animator>();
        if (!audioSrc) audioSrc = GetComponent<AudioSource>();
        SnapToGrid();
        Face(Dir.Right);
    }

    void Update()
    {
        ReadInput();

        if (!isLerping)
        {
            if (!TryStartMove(lastInput) && !TryStartMove(currentInput))
            {
                StopMoveAudio();
                UpdateAnim();
                return;
            }
        }
        else
        {
            t += cellsPerSecond * Time.deltaTime;
            float tt = Mathf.Clamp01(t);
            transform.position = Vector2.Lerp(currentGrid, targetGrid, tt);
            if (tt >= 1f)
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
            StartMoveAudio(next);
            return true;
        }
        return false;
    }

    // ===== only WALL layer (via wallMask) + gate by tag =====
    bool IsBlocked(Vector2 worldPos, Dir d)
    {
        Vector2 size = Vector2.one * (tileSize * 0.7f);

        // Walls only
        var wallHit = Physics2D.OverlapBox(worldPos, size, 0f, wallMask);
#if UNITY_EDITOR
        if (wallHit) Debug.Log($"Blocked by WALL: {wallHit.name} (layer {LayerMask.LayerToName(wallHit.gameObject.layer)})");
#endif
        if (wallHit) return true;

        // Gate by tag (can be on any layer)
        var hits = Physics2D.OverlapBoxAll(worldPos, size, 0f);
        foreach (var h in hits)
        {
            if (!h) continue;
            if (h.CompareTag("GhostExitWall"))
            {
#if UNITY_EDITOR
                Debug.Log($"Blocked by GATE: {h.name} tag={h.tag} layer={LayerMask.LayerToName(h.gameObject.layer)}");
#endif
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
        anim.speed = isLerping ? 1f : 0f;
    }

    bool NextCellHasPellet(Vector2 next)
    {
        float r = tileSize * 0.35f;
        foreach (var h in Physics2D.OverlapCircleAll(next, r))
            if (h.CompareTag("Pellet") || h.CompareTag("PowerPellet")) return true;
        return false;
    }

    void StartMoveAudio(Vector2 next)
    {
        if (!audioSrc) return;
        var clip = NextCellHasPellet(next) ? sfxMoveEat : sfxMove;
        if (audioSrc.clip != clip) { audioSrc.clip = clip; audioSrc.time = 0f; }
        if (!audioSrc.isPlaying) { audioSrc.loop = true; audioSrc.Play(); }
    }

    void StopMoveAudio()
    {
        if (audioSrc && audioSrc.isPlaying) audioSrc.Stop();
    }
}
