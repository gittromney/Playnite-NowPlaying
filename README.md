## NowPlaying Game Cacher, a library extension for [Playnite](https://github.com/JosefNemec/Playnite)

[![Downloads](https://img.shields.io/github/downloads/gittromney/Playnite-NowPlaying/total)](https://github.com/gittromney/Playnite-NowPlaying/archive/refs/heads/master.zip)
[![LatestVersion](https://img.shields.io/github/v/release/gittromney/Playnite-NowPlaying?include_prereleases)](https://github.com/gittromney/Playnite-NowPlaying/releases)
[![LastCommit](https://img.shields.io/github/last-commit/gittromney/Playnite-NowPlaying)](https://github.com/gittromney/Playnite-NowPlaying/commits/master)
[![License](https://img.shields.io/github/license/gittromney/Playnite-NowPlaying)](https://raw.githubusercontent.com/gittromney/Playnite-NowPlaying/master/LICENSE)
[![Crowdin](https://badges.crowdin.net/nowplaying-playnite-extension/localized.svg)](https://crowdin.com/project/nowplaying-playnite-extension)

This extension may be useful if you have a local library of PC Windows and/or [*emulated*](#emulated-platforms) games you legally own, such as Abandonware or public domain games not available on the usual platforms (steam, epic, xbox game pass, etc), and you have limited space on your fast storage device(s).  This extension allows you to host your games on big-and-slow storage devices (HDDs, network drives/NAS, etc.), and it can create & mangage game caches on your fast storage device(s) for the games you are actively playing. 

### <a id=emulated-platforms></a> Supported Emulated Platforms
Game caching is now possible for the following emulated platforms (See [here](#emulator-setup) for details):
- *Sony Playstation 2* (PCSX2)
- *Sony Playstation 3* (RPCS3)
- *Microsoft Xbox* (Xemu)
- *Microsoft Xbox 360* (Xenia)
- *Nintendo GameCube* (Dolphin)
- *Nintendo Wii* (Dolphin)
- *Nintendo Switch* (Ryujinx, Yuzu)

### Games must meet the following requirements to be eligible for game caching:
- Are locally installed (added via Add Game → Manually / Scan Automatically or emulator library scan).
- Are playable from the installation directory (i.e. not archived).
- Are listed under the 'Playnite' library.
- Have a single Play Action (see Game Details → Actions).
- Are on 'PC (Windows)' with no ROMs or a [*supported emulated platform*](#emulated-platforms) with 1 ROM.

***Note, the Play action's 'Path' must point to an executable/shortcut/symlink that is somewhere under the 'Work' directory.*** 


## How it works

When you enable a game for caching, it gets subsumed under the NowPlaying Game Cacher library. (If game caching is later disabled, the game is released back to the Playnite library.)

To play a game from the NowPlaying library, you first ***Install*** it to create a game cache on your fast storage device, and then you ***Play*** it as you normally would.  As an alternative, you can also ***Preview*** the game (via right-mouse click) without creating a cache. Preview plays a game directly from its installation directory, aka, from a big-and-slow storage device.  Once you finish a game, or if don't plan on playing it for awhile, you'll likely want to ***Uninstall*** it to delete its cache and free up space for other game caches.

While a game's cache is being created (which can take awhile depending on the size of the game, speed of the storage devices, etc), you are still able to browse and play games in Playnite; and you can also schedule cache installation for additional games (in a queue).  Depending on NowPlaying's settings, cache installation can continue in the background while you are playing games.

Playnite's built in interface can be used for basic game cache management (install/uninstall, as well as enable/disable game caching via right-mouse).  A progress indicator (at the top of Playnite's library view) will keep you informed of NowPlaying cache activity, and notifications will be issued for milestones such as when a game cache is installed and ready to play.  More advanced management as well as real-time, detailed installation progress are available from the NowPlaying view (via the sidebar). NowPlaying's settings are also accessible there, too.

#### Changes made to a Playnite Game when caching is enabled:
- Install Directory → points to where the game's cache will be created, under a NowPlaying cache root directory/device (i.e. on a fast storage device)  
- Plugin ID → NowPlaying Game Cacher's ID (game will be listed under the 'NowPlaying Game Cacher' library)
- Game Actions: original Play action is replaced by NowPlaying Play (play from cache) and Preview (play from original install directory) actions.

#### Changes made to a Playnite Game when caching is disabled:
- Install Directory → points back to the game's installation directory (i.e. on a big-and-slow storage device)  
- Plugin ID → 'empty' ID (game will be listed under the 'Playnite' library)
- Game Actions: the game's original Play action is restored.

### Initial setup: choose a cache root
After installing this extension, you'll want to specify at least one cache root directory/storage device and then you'll be able to enable games for caching.  This is done from the NowPlaying view (via sidebar). 

- Choose a new, empty ***Directory*** on a fast storage device to create game caches in, such as "C:\Games\NowPlaying".  Note, you can specify multiple cache roots but only one is allowed per storage device.

- Set the ***Maximum fill level*** allowed for the storage device when installing game caches, in a range of 50-100%.  This will limit the space used for game caches on the device and reserve a portion of it for other use.  It's recommended to set the maximum fill level to, at most, 75-85% (or so) for a cache root on your system disk (C:).  If the device is used exclusively for gaming, it can be set as high as 100%.


### A few notes about cache installation
- At most one active game cache installation can occur at a time.  Any additional game cache install requests are queued up if there is already an active install underway.
- From the NowPlaying view (via sidepanel), an active cache installation can be paused or cancelled, and queued installations can be cancelled (via right-mouse menu).  Game caches in a paused state can be resumed (or uninstalled) at a later time.
- Cache installation is automatically paused if the maximum fill level of the cache root device is exceeded (or if the device becomes 100% full).  You will need to free up some space on the device (or increase the maximum fill level setting of the cache root via the edit button) before the cache installation can be resumed.
- If you launch a Playnite game, your active/queued cache intallations can continue in the background, as normal, but you also have the option of pausing all installations or shifting them to a speed limited mode while you are actively playing games.  (See NowPlaying Settings.)  Paused or speed limited cache installations will automatically be resumed (at full speed) once you are done playing.

### Uninstalling 'dirty' game caches
Some games save settings and game save data locally. 
While playing one of these games, the cache can become 'dirty', or different than the installed files.
When a dirty game cache is uninstalled, you have the option of syncing updated game settings/save data back
to the installation directory. (See NowPlaying settings.)

## <a id="emulator-setup"></a> Recommended Emulator Setup
If using game caching for [*emulated platform*](#emulated-platforms) games, here are some recommendations:
- Install the emulator(s) on a fast device such as your C: drive.
- The emulator's operating storage (e.g. virtual file system) should also be located on a fast device 
  (this should be the default configuration).
- If applicable, max out the emulator's cache size settings. For RPCS3, the max setting is 10GB. (Edit config.yml, then under "VFS:", set "Disk cache maximum size (MB)" to 10240)
- Each game should have its own directory where game files/ROM are located.
#### Updates/DLCs
- To ensure there is only 1 ROM per game, you may may need to place game updates/DLCs under their own subdirectories and exclude those from Playnite's emulator library scans.
  As an example, a Switch game directory might look like this:
    ##### 
      E:\Games\Switch\MyFavoriteSwitchGame\
        DLCs\
          MyFavoriteSwitchGame [DLC pack 1].nsp
          MyFavoriteSwitchGame [DLC pack 2].nsp
        Updates\
          MyFavoriteSwitchGame [UPD 1.1].nsp
        MyFavoriteSwitchGame.nsp
  
- In Playnite, add 'DLCs' and 'Updates' to your Switch emulator's excluded folders list, found under Library → Configure Emulators... → Auto-scan configurations → Exclusions.
- If applicable, launch your emulator(s) separately to install DLCs and update ROM files on a per-game basis.
  To get the full benefit of game caching, you may want install the game's NowPlaying cache first and install the
  DLC/update files from the cache directory.

#### Multi-disk/multi-DVD games
If M3U files are a supported by the emulator, you can create one and import it as the game's ROM file:
- Confirm that 'm3u' is a supported file type (see Library → Configure Emulators... → Emulators → Profiles → General → Supported File Types)
- Place the game's per-disk/DVD ROM files (e.g ISOs) under a new subdirectory named 'Disks', 'DVDs', 'ISOs' or similar.
- In Playnite, add this subdirectory (e.g. 'DVDs') to the emulator's excluded folders list (Library → Configure Emulators... → Auto-scan configurations → Exclusions).
- Create a .m3u asii text file in the game directory (e.g. using Notepad) that lists the paths to the individual ROM files.
An example multi-disk GameCube game directory might look like this:
    #####
      E:\Games\GameCube\MyFavoriteGameCubeGame\
        DVDs\
          MyFavoriteGameCubeGame (DVD1).iso
          MyFavoriteGameCubeGame (DVD2).iso
        MyFavoriteGameCubeGame.m3u

    In this example, MyFavoriteGameCubeGame.m3u would contain
    #####
      DVDs\MyFavoriteGameCubeGame (DVD1).iso
      DVDs\MyFavoriteGameCubeGame (DVD2).iso

## Example use case:

1. Import PC Windows and/or supported emulator platform games into Playnite from your big-and-slow storage devices (Add Game → Manually, Add Game → Scan Automatically, or via emulator library scan)

    ***Make sure each game's Installation Folder points to its main folder. Also, avoid using a shortcut (with a hardcoded path to the big-and-slow device) as the game's executable*** 
    
    A good way to check this at a glance is to see if the Install Size seems reasonable. Sometimes Playnite sets the Installation Folder to a subdirectory where the game executable/ROM lives.  When that happens, the Install Size reported will be way too small.  Edit the game's Install Folder and play action/ROM Path, if needed.

    #### Incorrect 😒🚫

    - Installation Folder:  E:\Games\My Favorite Abandonware Game\Some subdirectory\bin
    - Install Size: 50 KB
    - Play action, Path:  {InstallDir}\MyFaveAbondonwareGame.exe

    #### Correct 😁👍

    - Installation Folder:  E:\Games\My Favorite Abandonware Game
    - Install Size: 5 GB
    - Play action, Path:  {InstallDir}\Some subdirectory\bin\MyFaveAbondonwareGame.exe


2. Install NowPlaying Game Cacher Extension, if you haven't already.
3. Navagate to the NowPlaying view (via sidebar).
4. Add at least one NowPlaying Cache Root (storage device, directory, max fill level), where game caches can be created.

   **Note, a maximum fill level can be set to prevent game caches from completely filling your storage device, if necessary.
   Definitely set this < 100% if using your C:\ drive for game caches, for example.**


5. Enable caching for the games you imported in step 1.  If you specified more than one Cache Root, you will be able to select which Root each game will be cached to, in this step.  There are two ways to enable games for caching:

    - From the NowPlaying view (via sidebar), click on the "+" under the NowPlaying Game Caches list. In the popup,
    filter/select from the list of cache-eligible games.
    - From Playnite's main (Library) view, right click on a game that is eligible for game caching, select "NowPlaying: Enable caching for selected game", and select a cache root if multiple cache roots are defined. 


6. Installing/uninstalling a game cache; there are several ways:

    - Use Playnite's built-in install/uninstall mechanisms for your cache enabled games (those listed under the NowPlaying Game Cacher library).
    - From the NowPlaying view (via sidebar), use install/uninstall buttons or right-mouse options to manage your game caches.


