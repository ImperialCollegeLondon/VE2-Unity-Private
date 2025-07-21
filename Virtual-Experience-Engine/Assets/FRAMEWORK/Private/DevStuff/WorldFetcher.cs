using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

[Serializable]
public class Passwords
{
    public string general;
    public string admin;
}

[Serializable]
public class WorldMetadata
{
    // THESE NAMES MUST MATCH YOUR JSON KEYS:
    public string name;
    public string description;
    public string author;
    public string published;
    public string windowsVersion;
    public string androidVersion;

    // Nested object:
    public Passwords passwords;

    // This one comes from your server-wrapped object, not the JSON file:
    public string imageUrl;
}

[Serializable]
public class WorldsResponse
{
    // This must match the wrapper you use: { "worlds": [ … ] }
    public List<WorldMetadata> worlds;
}

public class WorldFetcher : MonoBehaviour
{
    [Header("Server URL (include http://)")]
    public string worldsUrl = "http://localhost:5000/worlds";

    [Header("Demo Image Display")]
    public RawImage displayImage;

    private WorldsResponse _worldsData;

    public void FetchAllWorlds()
    {
        StartCoroutine(FetchWorldsCoroutine());
    }
    public void FetchWorldByName(string name)
    {
        StartCoroutine(FetchWorldByNameCoroutine(name));
    }
    private IEnumerator FetchWorldsCoroutine()
    {
        using (var uwr = UnityWebRequest.Get(worldsUrl))
        {
            yield return uwr.SendWebRequest();
            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error fetching worlds list: {uwr.error}");
                yield break;
            }

            // 1) Grab the raw text (already: { "worlds": [ … ] })
            string rawJson = uwr.downloadHandler.text;
            Debug.Log("Raw /worlds response:\n" + rawJson);

            // 2) Parse it directly—NO extra wrapping
            _worldsData = JsonUtility.FromJson<WorldsResponse>(rawJson);

            if (_worldsData?.worlds == null || _worldsData.worlds.Count == 0)
            {
                Debug.LogWarning("No worlds found in response.");
                yield break;
            }

            // 3) Log them
            foreach (var w in _worldsData.worlds)
            {
                Debug.Log(
                    $"--- World: {w.name} ---\n" +
                    $"Description: {w.description}\n" +
                    $"Author: {w.author}\n" +
                    $"Publish Date: {w.published}\n" +
                    $"Windows Version: {w.windowsVersion}\n" +
                    $"Android Version: {w.androidVersion}\n" +
                    $"General Password: {w.passwords.general}\n" +
                    $"Admin Password: {w.passwords.admin}\n" +
                    $"Image URL: {w.imageUrl}"
                );
            }

            StartCoroutine(FetchAndDisplayImage(_worldsData.worlds[0].imageUrl));
        }
    }

    private IEnumerator FetchWorldByNameCoroutine(string name)
    {
        string url = $"{worldsUrl}/{name}";
        using (var uwr = UnityWebRequest.Get(url))
        {
            yield return uwr.SendWebRequest();
            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error fetching world '{name}': {uwr.error}");
                yield break;
            }

            string rawJson = uwr.downloadHandler.text;
            Debug.Log($"Raw /worlds/{name} response:\n" + rawJson);

            WorldMetadata world = JsonUtility.FromJson<WorldMetadata>(rawJson);
            if (world == null)
            {
                Debug.LogWarning($"No data found for world '{name}'.");
                yield break;
            }

            Debug.Log(
                $"--- World: {world.name} ---\n" +
                $"Description: {world.description}\n" +
                $"Author: {world.author}\n" +
                $"Publish Date: {world.published}\n" +
                $"Windows Version: {world.windowsVersion}\n" +
                $"Android Version: {world.androidVersion}\n" +
                $"General Password: {world.passwords?.general}\n" +
                $"Admin Password: {world.passwords?.admin}\n" +
                $"Image URL: {world.imageUrl}"
            );

            if (!string.IsNullOrEmpty(world.imageUrl))
            {
                StartCoroutine(FetchAndDisplayImage(world.imageUrl));
            }
            else
            {
                Debug.LogWarning("No image URL provided for this world.");
            }
        }
    }

    private IEnumerator FetchAndDisplayImage(string imageUrl)
    {
        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error fetching image at {imageUrl}: {uwr.error}");
            }
            else
            {
                Texture2D tex = DownloadHandlerTexture.GetContent(uwr);
                displayImage.texture = tex;
            }
        }
    }
}
