using TMPro;
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager I;

    [Header("UI")]
    public TMP_Text scoreText;
    public TMP_Text ghostTimerText;
    public TMP_Text powerLabelText;
    public TMP_Text timerText;
    public Transform livesGroup;

    [Header("Round Overlay")]
    public GameObject roundOverlayPanel;
    public TMP_Text roundCountdownText;
    public float countdownStep = 1f;

    [Header("Ghosts")]
    public Animator[] ghostAnimators;

    [Header("Music")]
    public AudioSource musicSrc;
    public AudioClip normalBGM;
    public AudioClip scaredBGM;
    public AudioSource sfxSrc;
    public AudioClip pacDeathClip;
    public AudioClip ghostEatenClip;

    [Header("Pac")]
    public PacStudentController pac;
    public ParticleSystem pacDeathFXPrefab;
    public Vector2 pacStartWorld = new Vector2(20f, -20f);

    [Header("State")]
    public int score = 0;
    public int lives = 3;

    [Header("Power UI Animation")]
    public float powerUISlideDistance = 220f;
    public float powerUIAnimTime = 0.18f;

    [Header("Pellets")]
    public int pelletsRemaining = 0;

    float scaredTimer = 0f;
    bool isScared = false;
    bool isRecovering = false;

    float gameTime = 0f;
    bool gameOver = false;

    Vector3 pacStartPos;
    Vector3[] ghostStartPos;

    bool pacIsDying = false;
    float pacNoHitUntil = 0f;

    RectTransform powerRT;
    RectTransform timerRT;
    Vector2 powerPosTarget;
    Vector2 timerPosTarget;
    Coroutine powerUICo;

    public bool roundStarted { get; private set; }
    public bool IsScared => isScared;

    void Awake()
    {
        I = this;
        UpdateScoreUI();
        UpdateLivesUI();
        HideGhostTimerImmediate();
        if (timerText) timerText.text = "00:00:00";

        float z = pac ? pac.transform.position.z : 0f;
        pacStartPos = new Vector3(pacStartWorld.x, pacStartWorld.y, z);

        if (ghostAnimators != null && ghostAnimators.Length > 0)
        {
            ghostStartPos = new Vector3[ghostAnimators.Length];
            for (int i = 0; i < ghostAnimators.Length; i++)
                ghostStartPos[i] = ghostAnimators[i] ? ghostAnimators[i].transform.position : Vector3.zero;
        }
        else ghostStartPos = new Vector3[0];

        if (powerLabelText) powerRT = powerLabelText.transform as RectTransform;
        if (ghostTimerText) timerRT = ghostTimerText.transform as RectTransform;
        if (powerRT) powerPosTarget = powerRT.anchoredPosition;
        if (timerRT) timerPosTarget = timerRT.anchoredPosition;
    }

    void Start()
    {
        roundStarted = false;
        gameOver = false;
        gameTime = 0f;
        if (timerText) timerText.text = "00:00:00";

        pelletsRemaining = CountPelletsInScene();

        if (pac) { pac.RespawnAt(pacStartPos); pac.SetInputEnabled(false); pac.SetCollidable(false); }
        FreezeGhosts(true);
        if (roundOverlayPanel) roundOverlayPanel.SetActive(true);
        if (roundCountdownText) roundCountdownText.gameObject.SetActive(true);
        StartCoroutine(RoundStartRoutine());
    }

    void Update()
    {
        if (roundStarted && !gameOver)
        {
            gameTime += Time.deltaTime;
            UpdateMainTimerUI();
        }

        if (isScared && !gameOver)
        {
            scaredTimer -= Time.deltaTime;
            if (!isRecovering && scaredTimer <= 3f) { isRecovering = true; SetGhostsRecovering(); }
            UpdateGhostTimerUI(Mathf.Max(scaredTimer, 0f));
            if (scaredTimer <= 0f) EndScared();
        }
    }

    IEnumerator RoundStartRoutine()
    {
        if (roundCountdownText) roundCountdownText.text = "3";
        yield return new WaitForSeconds(countdownStep);
        if (roundCountdownText) roundCountdownText.text = "2";
        yield return new WaitForSeconds(countdownStep);
        if (roundCountdownText) roundCountdownText.text = "1";
        yield return new WaitForSeconds(countdownStep);
        if (roundCountdownText) roundCountdownText.text = "GO!";
        yield return new WaitForSeconds(1f);

        if (roundOverlayPanel) roundOverlayPanel.SetActive(false);
        if (roundCountdownText) roundCountdownText.gameObject.SetActive(false);

        roundStarted = true;
        if (pac) { pac.SetCollidable(true); pac.SetInputEnabled(true); }
        FreezeGhosts(false);
        SwitchMusic(normalBGM);
    }

    void UpdateMainTimerUI()
    {
        if (!timerText) return;
        int ms = Mathf.Max(0, Mathf.RoundToInt(gameTime * 1000f));
        int mm = ms / 60000;
        int ss = (ms % 60000) / 1000;
        int cs = (ms % 1000) / 10;
        timerText.text = mm.ToString("00") + ":" + ss.ToString("00") + ":" + cs.ToString("00");
    }

    public void AddScore(int v)
    {
        score += v;
        if (score < 0) score = 0;
        UpdateScoreUI();
    }

    public void StartScared(float duration = 10f)
    {
        scaredTimer = duration;
        isScared = true;
        isRecovering = false;
        SetGhostsScared();
        ShowGhostTimerAnimated();
        SwitchMusic(scaredBGM);
        UpdateGhostTimerUI(scaredTimer);
    }

    void EndScared()
    {
        isScared = false;
        isRecovering = false;
        scaredTimer = 0f;
        SetGhostsNormal();
        HideGhostTimerImmediate();
        SwitchMusic(normalBGM);
    }

    void UpdateScoreUI()
    {
        if (scoreText) scoreText.text = score.ToString("D6");
    }

    void UpdateLivesUI()
    {
        if (!livesGroup) return;
        int n = livesGroup.childCount;
        for (int i = 0; i < n; i++)
            livesGroup.GetChild(i).gameObject.SetActive(i < lives);
    }

    void ShowGhostTimerAnimated()
    {
        if (powerLabelText) powerLabelText.gameObject.SetActive(true);
        if (ghostTimerText) ghostTimerText.gameObject.SetActive(true);

        if (powerRT) powerRT.anchoredPosition = powerPosTarget + Vector2.left * powerUISlideDistance;
        if (timerRT) timerRT.anchoredPosition = timerPosTarget + Vector2.left * powerUISlideDistance;

        if (powerUICo != null) StopCoroutine(powerUICo);
        powerUICo = StartCoroutine(IE_PowerUI_PopIn());
    }

    IEnumerator IE_PowerUI_PopIn()
    {
        float t = 0f;
        while (t < powerUIAnimTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / powerUIAnimTime);
            k = k * k * (3f - 2f * k);
            if (powerRT) powerRT.anchoredPosition = Vector2.Lerp(powerPosTarget + Vector2.left * powerUISlideDistance, powerPosTarget, k);
            if (timerRT) timerRT.anchoredPosition = Vector2.Lerp(timerPosTarget + Vector2.left * powerUISlideDistance, timerPosTarget, k);
            yield return null;
        }
        if (powerRT) powerRT.anchoredPosition = powerPosTarget;
        if (timerRT) timerRT.anchoredPosition = timerPosTarget;
        powerUICo = null;
    }

    void HideGhostTimerImmediate()
    {
        if (ghostTimerText) ghostTimerText.gameObject.SetActive(false);
        if (powerLabelText) powerLabelText.gameObject.SetActive(false);
    }

    void UpdateGhostTimerUI(float seconds)
    {
        if (!ghostTimerText) return;
        int totalCentis = Mathf.Max(0, Mathf.RoundToInt(seconds * 100f));
        int mm = totalCentis / 6000;
        int ss = (totalCentis % 6000) / 100;
        int cs = totalCentis % 100;
        ghostTimerText.text = mm.ToString("00") + ":" + ss.ToString("00") + ":" + cs.ToString("00");
    }

    void SwitchMusic(AudioClip clip)
    {
        if (!musicSrc) return;
        if (musicSrc.clip == clip && musicSrc.isPlaying) return;
        musicSrc.clip = clip;
        musicSrc.loop = true;
        musicSrc.Play();
    }

    bool HasParam(Animator a, string p)
    {
        var ps = a.parameters;
        for (int i = 0; i < ps.Length; i++) if (ps[i].name == p) return true;
        return false;
    }

    bool IsDead(Animator a)
    {
        if (!a) return false;
        if (HasParam(a, "Dead") && a.GetBool("Dead")) return true;
        var clips = a.GetCurrentAnimatorClipInfo(0);
        for (int i = 0; i < clips.Length; i++)
            if (clips[i].clip && clips[i].clip.name.ToLower().Contains("dead")) return true;
        return false;
    }

    void SetBoolIfExists(Animator a, string param, bool v)
    {
        if (!a) return;
        var ps = a.parameters;
        for (int i = 0; i < ps.Length; i++)
            if (ps[i].name == param && ps[i].type == AnimatorControllerParameterType.Bool) { a.SetBool(param, v); return; }
    }

    void SetGhostsScared()
    {
        if (ghostAnimators == null) return;
        for (int i = 0; i < ghostAnimators.Length; i++)
        {
            var a = ghostAnimators[i];
            if (!a) continue;
            if (IsDead(a)) continue;
            SetBoolIfExists(a, "Recovering", false);
            SetBoolIfExists(a, "Scared", true);
            a.gameObject.BroadcastMessage("OnScaredStart", SendMessageOptions.DontRequireReceiver);
        }
    }

    void SetGhostsRecovering()
    {
        if (ghostAnimators == null) return;
        for (int i = 0; i < ghostAnimators.Length; i++)
        {
            var a = ghostAnimators[i];
            if (!a) continue;
            if (IsDead(a)) continue;
            SetBoolIfExists(a, "Scared", false);
            SetBoolIfExists(a, "Recovering", true);
            a.gameObject.BroadcastMessage("OnScaredRecovering", SendMessageOptions.DontRequireReceiver);
        }
    }

    void SetGhostsNormal()
    {
        if (ghostAnimators == null) return;
        for (int i = 0; i < ghostAnimators.Length; i++)
        {
            var a = ghostAnimators[i];
            if (!a) continue;
            if (IsDead(a)) continue;
            SetBoolIfExists(a, "Scared", false);
            SetBoolIfExists(a, "Recovering", false);
            a.gameObject.BroadcastMessage("OnScaredEnd", SendMessageOptions.DontRequireReceiver);
        }
    }

    public void OnPacHitByNormalGhost(Animator ghost)
    {
        if (pacIsDying) return;
        if (Time.time < pacNoHitUntil) return;
        if (!gameObject.activeInHierarchy) return;
        StartCoroutine(PacDeathSequence());
    }

    public void OnGhostEaten(Animator ghost)
    {
        if (!ghost) return;
        AddScore(300);
        if (sfxSrc && ghostEatenClip) sfxSrc.PlayOneShot(ghostEatenClip);
        var ctrl = ghost.GetComponent<GhostController>();
        SetBoolIfExists(ghost, "Scared", false);
        SetBoolIfExists(ghost, "Recovering", false);
        SetBoolIfExists(ghost, "Dead", true);
        if (ctrl) ctrl.EnterDeadState();
    }

    public void OnGhostRevived(Animator ghost)
    {
        if (!AnyGhostDead())
        {
            if (IsScared) SwitchMusic(scaredBGM);
            else SwitchMusic(normalBGM);
        }
    }

    public bool IAnyRecovering()
    {
        if (ghostAnimators == null) return false;
        for (int i = 0; i < ghostAnimators.Length; i++)
            if (HasParam(ghostAnimators[i], "Recovering") && ghostAnimators[i].GetBool("Recovering")) return true;
        return false;
    }

    bool AnyGhostDead()
    {
        if (ghostAnimators == null) return false;
        for (int i = 0; i < ghostAnimators.Length; i++)
            if (IsDead(ghostAnimators[i])) return true;
        return false;
    }

    IEnumerator PacDeathSequence()
    {
        pacIsDying = true;
        if (pac) { pac.ForceStopAtCurrentCell(); pac.SetCollidable(false); pac.SetInputEnabled(false); }
        FreezeGhosts(true);
        if (sfxSrc && pacDeathClip) sfxSrc.PlayOneShot(pacDeathClip);
        if (pacDeathFXPrefab && pac) { var ps = Instantiate(pacDeathFXPrefab, pac.transform.position, Quaternion.identity); ps.Play(); Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax + 0.25f); }
        if (isScared) EndScared();
        lives = Mathf.Max(0, lives - 1);
        UpdateLivesUI();
        yield return new WaitForSeconds(1.2f);
        if (lives <= 0) { StartCoroutine(GameOverRoutine()); pacIsDying = false; yield break; }
        RespawnPacAndGhosts();
        pacNoHitUntil = Time.time + 1f;
        if (pac) { pac.SetCollidable(true); pac.SetInputEnabled(true); }
        FreezeGhosts(false);
        pacIsDying = false;
    }

    void FreezeGhosts(bool v)
    {
        if (ghostAnimators == null) return;
        for (int i = 0; i < ghostAnimators.Length; i++)
        {
            var a = ghostAnimators[i];
            if (!a) continue;
            a.speed = v ? 0f : 1f;
            a.gameObject.BroadcastMessage(v ? "OnFreeze" : "OnUnfreeze", SendMessageOptions.DontRequireReceiver);
        }
    }

    void RespawnPacAndGhosts()
    {
        if (pac) pac.RespawnAt(pacStartPos);
        if (ghostAnimators != null)
        {
            for (int i = 0; i < ghostAnimators.Length; i++)
            {
                var a = ghostAnimators[i];
                if (!a) continue;
                a.transform.position = ghostStartPos.Length > i ? ghostStartPos[i] : a.transform.position;
                SetBoolIfExists(a, "Dead", false);
                SetBoolIfExists(a, "Scared", false);
                SetBoolIfExists(a, "Recovering", false);
            }
        }
        SwitchMusic(normalBGM);
    }

    public void OnPelletCollected()
    {
        pelletsRemaining = Mathf.Max(0, pelletsRemaining - 1);
        CheckGameOverByPellets();
    }

    int CountPelletsInScene()
    {
        int a = 0;
        var p = GameObject.FindGameObjectsWithTag("Pellet");
        var pp = GameObject.FindGameObjectsWithTag("PowerPellet");
        if (p != null) a += p.Length;
        if (pp != null) a += pp.Length;
        return a;
    }

    void CheckGameOverByPellets()
    {
        if (gameOver) return;
        if (pelletsRemaining <= 0) StartCoroutine(GameOverRoutine());
    }

    IEnumerator GameOverRoutine()
    {
        if (gameOver) yield break;
        gameOver = true;
        FreezeGhosts(true);
        if (pac) { pac.ForceStopAtCurrentCell(); pac.SetInputEnabled(false); pac.SetCollidable(false); }
        if (roundOverlayPanel) roundOverlayPanel.SetActive(true);
        if (roundCountdownText) { roundCountdownText.gameObject.SetActive(true); roundCountdownText.text = "GAME OVER"; }
        SaveIfHighScore();
        yield return new WaitForSeconds(5f);
        SceneManager.LoadScene("StartScene");
    }

    void SaveIfHighScore()
    {
        int prevScore = PlayerPrefs.GetInt("HighScore_Level1", 0);
        int prevMs = PlayerPrefs.GetInt("BestTime_Level1_ms", 0);
        int curMs = Mathf.Max(0, Mathf.RoundToInt(gameTime * 1000f));
        bool better = score > prevScore || (score == prevScore && (prevMs == 0 || curMs < prevMs));
        if (better)
        {
            PlayerPrefs.SetInt("HighScore_Level1", score);
            PlayerPrefs.SetInt("BestTime_Level1_ms", curMs);
            PlayerPrefs.Save();
        }
    }
}
