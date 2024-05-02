using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class GetPanoImage : MonoBehaviour
{
    public string apiUrl = "http://localhost:3000/generate-image";
    public string promptText = "Landscape -- 360-degree equirectangular panorama of a desert landscape with a clear blue sky, golden sandy dunes, boulders, rocks, shadows, and an acacia tree. Style: Realistic photo landscape. Details of image: Create a seamless 360-degree equirectangular panorama of a desert landscape with a clear blue sky, golden sandy dunes, large boulders, sandstone rocks, elongated shadows from a low-hanging sun, and an isolated acacia tree on the horizon. Color: Blue sky, golden sandy dunes, brown boulders and rocks, green acacia tree. Lighting: Natural sunlight casting elongated shadows, low-hanging sun. Seamless stitching assymetric tiling near the sides. High Quality 8k photorealistic landscape. 2:1";
    public Material targetMaterial;  // Assign this material in the Unity Editor

    void Start()
    {
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.P))
        {
            GetImage();
        }
    }

    async void GetImage()
    {
        Debug.Log("Fetching image...");
        using (var client = new HttpClient())
        {
            // Set the timeout to 2 minutes
            client.Timeout = TimeSpan.FromMinutes(2);

            var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
            string jsonContent = $"{{\"promptText\": \"360 View, {promptText}, 8k, masterpiece, trending on artstation\"}}";
            request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            try
            {
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                byte[] imageData = await response.Content.ReadAsByteArrayAsync();
                // string path = SaveImageToDisk(imageData);
                CreateCubemapFromImageData(imageData);
            }
            catch (HttpRequestException e)
            {
                Debug.LogError("Error fetching image: " + e.Message);
            }
            catch (TaskCanceledException e)
            {
                if (e.CancellationToken.IsCancellationRequested)
                {
                    Debug.LogError("Request was canceled intentionally: " + e.Message);
                }
                else
                {
                    Debug.LogError("Request timeout expired: " + e.Message);
                }
            }
        }
    }


    string SaveImageToDisk(byte[] imageData)
    {
        string path = Path.Combine(Application.persistentDataPath, $"panoImage_{System.DateTime.Now:yyyyMMddHHmmss}.png");
        File.WriteAllBytes(path, imageData);
        Debug.Log("Saved image to " + path);
        return path;
    }

    // This would typically be called after downloading image data
    public void CreateCubemapFromImageData(byte[] imageData)
    {
        // Create a Cubemap object
        Texture2D tex = LoadTextureFromImage(imageData);
        Cubemap cubemap = new Cubemap(tex.width, TextureFormat.RGBA32, false);
        ConvertTexture2DtoCubemap(tex, cubemap);

        Debug.Log("Cubemap created");

        // Assign the Cubemap to the targetMaterial's skybox property
        targetMaterial.SetTexture("_MainTex", cubemap);
    }

    void ConvertTexture2DtoCubemap(Texture2D tex2D, Cubemap cubeMap)
    {
        for (int i = 0; i < 6; i++)
        {
            CubemapFace face = (CubemapFace)i;
            int width = cubeMap.width;
            Color[] facePixels = new Color[width * width];

            for (int y = 0; y < width; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector3 dir = GetPixelDirection(face, x, y, cubeMap.width);
                    Vector2 uv = new Vector2(Mathf.Atan2(dir.z, dir.x) / (2 * Mathf.PI), Mathf.Acos(dir.y) / Mathf.PI);
                    facePixels[y * width + x] = tex2D.GetPixelBilinear(uv.x, uv.y);
                }
            }

            cubeMap.SetPixels(facePixels, face);
        }

        cubeMap.Apply();
    }

    // Helper method to create a Texture2D from a byte array
    private Texture2D LoadTextureFromImage(byte[] imageData)
    {
        Texture2D tex = new Texture2D(2944, 1408, TextureFormat.RGBA32, false);
        tex.LoadImage(imageData); // This automatically resizes the texture based on the image
        return tex;
    }

    // 2024-05-02 AI-Tag 
// This was created with assistance from Muse, a Unity Artificial Intelligence product

Vector3 GetPixelDirection(CubemapFace face, int x, int y, int resolution)
{
    Vector3 direction = Vector3.zero;
    float u = (x + 0.5f) / resolution * 2.0f - 1.0f;
    float v = (y + 0.5f) / resolution * 2.0f - 1.0f;

    switch (face)
    {
        case CubemapFace.PositiveX:
            direction = new Vector3(1, -v, -u);
            break;

        case CubemapFace.NegativeX:
            direction = new Vector3(-1, -v, u);
            break;

        case CubemapFace.PositiveY:
            direction = new Vector3(u, 1, v);
            break;

        case CubemapFace.NegativeY:
            direction = new Vector3(u, -1, -v);
            break;

        case CubemapFace.PositiveZ:
            direction = new Vector3(u, -v, 1);
            break;

        case CubemapFace.NegativeZ:
            direction = new Vector3(-u, -v, -1);
            break;
    }

    return direction.normalized;
}


}
