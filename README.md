# NowPlaying Game Cacher, an extension for Playnite

This extension may be useful if you have a local library of PC Windows games, such as Abandonware or public domain games not available on the usual platforms (steam, epic, xbox game pass, etc), and you have limited space on your fast storage device(s).  This extension allows to keep your games hosted on big-and-slow storage devices (HDDs, network drives/NAS, etc.), and can create & mangage cached copies of those games where there can be played from your fast storage device(s). 

Example use case:

1. Import PC Windows games into Playnite from your big-and-slow storage devices (Add Game -> Manually, or Add Game -> Scan Automatically)
2. Make sure each game's Install Directory points to the game's main folder. A good way to check this at a glance is to see if the Install Size seems reasonable. Sometimes Playnite sets the Install Directory to a subdirectory where the game executable lives.  When that happens, the Install Size reported will be way too small. Edit the game's Install directory and Play action path, if needed.

  Incorrect 😒🚫

  Installation Folder:  E:\Games\My Favorite Abandonware Game\Some subdirectory\bin
  Install Size: 50 KB
  Actions, play action, Path:  {InstallDir}\MyFaveAbondonwareGame.exe

  Correct 😁👍

  Installation Folder:  E:\Games\My Favorite Abandonware Game
  Install Size: 5 GB
  Actions, play action, Path:  {InstallDir}\Some subdirectory\bin\MyFaveAbondonwareGame.exe

3. Install NowPlaying Game Cacher Extension, if you haven't already.
4. Navagate to the NowPlaying Game Cacher view (click on sidebar icon).
5. Add at least one NowPLaying Cache Root (storage device, directory, max fill level), where game caches can be created.

   (Note, a maximum fill level can be set to prevent game caches from completely filling your storage device, if necessary.
   Definitely set this < 100% if using your C:\ drive for game caches, for example.)  

6. Enable game caching for the games you imported in step 1.  If you specified more than one Cache Root, you will be able to select which Root each game will be cached to, in this step.  

  Two ways to enable games for caching:
  6.1. From the NowPlaying Game Cacher view (click on sidebar icon), click on the "+" under the NowPlaying Game Caches list. In the popup,
  filter/select from the list of cache-eligible games.
  6.2. From Playnite's main (Library) view, right click on a game that is eligible for game caching, select 
  "NowPlaying: Enable caching for selected game", and select a cache root if multiple cache roots are defined. 

7. Installing a game cache.

  Several ways:
  7.1. Playnite's main view: use all of the normal mechanisms to Install a library game.



Requirements:
Games must meet the following requirements to be eligible for game caching:
. Locally installed (added via Add Game -> Manually / Scan Automatically)
    <sys:String x:Key="LOCNowPlayingRequirement2">Listed under the 'Playnite' library.</sys:String>
    <sys:String x:Key="LOCNowPlayingRequirement3">Have an empty Source (see Game Details -> General -> Source).</sys:String>
    <sys:String x:Key="LOCNowPlayingRequirement4">Playable from the installation directory (i.e. not in archived state).</sys:String>
    <sys:String x:Key="LOCNowPlayingRequirement5">Have no Roms.</sys:String>
    <sys:String x:Key="LOCNowPlayingRequirement6">Have a single Play Action (see Game Details -> Actions).</sys:String>
    <sys:String x:Key="LOCNowPlayingRequirement7">Platform is 'PC (Windows)'.</sys:String>
