# NowPlaying Game Cacher, an extension for [Playnite](https://github.com/JosefNemec/Playnite)

This extension may be useful if you have a local library of PC Windows games, such as Abandonware or public domain games not available on the usual platforms (steam, epic, xbox game pass, etc), and you have limited space on your fast storage device(s).  This extension allows you to host your games on big-and-slow storage devices (HDDs, network drives/NAS, etc.), and it can create & mangage game caches on your fast storage device(s) for the games you are actively playing. 

### Games must meet the following requirements to be eligible for game caching:
- Must be locally installed (e.g. added via Add Game -> Manually or Scan Automatically)
- Are playable from the installation directory on the big-and-slow host device (i.e. not in an archived state).
- Are listed under the 'Playnite' library.
- Have an empty Source (see Game Details -> General -> Source).
- Have no Roms.
- Have a single Play Action (see Game Details -> Actions).
- Platform is 'PC (Windows)'.

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

