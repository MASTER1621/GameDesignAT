using UnityEngine;

[ExecuteAlways]
public class LVLbuild : MonoBehaviour
{
    [Header("Tile Sprites (drag from Assets/Sprites/Tiles)")]
    public Sprite Empty;         
    public Sprite OutsideCorner;  
    public Sprite OutsideWall;     
    public Sprite InsideCorner;   
    public Sprite InsideWall;    
    public Sprite PelletSpot;     
    public Sprite PowerSpot;       
    public Sprite TJunction;       
    public Sprite GhostExitWall;   

    [Header("Grid")]
    public float tileSize = 1f;    

    [Header("Optional pellets (Sprites/Pickups)")]
    public bool placePellets = false;
    public Sprite Pellet_Normal;
    public Sprite Pellet_Power;
    public Transform pelletsParent; 

    int[,] levelMap =
    {
        {1,2,2,2,2,2,2,2,2,2,2,2,2,7}, {2,5,5,5,5,5,5,5,5,5,5,5,5,4}, {2,5,3,4,4,3,5,3,4,4,4,3,5,4}, {2,6,4,0,0,4,5,4,0,0,0,4,5,4}, {2,5,3,4,4,3,5,3,4,4,4,3,5,3}, {2,5,5,5,5,5,5,5,5,5,5,5,5,5}, {2,5,3,4,4,3,5,3,3,5,3,4,4,4}, {2,5,3,4,4,3,5,4,4,5,3,4,4,3}, {2,5,5,5,5,5,5,4,4,5,5,5,5,4}, {1,2,2,2,2,1,5,4,3,4,4,3,0,4}, {0,0,0,0,0,2,5,4,3,4,4,3,0,3}, {0,0,0,0,0,2,5,4,4,0,0,0,0,0}, {0,0,0,0,0,2,5,4,4,0,3,4,4,8}, {2,2,2,2,2,1,5,3,3,0,4,0,0,0}, {0,0,0,0,0,0,5,0,0,0,4,0,0,0},

    };

    [ContextMenu("Build Into Scene (Editor)")]
    public void BuildIntoScene()
    {
        if (!enabled) enabled = true;  
        ClearChildren();
        BuildLevel();
        Debug.Log("[LVLbuild] Built tiles into the scene.");
    }

    void Start()
    {
        
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
