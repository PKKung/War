using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class GeminiAIController : MonoBehaviour
{
    [Header("Settings")]
    public string apiKey = "ใส่_API_KEY_ของนายตรงนี้";

    // ใช้ v1beta และ gemini-1.5-flash เพื่อโควตาที่เยอะกว่าและฉลาดพอสำหรับวิเคราะห์ CSV
    private string url = "https://generativelanguage.googleapis.com/v1/models/gemini-2.5-flash-lite:generateContent?key=";

    [Header("UI Reference")]
    public TextMeshProUGUI uiDisplayText;

    [Header("UI to Hide/Show")]
    public GameObject endScreenContent;
    public GameObject restartButton;
    public GameObject backButton;
    public GameObject aiAnalysisButton;
    [Header("UI Reference")]
    public GameObject aiScrollView; // ลาก Object "Scroll View" ตัวแม่มาใส่ช่องนี้
     

    // 1. ฟังก์ชันเรียกใช้งาน (ผูกกับปุ่ม OnClick ของปุ่ม AI Analysis)
    public void RequestAIAnalysis()
    {
        var manager = AnalyticsManager.Instance;
        if (manager == null)
        {
            Debug.LogError("AnalyticsManager ไม่ทำงานในฉากนี้!");
            return;
        }

        // ซ่อนหน้าจอจบปกติ เพื่อโชว์หน้าจอ AI
        if (endScreenContent != null) endScreenContent.SetActive(false);
        if (restartButton != null) restartButton.SetActive(false);

        // --- สร้างข้อมูล CSV 7 คอลัมน์ตามที่ออกแบบไว้ ---
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Time,EventType,NPCTag,Sanity,DistanceToSafe,FamilyLeft,PlayerHP");

        foreach (var data in manager.sessionHistory)
        {
            sb.AppendLine($"{data.time:F2},{data.eventType},{data.npcTag},{data.sanity:F1},{data.distanceToSafe:F2},{data.familyLeft},{data.playerHP:F1}");
        }

        if (uiDisplayText != null)
        {
            uiDisplayText.gameObject.SetActive(true);
            aiScrollView.SetActive(true);
            uiDisplayText.text = "AI is analyzing your behavior...";
        }

        // เริ่ม Coroutine ส่งข้อมูล
        StartCoroutine(PostToGemini(sb.ToString()));
    }

    // 2. ฟังก์ชันส่งข้อมูลและรับคำตอบจาก Gemini API
    IEnumerator PostToGemini(string csvData)
    {
        var manager = AnalyticsManager.Instance;
        if (manager == null) yield break;

        // สร้าง Summary สั้นๆ แปะหัวเพื่อให้ AI มีตัวเลขตั้งต้นที่ถูกต้อง
        string summary = $"[Session Summary]\n" +
                         $"- Initial Family: {manager.initialFamilyCount}\n" +
                         $"- Family Saved: {manager.familySavedCount}\n" +
                         $"- Total Deaths: {manager.deadCount}\n\n";

        // Prompt ที่สั่งให้ AI เป็นนักจิตวิทยาและวิเคราะห์ความสัมพันธ์ของตัวแปรต่างๆ
        string prompt = "You are a professional Game Psychologist and Player Behavior Analyst. " +
                        "Analyze this player's data to judge their humanity and decision-making.\n\n" +
                        summary +
                        "[Detailed CSV Log]\n" + csvData + "\n\n" +
                        "### Instructions for Analysis:\n" +
                        "1. **Playstyle**: Did they prioritize family or NPCs? Did they stay selfless even at low 'PlayerHP'?\n" +
                        "2. **Stress Response**: How did they react during 'EXPLOSION_PANIC'? Look at 'Sanity' and 'DistanceToSafe'.\n" +
                        "3. **Moral Compass**: If family died while PlayerHP was high, point out their negligence.\n\n" +
                        "Provide a concise 3-point summary in English. Be insightful, direct, and witty.";

        // เตรียม JSON Request
        GeminiRequest request = new GeminiRequest
        {
            contents = new List<Content> {
                new Content { parts = new List<Part> { new Part { text = prompt } } }
            }
        };

        string json = JsonUtility.ToJson(request);
        string fullUrl = url + apiKey;

        using (UnityWebRequest www = new UnityWebRequest(fullUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                GeminiResponse response = JsonUtility.FromJson<GeminiResponse>(www.downloadHandler.text);
                if (response != null && response.candidates.Count > 0)
                {
                    string aiAnalysis = response.candidates[0].content.parts[0].text;

                    if (uiDisplayText != null) uiDisplayText.text = aiAnalysis;
                    if (backButton != null) backButton.SetActive(true); // โชว์ปุ่มกลับ

                    Debug.Log("AI Analysis Success!");
                }
            }
            else
            {
                Debug.LogError($"AI Error: {www.error} | Response: {www.downloadHandler.text}");
                if (uiDisplayText != null) uiDisplayText.text = "Error: AI could not analyze the data. Check your API Key or Quota.";
            }
        }
    }

    // ฟังก์ชันสำหรับปุ่ม Back (ปิดหน้า AI กลับไปหน้าจบปกติ)
    public void OnBackButtonClicked()
    {
        if (endScreenContent != null) endScreenContent.SetActive(true);
        if (restartButton != null) restartButton.SetActive(true);
        if (uiDisplayText != null) uiDisplayText.gameObject.SetActive(false);
        if (backButton != null) backButton.SetActive(false);
        if(aiScrollView != null) aiScrollView.SetActive(false);

        // ปิดปุ่มวิเคราะห์ไปเลย เพราะวิเคราะห์ไปแล้วครั้งหนึ่ง
        if (aiAnalysisButton != null) aiAnalysisButton.SetActive(false);
    }

    // รีเซ็ตหน้าจอทุกครั้งที่ Object นี้ถูกเปิดใช้งาน
    private void OnEnable()
    {
        if (uiDisplayText != null) uiDisplayText.gameObject.SetActive(false);
        if (backButton != null) backButton.SetActive(false);
        if (aiAnalysisButton != null) aiAnalysisButton.SetActive(true);
        if (endScreenContent != null) endScreenContent.SetActive(true);
        if (restartButton != null) restartButton.SetActive(true);
        if (aiScrollView != null) aiScrollView.SetActive(false);
    }
}