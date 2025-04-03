using UnityEngine;

public class BackToHubTest : MonoBehaviour
{
    public void BackToHub()
    {
        string packageName = $"com.ImperialCollegeLondon.VirtualExperienceEngineJan302";

        Debug.Log($"Try launch {packageName}");
        var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        var packageManager = currentActivity.Call<AndroidJavaObject>("getPackageManager");
        var launchIntent = packageManager.Call<AndroidJavaObject>("getLaunchIntentForPackage", packageName);

        if (launchIntent != null)
        {
            currentActivity.Call("startActivity", launchIntent);
            Debug.Log("App launched successfully");
        }
        else
        {
            Debug.LogError("Launch intent is null. The app might not be installed.");
        }
    }
}
