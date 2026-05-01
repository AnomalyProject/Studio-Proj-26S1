#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine.InputSystem;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Nestoras Angelopoulos
/// 
/// Editor tool to export all input bindings used in the project and match them to sprites from the Xelu free icon pack.
/// </summary>
public static class InputIconDatabaseBuilder
{
    private const string DATABASE_PATH = "Assets/Resources/InputIconDatabase.asset";
    private const string XELU_PATH = "Assets/Art/Xelu_Free_Controller&Key_Prompts";

    [MenuItem("Tools/Build Input Icon Database")]
    public static void Build()
    {
        InputIconDatabase db = LoadOrCreateDatabase();
        HashSet<string> bindings = CollectBindings();
        List<Sprite> allSprites = LoadAllSprites(XELU_PATH);

        // Clear any existing data
        db.keyboard.Clear();
        db.xbox.Clear();
        db.playstation.Clear();
        db.switchPro.Clear();

        foreach (string binding in bindings)
        {
            // Keyboard
            if (TryMatchKeyboard(binding, allSprites, out Sprite keySprite))
            {
                db.keyboard.Add(new InputIconDatabase.InputIconMapping
                {
                    controlPath = binding,
                    icon = keySprite
                });
            }

            // Mouse
            if (TryMatchMouse(binding, allSprites, out Sprite mouseSprite))
            {
                db.keyboard.Add(new InputIconDatabase.InputIconMapping
                {
                    controlPath = binding,
                    icon = mouseSprite
                });
            }

            // XBox
            if (TryMatchXbox(binding, allSprites, out Sprite xboxSprite))
            {
                db.xbox.Add(new InputIconDatabase.InputIconMapping
                {
                    controlPath = binding,
                    icon = xboxSprite
                });
            }

            // PlayStation
            if (TryMatchPlayStation(binding, allSprites, out Sprite psSprite))
            {
                db.playstation.Add(new InputIconDatabase.InputIconMapping
                {
                    controlPath = binding,
                    icon = psSprite
                });
            }

            // Switch
            if (TryMatchSwitch(binding, allSprites, out Sprite swSprite))
            {
                db.switchPro.Add(new InputIconDatabase.InputIconMapping
                {
                    controlPath = binding,
                    icon = swSprite
                });
            }
        }

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();

        Debug.Log("Input Icon Database built!");
    }
    private static InputIconDatabase LoadOrCreateDatabase()
    {
        InputIconDatabase db = AssetDatabase.LoadAssetAtPath<InputIconDatabase>(DATABASE_PATH);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<InputIconDatabase>();
            Directory.CreateDirectory("Assets/Resources");
            AssetDatabase.CreateAsset(db, DATABASE_PATH);
        }
        return db;
    }
    private static HashSet<string> CollectBindings()
    {
        InputActionAsset[] assets = Resources.FindObjectsOfTypeAll<InputActionAsset>();
        HashSet<string> result = new HashSet<string>();

        foreach (InputActionAsset asset in assets)
        {
            foreach (InputActionMap map in asset.actionMaps)
            {
                foreach (InputAction action in map.actions)
                {
                    foreach (InputBinding binding in action.bindings)
                    {
                        if (binding.isComposite) continue;

                        string path = binding.effectivePath;
                        if (string.IsNullOrEmpty(path)) continue;

                        result.Add(InputIconDatabase.Normalize(path));
                    }
                }
            }
        }

        return result;
    }
    private static List<Sprite> LoadAllSprites(string root)
    {
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { root });
        return guids.Select(g => AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(g))).Where(s => s != null).ToList();
    }

    #region Mathcing Logic
    // Keyboard & Mouse
    private static readonly Dictionary<string, string> keyboardSpecial = new Dictionary<string, string>()
    {
        { "space", "space" },
        { "enter", "enter" },
        { "escape", "esc" },
        { "leftctrl", "ctrl" },
        { "rightctrl", "ctrl" },
        { "leftshift", "shift" },
        { "rightshift", "shift" },
        { "tab", "tab" }
    };
    private static bool TryMatchKeyboard(string binding, List<Sprite> sprites, out Sprite sprite)
    {
        binding = binding.ToLower();

        if (keyboardSpecial.TryGetValue(binding, out string special))
        {
            sprite = sprites.FirstOrDefault(s => s.name.ToLower().Contains(special));
            return sprite != null;
        }

        sprite = sprites.FirstOrDefault(s =>
        {
            string name = s.name.ToLower();

            if (!name.Contains("_key_")) return false;

            // Extract key part: "f_key_dark" -> "f"
            string[] parts = name.Split('_');
            if (parts.Length < 2) return false;

            string keyName = parts[0]; // "f", "5", etc.

            return keyName == binding;
        });

        return sprite != null;
    }
    private static bool TryMatchMouse(string binding, List<Sprite> sprites, out Sprite sprite)
    {
        Dictionary<string, string> map = new Dictionary<string, string>
        {
            { "leftbutton", "mouse_left" },
            { "rightbutton", "mouse_right" },
            { "middlebutton", "mouse_middle" }
        };

        if (!map.TryGetValue(binding, out string key))
        {
            sprite = null;
            return false;
        }

        sprite = sprites.FirstOrDefault(s =>
            s.name.ToLower().Contains(key)
        );

        return sprite != null;
    }

    // XBox
    private static bool TryMatchXbox(string binding, List<Sprite> sprites, out Sprite sprite)
    {
        Dictionary<string, string> map = new Dictionary<string, string>
        {
            { "buttonsouth", "a" },
            { "buttoneast", "b" },
            { "buttonwest", "x" },
            { "buttonnorth", "y" },

            { "leftshoulder", "lb" },
            { "rightshoulder", "rb" },
            { "lefttrigger", "lt" },
            { "righttrigger", "rt" },

            { "dpad/up", "dpad_up" },
            { "dpad/down", "dpad_down" },
            { "dpad/left", "dpad_left" },
            { "dpad/right", "dpad_right" },

            { "leftstickpress", "left_stick_click" },
            { "rightstickpress", "right_stick_click" },
            { "leftstick", "left_stick" },
            { "rightstick", "right_stick" },
        };

        if (!map.TryGetValue(binding, out string key))
        {
            sprite = null;
            return false;
        }

        sprite = sprites.FirstOrDefault(s =>
        {
            string name = s.name.ToLower();
            return name.Contains("xbox") && (name.EndsWith("_" + key) || name.Contains("_" + key + "_"));
        });

        return sprite != null;
    }

    // PlayStation
    private static bool TryMatchPlayStation(string binding, List<Sprite> sprites, out Sprite sprite)
    {
        Dictionary<string, string> map = new Dictionary<string, string>
        {
            // Face buttons
            { "buttonsouth", "cross" },
            { "buttoneast", "circle" },
            { "buttonwest", "square" },
            { "buttonnorth", "triangle" },
        
            // Shoulders / triggers
            { "leftshoulder", "l1" },
            { "rightshoulder", "r1" },
            { "lefttrigger", "l2" },
            { "righttrigger", "r2" },
        
            // D-pad
            { "dpad/up", "dpad_up" },
            { "dpad/down", "dpad_down" },
            { "dpad/left", "dpad_left" },
            { "dpad/right", "dpad_right" },
        
            // Sticks
            { "leftstickpress", "left_stick_click" },
            { "rightstickpress", "right_stick_click" },
            { "leftstick", "left_stick" },
            { "rightstick", "right_stick" },
        };

        if (!map.TryGetValue(binding, out string key))
        {
            sprite = null;
            return false;
        }

        sprite = sprites.FirstOrDefault(s =>
        {
            string name = s.name.ToLower();
            return IsValidPlayStationSprite(name) && (name.EndsWith("_" + key) || name.Contains("_" + key + "_"));
        });

        return sprite != null;
    }
    private static bool IsValidPlayStationSprite(string name)
    {
        name = name.ToLower();

        // Exclude unwanted variants
        if (name.Contains("move")) return false;
        if (name.Contains("vr")) return false;

        return name.Contains("ps") || name.Contains("playstation");
    }

    // Switch
    private static bool TryMatchSwitch(string binding, List<Sprite> sprites, out Sprite sprite)
    {
        Dictionary<string, string> map = new Dictionary<string, string>
        {
            // Face buttons (swapped layout)
            { "buttonsouth", "b" },
            { "buttoneast", "a" },
            { "buttonwest", "y" },
            { "buttonnorth", "x" },
        
            // Shoulders / triggers
            { "leftshoulder", "l" },
            { "rightshoulder", "r" },
            { "lefttrigger", "zl" },
            { "righttrigger", "zr" },
        
            // D-pad
            { "dpad/up", "dpad_up" },
            { "dpad/down", "dpad_down" },
            { "dpad/left", "dpad_left" },
            { "dpad/right", "dpad_right" },
        
            // Sticks
            { "leftstickpress", "left_stick_click" },
            { "rightstickpress", "right_stick_click" },
            { "leftstick", "left_stick" },
            { "rightstick", "right_stick" },
        };

        if (!map.TryGetValue(binding, out string key))
        {
            sprite = null;
            return false;
        }

        sprite = sprites.FirstOrDefault(s => s.name.ToLower().Contains("switch") && s.name.ToLower().Contains(key));

        return sprite != null;
    }
    #endregion
}
#endif