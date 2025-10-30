using System.Collections;
using UnityEngine;

public class AudioController : MonoBehaviour
{
    public AudioSource source;
    public AudioClip introBGM;
    public AudioClip normalBGM;

    IEnumerator Start()
    {
        if (source == null) source = GetComponent<AudioSource>();
        if (GameManager.I) GameManager.I.musicSrc = source;

        source.loop = false;
        if (introBGM != null)
        {
            source.clip = introBGM;
            source.Play();
            float wait = Mathf.Min(introBGM.length, 3f);
            yield return new WaitForSeconds(wait);
        }

        while (GameManager.I && !GameManager.I.roundStarted) yield return null;

        while (GameManager.I && GameManager.I.IsScared) yield return null;

        source.loop = true;
        source.clip = normalBGM;
        source.Play();
    }
}
