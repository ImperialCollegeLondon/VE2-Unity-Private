using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ViRSE.Core.Shared.CoreCommonSerializables;

public interface IPlayerSettingsProvider
{
    public bool ArePlayerSettingsReady { get; }
    public event Action OnPlayerSettingsReady;
    public UserSettingsPersistable UserSettings { get; }
    public string GameObjectName { get; }   
    public bool IsEnabled { get; }
}

/*

Right, it's not just about local/remote avatar appearance, it's also about the settings that are saved in the database 
We aren't saving in the DB anything specific to the plugin 
The player spawner has plugin-specific settings, but these don't come from the platform
So these then NEED to be two different objects, one that comes from platform integration (stuff from the DB), and one that comes from the player spawner (overrides, transparency etc)

How does this work for the prospective customer 
They add a platform integration into their scene, they add their player settings 
But for the free version, they wouldn't actually HAVE a login, so basically, we need a "Guest settings" config? 
Well, no, guest settings are set already 
Really it's just the spoof settings 

Maybe we shouldn't have this whole player settings provider system
The motivation, is to allow developers to change their control settings in-editor 
But they have the UI for that 
The platform integration, when running in the editor, can just listen to the 

So if off-platform, what we see in the player spawner are "default settings", with an option to save/load from player prefs (and maybe a clear player prefs button)
In platform, we don't have a default, but we have a "save and load to player prefs in editor" button, then, we can just add something into the debug/editor service that listens to the settings changes, and saves/loads them to player prefs

This is all fine, but we still need to deal with the fact that we have two different appearance objects. One that comes from the platform, and one that's specific to the plugin....
Maybe there just ISN'T a way of making this make sense in multiple domains at once... so what's the primary one?
Well, we want to get people in with the free stuff, and be able to make sense of it 
But we also want the paid version to make sense too...

Free-version customers are going to confused as to why the override settings can't be saved to player prefs, or why the UI doesn't let you change the avatar override? 

Maybe just "AvatarAppearance" and "AvatarAppearanceOverrides"? 
The platform just knows "AvatarAppearance", and the instance knows both, encapsulated in a single object, "InstancedAvatarAppearance"


*/
