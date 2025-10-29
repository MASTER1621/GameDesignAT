using UnityEngine;

[ExecuteAlways]
public class LVLbuild : MonoBehaviour
{
    [Header("Tile Sprites (drag from Assets/Sprites/Tiles)")]
    public Sprite Empty;           // "Empty"
    public Sprite OutsideCorner;   // "OutsideCorner"
    public Sprite OutsideWall;     // "OutsideWall"
    public Sprite InsideCorner;    // "InsideCorner"
    public Sprite InsideWall;      // "InsideWall"
    public Sprite PelletSpot;      // "PelletSpot" (can be transparent)
    public Sprite PowerSpot;       // "PowerSpot"  (can be transparent)
    public Sprite TJunction;       // "TJunction"
    public Sprite GhostExitWall;   // "GhostExitWall"

    [Header("Grid")]
    public float tileSize = 1f;    // 1 unit per tile

    [Header("Optional pellets (Sprites/Pickups)")]
    public bool placePellets = false;
    public Sprite Pellet_Normal;
    public Sprite Pellet_Power;
    public Transform pelletsParent; // leave empty to parent to this builder

    // 0 Empty, 1 OutsideCorner, 2 OutsideWall, 3 InsideCorner, 4 InsideWall,
    // 5 PelletSpot, 6 PowerSpot, 7 TJunction, 8 GhostExitWall
    // IMPORTANT: paste your friend's numbers here.
    // If you forget, a tiny 4x4 test map will be used so you can see SOMETHING.
    int[,] levelMap =
    {
        // --- REMOVE THIS TEST BLOCK once you paste your real numbers ---
        {1,2,2,2,2,2,2,2,2,2,2,2,2,7}, {2,5,5,5,5,5,5,5,5,5,5,5,5,4}, {2,5,3,4,4,3,5,3,4,4,4,3,5,4}, {2,6,4,0,0,4,5,4,0,0,0,4,5,4}, {2,5,3,4,4,3,5,3,4,4,4,3,5,3}, {2,5,5,5,5,5,5,5,5,5,5,5,5,5}, {2,5,3,4,4,3,5,3,3,5,3,4,4,4}, {2,5,3,4,4,3,5,4,4,5,3,4,4,3}, {2,5,5,5,5,5,5,4,4,5,5,5,5,4}, {1,2,2,2,2,1,5,4,3,4,4,3,0,4}, {0,0,0,0,0,2,5,4,3,4,4,3,0,3}, {0,0,0,0,0,2,5,4,4,0,0,0,0,0}, {0,0,0,0,0,2,5,4,4,0,3,4,4,8}, {2,2,2,2,2,1,5,3,3,0,4,0,0,0}, {0,0,0,0,0,0,5,0,0,0,4,0,0,0},
        // --------------------------------------------------------------
    };

    [ContextMenu("Build Into Scene (Editor)")]
    public void BuildIntoScene()
    {
        if (!enabled) enabled = true;   // ensure script runs in edit
        ClearChildren();
        BuildLevel();
        Debug.Log("[LVLbuild] Built tiles into the scene.");
    }

    void Start()
    {
        // Runs in Play mode; not needed for marking but handy
        if (Application.isPlaying)
        {
            ClearChildren();
            BuildLevel();
        }
    }

    void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);
    }

    void BuildLevel()
    {
        Sprite[] byIndex = new Sprite[9] {
            Empty,         // 0
            OutsideCorner, // 1
            OutsideWall,   // 2
            InsideCorner,  // 3
            InsideWall,    // 4
            PelletSpot,    // 5
            PowerSpot,     // 6
            TJunction,     // 7
            GhostExitWall  // 8
        };

        int rows = levelMap.GetLength(0);
        int cols = levelMap.GetLength(1);

        if (rows == 0 || cols == 0)
        {
            Debug.LogWarning("[LVLbuild] levelMap is empty. Paste your numbers into the script.");
            return;
        }

        for (int y = 0; y < rows; y++)
        for (int x = 0; x < cols; x++)
        {
            int idx = levelMap[y, x];
            if (idx < 0 || idx >= byIndex.Length || byIndex[idx] == null) continue;

            var tile = new GameObject($"{idx}_{x}_{y}");
            tile.transform.SetParent(transform);
            tile.transform.localPosition = new Vector3(x * tileSize, -y * tileSize, 0f);

            var sr = tile.AddComponent<SpriteRenderer>();
            sr.sprite = byIndex[idx];

            // optionally drop pellet sprites on 5/6
            if (placePellets && (idx == 5 || idx == 6))
            {
                Transform parent = pelletsParent == null ? transform : pelletsParent;
                var pellet = new GameObject(idx == 6 ? $"Power_{x}_{y}" : $"Pellet_{x}_{y}");
                pellet.transform.SetParent(parent);
                pellet.transform.localPosition = tile.transform.localPosition;

                var psr = pellet.AddComponent<SpriteRenderer>();
                psr.sprite = (idx == 6) ? Pellet_Power : Pellet_Normal;
                psr.sortingOrder = sr.sortingOrder + 1; // on top
            }
        }
    }
}
