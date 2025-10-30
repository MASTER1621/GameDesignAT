using UnityEngine;
using System.Collections;

public class CherryController : MonoBehaviour
{
    public BoxCollider2D levelBounds;
    public bool useManualBounds = true;
    public Vector2 manualMin = new Vector2(0f, -520f);
    public Vector2 manualMax = new Vector2(540f, -10f);

    public Transform centerPoint;
    public bool useManualCenter = true;
    public Vector2 manualCenter = new Vector2(270f, -280f);

    public SpriteRenderer cherryPrefab;
    public float spawnDelay = 5f;
    public float margin = 30f;
    public float travelTime = 5f;
    public bool axisAligned = false;

    Transform current;
    Vector2 startPos, endPos, center;
    float t;
    int lastSide = -1; // 0=L,1=R,2=B,3=T

    void Start(){ StartCoroutine(Loop()); }

    IEnumerator Loop()
    {
        yield return new WaitForSeconds(spawnDelay);
        while (true)
        {
            Spawn();
            while (current != null) { MoveUpdate(); yield return null; }
            yield return new WaitForSeconds(spawnDelay);
        }
    }

    void ResolveBounds(out Bounds b)
    {
        if (!useManualBounds && levelBounds) { b = levelBounds.bounds; return; }
        var size = manualMax - manualMin;
        b = new Bounds((manualMin + manualMax) * 0.5f, new Vector3(size.x, size.y, 1f));
    }

    Vector2 ResolveCenter(Bounds b)
    {
        if (centerPoint) return centerPoint.position;
        if (useManualCenter) return manualCenter;
        return b.center;
    }

    int PickSide()
    {
        if (!axisAligned)
        {
            int s = Random.Range(0, 4);
            int tries = 0;
            while (s == lastSide && tries++ < 8) s = Random.Range(0, 4);
            lastSide = s;
            return s;
        }
        else
        {
            bool horizontal = Random.value < 0.5f;
            int s = horizontal ? (Random.value < 0.5f ? 0 : 1) : (Random.value < 0.5f ? 2 : 3);
            if (s == lastSide) s = (s + 1) % 4;
            lastSide = s;
            return s;
        }
    }

    void Spawn()
    {
        if (!cherryPrefab) return;

        ResolveBounds(out var b);
        center = ResolveCenter(b);

        int side = PickSide();
        float x = 0f, y = 0f;

        if (side == 0) { x = b.min.x - margin; y = Random.Range(b.min.y, b.max.y); } // left
        if (side == 1) { x = b.max.x + margin; y = Random.Range(b.min.y, b.max.y); } // right
        if (side == 2) { y = b.min.y - margin; x = Random.Range(b.min.x, b.max.x); } // bottom
        if (side == 3) { y = b.max.y + margin; x = Random.Range(b.min.x, b.max.x); } // top

        startPos = new Vector2(x, y);
        endPos = center + (center - startPos);

        var sr = Instantiate(cherryPrefab);
        current = sr.transform;
        current.position = startPos;

        sr.sortingLayerName = "Top";
        sr.sortingOrder = 1500;
        sr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

        t = 0f;
    }

    void MoveUpdate()
    {
        if (!current) return;
        t += Time.deltaTime / Mathf.Max(0.001f, travelTime);
        float tt = Mathf.Clamp01(t);
        current.position = Vector2.Lerp(startPos, endPos, tt);
        if (tt >= 1f) { Destroy(current.gameObject); current = null; }
    }
}
