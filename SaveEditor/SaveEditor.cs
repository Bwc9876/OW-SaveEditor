using OWML.ModHelper;
using OWML.Common.Menus;
using UnityEngine;

namespace SaveEditor
{
    public class SaveEditor : ModBehaviour
    {
        private static readonly Vector2 EditorMenuSize = new Vector2(600, 260);

        private bool _menuOpen;
        private bool _hasEchoes;
        private GameSave _saveData;
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

        private Vector2 GetMenuPosition(){
            Vector2 centerScreen = new Vector2(Screen.width / 2, Screen.Height / 2);
            return new Vector2(centerScreen.x - (int)EditorMenuSize.x / 2, centerScreen.y - (int)EditorMenuSize.y / 2);
        }

        private void OpenMenu(){
            _menuOpen = true;
            _saveData = PlayerData._currentGameSave;
        }

        private void CloseMenu(){
            _menuOpen = false;
            _saveData = null;
        }

        private bool CheckForDLC()
        {
            return EntitlementsManager.IsDlcOwned() == EntitlementsManager.AsyncOwnershipStatus.Owned;
        }
        
        private Texture2D MakeMenuBackgroundTexture()
        {
            Color[] pixels = new Color[EditorMenuSize.x*EditorMenuSize.y];
 
            for(int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.black;
            }
 
            Texture2D newTexture = new Texture2D(EditorMenuSize.x, EditorMenuSize.y);
            newTexture.SetPixels(pixels);
            newTexture.Apply();
 
            return newTexture;
        }

        private void Awake()
        {
            _editorMenuStyle = new GUIStyle
            {
                normal =
                {
                    background = MakeMenuBackgroundTexture()
                }
            };
        }

        private void Start()
        {
            _hasEchoes = CheckForDLC();
            ModHelper.Menus.MainMenu.OnInit += MainMenuInitHook;
            ModHelper.Menus.PauseMenu.OnInit += PauseMenuInitHook;
            LoadManager.OnCompleteSceneLoad += (fromScene, toScene) => {
                CloseMenu();
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
            _saveData.SetPersistentCondition(key, GUILayout.Toggle(_saveData.GetPersistentCondition(key), label));
        }

        private void OnGUI()
        {
            if (!_menuOpen) return;
            Vector2 menuPosition = GetMenuPosition();
            GUILayout.BeginArea(new Rect(menuPosition.x, menuPosition.y, EditorMenuSize[0], EditorMenuSize[1]), _editorMenuStyle);
            // LOOP
            _saveData.loopCount = GUILayout.Toggle(_saveData.loopCount > 1, "Time Loop Started (Restart Required)") ? 10 : 1;
            // FLAGS
            ConditionToggle("Learned Launch Codes", "LAUNCH_CODES_GIVEN");
            ConditionToggle("Learned Meditation", "KNOWS_MEDITATION");
            GUILayout.Space(5);
            ConditionToggle("Met Solanum", "MET_SOLANUM");
            if (_hasEchoes) ConditionToggle("Met Prisoner", "MET_PRISONER");
            GUILayout.Space(5);
            _saveData.warpedToTheEye = GUILayout.Toggle(_saveData.warpedToTheEye, "Warped To The Eye Of the Universe (RESTART REQUIRED OR PREPARE TO DIE)");
            GUILayout.Space(5);
            // SIGNALS & FREQUENCIES
            GUILayout.Label("Signals & Frequencies");
            GUILayout.BeginHorizontal();
            bool learnAllSignalsClicked = GUILayout.Button("Learn All");
            bool forgetAllSignalsClicked = GUILayout.Button("Forget All");
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            // BUTTONS
            bool saveClicked = GUILayout.Button("Save");
            bool cancelClicked = GUILayout.Button("Cancel");
            GUILayout.EndArea();
            
            // BUTTON LOGIC

            if (learnAllSignalsClicked)
            {
                _saveData.knownFrequencies = new[] {true, true, true, true, true, false, true};
                foreach (SignalName signal in AllSignals)
                {
                    _saveData.knownSignals[(int) signal] = true;
                }
            }
            else if (forgetAllSignalsClicked)
            {
                _saveData.knownFrequencies = new[] {false, false, false, false, false, false, false};
                foreach (SignalName signal in AllSignals)
                {
                    _saveData.knownSignals[(int) signal] = false;
                }
            }
            else if (saveClicked)
            {
                PlayerData._currentGameSave = _saveData;
                PlayerData.SaveCurrentGame();
                CloseMenu();
            }
            else if (cancelClicked)
            {
                CloseMenu();
            }
        }

        private void EditorButtonClickCallback()
        {
            OpenMenu();
        }
    }
}
