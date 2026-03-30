using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class GeminiAIController : MonoBehaviour
{
    [Header("Settings")]
    public string apiKey = "AIzaSyxxxx"; // ใส่แค่รหัสรัวๆ

    // ประกาศ URL ไว้แค่ถึงเครื่องหมาย = 
    private string url = "https://generativelanguage.googleapis.com/v1/models/gemini-2.5-flash:generateContent?key=";


    [Header("UI Reference")]
    public TextMeshProUGUI uiDisplayText;
    [Header("UI to Hide")]
    public GameObject endScreenContent;
    public GameObject restartButton;
    public GameObject backButton;
    public GameObject aiAnalysisButton;

    // 1. ฟังก์ชันเรียกใช้งาน (ผูกกับปุ่ม OnClick)
    public void RequestAIAnalysis()
    {
        var manager = AnalyticsManager.Instance;
        if (manager == null)
        {
            Debug.LogError("AnalyticsManager.Instance is null!");
            return;
        }
        if (endScreenContent != null) endScreenContent.SetActive(false);
        if (restartButton != null) restartButton.SetActive(false);

        // แปลง List ข้อมูลเป็น CSV string
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Time,Event,NPC,Sanity,FamilyLeft");

        foreach (var data in manager.sessionHistory)
        {
            sb.AppendLine($"{data.time:F1},{data.sanity:F1},{data.familyLeft}");
        }

        if (uiDisplayText != null)
        {
            uiDisplayText.gameObject.SetActive(true);
            uiDisplayText.text = "AI is analyzing your behavior...";
        }

        // เริ่มส่งข้อมูลไปหา AI
        StartCoroutine(PostToGemini(sb.ToString()));
    }

    // 2. ฟังก์ชันส่งข้อมูลเข้า Cloud
    IEnumerator PostToGemini(string csvData)
    {
        // สร้างคำสั่ง (Prompt)
        string prompt = "You are a professional game analyst. Analyze this CSV player behavior data: \n" + csvData +
                        "\nProvide a 3-point summary in English about their playstyle and ending.";

        // เตรียมข้อมูลส่ง (JSON)
        GeminiRequest request = new GeminiRequest
        {
            contents = new List<Content> {
                new Content { parts = new List<Part> { new Part { text = prompt } } }
            }
        };

        string json = JsonUtility.ToJson(request);
        string fullUrl = url + apiKey;

        // ใช้ new UnityWebRequest เพื่อแก้ปัญหา 404
        using (UnityWebRequest www = new UnityWebRequest(fullUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                // รับคำตอบและโชว์บนหน้าจอ
                GeminiResponse response = JsonUtility.FromJson<GeminiResponse>(www.downloadHandler.text);
                if (response != null && response.candidates.Count > 0)
                {
                    string aiAnalysis = response.candidates[0].content.parts[0].text;
                    if (uiDisplayText != null) uiDisplayText.text = aiAnalysis;
                    if (backButton != null) backButton.SetActive(true);
                    Debug.Log("AI Analysis Success: " + aiAnalysis);
                }
            }
            else
            {
                // ถ้าพัง รอบนี้มันจะบอกเหตุผลชัดเจนใน Console
                Debug.LogError($"AI Error: {www.error} | Response: {www.downloadHandler.text}");
                if (uiDisplayText != null) uiDisplayText.text = "Error: AI could not reach the server.";
            }
        }
    }
    public void RestoreEndScreen()
    {
        if (endScreenContent != null) endScreenContent.SetActive(true);
        if (restartButton != null) restartButton.SetActive(true);

        // ซ่อนตัวหนังสือ AI ไปด้วยเลยก็ได้ถ้าต้องการ
        if (uiDisplayText != null) uiDisplayText.gameObject.SetActive(false);
    }
    public void OnBackButtonClicked()
    {
        // 1. เปิดของเก่ากลับมา
        if (endScreenContent != null) endScreenContent.SetActive(true);
        if (restartButton != null) restartButton.SetActive(true);

        // 2. ปิดหน้าจอ AI และปุ่ม Back
        if (uiDisplayText != null) uiDisplayText.gameObject.SetActive(false);
        if (backButton != null) backButton.SetActive(false);

        // 3. ปิดปุ่ม AI Analysis ตามที่นายต้องการ
        if (aiAnalysisButton != null) aiAnalysisButton.SetActive(false);
    }
    private void OnEnable()
    {
        // ทันทีที่หน้า EndingScreen (ตัวแม่) ถูกสั่ง SetActive(true)
        // ฟังก์ชันนี้จะทำงานเพื่อ Reset หน้าจอให้พร้อมใช้งานทันที

        // 1. ปิดหน้าจอผลลัพธ์ AI และปุ่ม Back ไว้ก่อน (เพื่อไม่ให้มันโผล่มาแทรก)
        if (uiDisplayText != null) uiDisplayText.gameObject.SetActive(false);
        if (backButton != null) backButton.SetActive(false);

        // 2. มั่นใจว่าปุ่มวิเคราะห์ และหน้าจอจบปกติเปิดอยู่
        if (aiAnalysisButton != null) aiAnalysisButton.SetActive(true);
        if (endScreenContent != null) endScreenContent.SetActive(true);
        if (restartButton != null) restartButton.SetActive(true);
    }

}