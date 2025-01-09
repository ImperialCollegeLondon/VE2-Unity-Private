using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VE2_NonCore_FileSystem_Interfaces_Common;
using VE2_NonCore_FileSystem_Interfaces_Plugin;

public class FileUIObjectExample : MonoBehaviour
{
    [SerializeField] private TMP_Text _fileNameText;
    [SerializeField] private TMP_Text _fileSizeText;

    [SerializeField] private TMP_Text _availableLocalText;
    [SerializeField] private Button _uploadLocalButton;
    [SerializeField] private Button _deleteLocalButton;

    [SerializeField] private TMP_Text _availableRemoteText;
    [SerializeField] private Button _uploadRemoteButton;
    [SerializeField] private Button _deleteRemoteButton;

    [SerializeField] private GameObject _taskPanel;
    [SerializeField] private TMP_Text _currentTaskTypeText;
    [SerializeField] private TMP_Text _currentTaskProgressText;
    [SerializeField] private TMP_Text _currentTaskStatusText;
    [SerializeField] private Button _cancelTaskButton;

    private IRemoteFileTaskInfo _currentRemoteTask;

    private readonly IPluginFileSystem _pluginFileSystem;
    private readonly FileDetails _fileDetails;

    public FileUIObjectExample(IPluginFileSystem pluginFileSystem, FileDetails fileDetails, bool isAvailableLocally, bool isAvailableRemotely)
    {
        _pluginFileSystem = pluginFileSystem;

        _fileDetails = fileDetails;
        _fileNameText.text = fileDetails.NameAndPath;
        _fileSizeText.text = fileDetails.Size.ToString();

        _availableLocalText.text = isAvailableLocally ? "Available" : "Not Available";
        _uploadLocalButton.interactable = !isAvailableLocally && isAvailableRemotely;
        _deleteLocalButton.interactable = isAvailableLocally;

        _availableRemoteText.text = isAvailableRemotely ? "Available" : "Not Available";
        _uploadRemoteButton.interactable = !isAvailableRemotely && isAvailableLocally;
        _deleteRemoteButton.interactable = isAvailableRemotely;

        _taskPanel.SetActive(false); //No current tasks!
    }

    private void Update()
    {
        if (_currentRemoteTask == null)
            return;

        _currentTaskTypeText.text = _currentRemoteTask.Type.ToString();
        _currentTaskStatusText.text = _currentRemoteTask.Status.ToString();

        _currentTaskProgressText.gameObject.SetActive(_currentRemoteTask.Status == RemoteFileTaskStatus.InProgress);
        _currentTaskProgressText.text = $"{_currentRemoteTask.Progress}%";

        _cancelTaskButton.gameObject.SetActive(_currentRemoteTask.IsCancellable);
    }

    public void UploadLocalFile()
    {
        _currentRemoteTask = _pluginFileSystem.UploadFile(_fileDetails.NameAndPath);
        _currentRemoteTask.OnTaskCompleted += OnUploadLocalFileComplete;
    }

    public void OnUploadLocalFileComplete(IRemoteFileTaskInfo task)
    {
        _currentRemoteTask.OnTaskCompleted -= OnUploadLocalFileComplete;

        if (task.Status == RemoteFileTaskStatus.Succeeded)
        {
            _availableRemoteText.text = "Available";
            _uploadLocalButton.interactable = false;
            _deleteLocalButton.interactable = true;
        }
    }

    public void DeleteLocalFile()
    {
        //Unlike other tasks, this one is synchronous and happens immediately, doesn't return a task info object
        bool success = _pluginFileSystem.DeleteLocalFile(_fileDetails.NameAndPath);
        _currentRemoteTask = null;

        _currentTaskTypeText.text = "Delete Local File";
        _currentTaskStatusText.text = success ? "Success" : "Failed";
        _currentTaskProgressText.text = success ? "100%" : "0%";
    }

    public void DownloadRemoteFile()
    {
        _currentRemoteTask = _pluginFileSystem.UploadFile(_fileDetails.NameAndPath);
        _currentRemoteTask.OnTaskCompleted += HandleDownloadRemoteFileComplete;
    }

    public void HandleDownloadRemoteFileComplete(IRemoteFileTaskInfo task)
    {
        _currentRemoteTask.OnTaskCompleted -= HandleDownloadRemoteFileComplete;

        if (task.Status == RemoteFileTaskStatus.Succeeded)
        {
            _availableLocalText.text = "Available";
            _uploadRemoteButton.interactable = false;
            _deleteRemoteButton.interactable = true;
        }
    }

    public void DeleteRemoteFile()
    {
        _currentRemoteTask = _pluginFileSystem.DeleteRemoteFile(_fileDetails.NameAndPath);
        _currentRemoteTask.OnTaskCompleted += HandleDeleteRemoteFileComplete;
    }

    public void HandleDeleteRemoteFileComplete(IRemoteFileTaskInfo task)
    {
        _currentRemoteTask.OnTaskCompleted -= HandleDeleteRemoteFileComplete;

        if (_currentRemoteTask.Status == RemoteFileTaskStatus.Succeeded)
        {
            _availableRemoteText.text = "Not Available";
            _uploadRemoteButton.interactable = true;
            _deleteRemoteButton.interactable = false;
        }
    }

    public void CancelTask()
    {
        _currentRemoteTask.CancelRemoteFileTask();
    }

}
