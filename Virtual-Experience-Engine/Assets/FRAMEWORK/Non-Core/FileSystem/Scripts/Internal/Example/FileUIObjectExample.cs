using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VE2.NonCore.FileSystem.API;

using FileDetails = VE2.NonCore.FileSystem.API.FileDetails;

namespace VE2.NonCore.FileSystem.Internal
{
    internal class FileUIObjectExample : MonoBehaviour
    {
        [SerializeField] private TMP_Text _fileNameText;
        [SerializeField] private TMP_Text _fileSizeText;

        [SerializeField] private TMP_Text _availableLocalText;
        [SerializeField] private Button _uploadLocalButton;
        [SerializeField] private Button _deleteLocalButton;

        [SerializeField] private TMP_Text _availableRemoteText;
        [SerializeField] private Button _downloadRemoteButton;
        [SerializeField] private Button _deleteRemoteButton;

        [SerializeField] private GameObject _taskPanel;
        [SerializeField] private TMP_Text _currentTaskTypeText;
        [SerializeField] private TMP_Text _currentTaskProgressText;
        [SerializeField] private TMP_Text _currentTaskStatusText;
        [SerializeField] private Button _cancelTaskButton;

        private IRemoteFileTaskInfo _currentRemoteTask;
        private bool _isAvailableLocally;
        private bool _isAvailableRemotely;

        private IFileSystem _pluginFileSystem;
        private API.FileDetails _fileDetails;

        public void Setup(IFileSystem pluginFileSystem, API.FileDetails fileDetails, bool isAvailableLocally, bool isAvailableRemotely)
        {
            _pluginFileSystem = pluginFileSystem;

            _fileDetails = fileDetails;
            _fileNameText.text = fileDetails.NameAndPath;
            _fileSizeText.text = FormatBytes(fileDetails.Size);

            _isAvailableLocally = isAvailableLocally;
            _isAvailableRemotely = isAvailableRemotely;

            _availableLocalText.text = isAvailableLocally ? "Available" : "Not Available";
            _availableLocalText.color = isAvailableLocally ? Color.green : Color.red;
            _uploadLocalButton.interactable = isAvailableLocally && !isAvailableRemotely;
            _deleteLocalButton.interactable = isAvailableLocally;

            _availableRemoteText.text = isAvailableRemotely ? "Available" : "Not Available";
            _availableRemoteText.color = isAvailableRemotely ? Color.green : Color.red;
            _downloadRemoteButton.interactable = isAvailableRemotely && !isAvailableLocally;
            _deleteRemoteButton.interactable = isAvailableRemotely;

            _taskPanel.SetActive(false); //No current tasks!
            _cancelTaskButton.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_currentRemoteTask == null)
                return;

            _currentTaskTypeText.text = _currentRemoteTask.Type.ToString();
            _currentTaskStatusText.text = _currentRemoteTask.Status.ToString();

            _currentTaskProgressText.gameObject.SetActive(_currentRemoteTask.Status == RemoteFileTaskStatus.InProgress);
            _currentTaskProgressText.text = $"{Mathf.FloorToInt(_currentRemoteTask.Progress * 100f)}%";

            _cancelTaskButton.gameObject.SetActive(_currentRemoteTask.IsCancellable);
        }

        public void UploadLocalFile()
        {
            _currentRemoteTask = _pluginFileSystem.UploadFile(_fileDetails.NameAndPath);
            _currentRemoteTask.OnTaskCompleted += OnUploadLocalFileComplete;
            _taskPanel.SetActive(true);
        }

        public void OnUploadLocalFileComplete(IRemoteFileTaskInfo task)
        {
            _currentRemoteTask.OnTaskCompleted -= OnUploadLocalFileComplete;

            if (task.Status == RemoteFileTaskStatus.Succeeded)
            {
                _availableRemoteText.text = "Available";
                _availableRemoteText.color = Color.green;
                _uploadLocalButton.interactable = false;
                _deleteRemoteButton.interactable = true;
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

            _taskPanel.SetActive(true);

            if (success)
            {
                _availableLocalText.text = "Not Available";
                _availableLocalText.color = Color.red;

                _isAvailableLocally = false;
                _deleteLocalButton.interactable = false;
                _downloadRemoteButton.interactable = _isAvailableRemotely;
            }
        }

        public void DownloadRemoteFile()
        {
            _currentRemoteTask = _pluginFileSystem.DownloadFile(_fileDetails.NameAndPath);
            _currentRemoteTask.OnTaskCompleted += HandleDownloadRemoteFileComplete;
            _taskPanel.SetActive(true);
        }

        public void HandleDownloadRemoteFileComplete(IRemoteFileTaskInfo task)
        {
            _currentRemoteTask.OnTaskCompleted -= HandleDownloadRemoteFileComplete;

            if (task.Status == RemoteFileTaskStatus.Succeeded)
            {
                _isAvailableLocally = true;
                _availableLocalText.text = "Available";
                _availableLocalText.color = Color.green;

                _uploadLocalButton.interactable = false;
                _downloadRemoteButton.interactable = false;
                _deleteLocalButton.interactable = true;
            }
        }

        public void DeleteRemoteFile()
        {
            _currentRemoteTask = _pluginFileSystem.DeleteRemoteFile(_fileDetails.NameAndPath);
            _currentRemoteTask.OnTaskCompleted += HandleDeleteRemoteFileComplete;
            _taskPanel.SetActive(true);
        }

        public void HandleDeleteRemoteFileComplete(IRemoteFileTaskInfo task)
        {
            _currentRemoteTask.OnTaskCompleted -= HandleDeleteRemoteFileComplete;

            if (_currentRemoteTask.Status == RemoteFileTaskStatus.Succeeded)
            {
                _isAvailableRemotely = false;
                _availableRemoteText.text = "Not Available";
                _availableRemoteText.color = Color.red;

                _downloadRemoteButton.interactable = false;
                _deleteRemoteButton.interactable = false;

                _uploadLocalButton.interactable = _isAvailableLocally;
            }
        }

        public void CancelTask()
        {
            _currentRemoteTask.CancelRemoteFileTask();
        }

        private string FormatBytes(ulong bytes)
        {
            // Array of suffixes for the size units
            string[] suffixes = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            int suffixIndex = 0;
            double size = bytes;

            // Divide by 1024 until the size is less than 1024 or we run out of suffixes
            while (size >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                size /= 1024;
                suffixIndex++;
            }

            // Return the size with one decimal place and the corresponding suffix
            return $"{size:F1} {suffixes[suffixIndex]}";
        }

    }
}
