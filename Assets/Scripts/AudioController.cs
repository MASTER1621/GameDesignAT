using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioController : MonoBehaviour
{
    public AudioSource source;
    public AudioClip introBGM;
    public AudioClip normalBGM;

    IEnumerator Start()
    {
        if (source == null) source = GetComponent<AudioSource>();
        source.loop = false;                 // intro: no loop
        if (introBGM != null)
        {
            source.clip = introBGM;
            source.Play();
            float wait = Mathf.Min(introBGM.length, 3f);
            yield return new WaitForSeconds(wait);
        }
        source.loop = true;                  // normal: loop
        source.clip = normalBGM;
        source.Play();
    }
}
