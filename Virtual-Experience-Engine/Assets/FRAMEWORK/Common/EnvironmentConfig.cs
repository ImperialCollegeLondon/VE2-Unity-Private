using UnityEngine;

[CreateAssetMenu(fileName = "EnvironmentConfig", menuName = "Scriptable Objects/EnvironmentConfig")]
public class EnvironmentConfig : ScriptableObject
{
    public enum EnvironmentType
    {
        Windows, 
        Android, 
        Undefined,
    }

    public EnvironmentType Environment;

    public string EnvironmentName => Environment.ToString();
}
