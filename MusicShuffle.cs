using HutongGames.PlayMaker.Actions;
using Modding;
using Osmi.Utils;
using Satchel.BetterMenus;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

namespace MusicShuffle
{
    public enum Mode
    {
        Off,
        Fixed,
        Chaos
    }

    public class MusicShuffle : Mod, ICustomMenuMod, ILocalSettings<FixedMusicMap>, IGlobalSettings<Settings>
    {
        public MusicShuffle() : base("MusicShuffle") { }
        public override string GetVersion() => typeof(MusicShuffle).Assembly.GetName().Version.ToString();

        private MusicCue[] myMusicCues;
        private string lastRequestedMusicCueName = "Title";
        private MusicCue currentMusicCue = null;
        private bool hasGramaphone = false;
        private bool isDreaming = false;
        private AudioSource shadeMusicSource = null;
        
        private Dictionary<string, AudioMixerSnapshot> mySnapshots = [];

        private Mode mode = Mode.Chaos;
        private bool separateBosses = false;
        private bool includeShade = true;
        private bool includeColo = true;
        private Dictionary<string, string> musicMap;

        private Menu menuRef;
        public bool ToggleButtonInsideMenu => true;

        public override List<(string, string)> GetPreloadNames()
        {
            return Lists.PreloadNames;
        }

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            Log("init hooks");
            InitHooks();

            Log("fetch cues");
            FetchMusicCues();

            Log("fetch snapshots");
            FetchAudioMixerSnapshots();

            currentMusicCue = Array.Find(myMusicCues, (cue) => cue.name == "Title");
        }

        private void InitHooks()
        {
            ModHooks.NewGameHook += ModHooks_NewGameHook;
            On.AudioManager.ApplyMusicCue += AudioManager_ApplyMusicCue;
            On.HutongGames.PlayMaker.Actions.AudioPlaySimple.OnEnter += AudioPlaySimple_OnEnter;
            On.HutongGames.PlayMaker.Actions.AudioPlay.OnEnter += AudioPlay_OnEnter;
            On.HutongGames.PlayMaker.Actions.TransitionToAudioSnapshot.OnEnter += TransitionToAudioSnapshot_OnEnter;
            On.AudioManager.ApplyMusicSnapshot += AudioManager_ApplyMusicSnapshot;
            ModHooks.OnEnableEnemyHook += ModHooks_OnEnableEnemyHook;
        }


        #region FetchFromPreloads
        private void FetchMusicCues()
        {
            myMusicCues = Resources.FindObjectsOfTypeAll<MusicCue>();
            myMusicCues = Array.FindAll(myMusicCues, cue => cue.name != "None" && cue.name != "RestingGroundsWithDreamNail");

            for (int i = 0; i < myMusicCues.Length; i++)
            {
                if (myMusicCues[i].name == "Dirtmouth")
                {
                    //make a new dirtmouth cue that does not have the accordion alternative; we're keeping them separate
                    MusicCue.MusicChannelInfo mainChannel = myMusicCues[i].GetChannelInfo(MusicChannels.Main);
                    AudioClip dirtmouthClip = mainChannel.Clip;
                    MusicCue newDirtmouth = Util.CreateMusicCueFromClip(dirtmouthClip);
                    newDirtmouth.name = "Dirtmouth";
                    myMusicCues[i] = newDirtmouth;
                    break;
                }
            }

            AudioClip[] audioClips = Resources.FindObjectsOfTypeAll<AudioClip>();

            int coloCueCount = 0;
            foreach (AudioClip clip in audioClips)
            {
                if (clip.name.StartsWith("S57 COLOSSEUM INTENSITY"))
                {
                    MusicCue coloCue = Util.CreateMusicCueFromClip(clip);
                    myMusicCues = [.. myMusicCues.Append(coloCue)];
                    coloCueCount++;
                    if (coloCueCount == 6)
                    {
                        break;
                    }
                }
            }

            AudioSource[] audioSources = Resources.FindObjectsOfTypeAll<AudioSource>();
            int mySrcCount = 0;
            for (int i = audioSources.Length - 1; i >= 0; i--)
            {
                if (audioSources[i].name == "Play Music Strings and Choir(Clone)")
                {
                    MusicCue dreamerCue = Util.CreateMusicCueFromClip(audioSources[i].clip);
                    myMusicCues = [.. myMusicCues.Append(dreamerCue)];
                    mySrcCount++;
                }
                if (audioSources[i].name == "Shade")
                {
                    shadeMusicSource = audioSources[i];

                    MusicCue shadeCue = Util.CreateMusicCueFromClip(audioSources[i].clip);
                    myMusicCues = [.. myMusicCues.Append(shadeCue)];
                    mySrcCount++;
                }
                if (mySrcCount == 2)
                {
                    break;
                }
            }
        }

        private void FetchAudioMixerSnapshots()
        {
            AudioMixerSnapshot[] snapshots = Resources.FindObjectsOfTypeAll<AudioMixerSnapshot>();

            foreach (AudioMixerSnapshot snapshot in snapshots)
            {
                if (Lists.Snapshots.Contains(snapshot.name))
                {
                    if (!mySnapshots.ContainsKey(snapshot.name))
                    {
                        mySnapshots.Add(snapshot.name, snapshot);
                        if (mySnapshots.Count == Lists.Snapshots.Length)
                        {
                            break;
                        }
                    }
                }
            }
        }

        #endregion

        private void BuildMusicMap()
        {
            MusicCue[] includedCues = GetIncludedMusicCues();

            musicMap = new Dictionary<string, string>(includedCues.Length);
            if (!separateBosses)
            {
                MusicCue[] shuffledCues = [.. includedCues.Shuffle()];
                
                for (int i = 0; i < includedCues.Length; i++)
                {
                    musicMap.Add(includedCues[i].name, shuffledCues[i].name);
                }
            }
            else
            {
                MusicCue[] bossCues = [.. includedCues.Where(cue => Lists.BossThemes.Contains(cue.name))];
                MusicCue[] nonbossCues = [.. includedCues.Except(bossCues)];
                MusicCue[] shuffledBossCues = [.. bossCues.Shuffle()];
                MusicCue[] shuffledNonbossCues = [.. nonbossCues.Shuffle()];

                for (int i = 0; i < bossCues.Length; i++)
                {
                    musicMap.Add(bossCues[i].name, shuffledBossCues[i].name);
                }
                for (int i = 0; i < nonbossCues.Length; i++)
                {
                    musicMap.Add(nonbossCues[i].name, shuffledNonbossCues[i].name);
                }
            }
        }

        #region Hooks

        private void ModHooks_NewGameHook()
        {
            BuildMusicMap();
        }

        //normal music cue
        private void AudioManager_ApplyMusicCue(On.AudioManager.orig_ApplyMusicCue orig, AudioManager self, MusicCue musicCue, float delayTime, float transitionTime, bool applySnapshot)
        {
            hasGramaphone = false;

            if (mode == Mode.Off)
            {
                orig(self, musicCue, delayTime, transitionTime, applySnapshot);
                return;
            }

            if (musicCue.name == lastRequestedMusicCueName)
            {
                return;
            }

            isDreaming = musicCue.name == "DreamerScene";

            lastRequestedMusicCueName = musicCue.name;
            MusicCue cueToPlay = null;
            bool? isBossTheme = null;

            if (musicCue.name == "None" || (musicCue.name == "RestingGrounds" && !PlayerData.instance.hasDreamNail))
            {
                //leave silence as silence
                cueToPlay = musicCue;
            }
            else if (mode == Mode.Chaos)
            {
                if (separateBosses)
                {
                    isBossTheme = Lists.BossThemes.Contains(musicCue.name);
                }

                cueToPlay = GetRandomDifferentMusicCue(self.CurrentMusicCue, isBossTheme);
            }
            else if (mode == Mode.Fixed)
            {
                string cueName = musicCue.name;
                if (cueName == "Dirtmouth" && PlayerData.instance.nymmInTown)
                {
                    cueName = "DirtmouthAccordion";
                }
                if (musicMap.TryGetValue(cueName, out string newCueName))
                {
                    cueToPlay = Array.Find(myMusicCues, cue => cue.name == newCueName);
                }
            }

            if (cueToPlay == null)
            {
                cueToPlay = musicCue;
            }

            orig(self, cueToPlay, delayTime, transitionTime, applySnapshot);

            currentMusicCue = cueToPlay;
        }

        //used in colosseum and dreamer scenes
        private void AudioPlaySimple_OnEnter(On.HutongGames.PlayMaker.Actions.AudioPlaySimple.orig_OnEnter orig, HutongGames.PlayMaker.Actions.AudioPlaySimple self)
        {
            hasGramaphone = false;
            if (mode == Mode.Off || !includeColo)
            {
                orig(self);
                return;
            }
            string clipName = null;
            AudioSource src = null;

            if (self.oneShotClip != null && self.oneShotClip.Value != null)
            {
                clipName = self.oneShotClip.Value.name;
            }
            else
            {
                GameObject owner = self.Fsm.GetOwnerDefaultTarget(self.gameObject);
                if (owner != null)
                {
                    src = owner.GetComponent<AudioSource>();
                    if (src != null && src.clip != null)
                    {
                        clipName = src.clip.name;
                    }
                }
            }

            if (clipName != null && (clipName.StartsWith("S57 COLOSSEUM INTENSITY") || clipName == "S41-33 Strings and Choir"))
            {
                AudioClip newClip = null;
                MusicCue cueToPlay = null;

                if (mode == Mode.Chaos)
                {
                    cueToPlay = GetRandomDifferentMusicCue(currentMusicCue, separateBosses ? false : null);
                }
                else //Fixed mode
                {
                    if (musicMap.TryGetValue(clipName, out string newCueName))
                    {
                        cueToPlay = Array.Find(myMusicCues, cue => cue.name == newCueName);
                    }
                }

                if (cueToPlay != null)
                {
                    MusicCue.MusicChannelInfo actionChannel = cueToPlay.GetChannelInfo(MusicChannels.Action);

                    if (actionChannel != null && clipName != "S41-33 Strings and Choir")
                    {
                        newClip = actionChannel.Clip;
                    }
                    if (newClip == null)
                    {
                        MusicCue.MusicChannelInfo mainChannel = cueToPlay.GetChannelInfo(MusicChannels.Main);
                        if (mainChannel != null)
                        {
                            newClip = mainChannel.Clip;
                        }
                    }

                    if (newClip != null)
                    {
                        if (self.oneShotClip != null && self.oneShotClip.Value != null)
                        {
                            self.oneShotClip.Value = newClip;
                        }
                        else if (src != null)
                        {
                            src.clip = newClip;
                        }
                    }

                    currentMusicCue = cueToPlay;
                }
            }

            orig(self);
        }

        //used by gramaphones
        private void AudioPlay_OnEnter(On.HutongGames.PlayMaker.Actions.AudioPlay.orig_OnEnter orig, AudioPlay self)
        {
            if (mode == Mode.Off)
            {
                orig(self);
                return;
            }

            string clipName = null;
            AudioSource src = null;

            if (self.oneShotClip != null && self.oneShotClip.Value != null)
            {
                clipName = self.oneShotClip.Value.name;
            }
            else
            {
                GameObject owner = self.Fsm.GetOwnerDefaultTarget(self.gameObject);
                if (owner != null)
                {
                    src = owner.GetComponent<AudioSource>();
                    if (src != null && src.clip != null)
                    {
                        clipName = src.clip.name;
                    }
                }
            }

            if (clipName != null && clipName == "Safety")
            {
                AudioClip newClip = null;
                MusicCue cueToPlay = null;

                if (hasGramaphone)
                {
                    //gramaphones in trams play two of the same clip at once
                    cueToPlay = currentMusicCue;
                }
                else if (mode == Mode.Chaos)
                {
                    cueToPlay = GetRandomDifferentMusicCue(currentMusicCue, separateBosses ? false : null);
                }
                else //Fixed mode
                {
                    if (musicMap.TryGetValue("Safety", out string newCueName))
                    {
                        cueToPlay = Array.Find(myMusicCues, cue => cue.name == newCueName);
                    }
                }

                if (cueToPlay != null)
                {
                    hasGramaphone = true;

                    MusicCue.MusicChannelInfo mainChannel = cueToPlay.GetChannelInfo(MusicChannels.Main);
                    if (mainChannel != null)
                    {
                        newClip = mainChannel.Clip;
                        if (self.oneShotClip != null && self.oneShotClip.Value != null)
                        {
                            self.oneShotClip.Value = newClip;
                        }
                        else if (src != null)
                        {
                            src.clip = newClip;
                        }
                    }

                    currentMusicCue = cueToPlay;
                }
            }

            orig(self);
        }

        //when shade is encountered
        private bool ModHooks_OnEnableEnemyHook(GameObject enemy, bool isAlreadyDead)
        {
            hasGramaphone = false;
            if (mode != Mode.Off && !isAlreadyDead && includeShade && enemy.name == "Hollow Shade(Clone)" && shadeMusicSource != null)
            {
                MusicCue cueToPlay = null;
                if (mode == Mode.Chaos)
                {
                    cueToPlay = GetRandomDifferentMusicCue(currentMusicCue, separateBosses ? false : null);
                }
                else //Fixed mode
                {
                    if (musicMap.TryGetValue("Hollow Shade Music", out string newCueName))
                    {
                        cueToPlay = Array.Find(myMusicCues, cue => cue.name == newCueName);
                    }
                }

                if (cueToPlay != null)
                {
                    ChangeShadeMusic(cueToPlay);
                    currentMusicCue = cueToPlay;
                }

            }
            return isAlreadyDead;
        }

        //normal snapshot change
        private void AudioManager_ApplyMusicSnapshot(On.AudioManager.orig_ApplyMusicSnapshot orig, AudioManager self, AudioMixerSnapshot snapshot, float delayTime, float transitionTime)
        {
            if (mode == Mode.Off || !Lists.Snapshots.Contains(snapshot.name))
            {
                orig(self, snapshot, delayTime, transitionTime);
            }
            else
            {
                AudioMixerSnapshot newSnapshot = GetAppropriateSnapshot(snapshot);
                orig(self, newSnapshot, delayTime, transitionTime);
            }

        }

        //snapshot change in playmaker
        private void TransitionToAudioSnapshot_OnEnter(On.HutongGames.PlayMaker.Actions.TransitionToAudioSnapshot.orig_OnEnter orig, TransitionToAudioSnapshot self)
        {
            AudioMixerSnapshot ams = (AudioMixerSnapshot)self.snapshot.Value;
            
            if (ams != null && mode != Mode.Off && Lists.Snapshots.Contains(ams.name))
            {
                AudioMixerSnapshot newSnapshot = GetAppropriateSnapshot(ams);
                self.snapshot.Value = newSnapshot;
            }
            orig(self);
        }

        #endregion

        private MusicCue[] GetIncludedMusicCues()
        {
            MusicCue[] includedCues = [.. myMusicCues.Where(cue => 
                (includeShade || cue.name != "Hollow Shade Music")
                && (includeColo || !cue.name.StartsWith("S57 COLOSSEUM INTENSITY"))
            )];
            return includedCues;
        }

        private MusicCue GetRandomDifferentMusicCue(MusicCue exceptCue, bool? isBossTheme)
        {
            MusicCue[] includedCues = GetIncludedMusicCues();
            MusicCue[] musicCues = Array.FindAll(includedCues, cue => cue != exceptCue && (isBossTheme == null || isBossTheme == Lists.BossThemes.Contains(cue.name)));
            int rand = UnityEngine.Random.Range(0, musicCues.Length);
            return musicCues.ElementAt(rand);
        }

        private AudioMixerSnapshot GetAppropriateSnapshot(AudioMixerSnapshot origSnapshot)
        {
            if (currentMusicCue == null)
            {
                return origSnapshot;
            }
            MusicChannels[] activeChannels = Util.GetChannelsForMusicCue(currentMusicCue);
            bool hasAction = activeChannels.Contains(MusicChannels.Action);
            bool hasSub = activeChannels.Contains(MusicChannels.Sub);
            bool hasTension = activeChannels.Contains(MusicChannels.Tension);

            switch (origSnapshot.name)
            {
                case "Action":
                case "Action Only":
                    if (!hasAction)
                    {
                        return mySnapshots["Normal"];
                    }
                    break;
                case "Action and Sub":
                    if (!hasSub)
                    {
                        if (hasAction)
                        {
                            return mySnapshots["Action"];
                        }
                        else
                        {
                            return mySnapshots["Normal"];
                        }
                    }
                    else
                    {
                        if (!hasAction)
                        {
                            return mySnapshots["Sub Area"];
                        }
                    }
                    break;
                case "Sub Area":
                    if (!hasSub && !isDreaming) //special case: always allow sub area in dreamer scene
                    {
                        return mySnapshots["Main Only"];
                    }
                    break;
                case "Tension Only":
                    if (!hasTension)
                    {
                        return mySnapshots["Main Only"];
                    }
                    break;
            }
            return origSnapshot;
        }

        private void ChangeShadeMusic(MusicCue cue)
        {
            MusicCue.MusicChannelInfo mainChannel = cue.GetChannelInfo(MusicChannels.Main);
            if (mainChannel != null && shadeMusicSource != null)
            {
                shadeMusicSource.clip = mainChannel.Clip;
                shadeMusicSource.enabled = false;
                shadeMusicSource.enabled = true; //trick to get it to play
            }
        }

        #region SettingsStorage

        public void OnLoadLocal(FixedMusicMap s)
        {
            musicMap = s.MusicMap;
        }

        public FixedMusicMap OnSaveLocal()
        {
            FixedMusicMap data = new()
            {
                MusicMap = musicMap
            };
            return data;
        }

        public void OnLoadGlobal(Settings s)
        {
            mode = s.mode;
            separateBosses = s.separateBosses;
            includeShade = s.includeShade;
            includeColo = s.includeColo;
        }

        public Settings OnSaveGlobal()
        {
            Settings data = new()
            {
                mode = this.mode,
                separateBosses = this.separateBosses,
                includeShade = this.includeShade,
                includeColo = this.includeColo
            };
            return data;
        }

        #endregion

        #region Menu

        public MenuScreen GetMenuScreen(MenuScreen modListMenu, ModToggleDelegates? toggleDelegates)
        {
            HorizontalOption bossOption = new(
                name: "Boss Themes",
                Id: "Boss",
                description: "Whether to put boss themes in a separate pool",
                values: ["Mixed", "Separate"],
                applySetting: index => separateBosses = index == 1,
                loadSetting: () => separateBosses ? 1 : 0
            );

            HorizontalOption shadeOption = new(
                name: "Include Shade Music",
                Id: "Shade",
                description: "Whether to include the shade music in the pool",
                values: ["No", "Yes"],
                applySetting: index =>
                {
                    includeShade = index == 1;
                    if (index == 0)
                    {
                        MusicCue shadeMusic = Array.Find(myMusicCues, cue => cue.name == "Hollow Shade Music");
                        ChangeShadeMusic(shadeMusic); //change back to default
                    }
                },
                loadSetting: () => includeShade ? 1 : 0
            );

            HorizontalOption coloOption = new(
                name: "Include Colosseum Music",
                Id: "Colo",
                description: "Whether to include the colosseum music in the pool",
                values: ["No", "Yes"],
                applySetting: index => includeColo = index == 1,
                loadSetting: () => includeColo ? 1 : 0
            );

            MenuButton reButton = new(
                name: "Re-shuffle",
                Id: "Re",
                description: "Shuffle the tracks for Fixed mode",
                submitAction: (button) =>
                {
                    BuildMusicMap();
                }
            );

            menuRef ??= new Menu(
                name: "Music Shuffle",
                elements: new Element[]
                {
                    new HorizontalOption(
                        name: "Mode",
                        description: "Shuffle Mode",
                        values: Enum.GetNames(typeof(Mode)),
                        applySetting: index => {
                            mode = (Mode)index;
                            MenuButton reButton = menuRef.Find("Re") as MenuButton;
                            if (mode == Mode.Fixed)
                            {
                                reButton.Show();
                            }
                            else
                            {
                                reButton.Hide();
                            }
                            HorizontalOption bossOption = menuRef.Find("Boss") as HorizontalOption;
                            if (mode == Mode.Off)
                            {
                                bossOption.Hide();
                                shadeOption.Hide();
                                coloOption.Hide();

                                MusicCue shadeMusic = Array.Find(myMusicCues, cue => cue.name == "Hollow Shade Music");
                                ChangeShadeMusic(shadeMusic); //change back to default
                            }
                            else
                            {
                                bossOption.Show();
                                shadeOption.Show();
                                coloOption.Show();
                            }
                        },
                        loadSetting: () => (int)mode
                    ),
                    bossOption,
                    shadeOption,
                    coloOption,
                    reButton
                }
            );

            if (mode != Mode.Fixed)
            {
                reButton.isVisible = false;
            }
            if (mode == Mode.Off)
            {
                bossOption.isVisible = false;
                shadeOption.isVisible = false;
                coloOption.isVisible = false;
            }

            return menuRef.GetMenuScreen(modListMenu);
        }

        #endregion
    }
        
}
