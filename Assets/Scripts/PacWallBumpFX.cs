using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class PacWallBumpFX : MonoBehaviour
{
    public float tileSize = 20f;
    public LayerMask wallMask;               
    public string gateTag = "GhostExitWall"; 

    [Header("FX")]
    public ParticleSystem bumpParticlesPrefab; 
    public AudioSource sfxOneShotSource;       
    public AudioClip wallBumpClip;

    [Header("Grid (don’t hardcode)")]
    public Transform gridOrigin;     
    public float centerEpsilon = 0.05f; 
    public float bumpCooldown = 0.15f;  


    Vector3 _prevPos;
    Vector2 _lastMoveDir = Vector2.zero;
    float _lastBumpTime = -999f;
    CircleCollider2D _col;

    void Awake()
    {
        _col = GetComponent<CircleCollider2D>();
        _prevPos = transform.position;
    }

    void Update()
    {
     
        CheckTurnTapWallBump();

        CheckForwardHitWallBump();

        _prevPos = transform.position;
    }

    void CheckTurnTapWallBump()
    {
        Vector2 dir;
        if      (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))    dir = Vector2.up;
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))  dir = Vector2.down;
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))  dir = Vector2.left;
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) dir = Vector2.right;
        else return;

        Vector3 center = GridCenter(transform.position);
        Vector3 nextCenter = center + (Vector3)(dir * tileSize);

        if (IsBlocked(nextCenter))
        {
            PlayBump(dir);
        }
    }

    void CheckForwardHitWallBump()
    {
 
        Vector2 delta = (Vector2)(transform.position - _prevPos);
        if (delta.sqrMagnitude > 0.0001f)
        {
            
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y)) _lastMoveDir = new Vector2(Mathf.Sign(delta.x), 0f);
            else                                         _lastMoveDir = new Vector2(0f, Mathf.Sign(delta.y));
        }

        if (_lastMoveDir == Vector2.zero) return;

        Vector3 center = GridCenter(transform.position);
        float nearCenter = (transform.position - center).magnitude;
        if (nearCenter > tileSize * centerEpsilon) return;

        Vector3 nextCenter = center + (Vector3)(_lastMoveDir * tileSize);
        if (IsBlocked(nextCenter))
        {
        
            if (Time.time - _lastBumpTime > bumpCooldown)
            {
                PlayBump(_lastMoveDir);
            }
        }
    }

 
    Vector3 GridCenter(Vector3 world)
    {
        Vector2 origin = gridOrigin ? (Vector2)gridOrigin.position : Vector2.zero;
        float x = Mathf.Round((world.x - origin.x) / tileSize) * tileSize + origin.x;
        float y = Mathf.Round((world.y - origin.y) / tileSize) * tileSize + origin.y;
        return new Vector3(x, y, world.z);
    }

    bool IsBlocked(Vector3 worldCenterOfNextCell)
    {
     
        float r = tileSize * 0.4f;
        var hits = Physics2D.OverlapCircleAll(worldCenterOfNextCell, r, wallMask);
        if (hits != null && hits.Length > 0) return true;

    
        var all = Physics2D.OverlapCircleAll(worldCenterOfNextCell, r);
        foreach (var h in all)
        {
            if (h.CompareTag(gateTag)) return true;
        }
        return false;
    }

    void PlayBump(Vector2 dir)
    {
        _lastBumpTime = Time.time;

        
        Vector3 center = _col.bounds.center;
        float radiusWorld = _col.radius * Mathf.Max(transform.localScale.x, transform.localScale.y);
        Vector3 contact = center + (Vector3)(dir.normalized * radiusWorld);

        
        if (bumpParticlesPrefab)
        {
            var ps = Instantiate(bumpParticlesPrefab, contact, Quaternion.identity);
            ps.Play();
            Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax + 0.25f);
        }

        if (sfxOneShotSource && wallBumpClip)
        {
            sfxOneShotSource.PlayOneShot(wallBumpClip);
        }
    }

    public void ManualBump(Vector2 dir) => PlayBump(dir);
}
