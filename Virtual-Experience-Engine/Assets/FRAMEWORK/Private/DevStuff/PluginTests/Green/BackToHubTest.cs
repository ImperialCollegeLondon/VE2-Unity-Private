using UnityEngine;

public class BackToHubTest : MonoBehaviour
{
    private void Start() 
    {
        Debug.LogError("Try receive args...");

        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        using (AndroidJavaObject intent = currentActivity.Call<AndroidJavaObject>("getIntent"))
        {
            if (intent != null)
            {
                string arg0 = intent.Call<string>("getStringExtra", "arg0");
                string arg1 = intent.Call<string>("getStringExtra", "arg1");

                Debug.Log($"Received argument 0: {arg0}");
                Debug.Log($"Received argument 1: {arg1}");
            }
            else 
            {
                Debug.LogError("Tried to receive args but Intent is null");
            }
        }
    }

    public void BackToHub()
    {
        string packageName = $"com.ImperialCollegeLondon.VirtualExperienceEngine";

        Debug.Log($"Try go back to hub {packageName}");
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
