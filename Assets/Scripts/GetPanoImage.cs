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
    public Material targetMaterial;  // Assign this material in the Unity Editor

    // Cubemap History
    public List<Cubemap> cubemaps = new List<Cubemap>();

    public int currentCubemap = 0;

    public GameObject spherePrefab; // Assign your sphere prefab
    public Vector3 centerPosition = new Vector3(0, 50, 0); // Center of the circle
    public float radius = 360.0f; // Radius of the circle
    public List<GameObject> spheres = new List<GameObject>();

    void Start()
    {
        // Set the first cubemap as active
        if (cubemaps.Count > 0)
        {
            targetMaterial.SetTexture("_MainTex", cubemaps[currentCubemap]);
        }
    }

    void Update()
    {
        
        // Using key arrows, navigate through the cubemaps
        if (Input.GetKeyUp(KeyCode.RightArrow))
        {
            Debug.Log("Next Cubemap");
            PreviousCubemap();
        }
        else if (Input.GetKeyUp(KeyCode.LeftArrow))
        {
            Debug.Log("Previous Cubemap");
            NextCubemap();
        }

        // Render cubemaps on a canvas
        if (Input.GetKeyUp(KeyCode.B) && Input.GetKey(KeyCode.LeftControl))
        {
            Debug.Log("Render Cubemap");

            // Remove all the spheres
            foreach (GameObject sphere in spheres)
            {
                Destroy(sphere);
            }
            spheres.Clear(); // Clear the list after destroying the spheres
            
            PlaceCubemapsInCircle();
        }

        if (Input.GetKeyUp(KeyCode.D) && Input.GetKey(KeyCode.LeftControl)) {
            foreach (GameObject sphere in spheres)
            {
                // Toggle active state
                sphere.SetActive(!sphere.activeSelf);
            }
        }
    }

    void PlaceCubemapsInCircle()
    {
        for (int i = 0; i < cubemaps.Count; i++)
        {
            // Calculate angle step
            float angle = i * Mathf.PI * 2 / cubemaps.Count;

            // Calculate x, z coordinates
            float x = centerPosition.x + Mathf.Cos(angle) * radius;
            float z = centerPosition.z + Mathf.Sin(angle) * radius;

            // Create a new sphere instance
            GameObject sphere = Instantiate(spherePrefab, new Vector3(x, centerPosition.y, z), Quaternion.identity);

           // Check if we have a cubemap for this index, and apply it
            if (cubemaps != null && i < cubemaps.Count && cubemaps[i] != null)
            {
                Renderer renderer = sphere.GetComponent<Renderer>();
                if (renderer != null)
                {
                    // Create a new material with a Cubemap shader
                    Material cubemapMaterial = new Material(Shader.Find("BlockadeLabsSDK/BlockadeSkyboxDepth"));
                    cubemapMaterial.SetTexture("_MainTex", cubemaps[i]);
                    
                    // Apply the material to the sphere
                    renderer.material = cubemapMaterial;
                }
            }
            spheres.Add(sphere);
        }
    }

    public void NextCubemap()
    {
        if (cubemaps.Count > 0)
        {
            currentCubemap = (currentCubemap + 1) % cubemaps.Count;
            targetMaterial.SetTexture("_MainTex", cubemaps[currentCubemap]);
        }
    }

    public void PreviousCubemap()
    {
        if (cubemaps.Count > 0)
        {
            currentCubemap = (currentCubemap - 1 + cubemaps.Count) % cubemaps.Count;
            targetMaterial.SetTexture("_MainTex", cubemaps[currentCubemap]);
        }
    }

    public void CreateCubemapFromImageData(byte[] imageData)
    {
        // Create a Cubemap object
        Texture2D tex = LoadTextureFromImage(imageData);
        Cubemap cubemap = new Cubemap(tex.width, TextureFormat.RGBA32, false);
        ConvertTexture2DtoCubemap(tex, cubemap);

        Debug.Log("Cubemap created");
        cubemaps.Add(cubemap);
        currentCubemap = cubemaps.Count - 1;

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
