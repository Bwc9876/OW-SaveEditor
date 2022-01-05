using System.Collections.Generic;
using OWML.ModHelper;
using OWML.Common.Menus;
using UnityEngine;

namespace SaveEditor
{
    public class SaveEditor : ModBehaviour
    {
        private static readonly Vector2 EditorMenuSize = new Vector2(600, 320);

        private bool _menuOpen;
        private bool _hasEchoes;
        private bool _showShipLog;
        private GUIStyle _editorMenuStyle;

        private static readonly SignalName[] AllSignals =
        {
            SignalName.MapSatellite, SignalName.RadioTower, SignalName.Traveler_Chert, SignalName.Traveler_Esker,
            SignalName.Traveler_Feldspar, SignalName.Traveler_Feldspar, SignalName.Traveler_Gabbro,
            SignalName.Traveler_Nomai, SignalName.Traveler_Prisoner, SignalName.Traveler_Riebeck, SignalName.Quantum_QM,
            SignalName.Quantum_QM, SignalName.EscapePod_BH, SignalName.EscapePod_CT, SignalName.EscapePod_DB,
            SignalName.Quantum_BH_Shard, SignalName.Quantum_CT_Shard, SignalName.Quantum_GD_Shard,
            SignalName.Quantum_TH_GroveShard, SignalName.Quantum_TH_MuseumShard
        };

        private void OpenMenu(){
            MakeMenuStyle();
            _menuOpen = true;
        }

        private void CloseMenu(){
            _menuOpen = false;
        }

        private bool CheckForDLC()
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
            _hasEchoes = CheckForDLC();
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
            // LOOP
            PlayerData._currentGameSave.loopCount = GUILayout.Toggle(PlayerData._currentGameSave.loopCount > 1, "Time Loop Started (Restart Required)") ? 10 : 1;
            // FLAGS
            ConditionToggle("Learned Launch Codes", "LAUNCH_CODES_GIVEN");
            ConditionToggle("Learned Meditation", "KNOWS_MEDITATION");
            GUILayout.Space(5);
            ConditionToggle("Met Solanum", "MET_SOLANUM");
            if (_hasEchoes) ConditionToggle("Met Prisoner", "MET_PRISONER");
            GUILayout.Space(5);
            PlayerData._currentGameSave.warpedToTheEye = GUILayout.Toggle(PlayerData._currentGameSave.warpedToTheEye, "Warped To The Eye Of the Universe (Restart Required)");
            GUILayout.Space(5);
            // SIGNALS & FREQUENCIES
            GUILayout.Label("Signals & Frequencies");
            GUILayout.BeginHorizontal();
            bool learnAllSignalsClicked = GUILayout.Button("Learn All");
            bool forgetAllSignalsClicked = GUILayout.Button("Forget All");
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            // SHIP LOG
            GUILayout.Label("Ship Log");
            bool learnShipLogClicked = false;
            bool forgetShipLogClicked = false;
            if (_showShipLog)
            {
                GUILayout.BeginHorizontal();
                learnShipLogClicked = GUILayout.Button("Learn All");
                forgetShipLogClicked = GUILayout.Button("Forget All (Restart Required)");
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
                foreach (SignalName signal in AllSignals)
                {
                    PlayerData._currentGameSave.knownSignals[(int) signal] = true;
                }
            }
            else if (forgetAllSignalsClicked)
            {
                Locator.GetToolModeSwapper()?.GetSignalScope()?.SelectFrequency(SignalFrequency.Traveler);
                if (Locator.GetToolModeSwapper()?.GetSignalScope()?.IsEquipped() == true) Locator.GetToolModeSwapper()?.UnequipTool();
                PlayerData._currentGameSave.knownFrequencies = new[] {false, true, false, false, false, false, false};
                foreach (SignalName signal in AllSignals)
                {
                    PlayerData._currentGameSave.knownSignals[(int) signal] = false;
                }
            }
            else if (learnShipLogClicked)
            {
                Locator.GetShipLogManager().RevealAllFacts();
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
