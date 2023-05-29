# NowPlaying Game Cacher, a library extension for [Playnite](https://github.com/JosefNemec/Playnite)

This extension may be useful if you have a local library of PC Windows games, such as Abandonware or public domain games not available on the usual platforms (steam, epic, xbox game pass, etc), and you have limited space on your fast storage device(s).  This extension allows you to host your games on big-and-slow storage devices (HDDs, network drives/NAS, etc.), and it can create & mangage game caches on your fast storage device(s) for the games you are actively playing. 

### Games must meet the following requirements to be eligible for game caching:
- Are locally installed (e.g. added via Add Game -> Manually or Scan Automatically)
- Are playable from the installation directory (i.e. not archived).
- Are listed under the 'Playnite' library.
- Have a single Play Action (see Game Details -> Actions).
- Platform is 'PC (Windows)'.
- Have no Roms.

### How it works

When you enable a game for caching, it gets subsumed under the NowPlaying Game Cacher library. (If game caching is later disabled, the game is released back to the Playnite library.)

To play a game from the NowPlaying Game Cacher library, you will first install it, which creates a game cache on your fast storage device. As an alternative, you can also 'Preview' the game -- play it directly from the game's installation directory without creating a game cache, aka, play it from the big-and-slow storage device. Right-click on a game to access the 'Preview' game action.

Once you are no longer playing a game anymore, you can uninstall it to delete its game cache and free up space for other game caches.      

#### Changes made to a Playnite game when game caching is enabled:
- Install Directory (folder) changed to point to the game's cache directory, under a NowPlaying cache root directory/device (i.e. on a fast storage device)  
- Plugin ID → NowPlaying Game Cacher's ID (will be listed under the NowPlaying Game Cacher library)
- Game Actions: original Play action is replaced by NowPlaying Play and Preview actions.

#### Changes made to a NowPlaying game when game caching is disabled:
- Install Directory (folder) changed to point back to the game's installation directory (i.e. on a big-and-slow storage device)  
- Plugin ID → 'empty' ID (will be listed under the Playnite library)
- Game Actions: the game's original Play action is restored.

### Example use case:

1. Import PC Windows games into Playnite from your big-and-slow storage devices (Add Game -> Manually, or Add Game -> Scan Automatically)

    ***Make sure each game's Installation Folder points to its main folder.*** 
    
    A good way to check this at a glance is to see if the Install Size seems reasonable. Sometimes Playnite sets the Installation Folder to a subdirectory where the game executable lives. When that happens, the Install Size reported will be way too small. Edit the game's Install Folder and Play action path, if needed.

    #### Incorrect 😒🚫

    - Installation Folder:  E:\Games\My Favorite Abandonware Game\Some subdirectory\bin
    - Install Size: 50 KB
    - Play action, Path:  {InstallDir}\MyFaveAbondonwareGame.exe

    #### Correct 😁👍

    - Installation Folder:  E:\Games\My Favorite Abandonware Game
    - Install Size: 5 GB
    - Play action, Path:  {InstallDir}\Some subdirectory\bin\MyFaveAbondonwareGame.exe


2. Install NowPlaying Game Cacher Extension, if you haven't already.
3. Navagate to the NowPlaying Game Cacher view (click on sidebar icon).
4. Add at least one NowPLaying Cache Root (storage device, directory, max fill level), where game caches can be created.

   **Note, a maximum fill level can be set to prevent game caches from completely filling your storage device, if necessary.
   Definitely set this < 100% if using your C:\ drive for game caches, for example.**


5. Enable caching for the games you imported in step 1.  If you specified more than one Cache Root, you will be able to select which Root each game will be cached to, in this step.  There are two ways to enable games for caching:

    - From the NowPlaying Game Cacher view (via sidebar), click on the "+" under the NowPlaying Game Caches list. In the popup,
    filter/select from the list of cache-eligible games.
    - From Playnite's main (Library) view, right click on a game that is eligible for game caching, select "NowPlaying: Enable caching for selected game", and select a cache root if multiple cache roots are defined. 


6. Installing/uninstalling a game cache; there are several ways:

    - Use Playnite's built-in install/uninstall mechanisms for your cache enabled games (those listed under the NowPlaying Game Cacher library).
    - From the NowPlaying Game Cacher view (via sidebar), use install/uninstall buttons or right-mouse options to manage your game caches.

### A few notes about cache installation: 
- At most one active game cache installation can occur at a time. Any additional game cache install requests are queued up if there is already an active install underway.
- An active cache installation can be paused or cancelled, and queued installations can be cancelled (via right mouse menu). Game caches in a paused state can be resumed (or uninstalled) at a later time. 
- If you launch a Playnite game, your active/queued cache intallations can continue in the background, as normal, but you also have the option of pausing all installations or shifting them to a speed limited mode while you are actively playing games. (See NowPlaying Settings.) Paused or speed limited cache installations will automatically be resumed (at full speed) once you are done playing.

