﻿using System.Linq;
using OWML.ModHelper;
using OWML.Common.Menus;
using UnityEngine;

namespace SaveEditor
{
    public class SaveEditor : ModBehaviour
    {
        private static readonly Vector2 EditorMenuSize = new Vector2(600, 900);

        private bool _menuOpen;
        private bool _hasEchoes;
        private bool _showShipLog;
        private string currentRevealFactID = "TH_VILLAGE_X1";
        private GUIStyle _editorMenuStyle;

        private static readonly SignalName[] BaseGameSignals =
        {
            SignalName.Traveler_Chert, SignalName.Traveler_Esker,
            SignalName.Traveler_Feldspar, SignalName.Traveler_Gabbro,
            SignalName.Traveler_Nomai, SignalName.Traveler_Prisoner, SignalName.Traveler_Riebeck,
            SignalName.Quantum_QM, SignalName.EscapePod_BH, SignalName.EscapePod_CT, SignalName.EscapePod_DB,
            SignalName.Quantum_BH_Shard, SignalName.Quantum_CT_Shard, SignalName.Quantum_GD_Shard,
            SignalName.Quantum_TH_GroveShard, SignalName.Quantum_TH_MuseumShard
        };

        private static readonly SignalName[] EchoesSignals =
        {
            SignalName.MapSatellite, SignalName.RadioTower
        };

        private void OpenMenu(){
            MakeMenuStyle();
            _menuOpen = true;
        }

        private void CloseMenu(){
            _menuOpen = false;
        }

        private static bool IsEntitlementsManagerReady()
        {
            return EntitlementsManager.IsDlcOwned() != EntitlementsManager.AsyncOwnershipStatus.NotReady;
        }

        private static bool CheckForDLC()
        {
            return EntitlementsManager.IsDlcOwned() == EntitlementsManager.AsyncOwnershipStatus.Owned;
        }

        private Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width*height];
 
            for(int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
 
            Texture2D newTexture = new Texture2D(width, height);
            newTexture.SetPixels(pixels);
            newTexture.Apply();
            return newTexture;
        }
        
        private void MakeMenuStyle()
        {
            Texture2D bgTexture = MakeTexture((int)EditorMenuSize.x, (int)EditorMenuSize.y, Color.black);

            _editorMenuStyle = new GUIStyle
            {
                normal =
                {
                    background = bgTexture
                }
            };
        }

        private void Start()
        {
            ModHelper.Events.Unity.RunWhen(IsEntitlementsManagerReady, () => _hasEchoes = CheckForDLC());
            ModHelper.Menus.MainMenu.OnInit += MainMenuInitHook;
            ModHelper.Menus.PauseMenu.OnInit += PauseMenuInitHook;
            ModHelper.Menus.PauseMenu.OnClosed += CloseMenu;
            LoadManager.OnCompleteSceneLoad += (fromScene, toScene) => {
                CloseMenu();
                _showShipLog = toScene == OWScene.SolarSystem || toScene == OWScene.EyeOfTheUniverse;
            };
        }

        private void PauseMenuInitHook()
        {
            IModButton editorButton = ModHelper.Menus.PauseMenu.OptionsButton.Duplicate("Edit Save Data".ToUpper());
            editorButton.OnClick += EditorButtonClickCallback;
        }

        private void MainMenuInitHook()
        {
            IModButton editorButton = ModHelper.Menus.MainMenu.SwitchProfileButton.Duplicate("Edit Save Data".ToUpper());
            editorButton.OnClick += EditorButtonClickCallback;
        }

        private void ConditionToggle(string label, string key)
        {
            PlayerData._currentGameSave.SetPersistentCondition(key, GUILayout.Toggle(PlayerData._currentGameSave.GetPersistentCondition(key), label));
        }

        private void OnGUI()
        {
            if (!_menuOpen) return;
            Vector2 menuPosition = new Vector2(Screen.width - EditorMenuSize.x - 10, 10);
            GUILayout.BeginArea(new Rect(menuPosition.x, menuPosition.y, EditorMenuSize.x, EditorMenuSize.y), _editorMenuStyle);
            GUILayout.Label("*: Restart Required");
            // LOOP
            PlayerData._currentGameSave.loopCount = GUILayout.Toggle(PlayerData._currentGameSave.loopCount > 1, "*Time Loop Started") ? 10 : 1;
            // FLAGS
            ConditionToggle("Learned Launch Codes", "LAUNCH_CODES_GIVEN");
            ConditionToggle("Ship Log Tutorial Done", "COMPLETED_SHIPLOG_TUTORIAL");
            ConditionToggle("Mark on HUD Tutorial Done", "MARK_ON_HUD_TUTORIAL_COMPLETE");
            ConditionToggle("Learned Meditation", "KNOWS_MEDITATION");
            GUILayout.Space(5);
            ConditionToggle("Met Solanum", "MET_SOLANUM");
            if (_hasEchoes) ConditionToggle("Met Prisoner", "MET_PRISONER");
            ConditionToggle("Paradox", "PLAYER_ENTERED_TIMELOOPCORE");
            GUILayout.Space(5);
            var warpToEyeBefore = PlayerData._currentGameSave.warpedToTheEye;
            var warpToEyeToggle = GUILayout.Toggle(warpToEyeBefore, "*Warped To The Eye Of the Universe");
            if (warpToEyeToggle != warpToEyeBefore)
            {
                if (warpToEyeToggle)
                {
                    PlayerData._currentGameSave.warpedToTheEye = true;
                    PlayerData._currentGameSave.secondsRemainingOnWarp = 180;
                }
                else
                {
                    PlayerData._currentGameSave.warpedToTheEye = false;
                }


                if (LoadManager.GetCurrentScene() == OWScene.TitleScreen)
                {
                    var sceneToLoad = warpToEyeToggle ? SubmitActionLoadScene.LoadableScenes.EYE : SubmitActionLoadScene.LoadableScenes.GAME;
                    var newGame = GameObject.Find("TitleMenu/TitleCanvas/TitleLayoutGroup/MainMenuBlock/MainMenuLayoutGroup/Button-NewGame")?.GetComponent<SubmitActionLoadScene>();
                    var resumeGame = GameObject.Find("TitleMenu/TitleCanvas/TitleLayoutGroup/MainMenuBlock/MainMenuLayoutGroup/Button-ResumeGame")?.GetComponent<SubmitActionLoadScene>();
                    if (newGame != null) newGame._sceneToLoad = sceneToLoad;
                    if (resumeGame != null) resumeGame._sceneToLoad = sceneToLoad;
                }
            }
            GUILayout.Space(5);
            GUILayout.Label("Achievements");
            bool earnAllAchievementsClicked = GUILayout.Button("Earn All");
            // SIGNALS & FREQUENCIES
            GUILayout.Label("Signals & Frequencies");
            GUILayout.BeginHorizontal();
            bool learnAllSignalsClicked = GUILayout.Button("Learn All");
            bool learnJustBaseGameSignalsClicked = GUILayout.Button("Learn Just Base Game");
            bool forgetAllSignalsClicked = GUILayout.Button("Forget All");
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            // SHIP LOG
            GUILayout.Label("Ship Log");
            bool learnShipLogClicked = false;
            bool learnJustBaseGameShipLogClicked = false;
            bool forgetShipLogClicked = false;
            bool learnSpecificFactClicked = false;
            if (_showShipLog)
            {
                GUILayout.BeginHorizontal();
                learnShipLogClicked = GUILayout.Button("Learn All");
                learnJustBaseGameShipLogClicked = GUILayout.Button("Learn Just Base Game");
                forgetShipLogClicked = GUILayout.Button("*Forget All");
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                currentRevealFactID = GUILayout.TextField(currentRevealFactID);
                learnSpecificFactClicked = GUILayout.Button("Learn Fact");
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label("   You can't edit the ship log on the title screen");
            }
            GUILayout.Space(10);
            // BUTTONS
            bool closeClicked = GUILayout.Button("Close");
            GUILayout.EndArea();
            
            // BUTTON LOGIC

            if (learnAllSignalsClicked)
            {
                PlayerData._currentGameSave.knownFrequencies = new[] {true, true, true, true, true, false, true};
                foreach (SignalName signal in BaseGameSignals.Concat(EchoesSignals))
                {
                    PlayerData._currentGameSave.knownSignals[(int) signal] = true;
                }
            }
            else if (learnJustBaseGameSignalsClicked)
            {
                PlayerData._currentGameSave.knownFrequencies = new[] {true, true, true, true, true, false, false};
                foreach (SignalName signal in BaseGameSignals)
                {
                    PlayerData._currentGameSave.knownSignals[(int) signal] = true;
                }
            }
            else if (forgetAllSignalsClicked)
            {
                Locator.GetToolModeSwapper()?.GetSignalScope()?.SelectFrequency(SignalFrequency.Traveler);
                if (Locator.GetToolModeSwapper()?.GetSignalScope()?.IsEquipped() == true) Locator.GetToolModeSwapper()?.UnequipTool();
                PlayerData._currentGameSave.knownFrequencies = new[] {false, true, false, false, false, false, false};
                foreach (SignalName signal in BaseGameSignals)
                {
                    PlayerData._currentGameSave.knownSignals[(int) signal] = false;
                }
            }
            else if (learnShipLogClicked)
            {
                Locator.GetShipLogManager().RevealAllFacts();
            }
            else if (learnJustBaseGameShipLogClicked)
            {
                ShipLogManager manager = Locator.GetShipLogManager();
                foreach (ShipLogEntry entry in manager._entryList)
                {
                    if (entry.GetCuriosityName() != CuriosityName.InvisiblePlanet)
                    {
                        foreach (ShipLogFact rumorFact in entry.GetRumorFacts())
                        {
                            manager.RevealFact(rumorFact.GetID());
                        }
                        foreach (ShipLogFact exploreFact in entry.GetExploreFacts())
                        {
                            manager.RevealFact(exploreFact.GetID());
                        }
                    }
                }
            }
            else if (forgetShipLogClicked)
            {
                foreach (ShipLogFactSave savedFact in PlayerData._currentGameSave.shipLogFactSaves.Values)
                {
                    savedFact.newlyRevealed = false;
                    savedFact.read = false;
                    savedFact.revealOrder = -1;
                }
            }
            else if (learnSpecificFactClicked)
            {
                Locator.GetShipLogManager().RevealFact(currentRevealFactID, false, true);
                currentRevealFactID = "";
            }
            else if (earnAllAchievementsClicked)
            {
                Achievements.AchieveAll();
            }
            else if (closeClicked)
            {
                PlayerData.SaveCurrentGame();
                CloseMenu();
            }
        }

        private void EditorButtonClickCallback()
        {
            if (_menuOpen)
            {
                CloseMenu();
            }
            else
            {
                OpenMenu();  
            }
        }
    }
}
