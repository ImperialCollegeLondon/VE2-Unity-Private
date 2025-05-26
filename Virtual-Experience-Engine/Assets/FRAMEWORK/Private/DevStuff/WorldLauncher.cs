using UnityEngine;

public class WorldLauncher : MonoBehaviour
{
    //<br>java.lang.NullPointerException: Attempt to invoke virtual method 'boolean android.content.Intent.migrateExtraStreamToClipData(android.content.Context)' on a null object reference<br>java.lang.NullPointerException: Attempt to invoke virtual method 'boolean android.content.Intent.migrateExtraStreamToClipData(android.content.Context)' on a null object reference<br>&nbsp;&nbsp;at android.app.Instrumentation.execStartActivity(Instrumentation.java:1756)<br>&nbsp;&nbsp;at android.app.Activity.startActivityForResult(Activity.java:5411)<br>&nbsp;&nbsp;at androidx.activity.ComponentActivity.startActivityForResult(ComponentActivity.java:728)<br>&nbsp;&nbsp;at android.app.Activity.startActivityForResult(Activity.java:5369)<br>&nbsp;&nbsp;at androidx.activity.ComponentActivity.startActivityForResult(ComponentActivity.java:709)<br>&nbsp;&nbsp;at android.app.Activity.startActivity(Activity.java:5755)<br>&nbsp;&nbsp;at android.app.Activity.startActivity(Activity.java:5708)<br>  at UnityEngine.AndroidJNISafe.CheckException ()
    public void LaunchApp()
    {
        Debug.Log("Try launch app 1");
        string packageName = "com.ImperialCollegeLondon.VE2Hub24"; // Ensure this is the correct package name
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


    //<br>java.lang.NoSuchFieldError: no "Ljava/lang/Object;" field "currentActivity" in class "Lcom/unity3d/player/UnityPlayerGameActivity;" or its superclasses<br>java.lang.NoSuchFieldError: no "Ljava/lang/Object;" field "currentActivity" in class "Lcom/unity3d/player/UnityPlayerGameActivity;" or its superclasses<br>  at UnityEngine.AndroidJNISafe.CheckException () [0x00000] in <00000000000000000000000000000000>:0 <br>  at UnityEngine.AndroidJNISafe.GetStaticFieldID (System.IntPtr clazz, System.String name, System.String sig) [0x00000] in <00000000000000000000000000000000>:0 <br>  at UnityEngine._AndroidJNIHelper.GetFieldID (System.IntPtr jclass, System.String fieldName, System.String signature, System.Boolean isStatic) [0x00000] in <00000000000000000000000000000000>:0 <br>  at UnityEngine.AndroidJavaObject._GetStatic[FieldType] (System.String fieldName) [0x00000] in <00000000000000000000000000000000>:0 <br>  at WorldLauncher.LaunchApp2 () [0x00000] in < etc etc
    public void LaunchApp2()
    {
        Debug.Log("Try launch app 2");
        string packageName = "com.ImperialCollegeLondon.PluginTuesday"; // Ensure this is the correct package name
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
