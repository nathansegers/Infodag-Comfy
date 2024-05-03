using System.IO;
using UnityEngine;
using System.Text.RegularExpressions;

public class LoadJson : MonoBehaviour
{
    public string path = "workflow_api.json"; // Adjust the path as necessary
    public string jsonContent;

    void Start()
    {
        jsonContent = ReadJsonFile(path);
        // DebugMatches(jsonContent, "16");
        // jsonContent = ModifyPrompt(jsonContent, "16", "Your new positive prompt here", "Your new negative prompt here");
        // DebugMatches(jsonContent, "16");
        // DebugSeedMatches(jsonContent, new int[] { 16, 21, 58, 68 });  // Verify seed matches
        // jsonContent = ModifySeed(jsonContent, new int[] { 16, 21, 58, 68 }, UnityEngine.Random.Range(0, 10000));
        // DebugSeedMatches(jsonContent, new int[] { 16, 21, 58, 68 });  // Verify seed matches

        // WriteJsonFile(path, jsonContent);
        // Debug.Log("Saved Json file");

    }

    public string ReadJsonFile(string filePath)
    {
        string fullPath = Path.Combine(Application.dataPath, filePath);
        return File.ReadAllText(fullPath);
    }

    public void DebugMatches(string jsonContent, string nodeId)
    {
        string pattern = $"\"{nodeId}\"\\s*:\\s*{{\\s*\"inputs\"\\s*:\\s*{{\\s*\"positive_prompt\"\\s*:\\s*\"[^\"]*\"";
        var matches = Regex.Matches(jsonContent, pattern, RegexOptions.Singleline);

        Debug.Log($"Found {matches.Count} matches for node {nodeId}.");
        foreach (Match match in matches)
        {
            Debug.Log("Match: " + match.Value);
        }
    }

    public void DebugSeedMatches(string jsonContent, int[] seedNodeIds)
    {
        foreach (int nodeId in seedNodeIds)
        {
            string seedPattern = $"\"{nodeId}\"\\s*:\\s*{{\\s*\"inputs\"\\s*:\\s*{{[^}}]*\"seed\"\\s*:\\s*\\d+[^}}]*}}";
            var matches = Regex.Matches(jsonContent, seedPattern);

            Debug.Log($"Found {matches.Count} seed matches for node {nodeId}.");
            foreach (Match match in matches)
            {
                Debug.Log("Seed Match: " + match.Value);
            }
        }
    }



    public string ModifyPrompt(string jsonContent, string nodeId, string newPositivePrompt, string newNegativePrompt)
    {
        string positivePromptPattern = $"\"{nodeId}\"\\s*:\\s*{{\\s*\"inputs\"\\s*:\\s*{{\\s*\"positive_prompt\"\\s*:\\s*\"[^\"]*\"";
        string negativePromptPattern = $"\"{nodeId}\"\\s*:\\s*{{\\s*\"inputs\"\\s*:\\s*{{\\s*\"negative_prompt\"\\s*:\\s*\"[^\"]*\"";

        jsonContent = Regex.Replace(jsonContent, positivePromptPattern, $"\"{nodeId}\":{{\"inputs\":{{\"positive_prompt\":\"{newPositivePrompt}\"");
        jsonContent = Regex.Replace(jsonContent, negativePromptPattern, $"\"{nodeId}\":{{\"inputs\":{{\"negative_prompt\":\"{newNegativePrompt}\"");

        return jsonContent;
    }


    public string ModifySeed(string jsonContent, int[] seedNodeIds, int newSeedValue)
    {
        foreach (int nodeId in seedNodeIds)
        {
            // Enhanced regex pattern to handle 'seed' potentially not being the last entry in an object
            // Matches "seed": some_number, possibly followed by a comma and more properties, until it reaches a closing brace
            string seedPattern = $"\"{nodeId}\"\\s*:\\s*{{\\s*\"inputs\"\\s*:\\s*{{[^}}]*\"seed\"\\s*:\\s*\\d+[^}}]*}}";
            jsonContent = Regex.Replace(jsonContent, seedPattern, match =>
            {
                // Use regex to replace only the seed number, preserving everything before and after
                return Regex.Replace(match.Value, "\"seed\"\\s*:\\s*\\d+", $"\"seed\": {newSeedValue}");
            });
        }
        return jsonContent;
    }


    public void WriteJsonFile(string filePath, string content)
    {
        string fullPath = Path.Combine(Application.dataPath, "saved-" + filePath);
        File.WriteAllText(fullPath, content);
    }
}
