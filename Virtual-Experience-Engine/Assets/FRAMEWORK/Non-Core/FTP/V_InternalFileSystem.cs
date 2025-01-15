using VE2_NonCore_FileSystem_Interfaces_Internal;

namespace VE2_NonCore_FileSystem
{
    public class V_InternalFileSystem : V_FileSystemIntegrationBase, IInternalFileSystem
    {
        public override string LocalWorkingPath => $"VE2/Worlds";
    }
}