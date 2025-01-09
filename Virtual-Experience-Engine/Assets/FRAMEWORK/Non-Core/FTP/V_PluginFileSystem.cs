using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class V_PluginFileSystem : V_FileSystemIntegrationBase, IPluginFileSystem
{
    protected override string _LocalWorkingFilePath => $"VE2/PluginFiles/{SceneManager.GetActiveScene().name}";
}
