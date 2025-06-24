using System;
using System.Collections.Generic;

namespace VE2.NonCore.FileSystem.API
{
    internal interface IFileSystemInternal : IV_FileSystem
    {
        /// <summary>
        /// Allows the status of tasks to be updated outside play mode, useful for the plugin uploader
        /// </summary>
        public void Update();
    }
}
