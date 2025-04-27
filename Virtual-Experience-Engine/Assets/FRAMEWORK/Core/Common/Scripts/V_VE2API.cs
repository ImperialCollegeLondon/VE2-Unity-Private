using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Collections;
using System;
using Toolbox.Folders;
using UnityEditor;
using UnityEngine.SceneManagement;
//using VE2.Core.Player.API;
//using VE2.Core.UI.Internal;
namespace VE2.Core.Common
{
    public class V_VE2API : MonoBehaviour,IVE2API
    {
        private static V_VE2API _instance;
        public static V_VE2API Instance
        { //Reload-proof singleton
            get
            {
                if (_instance == null)
                    _instance = FindFirstObjectByType<V_VE2API>();

                if (_instance == null)
                    _instance = new GameObject($"V_VE2API-{SceneManager.GetActiveScene().name}").AddComponent<V_VE2API>();

                return _instance;
            }
        }

        private string _localUserID;
        private LayerMask _traversibleLayers;
        private GameObject _playerGO;
        private Camera _activeCamera;
        private Camera _playerCamera2D;
        private Camera _playerCameraVR;

        //private PlayerTransformData _playerTransformData;

        [Help("Must exist on PluginRoot GameObject, there can only be one of these in the scene!" +
               "\n\nUse this API to deal with anything VE2 that doesn't relate to Grabbables, " +
               "Activatables, Adjustables, InfoPoints, PluginSyncables, etc" +
               "\n\nMake sure you have a Plugin_Home on this GameObject to easily access the API's interface", UnityMessageType.Info)]
        [Hide]public string inspectorInfo = "";
        protected string GetDocsURL() => "https://github.com/ImperialCollegeLondon/VE2-Distribution";


        [EditorButton(nameof(OpenDocs), "<b>Open Docs</b>")]
        [Hide] public bool openDocs = false;
        private void OpenDocs()
        {
            Application.OpenURL(GetDocsURL());
        }
        [EditorButton(nameof(FindPrefabsFolder), "<b>Find Prefabs Folder</b>")]
        [Hide] public bool findPrefabsFolder = false;
        private void FindPrefabsFolder()
        {
#if UNITY_EDITOR
            UnityEngine.Object folderObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/FRAMEWORK/Core/Samples/Models");
            Selection.activeObject = folderObject;
            EditorUtility.FocusProjectWindow();
            EditorGUIUtility.PingObject(folderObject);
#endif
        }

        [EditorButton(nameof(FindSampleScene), "<b>Find Sample Scene</b>")]
        [Hide] public bool findSampleScene = false;
        private void FindSampleScene()
        {
#if UNITY_EDITOR
            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath("Assets/FRAMEWORK/Core/Samples/Scenes/SampleScene.unity", typeof(UnityEngine.Object));
            UnityEngine.Object[] selection = new UnityEngine.Object[1];
            selection[0] = obj;
            Selection.objects = selection;
            EditorUtility.FocusProjectWindow();
            EditorGUIUtility.PingObject(obj);
#endif
        }

        [SpaceArea(10.0f, Order = 1)]
        /// <summary>
        /// the ui panel that sits on the front page of the main menu
        /// </summary>
        public GameObject primaryPluginUIPanel = null; /*FindFirstObjectByType<PluginPrimaryUIHolderTag>().gameObject;*/

        [SpaceArea(10.0f, Order = 2)]
        /// <summary>
        /// the ui panel that will appear on the right hand wrist
        /// if 2d mode, will appear in the top tight corner
        /// </summary>
        public GameObject secondaryPluginUIPanel = null; /*FindFirstObjectByType<PluginSecondaryHolderUITag>().gameObject;*/

        [BeginGroup(Style = GroupStyle.Round)]
        [Title("Events", ApplyCondition = true)]
        public UnityEvent OnBecomeHost;
        
        public UnityEvent OnLoseHost;

        public UnityEvent OnBecomeAdmin;

        /// <summary>
        /// Invoked when a remote user has joined our instance, passes a string for client ID. Not invoked when WE join the instance
        /// </summary>
        public UserInstanceEvent OnUserJoinedInstance;

        /// <summary>
        /// Invoked when a remote user has left our instance, passes a string for client ID. Not invoked when WE leave the instance
        /// </summary>
        public UserInstanceEvent OnUserLeftInstance;

        //NOTE: All events starting here are active
        public UnityEvent OnSwitchToVRMode;

        public UnityEvent OnSwitchTo2DMode;

        public UnityEvent OnActivateMainMenu;

        public UnityEvent OnDeactivateMainMenu;

        public UnityEvent OnTeleport;

        public UnityEvent OnHorizontalDrag;

        public UnityEvent OnVerticalDrag;

        public UnityEvent<string> OnSnapTurn;

        public UnityEvent OnResetViewVR;
        //NOTE: All events ending here are active


        public LargeFileOperationFinishedEvent onLargeFileOperationFinished;

        [EndGroup]
        public ReceivedSmallFileEvent OnSmallFileReceivedFromServer;

        [BeginGroup(Style = GroupStyle.Round)]
        [Title("Plugin Avatars", ApplyCondition = true)]
        public List<GameObject> heads = new List<GameObject>();

        public List<GameObject> torsos = new List<GameObject>();

        [Help("Supply the right hand only, this will be mirrored to create the left hand",UnityMessageType.Info)]
        [EndGroup]
        public List<GameObject> hands = new List<GameObject>();

        //private void OnDestroy()
        //{
        //    if (StaticData.LocalPlayerOverrides.freeFlyMode)
        //    {
        //        StaticData.LocalPlayerOverrides.freeFlyMode = false;
        //        StaticData.LocalPlayerOverrides.onFreeFlyModeDisable.Invoke();
        //    }

        //    if (StaticData.PlayerGameObjects.camera2D != null)
        //    {
        //        StaticData.PlayerGameObjects.camera2D.farClipPlane = StaticData.Variables.defaultCamFarClip2D;
        //        StaticData.PlayerGameObjects.cameraVR.farClipPlane = StaticData.Variables.defaultCamFarClipVR;
        //    }

        //    StaticData.Variables.sprintSpeed = StaticData.Constants.DEFAULT_SPRINT_SPEED_2D;
        //    StaticData.Variables.teleportDistanceMult = 1;
        //}

        public bool DoesIDMatchLocalUser(string id)
        {
            return _localUserID.Equals(id);
        }

        //        public string GetDisplayNameWithClientID(string id)
        //        {
        //            ClientInfo clientInfo = StaticData.Networking.GetClientInfo(id);

        //            if (clientInfo == null)
        //            {
        //                V_Logger.Error("Tried to get display name of client " + id + " but no matching client ID found");
        //                return null;
        //            }

        //            return clientInfo.displayName;
        //        }

        //        public List<string> GetIDsOfUsersInInstance()
        //        {
        //            return new List<string>(StaticData.Networking.GetLocalInstanceInfo().clientInfos.Keys);
        //        }


        public GameObject GetLocalPlayerGameObject()
        {
            return _playerGO;
        }

        public string GetLocalUserID()
        {
            return _localUserID;
        }

        public string GetLocalUserDisplayName()
        {
            return $"LOCAL USER + {_localUserID}";
            //return StaticData.Networking.GetLocalClientInfo().displayName;
        }
        public string GetDisplayNameWithClientID(string id)
        {
            throw new NotImplementedException();
        }

        public List<string> GetIDsOfUsersInInstance()
        {
            throw new NotImplementedException();
        }

        public void ForceFreeHandMode()
        {
            throw new NotImplementedException();
        }
        public bool IsLocalUserLoggedInAsGuest()
        {
            return false;
            //return StaticData.Networking.userIdentity.samAccountName.Equals("guest");
        }

        public string GetLocalUserFirstName()
        {
            return "Fred";
            //return StaticData.Networking.userIdentity.firstName;
        }

        public string GetLocalUserLastName()
        {
            return "Tovey-Ansell";
            //return StaticData.Networking.userIdentity.lastName;
        }

        public string GetLocalUserDepartment()
        {
            return "Earth Science & Engineering";
            //return StaticData.Networking.userIdentity.department;
        }

        public string GetLocalUserJobTitle()
        {
            return "Chief of Cheeseburger Disposal Operations";
            //return StaticData.Networking.userIdentity.jobTitle;
        }

        public string GetCurrentInstanceCode()
        {
            return "Current Instance Code Not Gotten";
            //return StaticData.Networking.GetLocalInstanceInfo().instanceCode;
        }
        public int GetNumberOfUsersInInstance()
        {
            throw new NotImplementedException();
            //return V_PlayerSyncer.GetRemotePlayersInCurrentInstance().Count + 1;
        }

        public bool IsLocalUserAdmin()
        {
            throw new NotImplementedException();
            //return StaticData.Networking.GetLocalClientInfo().isAdmin;
        }

        public bool IsUserAdmin(string clientID)
        {
            throw new NotImplementedException();
            //ClientInfo clientInfo = StaticData.Networking.GetClientInfo(clientID);

            //if (clientInfo == null)
            //{
            //    V_Logger.Error("Tried to get admin state of client " + clientID + " but couldn't find client");
            //    return false;
            //}

            //return clientInfo.isAdmin;
        }

        public bool IsHost()
        {
            throw new NotImplementedException();
            //return StaticData.Networking.IsHost();
        }

        public bool IsVRMode()
        {
            throw new NotImplementedException();
            //return StaticData.Utils.vrMode;
        }

        public bool IsMainMenuActive()
        {
            throw new NotImplementedException();
            //return StaticData.Utils.frameworkUIActive;
        }

        public bool HasVRViewBeenCalibrated()
        {
            throw new NotImplementedException();
            //return StaticData.Utils.vrViewCalibrated;
        }

        ///Large File system - passthroughs to V_PluginLargeFileManager 
        public bool LargeFile_StartDownload(string filename)
        {
            throw new NotImplementedException();
            //return V_PluginLargeFileManager.StartDownload(filename);
        }

        public bool LargeFile_StartUpload(string filename)
        {
            throw new NotImplementedException();
            //return V_PluginLargeFileManager.StartUpload(filename);
        }

        public int LargeFile_GetPercentProgress(string filename)
        {
            throw new NotImplementedException();
            //return V_PluginLargeFileManager.GetPercentageProgress(filename);
        }

        public string LargeFile_GetLocalPath()
        {
            throw new NotImplementedException();
            //return V_PluginLargeFileManager.GetMyLocalFilePath();
        }

        public List<string> LargeFile_GetRemoteFileNames()
        {
            throw new NotImplementedException();
            //return V_PluginLargeFileManager.GetRemoteFileNames();
        }

        public List<string> LargeFile_GetLocalFileNames()
        {
            throw new NotImplementedException();
            //return V_PluginLargeFileManager.GetLocalFileNames();
        }

        public ulong LargeFile_GetFileSize(string filename)
        {
            throw new NotImplementedException();
            //return V_PluginLargeFileManager.GetFileSize(filename);
        }

        public bool LargeFile_IsAvailableLocally(string filename)
        {
            throw new NotImplementedException();
            //return V_PluginLargeFileManager.IsFileAvailableLocally(filename);
        }

        public bool LargeFile_IsAvailableRemotely(string filename)
        {
            throw new NotImplementedException();
            //return V_PluginLargeFileManager.IsFileAvailableRemotely(filename);
        }

        public bool LargeFile_IsQueuedForDownload(string filename)
        {
            throw new NotImplementedException();
            //return V_PluginLargeFileManager.IsFileQueuedForDownload(filename);
        }

        public bool LargeFile_DeleteLocal(string filename)
        {
            throw new NotImplementedException();
            //return V_PluginLargeFileManager.DeleteLocalFile(filename);
        }

        public bool LargeFile_DeleteRemote(string filename)
        {
            throw new NotImplementedException();
            //return V_PluginLargeFileManager.DeleteRemoteFile(filename);
        }

        public bool LargeFile_IsSystemReady()
        {
            throw new NotImplementedException();
            //return V_PluginLargeFileManager.IsSystemReady();
        }

        public string LargeFile_GetLocalPathForFile(string filename)
        {
            throw new NotImplementedException();
            //return V_PluginLargeFileManager.GetMyLocalFilePath() + "\\" + filename;
        }

        public void StoreFile(string fileName, byte[] data)
        {
            if (data.Length > 5000)
            {
                V_Logger.Error($"File {fileName} too big ({data.Length} bytes, limit is 5000)");
            }
            else
            {
                //Store File
                throw new NotImplementedException();
                //V_MasterNetworkController.StoreFile(fileName, data);
            }
                
        }

        public void RetrieveFile(string fileName)
        {
            throw new NotImplementedException();
            //V_MasterNetworkController.RetrieveFile(fileName);
        }

        public Camera GetLocalActivePlayerCamera()
        {
            return _activeCamera;
        }

        public Camera GetLocalPlayerCamera2D()
        {
            return _playerCamera2D;
        }

        public Camera GetLocalPlayerCameraVR()
        {
            return _playerCameraVR;
        }

        public void VibrateRightController(float amplitude, float duration) //Amplitude should be between 0 and 1
        {
            throw new NotImplementedException();
            //StaticData.Controllers.freeHandControllerRight.VibrateWithAmplitudeAndDuration(amplitude, duration);
        }

        public void VibrateLeftController(float amplitude, float duration) //Amplitude should be between 0 and 1
        {
            throw new NotImplementedException();
            //StaticData.Controllers.freeHandControllerLeft.VibrateWithAmplitudeAndDuration(amplitude, duration);
        }

        public void ToggleSnapTurnVR(bool toggle)
        {
            throw new NotImplementedException();
            //StaticData.LocalPlayerOverrides.snapTurnVREnabled = toggle;
        }

        public void ToggleHorizontalDrag(bool toggle)
        {
            throw new NotImplementedException();
            //StaticData.LocalPlayerOverrides.horizontalDragEnabled = toggle;
        }

        public void ToggleVerticalDrag(bool toggle)
        {
            throw new NotImplementedException();
            //StaticData.LocalPlayerOverrides.verticalDragEnabled = toggle;
        }

        public void ToggleTeleporter(bool toggle)
        {
            throw new NotImplementedException();
            //StaticData.LocalPlayerOverrides.teleporterEnabled = toggle;
        }

        public void ToggleMenuButton(bool toggle)
        {
            throw new NotImplementedException();
            //StaticData.LocalPlayerOverrides.menuButtonEnabled = toggle;
        }

        public void ToggleWASD(bool toggle)
        {
            throw new NotImplementedException();
            //StaticData.LocalPlayerOverrides.wasdEnabled = toggle;
        }

        public void ToggleMouseLook(bool toggle)
        {
            throw new NotImplementedException();
            //StaticData.LocalPlayerOverrides.mouseLookEnabled = toggle;
        }

        public void ToggleCrouch(bool toggle)
        {
            throw new NotImplementedException();
            //StaticData.LocalPlayerOverrides.crouchEnabled = toggle;
        }

        public void ToggleCycleToolTooltip(bool toggle)
        {
            throw new NotImplementedException();
            //foreach (VRTooltipHandler tooltipHandler in StaticData.Controllers.vrTooltipHandlersFreeHand)
            //    tooltipHandler.ToggleHighlightCycleToolButtons(toggle);
            //foreach (VRTooltipHandler tooltipHandler in StaticData.Controllers.vrTooltipHandlersLocomotion)
            //    tooltipHandler.ToggleHighlightCycleToolButtons(toggle);
        }

        public void ToggleThumbstickTooltipFreeHand(bool toggle)
        {
            throw new NotImplementedException();
            //foreach (VRTooltipHandler tooltipHandler in StaticData.Controllers.vrTooltipHandlersFreeHand)
            //    tooltipHandler.ToggleHighlightThumbstick(toggle);
        }

        public void ToggleThumbstickTooltipLocomotion(bool toggle)
        {
            throw new NotImplementedException();
            //foreach (VRTooltipHandler tooltipHandler in StaticData.Controllers.vrTooltipHandlersLocomotion)
            //    tooltipHandler.ToggleHighlightThumbstick(toggle);
        }

        public void ToggleGripTooltipFreeHand(bool toggle)
        {
            throw new NotImplementedException();
            //foreach (VRTooltipHandler tooltipHandler in StaticData.Controllers.vrTooltipHandlersFreeHand)
            //    tooltipHandler.ToggleHighlightGripButtons(toggle);
        }

        public void ToggleGripTooltipLocomotion(bool toggle)
        {
            throw new NotImplementedException();
            //foreach (VRTooltipHandler tooltipHandler in StaticData.Controllers.vrTooltipHandlersLocomotion)
            //    tooltipHandler.ToggleHighlightGripButtons(toggle);
        }

        public void ToggleTriggerTooltipFreeHand(bool toggle)
        {
            throw new NotImplementedException();
            //foreach (VRTooltipHandler tooltipHandler in StaticData.Controllers.vrTooltipHandlersFreeHand)
            //    tooltipHandler.ToggleHighlightTriggerButtons(toggle);
        }

        public void ToggleTriggerTooltipLocomotion(bool toggle)
        {
            throw new NotImplementedException();
            //foreach (VRTooltipHandler tooltipHandler in StaticData.Controllers.vrTooltipHandlersLocomotion)
            //    tooltipHandler.ToggleHighlightTriggerButtons(toggle);
        }

        public void SetLocomotionTooltipsOff()
        {
            throw new NotImplementedException();
            //foreach (VRTooltipHandler tooltipHandler in StaticData.Controllers.vrTooltipHandlersLocomotion)
            //    tooltipHandler.DisableAllTooltips();
        }
        public void SetFreeHandTooltipsOff()
        {
            throw new NotImplementedException();
            //foreach (VRTooltipHandler tooltipHandler in StaticData.Controllers.vrTooltipHandlersFreeHand)
            //    tooltipHandler.DisableAllTooltips();
        }

        public void ToggleMenuTooltip(bool toggle)
        {
            throw new NotImplementedException();
            //foreach (VRTooltipHandler tooltipHandler in StaticData.Controllers.vrTooltipHandlersFreeHand)
            //    tooltipHandler.TryToggleHighlightMenuButton(toggle);
            //foreach (VRTooltipHandler tooltipHandler in StaticData.Controllers.vrTooltipHandlersLocomotion)
            //    tooltipHandler.TryToggleHighlightMenuButton(toggle);
        }

        public void SetLocalUserID(string id)
        {
            _localUserID = id;
        }

        public void SetTraversibleLayers(LayerMask layerName)
        {
            _traversibleLayers = layerName;
        }

        public void SetPlayerActiveCamera(Camera camera)
        {
            _activeCamera = camera;
        }

        public void SetPlayerCameras(Camera playerCamera2D, Camera playerCameraVR)
        {
            _playerCamera2D = playerCamera2D;
            _playerCameraVR = playerCameraVR;
        }
        public void SetDragMoveLabelActive(bool value)
        {
            throw new NotImplementedException();
            //foreach (V_LocomotionLabelManager locomotionLabelManager in StaticData.Controllers.locomotionLabelManagers)
            //{
            //    locomotionLabelManager.SetDragMoveLabelActive(value);
            //}
        }

        public void SetAdjustHeightLabelActive(bool value)
        {
            throw new NotImplementedException();
            //foreach (V_LocomotionLabelManager locomotionLabelManager in StaticData.Controllers.locomotionLabelManagers)
            //{
            //    locomotionLabelManager.SetAdjustHeightLabelActive(value);
            //}
        }

        public void SetMenuLabelActive(bool value)
        {
            throw new NotImplementedException();
            //foreach (V_LocomotionLabelManager locomotionLabelManager in StaticData.Controllers.locomotionLabelManagers)
            //{
            //    locomotionLabelManager.TrySetMenuLabelActive(value);
            //}
        }

        public void SetTurnOnlyLabelActive(bool value)
        {
            throw new NotImplementedException();
            //foreach (V_LocomotionLabelManager locomotionLabelManager in StaticData.Controllers.locomotionLabelManagers)
            //{
            //    locomotionLabelManager.SetTurnOnlyLabelActive(value);
            //}
        }

        public void SetTurnTeleportLabelActive(bool value)
        {
            throw new NotImplementedException();
            //foreach (V_LocomotionLabelManager locomotionLabelManager in StaticData.Controllers.locomotionLabelManagers)
            //{
            //    locomotionLabelManager.SetTurnTeleportLabelActive(value);
            //}
        }

        public LayerMask GetTraversibleLayers()
        {
            return _traversibleLayers;
        }

        public void OverrideLocalPlayerPosition(Vector3 position)
        {
            throw new NotImplementedException();
            //StaticData.PlayerGameObjects.player.GetComponent<CharacterController>().enabled = false;

            //Vector3 currentPosition = StaticData.PlayerGameObjects.ActivePlayer().transform.parent.position;
            //Vector3 offset = position - currentPosition;

            //StaticData.PlayerGameObjects.ActivePlayer().transform.parent.position += offset;
            //StaticData.PlayerGameObjects.vrTruePosition.transform.position += offset;

            //StaticData.PlayerGameObjects.player.GetComponent<CharacterController>().enabled = true;
        }

        public void OverrideLocalPlayerForward(Vector3 forward)
        {
            StartCoroutine(RotatePlayerRig(forward));
        }

        private IEnumerator RotatePlayerRig(Vector3 newForward)
        {
            throw new NotImplementedException();
            //yield return new WaitForEndOfFrame();

            //Vector3 currentForward = StaticData.PlayerGameObjects.ActivePlayerCamera().transform.forward;
            //Vector3 correctedCurrentForward = new(currentForward.x, 0, currentForward.z);

            //Vector3 correctedNewForward = new(newForward.x, 0, newForward.z);

            //float offset = Vector3.SignedAngle(correctedCurrentForward, correctedNewForward, Vector3.up);
            //StaticData.PlayerGameObjects.ActivePlayer().transform.parent.Rotate(Vector3.up, offset);
        }

        public void ToggleHighlightBackToHubButton(bool toggle)
        {
            throw new NotImplementedException();
            //V_PrimaryUIManager.instance.ToggleHighlightBackToHubButton(toggle);
        }

        //GETTERS FOR EVENTS----------------------------------------

        public UnityEvent GetEvent_OnBecomeHost()
        {
            return OnBecomeHost;
        }

        public UnityEvent GetEvent_OnLoseHost()
        {
            return OnLoseHost;
        }
        public UnityEvent GetEvent_OnBecomeAdmin()
        {
            return OnBecomeAdmin;
        }

        public UserInstanceEvent GetEvent_OnUserJoinedInstance()
        {
            return OnUserJoinedInstance;
        }

        public UserInstanceEvent GetEvent_OnUserLeftInstance()
        {
            return OnUserLeftInstance;
        }

        public UnityEvent GetEvent_OnSwitchToVRMode()
        {
            return OnSwitchToVRMode;
        }

        public UnityEvent GetEvent_OnSwitchTo2DMode()
        {
            return OnSwitchTo2DMode;
        }

        public UnityEvent GetEvent_OnActivateMainMenu()
        {
            return OnActivateMainMenu;
        }

        public UnityEvent GetEvent_OnDeactivateMainMenu()
        {
            return OnDeactivateMainMenu;
        }

        public UnityEvent GetEvent_OnTeleport()
        {
            return OnTeleport;
        }

        public UnityEvent GetEvent_OnHorizontalDrag()
        {
            return OnHorizontalDrag;
        }

        public UnityEvent GetEvent_OnVerticalDrag()
        {
            return OnVerticalDrag;
        }

        public UnityEvent<string> GetEvent_OnSnapTurn()
        {
            return OnSnapTurn;
        }

        public UnityEvent GetEvent_OnResetViewVR()
        {
            return OnResetViewVR;
        }

        public UnityEvent GetEvent_OnResetVRView()
        {
            return OnResetViewVR;
        }

        public ReceivedSmallFileEvent GetEvent_OnSmallFileReceivedFromServer()
        {
            return OnSmallFileReceivedFromServer;
        }

        public LargeFileOperationFinishedEvent GetEvent_OnLargeFileOperationFinished()
        {
            return onLargeFileOperationFinished;
        }

        public void MainUI_Show(bool on)
        {
            throw new NotImplementedException();
            //if (StaticData.Utils.frameworkUIActive && !on)
            //    MainUI_Toggle();
            //else if (!StaticData.Utils.frameworkUIActive && on)
            //    MainUI_Toggle();
        }

        public void MainUI_Toggle()
        {
            throw new NotImplementedException();
            //StaticData.Controllers.frameworkUIToggler.ToggleUI(true); //True for showing UI even if menu button is off
        }

        public bool MainUI_IsShowing()
        {
            throw new NotImplementedException();
            //return StaticData.Utils.frameworkUIActive;
        }

        public Vector3 GetPositionForPlayer(string playerID)
        {
            throw new NotImplementedException();
            //if (playerID.Equals(StaticData.Networking.GetLocalClientInfo().clientID))
            //    return StaticData.PlayerGameObjects.player.transform.position;

            //if (V_PlayerSyncer.GetRemotePlayersInCurrentInstance().TryGetValue(playerID, out V_RemotePlayerController remotePlayer))
            //{
            //    return remotePlayer.origin.transform.position;
            //}

            //V_Logger.Error("Tried to get position for player with ID " + playerID + " but could not find that player in our instance");
            //return Vector3.zero;
        }

        public Vector3 GetLookPositionForPlayer(string playerID)
        {
            throw new NotImplementedException();
            //if (playerID.Equals(StaticData.Networking.GetLocalClientInfo().clientID))
            //    return StaticData.PlayerGameObjects.ActivePlayerCamera().transform.position;

            //if (V_PlayerSyncer.GetRemotePlayersInCurrentInstance().TryGetValue(playerID, out V_RemotePlayerController remotePlayer))
            //{
            //    return remotePlayer.headHolder.transform.position;
            //}

            //V_Logger.Error("Tried to get look direction for player with ID " + playerID + " but could not find that player in our instance");
            //return Vector3.zero;
        }

        public Vector3 GetLookDirectionForPlayer(string playerID)
        {
            throw new NotImplementedException();
            //if (playerID.Equals(StaticData.Networking.GetLocalClientInfo().clientID))
            //    return StaticData.PlayerGameObjects.ActivePlayerCamera().transform.forward;

            //if (V_PlayerSyncer.GetRemotePlayersInCurrentInstance().TryGetValue(playerID, out V_RemotePlayerController remotePlayer))
            //{
            //    return remotePlayer.headHolder.transform.forward;
            //}

            //V_Logger.Error("Tried to get look direction for player with ID " + playerID + " but could not find that player in our instance");
            //return Vector3.zero;
        }

        public Quaternion GetRotationForPlayer(string playerID)
        {
            throw new NotImplementedException();
            //if (playerID.Equals(StaticData.Networking.GetLocalClientInfo().clientID))
            //    return StaticData.PlayerGameObjects.ActivePlayerCamera().transform.rotation;

            //if (V_PlayerSyncer.GetRemotePlayersInCurrentInstance().TryGetValue(playerID, out V_RemotePlayerController remotePlayer))
            //{
            //    return remotePlayer.headHolder.transform.rotation;
            //}

            //V_Logger.Error("Tried to get rotation for player with ID " + playerID + " but could not find that player in our instance");
            //return Quaternion.identity;
        }

        public void SetLocalHeadAvatarOverride(int headAvatar)
        {
            throw new NotImplementedException();
            //if (heads.Count < headAvatar - 1)
            //{
            //    V_Logger.Error("Tried to set local user head avatar to #" + headAvatar + " but that head wasn't found");
            //    return;
            //}

            //AvatarDetails desiredAvatarDetails = new(StaticData.Networking.GetLocalClientInfo().avatarDetails);

            //if (headAvatar == desiredAvatarDetails.headPluginAvatarType)
            //    return;

            //desiredAvatarDetails.headPluginAvatarType = headAvatar;
            ////todo, don't want to send off three different messages...
        }

        /// <summary>
        /// Takes selections for the head, torso and hands. Pass -1s to remove the avatar override and switch back to framework avatars
        /// </summary>
        public void SetLocalAvatarOverride(int headAvatar, int torsovatar, int handsAvatar)
        {
            throw new NotImplementedException();
            //if (heads.Count < headAvatar - 1 || torsos.Count < torsovatar - 1 || hands.Count < handsAvatar - 1)
            //{
            //    V_Logger.Error("Tried to set local user avatar override but selection invalid");
            //    return;
            //}

            //AvatarDetails desiredAvatarDetails = new(StaticData.Networking.GetLocalClientInfo().avatarDetails);

            //if (headAvatar == desiredAvatarDetails.headPluginAvatarType &&
            //    torsovatar == desiredAvatarDetails.torsoPluginAvatarType &&
            //    handsAvatar == desiredAvatarDetails.handsPluginAvatarType)
            //    return;

            //desiredAvatarDetails.headPluginAvatarType = headAvatar;
            //desiredAvatarDetails.torsoPluginAvatarType = torsovatar;
            //desiredAvatarDetails.handsPluginAvatarType = handsAvatar;

            //V_NetworkCommsHandler.Send.UpdateAvatarDetails(desiredAvatarDetails);
        }

        public void RemoveAllLocalAvatarOverrides()
        {
            SetLocalAvatarOverride(-1, -1, -1);
            ToggleHideLocalAvatar(true);
        }

        public void ToggleHideLocalAvatar(bool toggle)
        {
            throw new NotImplementedException();
            //AvatarDetails desiredAvatarDetails = new(StaticData.Networking.GetLocalClientInfo().avatarDetails);

            //if (desiredAvatarDetails.showAvatar != toggle)
            //    return;

            //desiredAvatarDetails.showAvatar = !toggle;

            //V_NetworkCommsHandler.Send.UpdateAvatarDetails(desiredAvatarDetails);
        }

        public void ToggleForceLocalMute(bool toggle)
        {
            V_Logger.Warning("Not yet implemented: Toggling force local mute - " + toggle);
        }

        public void ToggleLocalFreeFlyMode(bool toggle)
        {
            throw new NotImplementedException();
            //if (StaticData.LocalPlayerOverrides.freeFlyMode != toggle)
            //{
            //    StaticData.LocalPlayerOverrides.freeFlyMode = toggle;

            //    if (toggle)
            //        StaticData.LocalPlayerOverrides.onFreeFlyModeEnable.Invoke();
            //    else
            //        StaticData.LocalPlayerOverrides.onFreeFlyModeDisable.Invoke();
            //}
        }

        public void UpdateLocalCameraFarClipDistance(float distance)
        {
            throw new NotImplementedException();
            //StaticData.PlayerGameObjects.camera2D.farClipPlane = distance;
            //StaticData.PlayerGameObjects.cameraVR.farClipPlane = distance;
        }

        public void ResetLocalCameraFarClipDistanceToDefault()
        {
            throw new NotImplementedException();
            //StaticData.PlayerGameObjects.camera2D.farClipPlane = StaticData.Variables.defaultCamFarClip2D;
            //StaticData.PlayerGameObjects.cameraVR.farClipPlane = StaticData.Variables.defaultCamFarClipVR;
        }

        public void UpdateLocalSprintSpeed2D(float speed)
        {
            throw new NotImplementedException();
            //StaticData.Variables.sprintSpeed = speed;
        }

        public void ResetLocalSprintSpeed2DToDefault()
        {
            throw new NotImplementedException();
            //StaticData.Variables.sprintSpeed = StaticData.Constants.DEFAULT_SPRINT_SPEED_2D;
        }

        public void UpdateMaxLocalTeleportDistanceMultiplier(float mult)
        {
            throw new NotImplementedException();
            //StaticData.Variables.teleportDistanceMult = mult;
        }

        public void ResetMaxTeleportDistanceToDefault()
        {
            throw new NotImplementedException();
            //StaticData.Variables.teleportDistanceMult = 1;
        }

        public void ToggleCameraCovers(bool toggle, float fadeTime)
        {
            throw new NotImplementedException();
            //StaticData.Controllers.cameraFader.ToggleCameraCovers(toggle, fadeTime);
        }

        public GameObject SpawnLocalPing(Vector3 position, string pingMessage)
        {
            throw new NotImplementedException();
            //return null;
            //V_PingNotificationHandler.instance.ShowPing(position, pingMessage);

            //return V_PingNotificationHandler.instance.gameObject;
        }

        public GameObject SpawnLocalPing(GameObject goToPing, string pingMessage)
        {
            throw new NotImplementedException();
            //return SpawnLocalPing(goToPing.transform.position, pingMessage);
        }

        public void KillCurrentPing()
        {
            throw new NotImplementedException();
            //V_PingNotificationHandler.instance.KillPing();
        }

        /// <summary>
        /// Careful! Will throw an exception if not in 2d mode
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        public Nullable<Vector3> GetRaycastDirection2D()
        {
            throw new NotImplementedException();
            //return null;
            //if (StaticData.Utils.vrMode)
            //    return null;

            //return StaticData.InteractableData.TwoDRaycastDirection;
        }

        /// <summary>
        /// Careful! Will throw an exception if not in vr mode, and right hand isn't in FreeHand mode!
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        public Nullable<Vector3> GetRaycastDirectionVRRight()
        {
            throw new NotImplementedException();
            //return null;
            //if (!StaticData.Utils.vrMode)
            //    return null;

            //if (GetHandModeRight() != HandMode.FreeHand)
            //    return null;

            //return StaticData.InteractableData.rightHandRaycastDirection;
        }

        /// <summary>
        /// Careful! Will throw an exception if not in vr mode, and right hand isn't in FreeHand mode!
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        public Nullable<Vector3> GetRaycastDirectionVRLeft()
        {
            throw new NotImplementedException();
            //return null;
            //if (!StaticData.Utils.vrMode)
            //    return null;

            //if (GetHandModeLeft() != HandMode.FreeHand)
            //    return null;

            //return StaticData.InteractableData.leftHandRaycastDirection;
        }

        public Vector3? GetRaycastHitPosition2D()
        {
            throw new NotImplementedException();
            //return null;
            //if (StaticData.Utils.vrMode)
            //    return null;

            ////Interestingly, the pos can be 0 when the GO is null
            ////Probably a bug/quirk of how its stored in StaticData
            ////Regardless, should return null in this case!
            //if (StaticData.InteractableData.TwoDRaycastHitGameObject == null)
            //    return null;

            //return StaticData.InteractableData.TwoDRaycastPosition;
        }

        public Vector3? GetRaycastHitPositionVRRight()
        {
            throw new NotImplementedException();
            //return null;
            //if (!StaticData.Utils.vrMode)
            //    return null;

            //if (GetHandModeRight() != HandMode.FreeHand)
            //    return null;

            ////Interestingly, the pos can be 0 when the GO is null
            ////Probably a bug/quirk of how its stored in StaticData
            ////Regardless, should return null in this case!
            //if (StaticData.InteractableData.rightHandRaycastHitGameObject == null)
            //    return null;

            //return StaticData.InteractableData.rightHandRaycastPosition;
        }

        public Vector3? GetRaycastHitPositionVRLeft()
        {
            throw new NotImplementedException();
            //return null;
            //if (!StaticData.Utils.vrMode)
            //    return null;

            //if (GetHandModeLeft() != HandMode.FreeHand)
            //    return null;

            ////Interestingly, the pos can be 0 when the GO is null
            ////Probably a bug/quirk of how its stored in StaticData
            ////Regardless, should return null in this case!
            //if (StaticData.InteractableData.leftHandRaycastHitGameObject == null)
            //    return null;

            //return StaticData.InteractableData.leftHandRaycastPosition;
        }

        public GameObject GetRaycastHitGameObject2D()
        {
            throw new NotImplementedException();
            //return null;
            //if (StaticData.Utils.vrMode)
            //    return null;

            //return StaticData.InteractableData.TwoDRaycastHitGameObject;
        }

        public GameObject GetRaycastHitGameObjectVRRight()
        {
            throw new NotImplementedException();
            //return null;
            //{
            //    if (!StaticData.Utils.vrMode)
            //        return null;

            //    if (GetHandModeRight() != HandMode.FreeHand)
            //        return null;

            //    return StaticData.InteractableData.rightHandRaycastHitGameObject;
            //}
        }

        public GameObject GetRaycastHitGameObjectVRLeft()
        {
            throw new NotImplementedException();
            //return null;
            //if (!StaticData.Utils.vrMode)
            //    return null;

            //if (GetHandModeLeft() != HandMode.FreeHand)
            //    return null;

            //return StaticData.InteractableData.leftHandRaycastHitGameObject;
        }
    }
}

