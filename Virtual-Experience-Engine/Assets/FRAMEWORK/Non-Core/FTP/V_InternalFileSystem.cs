using VE2_NonCore_FileSystem_Interfaces_Internal;

namespace VE2_NonCore_FileSystem
{
    public class V_InternalFileSystem : V_FileSystemIntegrationBase, IInternalFileSystem
    {
        protected override string _LocalWorkingFilePath => $"VE2/Worlds";
    }
}