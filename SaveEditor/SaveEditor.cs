using OWML.ModHelper;
using OWML.Common.Menus;
using UnityEngine;

namespace SaveEditor
{
    public class SaveEditor : ModBehaviour
    {
        private static readonly int[] EditorMenuSize = {600, 260};

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

        private bool CheckForDLC()
        {
            return EntitlementsManager.IsDlcOwned() == EntitlementsManager.AsyncOwnershipStatus.Owned;
        }
        
        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width*height];
 
            for(int i = 0; i < pix.Length; i++)
                pix[i] = col;
 
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
 
            return result;
        }

        private void Awake()
        {
            _editorMenuStyle = new GUIStyle
            {
                normal =
                {
                    background = MakeTex(EditorMenuSize[0], EditorMenuSize[1], Color.black)
                }
            };
        }

        private void Start()
        {
            ModHelper.Menus.MainMenu.OnInit += MainMenuInitHook;
            SceneManager.activeSceneChanged += SceneChangedHook;
        }

        private void MainMenuInitHook()
        {
            _hasEchoes = CheckForDLC();
            IModButton editorButton = ModHelper.Menus.MainMenu.SwitchProfileButton.Duplicate("Edit Save Data".ToUpper());
            editorButton.OnClick += EditorButtonClickCallback;
        }

        private void SceneChangedHook()
        {
            if (_menuOpen) {
                _menuOpen = false;
            }
        }

        private void ConditionToggle(string label, string key)
        {
            _saveData.SetPersistentCondition(key, GUILayout.Toggle(_saveData.GetPersistentCondition(key), label));
        }

        private void OnGUI()
        {
            if (!_menuOpen) return;
            GUILayout.BeginArea(new Rect(10, 10, EditorMenuSize[0], EditorMenuSize[1]), _editorMenuStyle);
            // LOOP
            _saveData.loopCount = GUILayout.Toggle(_saveData.loopCount > 1, "Time Loop Started (Restart Required)") ? _saveData.loopCount + 1 : 1;
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
            GUILayout.Space(5);
            // SHIP LOG
            GUILayout.BeginHorizontal();
            bool learnAllFactsClicked = GUILayout.Button("Learn All");
            bool forgetAllFactsClicked = GUILayout.Button("Forget All");
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
                foreach (SignalName signalsKey in AllSignals)
                {
                    _saveData.knownSignals[(int)signalsKey] = false;
                }
            }

            if (learnAllFactsClicked) {
                // Learn All Facts
            }
            else if (forgetAllFactsClicked) {
                // Forget All Facts
            }

            if (saveClicked)
            {
                PlayerData._currentGameSave = _saveData;
                PlayerData.SaveCurrentGame();
                _saveData = null;
                _menuOpen = false;
            }
            else if (cancelClicked)
            {
                _saveData = null;
                _menuOpen = false;
            }
        }

        private void EditorButtonClickCallback()
        {
            _saveData = PlayerData._currentGameSave;
            _menuOpen = true;
        }
    }
}