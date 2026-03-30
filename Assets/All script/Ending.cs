using StarterAssets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class GameResultManager : MonoBehaviour
{
    public static GameResultManager Instance;

    [Header("Settings")]
    public int totalTargetNPC = 75;
    public GameObject endScreenUI;
    public TextMeshProUGUI resultText;
    public float typingSpeed = 0.05f;

    [Header("References")]
    public PlayerHealth pHealth;

    private bool isGameEnded = false;

    void Awake()
    {
        Instance = this;
        if (endScreenUI != null) endScreenUI.SetActive(false);
    }

    void Update()
    {
        if (isGameEnded) return;

        CheckStatusConditions();
        CheckNPCConditions();
    }

    void CheckStatusConditions()
    {
        if (pHealth == null) pHealth = FindObjectOfType<PlayerHealth>();

        if (pHealth != null && pHealth.currentHealth <= 0)
        {
            EndGame("ENDING\nYour body can no longer withstand the trauma... Even with a determined spirit to help others, without life, you cannot protect anyone. Data shows you often put yourself in too much risk. Remember... a dead hero can't save anyone else.");
        }

        if (SanityManager.Instance != null && SanityManager.Instance.currentSanity <= 0)
        {
            EndGame("ENDING\nThe sight of death before your eyes shattered your sanity... Information indicates you were carrying an overwhelming burden of guilt, more than any human being could bear. In this cruel world, sometimes the first thing to break is your own mind.");
        }
    }

    void CheckNPCConditions()
    {
        if (AnalyticsManager.Instance != null)
        {
            // เมื่อจัดการ NPC จนครบจำนวนเป้าหมาย
            if (AnalyticsManager.Instance.totalProcessedNPC >= totalTargetNPC)
            {
                string finalMessage = GetBranchingEndingMessage();
                EndGame(finalMessage);
            }
        }
    }

    // 🔥 ฟังก์ชันคำนวณฉากจบตามเงื่อนไข 4 แบบที่นายต้องการ
    string GetBranchingEndingMessage()
    {
        int familySaved = AnalyticsManager.Instance.familySavedCount;
        int totalDead = AnalyticsManager.Instance.deadCount;

        // เงื่อนไข 1: ครอบครัวรอดครบ 4 คน
        if (familySaved >= 4)
        {
            if (totalDead < 3)
            {
                return "ENDING[ THE HERO ]\nYou did it! You protected your entire family and managed to save almost everyone else. Your compassion and skill have brought a rare light to this dark world.";
            }
            else
            {
                return "ENDING[ THE PAINFUL CHOICE ]\nYour family is safe, but it came at a high cost. Many strangers were left behind to secure the ones you love. You are a protector, but the shadows of those you couldn't save will follow you.";
            }
        }
        // เงื่อนไข 2: ครอบครัวรอดไม่ครบ 4 คน
        else
        {
            if (totalDead < 3)
            {
                return "ENDING[ THE SACRIFICE ]\nYou fought to save as many souls as possible, but in your kindness for strangers, you couldn't keep your own family whole. A heavy price for the greater good.";
            }
            else
            {
                return "ENDING[ THE TRAGEDY ]\nMission ends in despair. You couldn't protect your family, and death took many others under your watch. This war has left you with nothing but bitter memories.";
            }
        }
    }

    void EndGame(string message)
    {
        isGameEnded = true;
        Time.timeScale = 0;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (pHealth != null)
        {
            var controller = pHealth.GetComponent<ThirdPersonController>();
            if (controller != null) controller.enabled = false;

            var sapInput = pHealth.GetComponent<StarterAssetsInputs>();
            if (sapInput != null)
            {
                sapInput.cursorLocked = false;
                sapInput.cursorInputForLook = false;
            }
        }

        if (endScreenUI != null) endScreenUI.SetActive(true);

        if (resultText != null)
        {
            StopAllCoroutines();
            StartCoroutine(TypeText(message));
        }

        Debug.Log("<color=gold>" + message + "</color>");

        if (AnalyticsManager.Instance != null)
        {
            AnalyticsManager.Instance.LogEvent("GAME_ENDED", "FINISH_SCENE", SanityManager.Instance.currentSanity);
        }
    }

    IEnumerator TypeText(string textToType)
    {
        resultText.text = "";
        foreach (char letter in textToType.ToCharArray())
        {
            resultText.text += letter;
            yield return new WaitForSecondsRealtime(typingSpeed);
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        MissileRain.isMissileActive = false;
        // ✨ เพิ่มตรงนี้: หา NPC ทุกตัวในฉากแล้วสั่งลบทิ้งให้หมดก่อนโหลดใหม่
        // เพื่อป้องกันตัวที่อาจจะติด DontDestroyOnLoad หรือเป็นลูกของ Object อื่นอยู่
        NPC_QueryMovement[] allNPCs = FindObjectsOfType<NPC_QueryMovement>();
        foreach (NPC_QueryMovement npc in allNPCs)
        {
            // ถ้ามันเป็นลูกของใครอยู่ (เช่น โดนอุ้ม) ให้เอาออกก่อนลบ
            npc.transform.SetParent(null);
            Destroy(npc.gameObject);
        }

        if (AnalyticsManager.Instance != null)
        {
            AnalyticsManager.Instance.totalProcessedNPC = 0;
            AnalyticsManager.Instance.familySavedCount = 0;
            AnalyticsManager.Instance.deadCount = 0;
        }

        // แล้วค่อยโหลด Scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void ExitToMainMenu()
    {
        Time.timeScale = 1;
        Application.Quit();
    }
}