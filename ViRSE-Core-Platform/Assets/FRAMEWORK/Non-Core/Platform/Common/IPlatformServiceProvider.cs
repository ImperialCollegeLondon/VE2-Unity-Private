using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPlatformServiceProvider
{
    IPlatformService PlatformService { get; } //Implementation will create a new platform service if it doesn't yet exist
}
