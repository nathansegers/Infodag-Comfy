// 2024-05-02 AI-Tag 
// This was created with assistance from Muse, a Unity Artificial Intelligence product

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;
using Newtonsoft.Json.Linq;
using WebSocketSharp;
using UnityEngine.UI;
using TMPro;

// This was created with assistance from Muse, a Unity Artificial Intelligence product

[Serializable]
public class WebSocketMessage
{
    public string type;
    public WSMessageData data;
}

[Serializable]
public class WSMessageData
{
    public string prompt_id;
    public string node;
}

[Serializable]
public class OutputImage
{
    public string filename { get; set; }
    public string subfolder { get; set; }
    public string type { get; set; }
}

[Serializable]
public class OutputImageList
{
    public List<OutputImage> images;
}

[Serializable]
public class PromptHistory
{
    public object[] prompt;  // Though unused, kept for complete mapping
    public Dictionary<string, OutputImageList> outputs;
    public object status;    // Simplified as object assuming no further detail is needed
}
[Serializable]
// HistoryResult is directly a dictionary mapping from string to PromptHistory
public class HistoryResult : Dictionary<string, PromptHistory>
{
}

public class QueueResponse
{
    public object[] queue_running; // replacing 'QueueData' with 'object'
    public object[] queue_pending; // replacing 'QueueData' with 'object'
}


public class Node
{
    public Dictionary<string, object> inputs { get; set; }
    public string class_type { get; set; }
}

public class Prompt
{
    public Dictionary<string, Node> nodes { get; set; }
}

// 2024-05-02 AI-Tag 
// This was created with assistance from Muse, a Unity Artificial Intelligence product

public class ComfyUIError
{
    public string type { get; set; }
    public string message { get; set; }
    public string details { get; set; }
    public object extra_info { get; set; }
}

public class QueuePromptResult
{
    public string prompt_id { get; set; }
    public int number { get; set; }
    public Dictionary<string, ComfyUIError> node_errors { get; set; }
}

public class UploadImageResult
{
    public string name { get; set; }
    public string subfolder { get; set; }
    public string type { get; set; }
}

public class ImageContainer
{
    public byte[] blob { get; set; }
    public OutputImage image { get; set; }
}

public class ImagesResponse : Dictionary<string, ImageContainer[]>
{
    public Dictionary<string, ImageContainer[]> nodes { get; set; }
}


public class WebSocketClient : MonoBehaviour
{

    private WebSocket websocket;
    private string httpProtocol = "https";
    private string wsProtocol = "wss";
    private string serverAddress = "comfy.sizingservers.be";

    private HttpClient _httpClient;
    private string _baseAddress;

    private string clientId = "Comfy-ImmersiveRoom";

    public LoadJson loadJson;

    private Dictionary<string, List<byte[]>> outputImages = new Dictionary<string, List<byte[]>>();
    public string prompt_id = "";
    private string current_node = "";

    public string positivePrompt = "";

    public GetPanoImage panoImage;

    [SerializeField]
    public Slider receivedMessages;
    public TMP_InputField positivePromptInput;
    public Canvas canvas;
    public Button btn;

    void Start()
    {
        var clientId = "Comfy-ImmersiveRoom";
        var wsUrl = $"{wsProtocol}://{serverAddress}/ws?clientId={clientId}";
        websocket = new WebSocket(wsUrl);
        websocket.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
        websocket.SslConfiguration.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

        Debug.Log("Connecting to websocket server...: " + wsUrl);

        websocket.OnOpen += (sender, e) =>
        {
            Debug.Log("Connection open!");
            // Here you can send messages, e.g. websocket.SendString("Hello Server!");
        };

        websocket.OnError += (sender, e) =>
        {
            Debug.LogError("Error occured: " + e);
        };

        websocket.OnClose += (sender, e) =>
        {
            Debug.Log($"Connection closed: Was clean? {e.WasClean}, Code: {e.Code}, Reason: {e.Reason}");
        };

        websocket.OnMessage += OnMessage;

        btn.onClick.AddListener(GenerateImage);
        // waiting for messages
        websocket.ConnectAsync();
    }

    void Awake()
    {
        _httpClient = new HttpClient();
        // Optionally add headers to the HttpClient instance if needed for all requests
        // _httpClient.DefaultRequestHeaders.Accept.Clear();
        // _httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        _baseAddress = $"{httpProtocol}://{serverAddress}/";
        Debug.Log("Awoken");
        Debug.Log("Base address: " + _baseAddress);
    }

    void GenerateImage()
    {
        positivePrompt = positivePromptInput.text;
        Debug.Log("Positive prompt: " + positivePrompt);
        // Use the LoadJson class and update the value using a new prompt
        loadJson.jsonContent = loadJson.ModifyPrompt(loadJson.jsonContent, "16", $"360 View, {positivePrompt}, 8k, masterpiece, trending on artstation", "nsfw, text, watermark, ");
        loadJson.jsonContent = loadJson.ModifySeed(loadJson.jsonContent, new int[] { 16, 21, 58, 68 }, UnityEngine.Random.Range(0, 10000));
        loadJson.WriteJsonFile(loadJson.path, loadJson.jsonContent);

        receivedMessages.value = 0;
        GetImages(loadJson.jsonContent);
    }

    void Update()
    {

        if (Input.GetKeyDown(KeyCode.H) && Input.GetKey(KeyCode.LeftControl))
        {
            canvas.enabled = !canvas.enabled;
        }
    }

    private async Task OnApplicationQuit()
    {
        if (websocket != null)
        {
            websocket.Close();
            websocket = null;
        }
        _httpClient.Dispose();
    }

    private async Task OnDestroy()
    {
        if (websocket != null)
        {
            websocket.Close();
            websocket = null;
        }
        _httpClient.Dispose();
    }


    private void OnMessage(object sender, MessageEventArgs e)
    {
        try
        {

            // Previews are binary data
            if (e.IsBinary)
            {
                return;
            }

            if (e.IsText)
            {
                var message = JsonUtility.FromJson<WebSocketMessage>(e.Data);

                if (message.type == "executing")
                {
                    if (message.data.prompt_id == prompt_id)
                    {
                        if (message.data.node == null)
                        {
                            Debug.Log("Execution is done");

                            FetchAndHandleImages(prompt_id);

                            // websocket.Close(); // Close connection if execution is done
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.Log("Exception caught: " + ex.Message);
        }
    }



    public async Task<ImagesResponse> GetImages(string prompt)
    {

        QueuePromptResult queue = await QueuePrompt(prompt);
        prompt_id = queue.prompt_id;

        try
        {

            TaskCompletionSource<ImagesResponse> completionSource = new TaskCompletionSource<ImagesResponse>();
            ImagesResponse outputImages = new ImagesResponse();


            return await completionSource.Task;
        }
        catch (Exception e)
        {
            Debug.Log("Exception caught: " + e.Message);
            return null;
        }

    }

    public async Task<HistoryResult> GetHistory(string promptId = null)
    {
        string url = _baseAddress + "history";
        if (!string.IsNullOrEmpty(promptId)) url += $"/{promptId}";

        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync();
            HistoryResult result = JsonConvert.DeserializeObject<HistoryResult>(json);
            return result;
        }
        catch (HttpRequestException e)
        {
            Debug.LogError($"Request failed: {e.Message}");
            throw;
        }
    }

    public string CreateJsonPayload(string innerJson, string clientId)
    {
        // Parse the inner JSON to a JObject
        var innerJsonObject = JObject.Parse(innerJson);


        // Create the outer JSON object using JObject
        var jsonPayload = new JObject
        {
            ["prompt"] = innerJsonObject,
            ["client_id"] = clientId
        };

        // Convert the JObject to string
        return jsonPayload.ToString();
    }

    public async Task<QueuePromptResult> QueuePrompt(string prompt)
    {
        string url = _baseAddress + "prompt";
        string jsonBody = CreateJsonPayload(prompt, clientId);
        HttpContent content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        Debug.Log(jsonBody);
        // 2024-05-03 AI-Tag 
        // This was created with assistance from Muse, a Unity Artificial Intelligence product

        try
        {
            HttpResponseMessage response = await _httpClient.PostAsync(url, content);
            Debug.Log(response);
            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync();
            Debug.Log(json);
            QueuePromptResult result = JsonConvert.DeserializeObject<QueuePromptResult>(json);
            return result;
        }
        catch (Exception e)
        {
            Debug.Log("Exception caught: " + e.Message);
            return null;

        }

        return null;

    }

    public async Task<byte[]> GetImage(string filename, string subfolder, string type)
    {
        string url = $"{_baseAddress}view?filename={Uri.EscapeDataString(filename)}&subfolder={Uri.EscapeDataString(subfolder)}&type={Uri.EscapeDataString(type)}";
        Debug.Log($"Fetching image from: {url}");
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            byte[] imageData = await response.Content.ReadAsByteArrayAsync();
            return imageData;
        }
        catch (HttpRequestException e)
        {
            Debug.LogError($"Request failed: {e.Message}");
            return null;
        }
    }

    async void FetchAndHandleImages(string promptId)
    {
        // try
        // {
        HistoryResult historyResult = await GetHistory(promptId);
        Debug.Log($"History result retrieved for prompt {promptId}");
        // Assuming you know the key for the last node or it's always "70"
        Debug.Log(historyResult.ToString());
        string lastNodeKey = "70";
        if (historyResult.ContainsKey(promptId))
        {
            var historyRes = historyResult[promptId];
            var outputImageList = historyRes.outputs[lastNodeKey];
            if (outputImageList != null)
            {
                foreach (var image in outputImageList.images)
                {
                    byte[] blob = await GetImage(image.filename, image.subfolder, image.type);
                    panoImage.CreateCubemapFromImageData(blob);
                    Debug.Log($"Image retrieved: {image.filename}");
                    // Additional handling of the image as needed
                }
            }
        }
        // }
        // catch (Exception e)
        // {
        //     Debug.LogError($"Failed to retrieve images: {e.Message}");
        // }
    }

}