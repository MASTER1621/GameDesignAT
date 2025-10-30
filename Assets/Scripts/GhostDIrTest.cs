using UnityEngine;

public class GhostDirTest : MonoBehaviour
{
    public Animator anim;
    void Awake(){ if(!anim) anim = GetComponent<Animator>(); }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))    anim.SetInteger("Dir", 0);
        if (Input.GetKeyDown(KeyCode.RightArrow)) anim.SetInteger("Dir", 1);
        if (Input.GetKeyDown(KeyCode.DownArrow))  anim.SetInteger("Dir", 2);
        if (Input.GetKeyDown(KeyCode.LeftArrow))  anim.SetInteger("Dir", 3);
    }
}
