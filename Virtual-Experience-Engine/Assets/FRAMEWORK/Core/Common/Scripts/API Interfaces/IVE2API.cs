using DarkRift;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Events;


namespace VE2.Core.Common
{
    [Serializable] public class LargeFileOperationFinishedEvent : UnityEvent<string, bool, string> { }
    [Serializable] public class ReceivedSmallFileEvent : UnityEvent<string, byte[]> { }
    [Serializable] public class UserInstanceEvent : UnityEvent<string> { }
    public interface IVE2API
    {
        public UnityEvent GetEvent_OnBecomeHost();
        public UnityEvent GetEvent_OnLoseHost();
        public UnityEvent GetEvent_OnBecomeAdmin();
        public UserInstanceEvent GetEvent_OnUserJoinedInstance();
        public UserInstanceEvent GetEvent_OnUserLeftInstance();
        public UnityEvent GetEvent_OnSwitchToVRMode();
        public UnityEvent GetEvent_OnSwitchTo2DMode();
        public UnityEvent GetEvent_OnActivateMainMenu();
        public UnityEvent GetEvent_OnDeactivateMainMenu();
        public UnityEvent GetEvent_OnTeleport();
        public UnityEvent GetEvent_OnHorizontalDrag();
        public UnityEvent GetEvent_OnVerticalDrag();
        public UnityEvent<string> GetEvent_OnSnapTurn();
        public UnityEvent GetEvent_OnResetVRView();
        public ReceivedSmallFileEvent GetEvent_OnSmallFileReceivedFromServer();
        public LargeFileOperationFinishedEvent GetEvent_OnLargeFileOperationFinished();

        public bool IsHost();

        public bool IsUserAdmin(string clientID);

        public bool IsLocalUserAdmin();

        public string GetLocalUserID();

        public string GetLocalUserDisplayName();

        public bool IsLocalUserLoggedInAsGuest();

        public string GetLocalUserFirstName();

        public string GetLocalUserLastName();

        public string GetLocalUserDepartment();

        public string GetLocalUserJobTitle();

        public string GetCurrentInstanceCode();
        /// <summary>
        /// Returns true if the passed id matches the local user's
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool DoesIDMatchLocalUser(string id);

        public string GetDisplayNameWithClientID(string id);

        /// <summary>
        /// Includes the local player.
        /// </summary>
        /// <returns></returns>
        public int GetNumberOfUsersInInstance();

        public List<string> GetIDsOfUsersInInstance();
        public bool IsVRMode();
        /// <summary>
        /// Returns the VR player if in VR mode, else returns 2d player
        /// </summary>
        /// <returns></returns>
        public GameObject GetLocalPlayerGameObject();

        public bool HasVRViewBeenCalibrated();

        /// <summary>
        /// Store data as a file on server. Limit is 5000 bytes.
        /// </summary>
        /// <param name="fileName">A filename - do not use special characters</param>
        /// <param name="data">The byte array to store</param>
        public void StoreFile(string fileName, byte[] data);

        /// <summary>
        /// Retrieve data from file on server. Use P_ReceiveStoredData to register a method for returned data.
        /// </summary>
        /// <param name="fileName">A filename - do not use special characters</param>
        public void RetrieveFile(string fileName);

        //Large File Storage API

        /// <summary>
        /// Queue a file for download from remote server to local storage
        /// This is asynchronous  - it may take some time. The ViRSE
        /// framework will notify you when the download is complete - 
        /// or you can check with LargeFile_IsAvailableLocally
        /// </summary>
        /// <param name="filename"></param>
        /// <returns>false if the file did not exist on the server</returns>
        public bool LargeFile_StartDownload(string filename);

        /// <summary>
        /// Queue a file for upload to the remote server to local storage
        /// This is asynchronous  - it may take some time. The ViRSE
        /// framework will notify you when the download is complete - 
        /// or you can check with LargeFile_IsAvailableRemotely
        /// </summary>
        /// <param name="filename"></param>
        /// <returns>false if the file did not exist in local storage</returns>
        public bool LargeFile_StartUpload(string filename);

        /// <summary>
        /// Use to obtain feedback on upload/download progress
        /// Queued uploads/downloads that have not yet started will give 0
        /// Completed uploads/downloads will give 100
        /// </summary>
        /// <param name="filename"></param>
        /// <returns>Integer value 0-100</returns>
        public int LargeFile_GetPercentProgress(string filename);

        /// <summary>
        /// Gives the local path where files for your world should be stored
        /// </summary>
        /// <returns></returns>
        public string LargeFile_GetLocalPath();

        /// <summary>
        /// Gives the local filename in your worlds local storage area
        /// for a particular file
        /// </summary>
        /// <returns></returns>
        public string LargeFile_GetLocalPathForFile(string filename);

        /// <summary>
        /// Provides a list of filenames for all files for your world stored
        /// on the remote server. This is syncronous - you don't have to wait,
        /// ViRSE automatically downloads this list from the server when your
        /// World starts
        /// </summary>
        /// <returns>List of strings containing filenames</returns>
        public List<string> LargeFile_GetRemoteFileNames();

        /// <summary>
        /// Provides a list of all files in your world' local storage folder
        /// </summary>
        /// <returns>List of strings containing filenames</returns>
        public List<string> LargeFile_GetLocalFileNames();

        /// <summary>
        /// Gives the size of a file, in bytes. If the file does not exist, it
        /// will give you 0 (and an error message in the console).
        /// If the remote and local files are not the same size, you will get 
        /// the size of the local one.
        /// Note that this works on files even when they only exist remotely
        /// </summary>
        /// <param name="filename"></param>
        /// <returns>Size in bytes (note - this is not an int, it's a 64-bit ulong()</returns>
        public ulong LargeFile_GetFileSize(string filename);

        /// <summary>
        /// Use to check whether a file exists on your local system. If they file DOES
        /// technically exist but is only 'partial' (still downloading) it will return false
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public bool LargeFile_IsAvailableLocally(string filename);

        /// <summary>
        /// Use to check whether file exists remotely. If the file is still uploading,
        /// it will return false
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public bool LargeFile_IsAvailableRemotely(string filename);

        /// <summary>
        /// Returns true if the file specified is queued for downloading,
        /// or has started but not yet completed downloading
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public bool LargeFile_IsQueuedForDownload(string filename);

        /// <summary>
        /// Delete a file from your local file storage
        /// Returns false  if failed (because file did not exist, or
        /// for some other reason).
        /// This does NOT remove the file from remote storage
        /// </summary>
        /// <param name="filename"></param>
        public bool LargeFile_DeleteLocal(string filename);

        /// <summary>
        /// Delete a file from remote storage
        /// Returns false  if failed because file did not exist, but other failures
        /// are possible after the operation starts. To confirm this has succeeded, 
        /// you should check the remote file list some time after using this method
        /// </summary>
        /// <param name="filename"></param>
        public bool LargeFile_DeleteRemote(string filename);

        /// <summary>
        /// The large file system must retrieve a file list from the remote server 
        /// before some methods (e.g. downloads) can proceed. Use this to check
        /// that it is ready.
        /// </summary>
        /// <returns></returns>
        public bool LargeFile_IsSystemReady();

        public Camera GetLocalActivePlayerCamera(); //Returns the local 2d or VR camera, depending on current mode

        public Camera GetLocalPlayerCamera2D();

        public Camera GetLocalPlayerCameraVR();

        public void OverrideLocalPlayerPosition(Vector3 position);

        public void OverrideLocalPlayerForward(Vector3 forward);

        public void VibrateRightController(float amplitude, float duration); //Amplitude should be between 0 and 1

        public void VibrateLeftController(float amplitude, float duration); //Amplitude should be between 0 and 1

        public void ToggleHighlightBackToHubButton(bool toggle);


        //RESTRICTED FUNCTIONS - Contact the ViRSE developers if you need these 

        public void ToggleSnapTurnVR(bool toggle);

        public void ToggleHorizontalDrag(bool toggle);

        public void ToggleVerticalDrag(bool toggle);

        public void ForceFreeHandMode();

        public void ToggleTeleporter(bool toggle);

        public void ToggleMenuButton(bool toggle);

        public void ToggleWASD(bool toggle);

        public void ToggleMouseLook(bool toggle);

        public void ToggleCrouch(bool toggle);

        public void ToggleCycleToolTooltip(bool toggle);

        public void ToggleThumbstickTooltipFreeHand(bool toggle);

        public void ToggleGripTooltipFreeHand(bool toggle);

        public void ToggleTriggerTooltipFreeHand(bool toggle);

        public void ToggleThumbstickTooltipLocomotion(bool toggle);

        public void ToggleGripTooltipLocomotion(bool toggle);

        public void ToggleTriggerTooltipLocomotion(bool toggle);

        public void SetLocomotionTooltipsOff();

        public void SetFreeHandTooltipsOff();

        public void ToggleMenuTooltip(bool toggle);

        public void SetDragMoveLabelActive(bool value);

        public void SetAdjustHeightLabelActive(bool value);

        public void SetMenuLabelActive(bool value);

        public void SetTurnOnlyLabelActive(bool value);

        public void SetTurnTeleportLabelActive(bool value);

        //END OF RESTRICTED

        public void MainUI_Show(bool on);

        public void MainUI_Toggle();

        public bool MainUI_IsShowing();

        public LayerMask GetTraversibleLayers();

        public Vector3 GetPositionForPlayer(string playerID); //Returns floor position, if in FreeFly mode, use GetLookPosition instead

        public Quaternion GetRotationForPlayer(string playerID);

        public Vector3 GetLookPositionForPlayer(string playerID);

        public Vector3 GetLookDirectionForPlayer(string playerID);

        public void SetLocalAvatarOverride(int headAvatar, int bodyAvatar, int handsAvatar);

        public void RemoveAllLocalAvatarOverrides(); //Will also show avatar if it is hidden

        public void ToggleHideLocalAvatar(bool toggle);

        public void ToggleForceLocalMute(bool toggle);

        public void ToggleLocalFreeFlyMode(bool toggle);

        public void UpdateLocalCameraFarClipDistance(float distance);

        public void ResetLocalCameraFarClipDistanceToDefault();

        public void UpdateLocalSprintSpeed2D(float speed); //6 is default

        public void ResetLocalSprintSpeed2DToDefault();

        public void UpdateMaxLocalTeleportDistanceMultiplier(float mult);

        public void ResetMaxTeleportDistanceToDefault();

        public void ToggleCameraCovers(bool toggle, float fadeTime);

        public GameObject SpawnLocalPing(Vector3 position, string pingMessage);

        public GameObject SpawnLocalPing(GameObject goToPing, string pingMessage);

        /// <summary>
        /// Will return null if not in 2d mode
        /// </summary>
        /// <returns></returns>
        public Nullable<Vector3> GetRaycastDirection2D();

        /// <summary>
        /// Will return null if not in VR mode, or if right hand mode isn't FreeHand
        /// </summary>
        /// <returns></returns>
        public Nullable<Vector3> GetRaycastDirectionVRRight();

        /// <summary>
        /// Will return null if not in VR mode, or if left hand mode isn't FreeHand
        /// </summary>
        /// <returns></returns>
        public Nullable<Vector3> GetRaycastDirectionVRLeft();

        /// <summary>
        /// Will return 
        /// Will return null if not in 2d mode
        /// </summary>
        /// <returns></returns>
        public Nullable<Vector3> GetRaycastHitPosition2D();

        /// <summary>
        /// Will return null if not in VR mode, or if right hand mode isn't FreeHand, or if ray isn't currently hitting anything
        /// </summary>
        /// <returns></returns>
        public Nullable<Vector3> GetRaycastHitPositionVRRight();

        /// <summary>
        /// Will return null if not in VR mode, or if left hand mode isn't FreeHand , or if ray isn't currently hitting anything
        /// </summary>
        /// <returns></returns>
        public Nullable<Vector3> GetRaycastHitPositionVRLeft();

        /// <summary>
        /// Will return 
        /// Will return null if not in 2d mode
        /// </summary>
        /// <returns></returns>
        public GameObject GetRaycastHitGameObject2D();

        /// <summary>
        /// Will return null if not in VR mode, or if right hand mode isn't FreeHand, or if ray isn't currently hitting anything
        /// </summary>
        /// <returns></returns>
        public GameObject GetRaycastHitGameObjectVRRight();

        /// <summary>
        /// Will return null if not in VR mode, or if left hand mode isn't FreeHand , or if ray isn't currently hitting anything
        /// </summary>
        /// <returns></returns>
        public GameObject GetRaycastHitGameObjectVRLeft();
    }
}
