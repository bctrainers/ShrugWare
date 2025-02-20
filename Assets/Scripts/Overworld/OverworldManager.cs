using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace ShrugWare
{

    /* 
        this connects the game world together by OverworldLevels
            each OverworldLevel will represent a single playable game level space
                type that can be seen in DataManager.OverworldLevelTypes
                list of OverworldLevels that it unlocks upon completion
            one game scenario per OverworldLevel (merchant, trash, boss...)
        the manager controls things, NOT the levels
    */

    public class OverworldManager : MonoBehaviour
    {
        public static OverworldManager Instance { get; private set; } = null;

        [SerializeField]
        OverworldUIManager overworldUIManager;

        [SerializeField]
        GameObject playerObj;

        Dictionary<int, OverworldLevel> overworldMap = new Dictionary<int, OverworldLevel>();

        private OverworldLevel curLevel = null;
        public OverworldLevel CurLevel
        {
            get { return curLevel; }
            set { curLevel = value; }
        }

        private PlayerInventory playerInventory = new PlayerInventory();
        public PlayerInventory PlayerInventory { get { return playerInventory; } }

        [SerializeField]
        private List<RandomEvent> randomEventList = new List<RandomEvent>();

        // the current random event that is triggered
        private RandomEvent curRandomEvent;
        public RandomEvent CurRandomEvent 
        {
            get { return curRandomEvent; }
            set { curRandomEvent = value; }
        }

        // it's possible that when reading a random event, we click a level on accident underneath the ui. don't allow that
        private bool waitingOnRandomEvent = false;
        public bool WaitingOnRandomEvent
        {
            get { return waitingOnRandomEvent; }
            set { waitingOnRandomEvent = value; }
        }

        [SerializeField]
        GameObject cameraObj;

        private bool isDebugMode = false;
        public bool IsDebugMode
        {
            get { return isDebugMode; }
            set { isDebugMode = value; }
        }

        [SerializeField]
        GameObject eventSystemObj;

        [SerializeField]
        GameObject audioListenerObj;

        [SerializeField]
        List<Sprite> microgameBackgrounds = new List<Sprite>();
        public List<Sprite> GetMicrogameBackgrounds() { return microgameBackgrounds; }

        [SerializeField]
        OptionsMenu optionsMenu;

        [SerializeField]
        AudioClipData overworldMusicData;

        // don't allow the player to move multiple spaces at once
        private bool isMoving = false;
        public bool IsMoving
        {
            get { return isMoving; }
        }

        // when we click a non-adjacent level, we need to keep track of our path
        private List<int> inOrderPathToLevel = new List<int>();
        private List<int> outOfOrderPathToLevel = new List<int>();

        private const float PLAYER_X_OFFSET = 13.0f;
        private const float PLAYER_Y_OFFSET = 8.0f;

        public enum OverworldGameState
        {
            None = 0,
            Overworld,
            TrashEncounter,
            BossFight,
            InfiniteMode,
            GearScreen,
            Tutorial,
            Merchant
        }

        private OverworldGameState gameState = OverworldGameState.None;
        public OverworldGameState GetOverworldGameState() { return gameState; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);

                gameState = Instance.gameState;
                optionsMenu = Instance.optionsMenu;
                audioListenerObj = Instance.audioListenerObj;
                eventSystemObj = Instance.eventSystemObj;
                overworldMap = Instance.overworldMap;
                curLevel = Instance.curLevel;
                playerObj = Instance.playerObj;
                playerObj.SetActive(true);
            }

            if (AudioManager.Instance != null && !AudioManager.Instance.IsMusicPlaying())
            {
                PlayOverworldMusic();
            }

            overworldUIManager.UpdateUI();
            ReadyScene(true);

            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // for some reason when we put this in Awake, it's not being hit
            if (AudioManager.Instance != null && !AudioManager.Instance.IsMusicPlaying())
            {
                PlayOverworldMusic();
            }

            // set fps so players don't have varying speeds
            Application.targetFrameRate = 60;

            if (playerInventory == null)
            {
                playerInventory = new PlayerInventory();
            }
        }

        private void Update()
        {
            // tilde
            if (Input.GetKeyDown(KeyCode.BackQuote))
            {
                if (isDebugMode)
                {
                    overworldUIManager.OnStopDebuggingPressed();
                }
                else
                {
                    overworldUIManager.EnableDebugging();
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SetupOptionsVisibility();
            }

            if(Input.GetKeyDown(KeyCode.I))
            {
                OnGearScreenPressed();
            }
        }

        public void AddLevel(OverworldLevel newLevel)
        {
            if(!overworldMap.ContainsKey(newLevel.LevelID))
            {
                overworldMap.Add(newLevel.LevelID, newLevel);
            }

            // we need an initial starting level
            if (curLevel == null && newLevel.LevelType == DataManager.OverworldLevelType.Start)
            {
                SetCurLevelById(newLevel.LevelID);
            }

            // we want to show the level as locked if we've not completed it and it's locked
            if (newLevel.Locked && !newLevel.Completed)
            {
                newLevel.GetComponent<SpriteRenderer>().color = new Color(255, 0, 0);
            }
        }

        public void ReadyScene(bool enabled)
        {
            Time.timeScale = 1.0f;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.ResetPitch(true);
            }

            if (enabled)
            {
                gameState = OverworldGameState.Overworld;

                // hack. how do we fix this? Instance is different from this for some reason and it's not taking the change
                OverworldManager.Instance.gameState = OverworldGameState.Overworld;

                EnablePlayerObject();
                EnableCamera();
                EnableEventSystem();
                overworldUIManager.SetCanvasEnabled(true);
                //EnableAudioListener();

                // move player to cur level (this will be null on start)
                if (curLevel != null)
                {
                    playerObj.transform.position = new Vector3(curLevel.transform.position.x - PLAYER_X_OFFSET, curLevel.transform.position.y - PLAYER_Y_OFFSET, curLevel.transform.position.z);
                }

                // only do this on trash/boss/infinite levels because those have their own audio
                if (curLevel != null && (curLevel.LevelType == DataManager.OverworldLevelType.Trash || curLevel.LevelType == DataManager.OverworldLevelType.Boss
                    || curLevel.LevelType == DataManager.OverworldLevelType.Infinite))
                {
                    // give the audio manager time
                    Invoke("PlayOverworldMusic", 0.1f);
                }
            }
            else
            {
                // don't stop the music because we won't be starting any new music here (yet at least)
                if (curLevel.LevelType != DataManager.OverworldLevelType.Tutorial && curLevel.LevelType != DataManager.OverworldLevelType.Merchant
                    && gameState != OverworldGameState.GearScreen)
                {
                    StopMusic();
                }

                DisablePlayerObject();
                DisableCamera();
                DisableEventSystem();
                //DisableAudioListener();
            }
        }

        public bool IsLevelLocked(int levelID)
        {
#if UNITY_EDITOR
            return false;
#endif

#pragma warning disable CS0162 // Unreachable code detected
            OverworldLevel overworldLevel = GetOverworldLevelByID(levelID);
#pragma warning restore CS0162 // Unreachable code detected
            if (overworldLevel != null)
            {
                return overworldLevel.Locked;
            }

            return false;
        }

        public void CompleteLevel(int completedLevelID)
        {
            OverworldLevel overworldLevel = GetOverworldLevelByID(completedLevelID);
            if (overworldLevel != null)
            {
                overworldLevel.Completed = true;

                // now unlock the next level(s)
                foreach (int idToUnlock in overworldLevel.LevelIDsToUnlock)
                {
                    OverworldLevel overworldLevelToUnlock = GetOverworldLevelByID(idToUnlock);
                    if(overworldLevelToUnlock != null)
                    {
                        if (!overworldLevelToUnlock.preRequisitesFulfulled.Contains(overworldLevel.LevelID))
                        {
                            overworldLevelToUnlock.preRequisitesFulfulled.Add(overworldLevel.LevelID);
                        }

                        if(overworldLevelToUnlock.preRequisitesList.Count == overworldLevelToUnlock.preRequisitesFulfulled.Count)
                        {
                            overworldLevelToUnlock.Locked = false;
                            SetDefaultLevelColor(overworldLevelToUnlock);
                        }

                        // update the connection of our current level
                        foreach(GameObject levelConnection in overworldLevel.OutgoingLevelConnections)
                        {
                            levelConnection.GetComponent<MeshRenderer>().material.color = Color.green;
                        }
                    }
                }
            }
        }

        private void SetDefaultLevelColor(OverworldLevel overworldLevelToUnlock)
        {
            if (!overworldLevelToUnlock.Completed)
            {
                overworldLevelToUnlock.GetComponent<SpriteRenderer>().color = new Color(255, 255, 255);
            }
        }
        
        public void SetCurLevelById(int levelID)
        {
            if(optionsMenu.isActiveAndEnabled)
            {
                return;
            }

            // only change if we've unlocked it
            OverworldLevel newOverworldLevel = GetOverworldLevelByID(levelID);
            if (newOverworldLevel != null)
            {
                bool force = isDebugMode;
#if UNITY_EDITOR
                force = true;
#endif
                // in the beginning our curLevel will be null before it's set
                if(curLevel == null)
                {
                    curLevel = newOverworldLevel;
                }

                if ((!newOverworldLevel.Locked && !isMoving) || force)
                {
                    if (!isMoving)
                    {
                        inOrderPathToLevel.Clear();
                        List<int> visitedList = new List<int>();

                        visitedList.Add(curLevel.LevelID);
                        inOrderPathToLevel.Clear();
                        GeneratePathToLevel(curLevel.LevelID, newOverworldLevel.LevelID, ref visitedList, true);
                        visitedList.Clear();
                        outOfOrderPathToLevel.Clear();
                        GeneratePathToLevel(curLevel.LevelID, newOverworldLevel.LevelID, ref visitedList, false);

                        inOrderPathToLevel.Reverse();
                        outOfOrderPathToLevel.Reverse();
                        StartCoroutine(FollowPathToLevel());
                    }

                    overworldUIManager.UpdateUI();
                }
                else
                {
                    Debug.Log("Level not unlocked");
                }
            }
        }

        private IEnumerator MovePlayerToLevel(OverworldLevel targetOverworldLevel)
        {
            isMoving = true;

            curLevel = targetOverworldLevel;

            // be at the left and center of the level object
            Vector3 targetPos = new Vector3(curLevel.transform.position.x - PLAYER_X_OFFSET, curLevel.transform.position.y - PLAYER_Y_OFFSET, curLevel.transform.position.z);
            while (Vector3.Distance(playerObj.transform.position, targetPos) > 0.1f)
            {
                playerObj.transform.position = Vector3.MoveTowards(playerObj.transform.position, targetPos, 50 * Time.deltaTime);
                yield return 0;
            }

            isMoving = false;
        }
        
        private bool GeneratePathToLevel(int lookedAtLevelID, int targetLevelID, ref List<int> visited, bool inOrder = true)
        {
            if(lookedAtLevelID == targetLevelID)
            {
                return true;
            }

            bool force = isDebugMode;
#if UNITY_EDITOR
            force = true;
#endif
            OverworldLevel level = GetOverworldLevelByID(lookedAtLevelID);
            if(level.Locked && !force)
            {
                return false;
            }

            if (inOrder)
            {
                foreach (int connectingLevelID in level.AdjacentMapLevels)
                {
                    if (visited.Contains(connectingLevelID))
                    {
                        continue;
                    }

                    visited.Add(connectingLevelID);
                    if (GeneratePathToLevel(connectingLevelID, targetLevelID, ref visited, inOrder))
                    {
                        inOrderPathToLevel.Add(connectingLevelID);
                        return true;
                    }
                }

                inOrderPathToLevel.Remove(lookedAtLevelID);
            }
            else
            {
                for (int i = level.AdjacentMapLevels.Count - 1; i >= 0; --i)
                {
                    int connectingLevelID = level.AdjacentMapLevels[i];
                    if (visited.Contains(connectingLevelID))
                    {
                        continue;
                    }

                    visited.Add(connectingLevelID);
                    if (GeneratePathToLevel(connectingLevelID, targetLevelID, ref visited, inOrder))
                    {
                        outOfOrderPathToLevel.Add(connectingLevelID);
                        return true;
                    }
                }

                outOfOrderPathToLevel.Remove(lookedAtLevelID);
            }

            return false;
        }

        IEnumerator FollowPathToLevel()
        {
            isMoving = true;
            overworldUIManager.DisableStartButton();

            if(inOrderPathToLevel.Count < outOfOrderPathToLevel.Count)
            {
                foreach (int levelID in inOrderPathToLevel)
                {
                    OverworldLevel level = GetOverworldLevelByID(levelID);
                    StartCoroutine(MovePlayerToLevel(level));

                    // we're going to maybe be moving in MovePlayerToLevel. wait until we're done before going to the next level node
                    while (isMoving)
                    {
                        yield return null;
                    }
                }
            }
            else
            {
                foreach (int levelID in outOfOrderPathToLevel)
                {
                    OverworldLevel level = GetOverworldLevelByID(levelID);
                    StartCoroutine(MovePlayerToLevel(level));

                    // we're going to maybe be moving in MovePlayerToLevel. wait until we're done before going to the next level node
                    while (isMoving)
                    {
                        yield return null;
                    }
                }
            }


            isMoving = false;

            if (curLevel.LevelType != DataManager.OverworldLevelType.Start)
            {
                overworldUIManager.EnableStartButton();
            }

            overworldUIManager.UpdateUI();
        }

        public OverworldLevel GetOverworldLevelByID(int levelID)
        {
            OverworldLevel overworldLevel = null;
            overworldMap.TryGetValue(levelID, out overworldLevel);
            if (overworldLevel != null)
            {
                return overworldLevel;
            }

            return null;
        }

        public void EnterLevel(OverworldLevel level)
        {
            bool force = IsDebugMode;
#if UNITY_EDITOR
            force = true;
#endif

            if (level.LevelType == DataManager.OverworldLevelType.Start || (level.Locked && !force))
            {
                // we don't enter these
                return;
            }

            // don't let them enter a level while moving, this is for safety even though we removed it in the ui
            if(isMoving)
            {
                return;
            }

            // 15% chance we trigger an event
            bool isTrashOrBoss = level.LevelType == DataManager.OverworldLevelType.Trash || level.LevelType == DataManager.OverworldLevelType.Boss;
            int rand = UnityEngine.Random.Range(0, 100);
            if (rand < 15 && isTrashOrBoss)
            {
                // pick a random event
                int randomEventIndex = UnityEngine.Random.Range(0, randomEventList.Count);
                curRandomEvent = randomEventList[randomEventIndex];
                
                // don't need to do anything else, we use this cached value later
                curLevel = level;
                overworldUIManager.OnRandomEventTriggered();
                waitingOnRandomEvent = true;

                overworldUIManager.EnableOptionsButton(false);
                overworldUIManager.EnableGearScreenButton(false);

                return;
            }

            playerObj.SetActive(false);

            // only stop if we enter a trash/boss/infinite level
            if(AudioManager.Instance != null && (isTrashOrBoss || level.LevelType == DataManager.OverworldLevelType.Infinite))
            {
                AudioManager.Instance.StopAudio();
            }

            SetGameState(level.LevelType);

            OverworldUIManager.Instance.SetCanvasEnabled(false);
            ReadyScene(false);
            SceneManager.LoadScene((int)level.SceneIDToLoad, LoadSceneMode.Additive);
        }

        private void PlayOverworldMusic()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.LoopMusic(true);
                AudioManager.Instance.PlayMusicClip(overworldMusicData);
            }
        }

        public void RandomEventContinuePressed()
        {            
            // hide the player
            // todo - fix this, it doesn't work. for whatever reason it only works when attached to a debugger
            playerObj.SetActive(false);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopAudio();
            }
        }

        public void PlayMusicClip(AudioClipData audioClipData)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayMusicClip(audioClipData.clip, audioClipData.audioEffectType, audioClipData.maxVolume);
            }
        }

        public void StopMusic()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopAudio();
            }
        }

        public void EnableCamera()
        {
            cameraObj.SetActive(true);
        }

        public void DisableCamera()
        {
            cameraObj.SetActive(false);
        }

        private void EnableEventSystem()
        {
            eventSystemObj.SetActive(true);
        }

        private void DisableEventSystem()
        {
            eventSystemObj.SetActive(false);
        }

        private void EnableAudioListener()
        {
            audioListenerObj.SetActive(true);
        }

        private void DisableAudioListener()
        {
            audioListenerObj.SetActive(false);
        }

        public void ResetAudioPitch()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.ResetPitch();
            }
        }

        public void UnpauseGame()
        {
            RevertTimescale();
        }
        
        public void RevertTimescale()
        {
            // we might be in a boss fight or infinite, in which case we set it back to what we had before
            if (BossGameManager.Instance != null)
            {
                Time.timeScale = BossGameManager.Instance.GetCurTimeScale();
            }
            else if (InfiniteModeManager.Instance != null)
            {
                Time.timeScale = InfiniteModeManager.Instance.GetCurTimeScale();
            }
            else
            {
                Time.timeScale = 1.0f;
            }
        }

        private void EnablePlayerObject()
        {
            playerObj.SetActive(true);
        }

        private void DisablePlayerObject()
        {
            playerObj.SetActive(false);
        }

        public void OnGearScreenPressed()
        {
            OverworldUIManager.Instance.SetCanvasEnabled(false);
            gameState = OverworldGameState.GearScreen;
            ReadyScene(false);

            SceneManager.LoadScene((int)DataManager.Scenes.GearScreen, LoadSceneMode.Additive);
        }

        public void SetupOptionsVisibility()
        {
            if (optionsMenu.isActiveAndEnabled)
            {
                RevertTimescale();
                optionsMenu.gameObject.SetActive(false);
            }
            else
            {
                Time.timeScale = 0.0f;
                optionsMenu.gameObject.SetActive(true);
            }
        }

        public void OnOptionsScreenPressed()
        {
            SetupOptionsVisibility();
        }

        private void SetGameState(DataManager.OverworldLevelType levelType)
        {
            if (levelType == DataManager.OverworldLevelType.Trash)
            {
                gameState = OverworldGameState.TrashEncounter;
            }
            else if (levelType == DataManager.OverworldLevelType.Boss)
            {
                gameState = OverworldGameState.BossFight;
            }
            else if (levelType == DataManager.OverworldLevelType.Infinite)
            {
                gameState = OverworldGameState.InfiniteMode;
            }
            else if (levelType == DataManager.OverworldLevelType.Tutorial)
            {
                gameState = OverworldGameState.Tutorial;
            }
            else if (levelType == DataManager.OverworldLevelType.Merchant)
            {
                gameState = OverworldGameState.Merchant;
            }
            else if (levelType == DataManager.OverworldLevelType.GearScreen)
            {
                gameState = OverworldGameState.GearScreen;
            }
        }

        public OptionsMenu GetOptionsMenu()
        {
            return optionsMenu;
        }

        public void DisablePlayer()
        {
            playerObj.SetActive(false);
        }
    }
}
