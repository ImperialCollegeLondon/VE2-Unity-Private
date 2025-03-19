using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static VE2.NonCore.Platform.API.PlatformPublicSerializables;
using static VE2.NonCore.Platform.Internal.PlatformSerializables;

namespace VE2.NonCore.Platform.Internal
{
    internal interface IPlatformSettingsHandler 
    {
        public ServerConnectionSettings PlatformServerConnectionSettings {get; set;}
        public string PlatformCustomerName {get; set;}
        public string PlatformCustomerPassword {get; set;}
        public ushort PlatformClientID {get; set;}
        public string InstanceCode {get; set;}

        public Dictionary<string, WorldDetails> ActiveWorlds {get; set;}
        public ServerConnectionSettings WorldBuildsFTPServerSettings {get; set;}
        public ServerConnectionSettings FallbackWorldSubStoreFTPServerSettings {get; set;}
        public ServerConnectionSettings FallbackInstanceServerSettings {get; set;}

        public AndroidJavaObject AddArgsToIntent(AndroidJavaObject intent);

        //public void SetDefaults(string defaultPlatformCustomerName, string defaultPlatformPassword, string instanceCode, ServerConnectionSettings defaultWorldSubStoreFTPServerSettings, ServerConnectionSettings defaultFallbackInstanceServerSettings);
    }

    /// <summary>
    /// Call SetDefaults after instantiating 
    /// </summary>
    [ExecuteAlways]
    internal class PlatformPersistentDataHandler : MonoBehaviour, IPlatformSettingsHandler  
    {
        private const string HasArgsArgName = "hasArgs";

        //TODO - these can probably all be private, if we expose a function for adding intent args?
        public static string PlatformConnectionSettingsArgName => "platformConnectionSettings";
        public static string PlatformCustomerNameArgName => "platformCustomerName";
        public static string PlatformPasswordArgName => "platformPassword";
        public static string PlatformClientIDArgName => "platformClientID";
        public static string InstanceCodeArgName => "instanceCode";

        public static string NumberOfActiveWorldsArgName => "numberOfActiveWorlds";
        public static string ActiveWorldsArgName => "activeWorlds";
        public static string WorldBuildsFTPServerSettingsArgName => "worldBuildsFTPServerSettings";
        public static string FallbackWorldSubStoreFTPServerSettingsArgName => "fallbackWorldSubStoreFTPServerSettings";
        public static string FallbackInstanceServerSettingsArgName => "fallbackInstanceServerSettings";


        //private ServerConnectionSettings _defaultPlatformIPAddress =  new("127.0.0.1", 4287, "dev", "dev");
        [SerializeField, Disable] private bool _platformServerConnectionSettingsSetup = false;
        [SerializeField, Disable] private ServerConnectionSettings _platformServerConnectionSettings;
        public ServerConnectionSettings PlatformServerConnectionSettings 
        {
            get 
            {
                if (!_platformServerConnectionSettingsSetup)
                {
                    if (Application.platform == RuntimePlatform.Android)
                    {
                        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                        using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                        using (AndroidJavaObject intent = currentActivity.Call<AndroidJavaObject>("getIntent"))
                        {
                            bool hasArgs = intent == null ? false : intent.Call<bool>("getBooleanExtra", HasArgsArgName, false);

                            if (hasArgs)
                            {
                                string platformServerSettingsBytesAsString = intent.Call<string>("getStringExtra", PlatformConnectionSettingsArgName);
                                byte[] platformServerConnectionSettingsAsBytes = System.Convert.FromBase64String(platformServerSettingsBytesAsString);
                                _platformServerConnectionSettings = new ServerConnectionSettings(platformServerConnectionSettingsAsBytes);
                            }
                            else 
                            {
                                _platformServerConnectionSettings = null;
                            }
                        }   
                    } 
                    else 
                    {
                        _platformServerConnectionSettings = null;
                    }
                    _platformServerConnectionSettingsSetup = true;
                }

                return _platformServerConnectionSettings;
            }
            set 
            {
                _platformServerConnectionSettingsSetup = true;
                _platformServerConnectionSettings = value;
            }
        }


        private string _defaultPlatformCustomerName = "dev";
        [SerializeField, Disable] private bool _platformCustomerNameSetup = false;
        [SerializeField, Disable] private string _platformCustomerName;
        public string PlatformCustomerName 
        {
            get 
            {
                if (!_platformCustomerNameSetup)
                {
                    if (Application.platform == RuntimePlatform.Android)
                    {
                        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                        using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                        using (AndroidJavaObject intent = currentActivity.Call<AndroidJavaObject>("getIntent"))
                        {
                            bool hasArgs = intent == null ? false : intent.Call<bool>("getBooleanExtra", HasArgsArgName, false);

                            if (hasArgs)
                            {
                                _platformCustomerName = intent.Call<string>("getStringExtra", PlatformCustomerNameArgName);
                            }
                            else 
                            {
                                _platformCustomerName = _defaultPlatformCustomerName;
                            }
                        }   
                    } 
                    else 
                    {
                        _platformCustomerName = _defaultPlatformCustomerName;
                    }
                    _platformCustomerNameSetup = true;
                }

                return _platformCustomerName;
            }
            set 
            {
                _platformCustomerNameSetup = true;
                _platformCustomerName = value;
            }
        }

        private string _defaultPlatformPassword = "dev";
        [SerializeField, Disable] private bool _platformPasswordSetup = false;
        [SerializeField, Disable] private string _platformPassword;
        public string PlatformCustomerPassword 
        {
            get 
            {
                if (!_platformPasswordSetup)
                {
                    if (Application.platform == RuntimePlatform.Android)
                    {
                        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                        using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                        using (AndroidJavaObject intent = currentActivity.Call<AndroidJavaObject>("getIntent"))
                        {
                            bool hasArgs = intent == null ? false : intent.Call<bool>("getBooleanExtra", HasArgsArgName, false);

                            if (hasArgs)
                            {
                                _platformPassword = intent.Call<string>("getStringExtra", PlatformPasswordArgName);
                            }
                            else 
                            {
                                _platformPassword = _defaultPlatformPassword;
                            }
                        }   
                    } 
                    else 
                    {
                        _platformPassword = _defaultPlatformPassword;
                    }
                    _platformPasswordSetup = true;
                }

                return _platformPassword;
            }
            set 
            {
                _platformPasswordSetup = true;
                _platformPassword = value;
            }
        }

        [SerializeField, Disable] private bool _platformClientIDSetup = false;
        [SerializeField, Disable] private ushort _platformClientID;
        public ushort PlatformClientID 
        {
            get 
            {
                if (!_platformClientIDSetup)
                {
                    if (Application.platform == RuntimePlatform.Android)
                    {
                        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                        using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                        using (AndroidJavaObject intent = currentActivity.Call<AndroidJavaObject>("getIntent"))
                        {
                            bool hasArgs = intent == null ? false : intent.Call<bool>("getBooleanExtra", HasArgsArgName, false);

                            if (hasArgs)
                            {
                                _platformClientID = intent.Call<ushort>("getIntExtra", PlatformClientIDArgName, 0);
                            }
                            else 
                            {
                                _platformClientID = ushort.MaxValue;
                            }
                        }   
                    } 
                    else 
                    {
                        _platformClientID = ushort.MaxValue;
                    }
                    _platformClientIDSetup = true;
                }

                return _platformClientID;
            }
            set 
            {
                _platformClientIDSetup = true;
                _platformClientID = value;
            }
        }

        //[SerializeField, Disable] private string _defaultInstanceCode;
        [SerializeField, Disable] private bool _instanceCodeSetup = false;
        [SerializeField, Disable] private string _instanceCode;
        public string InstanceCode 
        {
            get 
            {
                if (!_instanceCodeSetup)
                {
                    if (Application.platform == RuntimePlatform.Android)
                    {
                        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                        using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                        using (AndroidJavaObject intent = currentActivity.Call<AndroidJavaObject>("getIntent"))
                        {
                            bool hasArgs = intent == null ? false : intent.Call<bool>("getBooleanExtra", HasArgsArgName, false);

                            if (hasArgs)
                            {
                                _instanceCode = intent.Call<string>("getStringExtra", InstanceCodeArgName);
                            }
                            else 
                            {
                                //_instanceCode = _defaultInstanceCode;
                                _instanceCode = null;
                            }
                        }   
                    } 
                    else 
                    {
                        //_instanceCode = _defaultInstanceCode;
                        _instanceCode = null;
                    }
                    _instanceCodeSetup = true;
                }

                return _instanceCode;
            }
            set 
            {
                _instanceCodeSetup = true;
                _instanceCode = value;
            }
        }

        [SerializeField, Disable] private bool _activeWorldsSetup = false;
        [SerializeField, Disable] private List<WorldDetails> _activeWorldsList;
        public Dictionary<string, WorldDetails> ActiveWorlds
        {
            get 
            {
                if (!_activeWorldsSetup)
                {
                    if (Application.platform == RuntimePlatform.Android)
                    {
                        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                        using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                        using (AndroidJavaObject intent = currentActivity.Call<AndroidJavaObject>("getIntent"))
                        {
                            bool hasArgs = intent == null ? false : intent.Call<bool>("getBooleanExtra", HasArgsArgName, false);

                            if (hasArgs)
                            {
                                _activeWorldsList = new();
                                int numberOfActiveWorlds = intent.Call<int>("getIntExtra", NumberOfActiveWorldsArgName, 0);

                                string activeWorldsBytesAsString = intent.Call<string>("getStringExtra", ActiveWorldsArgName);
                                byte[] activeWorldsBytes = System.Convert.FromBase64String(activeWorldsBytesAsString);
                                
                                using MemoryStream stream = new(activeWorldsBytes);
                                using BinaryReader reader = new(stream);

                                for (int i = 0; i < numberOfActiveWorlds; i++)
                                {
                                    ushort activeWorldsBytesLength = reader.ReadUInt16(); //We need the length of each byte array
                                    //We need the length of each byte array
                                    WorldDetails worldDetails = new WorldDetails(reader.ReadBytes(activeWorldsBytesLength));
                                    _activeWorldsList.Add(worldDetails);
                                }
                            }
                            else 
                            {
                                _activeWorldsList = new();
                            }
                        }   
                    } 
                    else 
                    {
                        _activeWorldsList = new();
                    }
                    _activeWorldsSetup = true;
                }

                Dictionary<string, WorldDetails> activeWorlds = new();
                foreach (WorldDetails worldDetails in _activeWorldsList)
                    activeWorlds.Add(worldDetails.Name, worldDetails);
                
                return activeWorlds;
            }
            set 
            {
                _activeWorldsSetup = true;
                _activeWorldsList = new List<WorldDetails>(value.Values);
            }
        }

        //[SerializeField, Disable] private ServerConnectionSettings _defaultWorldBuildsFTPServerSettings; 
        [SerializeField, Disable] private bool _worldBuildsFTPServerSettingsSetup = false;
        [SerializeField, Disable] private ServerConnectionSettings _worldBuildsFTPServerSettings; 
        public ServerConnectionSettings WorldBuildsFTPServerSettings {
            get 
            {
                if (!_worldBuildsFTPServerSettingsSetup)
                {
                    if (Application.platform == RuntimePlatform.Android)
                    {
                        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                        using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                        using (AndroidJavaObject intent = currentActivity.Call<AndroidJavaObject>("getIntent"))
                        {
                            bool hasArgs = intent == null ? false : intent.Call<bool>("getBooleanExtra", HasArgsArgName, false);

                            if (hasArgs)
                            {
                                string worldBuildsFTPServerSettingsBytesAsString = intent.Call<string>("getStringExtra", WorldBuildsFTPServerSettingsArgName);
                                byte[] worldBuildsFTPServerSettingsBytes = System.Convert.FromBase64String(worldBuildsFTPServerSettingsBytesAsString);
                                _worldBuildsFTPServerSettings = new ServerConnectionSettings(worldBuildsFTPServerSettingsBytes);
                            }
                            else 
                            {
                                _worldBuildsFTPServerSettings = null;
                            }
                        }   
                    } 
                    else 
                    {
                        _worldBuildsFTPServerSettings = null; //There is no default for this, can only come in from platform server
                    }
                    _worldBuildsFTPServerSettingsSetup = true;
                }

                return _worldBuildsFTPServerSettings;
            }
            set 
            {
                _worldBuildsFTPServerSettingsSetup = true;
                _worldBuildsFTPServerSettings = value;
            }
        }

        //[SerializeField, Disable] private ServerConnectionSettings _defaultFallbackWorldSubStoreFTPServerSettings;
        [SerializeField, Disable] private bool _fallbackWorldSubStoreFTPServerSettingsSetup = false;
        [SerializeField, Disable] private ServerConnectionSettings _fallbackWorldSubStoreFTPServerSettings; 
        public ServerConnectionSettings FallbackWorldSubStoreFTPServerSettings 
        {
            get 
            {
                if (!_fallbackWorldSubStoreFTPServerSettingsSetup)
                {
                    if (Application.platform == RuntimePlatform.Android)
                    {
                        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                        using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                        using (AndroidJavaObject intent = currentActivity.Call<AndroidJavaObject>("getIntent"))
                        {
                            bool hasArgs = intent == null ? false : intent.Call<bool>("getBooleanExtra", HasArgsArgName, false);

                            if (hasArgs)
                            {
                                string worldSubStoreFTPServerSettingsBytesAsString = intent.Call<string>("getStringExtra", FallbackWorldSubStoreFTPServerSettingsArgName);
                                byte[] worldSubStoreFTPServerSettingsBytes = System.Convert.FromBase64String(worldSubStoreFTPServerSettingsBytesAsString);
                                _fallbackWorldSubStoreFTPServerSettings = new ServerConnectionSettings(worldSubStoreFTPServerSettingsBytes);
                            }
                            else 
                            {
                                //_fallbackWorldSubStoreFTPServerSettings = _defaultFallbackWorldSubStoreFTPServerSettings;
                                _fallbackWorldSubStoreFTPServerSettings = null;
                            }
                        }   
                    } 
                    else 
                    {
                        //_fallbackWorldSubStoreFTPServerSettings = _defaultFallbackWorldSubStoreFTPServerSettings;
                        _fallbackWorldSubStoreFTPServerSettings = null;
                    }
                    _fallbackWorldSubStoreFTPServerSettingsSetup = true;
                }

                return _fallbackWorldSubStoreFTPServerSettings;
            }
            set 
            {
                _fallbackWorldSubStoreFTPServerSettingsSetup = true;
                _fallbackWorldSubStoreFTPServerSettings = value;
            }
        }

        //[SerializeField, Disable] private ServerConnectionSettings _defaultFallbackInstanceServerSettings;
        [SerializeField, Disable] private bool _fallbackInstanceServerSettingsSetup = false;
        [SerializeField, Disable] private ServerConnectionSettings _fallbackInstanceServerSettings; 
        public ServerConnectionSettings FallbackInstanceServerSettings 
        {
            get 
            {
                if (!_fallbackInstanceServerSettingsSetup)
                {
                    if (Application.platform == RuntimePlatform.Android)
                    {
                        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                        using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                        using (AndroidJavaObject intent = currentActivity.Call<AndroidJavaObject>("getIntent"))
                        {
                            bool hasArgs = intent == null ? false : intent.Call<bool>("getBooleanExtra", HasArgsArgName, false);

                            if (hasArgs)
                            {
                                string instanceServerSettingsBytesAsString = intent.Call<string>("getStringExtra", FallbackInstanceServerSettingsArgName);
                                byte[] instanceServerSettingsBytes = System.Convert.FromBase64String(instanceServerSettingsBytesAsString);
                                _fallbackInstanceServerSettings = new ServerConnectionSettings(instanceServerSettingsBytes);
                            }
                            else 
                            {
                                //_fallbackInstanceServerSettings = _defaultFallbackInstanceServerSettings;
                                _fallbackInstanceServerSettings = null;
                            }
                        }   
                    }
                    else
                    {
                        //_fallbackInstanceServerSettings = _defaultFallbackInstanceServerSettings;
                        _fallbackInstanceServerSettings = null;
                    }
                    _fallbackInstanceServerSettingsSetup = true;
                }

                return _fallbackInstanceServerSettings;
            }
            set 
            {
                _fallbackInstanceServerSettingsSetup = true;
                _fallbackInstanceServerSettings = value;
            }
        }


        public AndroidJavaObject AddArgsToIntent(AndroidJavaObject intent)
        {
            intent.Call<AndroidJavaObject>("putExtra", HasArgsArgName, true);

            intent.Call<AndroidJavaObject>("putExtra", PlatformConnectionSettingsArgName, Convert.ToBase64String(PlatformServerConnectionSettings.Bytes));

            intent.Call<AndroidJavaObject>("putExtra", PlatformCustomerNameArgName, PlatformCustomerName);
            intent.Call<AndroidJavaObject>("putExtra", PlatformPasswordArgName, PlatformCustomerPassword);

            intent.Call<AndroidJavaObject>("putExtra", PlatformClientIDArgName, (int)PlatformClientID);
            intent.Call<AndroidJavaObject>("putExtra", InstanceCodeArgName, InstanceCode);

            intent.Call<AndroidJavaObject>("putExtra", NumberOfActiveWorldsArgName, _activeWorldsList.Count);
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);
            foreach (WorldDetails activeWorld in _activeWorldsList)
            {
                ushort activeWorldBytesLength = (ushort)activeWorld.Bytes.Length;
                writer.Write(activeWorldBytesLength);
                writer.Write(activeWorld.Bytes);
            }
            intent.Call<AndroidJavaObject>("putExtra", ActiveWorldsArgName, Convert.ToBase64String(stream.ToArray()));

            intent.Call<AndroidJavaObject>("putExtra", WorldBuildsFTPServerSettingsArgName, Convert.ToBase64String(WorldBuildsFTPServerSettings.Bytes));
            intent.Call<AndroidJavaObject>("putExtra", FallbackWorldSubStoreFTPServerSettingsArgName, Convert.ToBase64String(FallbackWorldSubStoreFTPServerSettings.Bytes));
            intent.Call<AndroidJavaObject>("putExtra", FallbackInstanceServerSettingsArgName,  Convert.ToBase64String(FallbackInstanceServerSettings.Bytes));

            return intent;
        }

        private void Awake()
        {
            if (FindObjectsByType<PlatformPersistentDataHandler>(FindObjectsSortMode.None).Length > 1)
            {
                Debug.LogError("There should only be one PlatformSettingsHandler in the scene, but a new one was created. Deleting the new one.");
                Destroy(gameObject);
                return;
            }

            ResetData();

            //gameObject.hideFlags = HideFlags.HideInHierarchy; //To hide
            gameObject.hideFlags &= ~HideFlags.HideInHierarchy; //To show
            DontDestroyOnLoad(this);
        }

        private void OnDisable()
        {
            ResetData();
        }

        private void ResetData() 
        {
            _platformClientIDSetup = false;
            _platformCustomerNameSetup = false;
            _platformPasswordSetup = false;
            _instanceCodeSetup = false;
            _activeWorldsSetup = false;
            _worldBuildsFTPServerSettingsSetup = false;
            _fallbackWorldSubStoreFTPServerSettingsSetup = false;
            _fallbackInstanceServerSettingsSetup = false;
        }
    }
}
