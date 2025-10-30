using UnityEngine;

public class PacStudentController : MonoBehaviour
{
    public enum Dir { None, Up, Down, Left, Right }

    [Header("Grid")]
    public float tileSize = 20f;
    public float cellsPerSecond = 6f;
    public LayerMask wallMask;

    [Header("Anim")]
    public Animator anim;

    [Header("Audio (move loop)")]
    public AudioSource audioSrc;
    public AudioClip sfxMove;
    public AudioClip sfxMoveEat;

    [Header("VFX (movement)")]
    public ParticleSystem moveDust;

    [Header("Wall Bump FX (one-shot)")]
    public AudioSource bumpAudioSrc;
    public AudioClip wallBumpClip;
    public ParticleSystem wallBumpFXPrefab;

    [Header("Teleporters")]
    public Transform teleLeft;
    public Transform teleRight;
    public string teleLeftTag = "TeleLeft";
    public string teleRightTag = "TeleRight";
    public float teleCooldown = 0.2f;

    [HideInInspector] public Dir lastInput = Dir.None;
    [HideInInspector] public Dir currentInput = Dir.None;

    Vector2 currentGrid, targetGrid;
    bool isLerping = false;
    float t = 0f;

    Vector2 _lastBumpCell = new Vector2(float.NaN, float.NaN);
    Dir _lastBumpDir = Dir.None;
    float _lastTeleTime = -999f;

    void Start()
    {
        if (!anim) anim = GetComponent<Animator>();
        if (!audioSrc) audioSrc = GetComponent<AudioSource>();
        if (moveDust) moveDust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        SnapToGrid();
        Face(Dir.Right);
        if (!teleLeft) { var tl = GameObject.Find("TeleLeft"); if (tl) teleLeft = tl.transform; }
        if (!teleRight) { var tr = GameObject.Find("TeleRight"); if (tr) teleRight = tr.transform; }
    }

    void Update()
    {
        ReadInput();

        if (!isLerping)
        {
            if (!TryStartMove(lastInput))
            {
                if (!TryStartMove(currentInput))
                {
                    StopMoveAudio();
                    StopDust();
                    UpdateAnim();
                    return;
                }
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

        if (IsBlocked(next, d))
        {
            bool attemptedTurnIntoWall = (d == lastInput && d != currentInput);
            bool ranStraightIntoWall = (d == currentInput);
            if ((attemptedTurnIntoWall || ranStraightIntoWall) && ShouldBumpOnce(currentGrid, d))
            {
                PlayWallBump(d, currentGrid, next);
            }
            return false;
        }

        currentInput = d;
        isLerping = true;
        t = 0f;
        targetGrid = next;
        Face(d);
        StartMoveAudio(next);
        PlayDust();

        _lastBumpDir = Dir.None;
        _lastBumpCell = new Vector2(float.NaN, float.NaN);
        return true;
    }

    bool IsBlocked(Vector2 worldPos, Dir d)
    {
        Vector2 size = Vector2.one * (tileSize * 0.7f);
        var wallHit = Physics2D.OverlapBox(worldPos, size, 0f, wallMask);
        if (wallHit) return true;

        var hits = Physics2D.OverlapBoxAll(worldPos, size, 0f);
        foreach (var h in hits)
        {
            if (!h) continue;
            if (h.CompareTag("GhostExitWall")) return true;
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

    void PlayDust()
    {
        if (!moveDust) return;
        if (!moveDust.isPlaying) moveDust.Play();
    }

    void StopDust()
    {
        if (moveDust && moveDust.isPlaying) moveDust.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    bool ShouldBumpOnce(Vector2 cellCenter, Dir d)
    {
        if (_lastBumpDir == d && !float.IsNaN(_lastBumpCell.x))
        {
            if ((cellCenter - _lastBumpCell).sqrMagnitude < 0.01f) return false;
        }
        _lastBumpDir = d;
        _lastBumpCell = cellCenter;
        return true;
    }

    void PlayWallBump(Dir d, Vector2 fromCell, Vector2 toCell)
    {
        Vector3 contact = Vector3.Lerp(fromCell, toCell, 0.5f);
        contact.z = transform.position.z;
        if (wallBumpFXPrefab)
        {
            var ps = Instantiate(wallBumpFXPrefab, contact, Quaternion.identity);
            ps.Play();
            Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax + 0.25f);
        }
        if (bumpAudioSrc && wallBumpClip)
        {
            bumpAudioSrc.PlayOneShot(wallBumpClip);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Pellet"))
        {
            Destroy(other.gameObject);
            if (GameManager.I) GameManager.I.AddScore(10);
            return;
        }
        if (other.CompareTag("Cherry"))
        {
            Destroy(other.gameObject);
            if (GameManager.I) GameManager.I.AddScore(100);
            return;
        }

        if (Time.time - _lastTeleTime < teleCooldown) return;
        bool hitLeft = other.CompareTag(teleLeftTag) || other.name == "TeleLeft";
        bool hitRight = other.CompareTag(teleRightTag) || other.name == "TeleRight";
        if (hitLeft && teleRight)
        {
            TeleportTo(teleRight, currentInput != Dir.None ? currentInput : lastInput);
        }
        else if (hitRight && teleLeft)
        {
            TeleportTo(teleLeft, currentInput != Dir.None ? currentInput : lastInput);
        }
    }

    void TeleportTo(Transform exitT, Dir keepDir)
    {
        _lastTeleTime = Time.time;
        Vector2 snapped = new Vector2(
            Mathf.Round(exitT.position.x / tileSize) * tileSize,
            Mathf.Round(exitT.position.y / tileSize) * tileSize
        );
        isLerping = false;
        t = 0f;
        currentGrid = snapped;
        transform.position = snapped;
        if (keepDir == Dir.None) keepDir = Dir.Left;
        lastInput = keepDir;
        currentInput = keepDir;
        if (!TryStartMove(keepDir))
        {
            Face(keepDir);
            StopMoveAudio();
            StopDust();
        }
    }
}
