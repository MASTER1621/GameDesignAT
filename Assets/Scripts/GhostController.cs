using UnityEngine;
using System.Collections.Generic;

public class GhostController : MonoBehaviour
{
    public enum Dir { None, Up, Down, Left, Right }
    public enum Kind { G1_Farther, G2_Closer, G3_Random, G4_Clockwise }

    [Header("Grid Move")]
    public float tileSize = 20f;
    public float cellsPerSecond = 6f;
    public LayerMask wallMask;

    [Header("Refs")]
    public Animator anim;
    public PacStudentController pac;
    public BoxCollider2D spawnArea;
    public Transform startPoint;
    public Transform topExit;
    public Transform bottomExit;
    public bool useTopExit = true;
    public Kind kind = Kind.G3_Random;

    [Header("Dead Move")]
    public float deadUnitsPerSecond = 120f;

    [Header("Teleport Tags (blocked for ghosts)")]
    public string teleLeftTag = "TeleLeft";
    public string teleRightTag = "TeleRight";

    Vector2 currentGrid, targetGrid;
    Dir currentDir = Dir.None;
    Dir lastDir = Dir.None;
    bool isLerping = false;
    float t = 0f;
    bool frozen = false;

    void Start()
    {
        SnapToGrid();
        if (anim == null) anim = GetComponent<Animator>();
        if (pac == null && GameManager.I) pac = GameManager.I.pac;
        if (deadUnitsPerSecond <= 0f) deadUnitsPerSecond = cellsPerSecond * tileSize;
        if (currentDir == Dir.None) currentDir = Dir.Right;
        TryStartMove(currentDir);
    }

    void Update()
    {
        if (GameManager.I && !GameManager.I.roundStarted) return;
        if (frozen) return;

        if (IsDead())
        {
            MoveDeadTowardHome();
            return;
        }

        if (!isLerping)
        {
            Dir next = DecideNextDir();
            if (!TryStartMove(next))
            {
                List<Dir> all = AllDirs();
                for (int i = 0; i < all.Count; i++)
                    if (TryStartMove(all[i])) break;
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
    }

    void SnapToGrid()
    {
        float x = Mathf.Round(transform.position.x / tileSize) * tileSize;
        float y = Mathf.Round(transform.position.y / tileSize) * tileSize;
        currentGrid = new Vector2(x, y);
        transform.position = currentGrid;
    }

    bool TryStartMove(Dir d)
    {
        if (d == Dir.None) return false;
        Vector2 dir = ToVec(d) * tileSize;
        Vector2 next = currentGrid + dir;

        if (!IsWalkable(next))
        {
            return false;
        }

        lastDir = currentDir;
        currentDir = d;
        isLerping = true;
        t = 0f;
        targetGrid = next;
        Face(d);
        return true;
    }

    Dir DecideNextDir()
    {
        Vector2 pacPos = pac ? (Vector2)pac.transform.position : currentGrid;
        float curDist = (pacPos - currentGrid).magnitude;

        List<Dir> options = ValidDirsExcludingBack(currentDir);
        if (options.Count == 0)
        {
            Dir back = Opposite(currentDir);
            if (IsWalkable(currentGrid + ToVec(back) * tileSize)) return back;
            return currentDir;
        }

        if (InsideSpawn() && !IsDead())
        {
            Dir exitDir = useTopExit ? Dir.Up : Dir.Down;
            if (IsWalkable(currentGrid + ToVec(exitDir) * tileSize)) return exitDir;
            for (int i = 0; i < options.Count; i++)
                if (options[i] != Opposite(exitDir)) return options[i];
            return options[0];
        }

        if (kind == Kind.G3_Random)
        {
            return options[Random.Range(0, options.Count)];
        }
        else if (kind == Kind.G4_Clockwise)
        {
            Dir r = RightOf(currentDir);
            Dir s = currentDir;
            Dir l = LeftOf(currentDir);
            Dir b = Opposite(currentDir);
            if (options.Contains(r)) return r;
            if (options.Contains(s)) return s;
            if (options.Contains(l)) return l;
            return b;
        }
        else if (kind == Kind.G1_Farther)
        {
            List<Dir> farther = new List<Dir>();
            for (int i = 0; i < options.Count; i++)
            {
                Vector2 n = currentGrid + ToVec(options[i]) * tileSize;
                float nd = (pacPos - n).magnitude;
                if (nd >= curDist) farther.Add(options[i]);
            }
            if (farther.Count > 0) return farther[Random.Range(0, farther.Count)];
            return options[Random.Range(0, options.Count)];
        }
        else
        {
            List<Dir> closer = new List<Dir>();
            for (int i = 0; i < options.Count; i++)
            {
                Vector2 n = currentGrid + ToVec(options[i]) * tileSize;
                float nd = (pacPos - n).magnitude;
                if (nd <= curDist) closer.Add(options[i]);
            }
            if (closer.Count > 0) return closer[Random.Range(0, closer.Count)];
            return options[Random.Range(0, options.Count)];
        }
    }

    List<Dir> ValidDirsExcludingBack(Dir from)
    {
        List<Dir> outList = new List<Dir>();
        Dir back = Opposite(from);
        var all = AllDirs();
        for (int i = 0; i < all.Count; i++)
        {
            Dir d = all[i];
            if (d == back && HasAlternative(all)) continue;
            Vector2 next = currentGrid + ToVec(d) * tileSize;
            if (IsWalkable(next)) outList.Add(d);
        }
        return outList;
    }

    bool HasAlternative(List<Dir> all)
    {
        int count = 0;
        for (int i = 0; i < all.Count; i++)
        {
            Vector2 next = currentGrid + ToVec(all[i]) * tileSize;
            if (IsWalkable(next)) count++;
        }
        return count > 1;
    }

    bool IsWalkable(Vector2 cellCenter)
    {
        if (!IsDead())
        {
            if (!InsideSpawn() && spawnArea && IsPointInside(spawnArea, cellCenter)) return false;
            Vector2 size = Vector2.one * (tileSize * 0.7f);
            var wallHit = Physics2D.OverlapBox(cellCenter, size, 0f, wallMask);
            if (wallHit) return false;

            var hits = Physics2D.OverlapBoxAll(cellCenter, size, 0f);
            for (int i = 0; i < hits.Length; i++)
            {
                var h = hits[i];
                if (!h) continue;
                if (h.CompareTag("GhostExitWall")) return false;
                if (h.CompareTag(teleLeftTag) || h.CompareTag(teleRightTag)) return false;
            }
            return true;
        }
        else
        {
            return true;
        }
    }

    bool InsideSpawn()
    {
        if (!spawnArea) return false;
        return IsPointInside(spawnArea, transform.position);
    }

    bool IsPointInside(BoxCollider2D box, Vector2 worldPt)
    {
        var l2w = box.transform.localToWorldMatrix;
        var center = l2w.MultiplyPoint(box.offset);
        float angle = box.transform.eulerAngles.z * Mathf.Deg2Rad;
        float cos = Mathf.Cos(-angle);
        float sin = Mathf.Sin(-angle);
        Vector2 p = worldPt - (Vector2)center;
        Vector2 lp = new Vector2(p.x * cos - p.y * sin, p.x * sin + p.y * cos);
        Vector2 ext = box.size * 0.5f;
        return Mathf.Abs(lp.x) <= ext.x && Mathf.Abs(lp.y) <= ext.y;
    }

    void MoveDeadTowardHome()
    {
        Vector2 target = startPoint ? (Vector2)startPoint.position : currentGrid;
        Vector2 pos = transform.position;
        Vector2 dir = (target - pos);
        float dist = dir.magnitude;
        if (dist < tileSize * 0.2f)
        {
            ExitDeadState();
            return;
        }
        dir.Normalize();
        transform.position = pos + dir * deadUnitsPerSecond * Time.deltaTime;
        currentGrid = new Vector2(Mathf.Round(transform.position.x / tileSize) * tileSize,
                                  Mathf.Round(transform.position.y / tileSize) * tileSize);
    }

    public void EnterDeadState()
    {
        SetBoolIfExists("Scared", false);
        SetBoolIfExists("Recovering", false);
        SetBoolIfExists("Dead", true);
        DisableAllColliders(true);
        isLerping = false;
        t = 0f;
    }

    void ExitDeadState()
    {
        SetBoolIfExists("Dead", false);
        if (GameManager.I && GameManager.I.IsScared)
        {
            bool anyRec = GameManager.I.IAnyRecovering();
            if (anyRec) SetBoolIfExists("Recovering", true);
            else SetBoolIfExists("Scared", true);
        }
        else
        {
            SetBoolIfExists("Recovering", false);
            SetBoolIfExists("Scared", false);
        }
        DisableAllColliders(false);
        if (GameManager.I) GameManager.I.OnGhostRevived(anim);
        if (!InsideSpawn())
        {
            currentDir = Dir.Right;
            TryStartMove(currentDir);
        }
    }

    void DisableAllColliders(bool v)
    {
        var cols = GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < cols.Length; i++) cols[i].enabled = !v;
    }

    void Face(Dir d)
    {
        SetBoolIfExists("FaceUp", d == Dir.Up);
        SetBoolIfExists("FaceDown", d == Dir.Down);
        SetBoolIfExists("FaceLeft", d == Dir.Left);
        SetBoolIfExists("FaceRight", d == Dir.Right);
    }

    void SetBoolIfExists(string p, bool v)
    {
        if (!anim) return;
        var ps = anim.parameters;
        for (int i = 0; i < ps.Length; i++)
            if (ps[i].name == p && ps[i].type == AnimatorControllerParameterType.Bool) { anim.SetBool(p, v); return; }
    }

    bool IsDead()
    {
        if (!anim) return false;
        if (HasParam("Dead") && anim.GetBool("Dead")) return true;
        return false;
    }

    bool HasParam(string p)
    {
        if (!anim) return false;
        var ps = anim.parameters;
        for (int i = 0; i < ps.Length; i++) if (ps[i].name == p) return true;
        return false;
    }

    List<Dir> AllDirs()
    {
        return new List<Dir> { Dir.Up, Dir.Right, Dir.Down, Dir.Left };
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

    Dir Opposite(Dir d)
    {
        switch (d)
        {
            case Dir.Up: return Dir.Down;
            case Dir.Down: return Dir.Up;
            case Dir.Left: return Dir.Right;
            case Dir.Right: return Dir.Left;
        }
        return Dir.None;
    }

    Dir RightOf(Dir d)
    {
        switch (d)
        {
            case Dir.Up: return Dir.Right;
            case Dir.Right: return Dir.Down;
            case Dir.Down: return Dir.Left;
            case Dir.Left: return Dir.Up;
        }
        return Dir.None;
    }

    Dir LeftOf(Dir d)
    {
        switch (d)
        {
            case Dir.Up: return Dir.Left;
            case Dir.Right: return Dir.Up;
            case Dir.Down: return Dir.Right;
            case Dir.Left: return Dir.Down;
        }
        return Dir.None;
    }

    void OnFreeze() { frozen = true; }
    void OnUnfreeze() { frozen = false; }
}
