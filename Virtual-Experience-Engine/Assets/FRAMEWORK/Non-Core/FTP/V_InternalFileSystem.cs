using System.Collections.Generic;
using UnityEngine;

public class V_InternalFileSystem : V_FileSystemIntegrationBase, IInternalFileSystem
{
    protected override string _LocalWorkingFilePath => $"VE2/Worlds";
}
