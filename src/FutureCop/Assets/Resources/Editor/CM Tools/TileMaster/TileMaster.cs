﻿using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Assets.Resources.Editor.CM_Tools.TileMaster
{
    [ExecuteInEditMode]
    public class TileMaster : EditorWindow
    {
        static Texture[] cmTileSets;
        static Sprite[] cmSprites;
        static Sprite[] cmCurSprites;
        static int cmSelectedTileSet;
        static List<int> cmSelectedTile = new List<int>();
        static GameObject cmquad;
        static Texture2D cmSelectedColor;
        static Vector2 tileScrollPosition = Vector2.zero;
        static List<Sprite> cmCurSprite = new List<Sprite>();
        static int curTool;
        static int curMode;
        static GameObject curLayer;
        static int selectedLayer;
        static Vector3 cmCurPos;
        static List<Transform> layers = new List<Transform>();
        static bool highlightLayer;
        static Vector2 drawBox;
        static bool makeCollider;
        //static bool toggleAdvanced = false; //Added it for features later down the line possibly.
        static EditorWindow window;
        static int renameId = -1;
        static Texture2D texVisible;
        static Texture2D texHidden;
        static Color highlightColor = Color.red;
        static Event e;

        [MenuItem("Window/CM Tools/Tile Master")]
        public static void OnEnable()
        {
            //Reset variables chunk. This is for new files being added, generated, etc.
            AssetDatabase.Refresh();
            cmTileSets = new Texture[0];
            cmSprites = new Sprite[0];
            layers.Clear();

            SceneView.onSceneGUIDelegate += OnSceneGUI; //Sets delegate for adding the OnSceneGUI event

            cmTileSets = UnityEngine.Resources.LoadAll<Texture>("Tilesets"); //Load all tilesets as texture
            cmSprites = UnityEngine.Resources.LoadAll<Sprite>("Tilesets"); //Load all tileset sub objects as tiles
            texVisible = UnityEngine.Resources.Load("Editor/CM Tools/TileMaster/Visible") as Texture2D; //Load visible icon
            texHidden = UnityEngine.Resources.Load("Editor/CM Tools/TileMaster/Hidden") as Texture2D; //Load hidden icon

            LoadTileset(0);//processes loaded tiles into proper tilesets

            cmSelectedColor = new Texture2D(1, 1); //makes highlight color for selecting tiles
            cmSelectedColor.alphaIsTransparency = true;
            cmSelectedColor.filterMode = FilterMode.Point;
            cmSelectedColor.SetPixel(0, 0, new Color(.5f, .5f, 1f, .5f));
            cmSelectedColor.Apply();

            window = GetWindow(typeof(TileMaster));//Initialize window
            window.minSize = new Vector2(325, 400);
        }

        static void AddLayer()
        {
            //Creates new layer, renames it, and sets it's proper layer settings
            layers.Add(new GameObject().transform);
            var index = layers.Count - 1;
            layers[index].name = "New Layer";
            layers[index].transform.parent = cmquad.transform;
            var tmpRenderer = layers[index].gameObject.AddComponent<SpriteRenderer>();
            tmpRenderer.sortingOrder = index;
        }

        static void ResetLayers()
        {
            layers.Clear(); //Rebuilds a list of all of the layers.

            foreach (Transform t in cmquad.GetComponentsInChildren(typeof(Transform), true))
            {
                //gets a list of all possible layers, and checks to see if the parent is the main game object for the tiles.
                if (t.parent == cmquad.transform)
                {
                    layers.Add(t);
                }
            }
        }

        static void ResetManager()
        {
            //Intended to create objects required for the tileset editor to work
            if (cmquad == null)
            {
                cmquad = GameObject.Find("TileMasterField"); //Look for my tileset object. If it doesn't exist, create a very large quad.
                if (cmquad == null)
                {
                    cmquad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    cmquad.name = "TileMasterField";
                    cmquad.transform.localScale = new Vector3(1000000, 1000000, 1000000);
                    AddLayer();
                    ResetLayers();
                    curLayer = layers[0].gameObject;
                }

                cmquad.GetComponent<Renderer>().enabled = false; //disable quad's renderer

                EditorUtility.SetSelectedRenderState(cmquad.GetComponent<Renderer>(), EditorSelectedRenderState.Wireframe);
            }
            if (window != null)
            {
                //force repaint to show proper layers if the window exists.
                window.Repaint();
            }
        }

        static void LoadTileset(int tileSetID)
        {
            //loads the tilesets into proper variables
            ResetManager();

            cmCurSprites = new Sprite[cmSprites.Length];

            var curCount = 0;
            var i = 0;

            //sets the displayed tileset based on the name of the tile. Also counts the number of tiles in the new tileset that's loaded.
            for (i = 0; i < cmSprites.Length; i++)
            {
                if (cmSprites[i].texture.name == cmTileSets[tileSetID].name)
                {
                    cmCurSprites[curCount] = cmSprites[i];
                    curCount++;
                }
            }

            //resizes the displayed tileset's array size to match the new size
            var tmpSprite = new Sprite[curCount];
            for (i = 0; i < curCount; i++)
            {
                tmpSprite[i] = cmCurSprites[i];
            }
            cmCurSprites = tmpSprite;
        }


        void OnGUI()
        {
            int i, j;
            ResetManager();//Remakes game objects require to operate.
            e = Event.current; //Gets current event (mouse move, repaint, keyboard press, etc)

            if (renameId != -1 && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter))
            {
                renameId = -1;
            }

            if (cmTileSets == null) //Check to make certain there is actually a tileset in the resources/tileset folder.
            {
                EditorGUILayout.LabelField("No tilesets found. Retrying.");
                OnEnable();
            }
            else
            {
                var names = new string[cmTileSets.Length]; //Gets the name of the tilesets into a useable list
                for (i = 0; i < cmTileSets.Length; i++)
                {
                    try
                    {
                        names[i] = cmTileSets[i].name;
                    }
                    catch (Exception ex)
                    {
                        Debug.Log("There was an error getting the names of the files. We'll try to reload the tilesets. If this continues to show, please close the script and try remimporting and check your images.");
                        Debug.Log("Full system error: " + ex.Message);
                        OnEnable();
                    }
                }

                //Mode variable to swith between major features.
                string[] mode = { "Tile Painter", "Help Video", "Exit" };
                curMode = GUILayout.Toolbar(curMode, mode);

                if (curMode == 0)
                {
                    //If in standard paint mode, display the proper gui elements for the user to use.
                    EditorGUILayout.BeginHorizontal();
                    var tmpInt = EditorGUILayout.Popup("Tileset", cmSelectedTileSet, names);
                    if (GUILayout.Button("Reload"))
                    {
                        OnEnable();
                    }
                    EditorGUILayout.EndHorizontal();

                    string[] tools = { "Paint", "Erase", "Box Paint" };
                    //curTool = EditorGUILayout.Popup("Tool", curTool, tools);

                    EditorGUILayout.BeginHorizontal(GUILayout.Width(position.width));
                    //Causes an error on editor load if the window is visible.
                    //This seems to be a problem with how the gui is drawn the first
                    //loop of the script. It only happens the once, and I can't figure
                    //out why. I've been trying for literally weeks and still can't
                    //find an answer. This is the only known bug, but it doesn't
                    //stop the functionality of the script in any way, and only serves
                    //as an annoying message on unity load or this script being 
                    //recompiled. Sorry for this bug. I am looking for a way to remove
                    //this error, but I really am stummped as to why it's happening
                    //and I just can not find an answer online.


                    EditorGUILayout.LabelField("Tool", GUILayout.Width(50));
                    GUILayout.FlexibleSpace();
                    curTool = GUILayout.Toolbar(curTool, tools);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Paint With Collider", GUILayout.Width(150));
                    makeCollider = EditorGUILayout.Toggle(makeCollider);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Highlight Current Layer", GUILayout.Width(150));
                    highlightLayer = EditorGUILayout.Toggle(highlightLayer, GUILayout.Width(25));
                    highlightColor = EditorGUILayout.ColorField(highlightColor);
                    EditorGUILayout.EndHorizontal();

                    if (tmpInt != cmSelectedTileSet) //Forces selection of first tileset if none are selected.
                    {
                        LoadTileset(tmpInt);
                    }

                    cmSelectedTileSet = tmpInt; //sets the selected tileset value

                    i = 0;
                    var columnCount = Mathf.RoundToInt((position.width) / 38) - 2; //figures out how many columns are required for the tileset
                    j = 0;
                    var current = 0;

                    tileScrollPosition = EditorGUILayout.BeginScrollView(tileScrollPosition, false, true, GUILayout.Width(position.width));
                    //creates scrollbox area for tiles inside of the current tileset.

                    GUILayout.BeginHorizontal(); //Initializing first row

                    for (var q = 0; q < cmSprites.Length; q++)
                    {
                        var child = cmSprites[q];
                        //for every tile inside the currently selected tileset, add a tile
                        try
                        {
                            if (child.texture.name == names[cmSelectedTileSet] && child.name != names[cmSelectedTileSet])
                            {
                                //if it's the tiles inside the image, not the entire image

                                var newRect = new Rect(
                                    child.rect.x / child.texture.width,
                                    child.rect.y / child.texture.height,
                                    child.rect.width / child.texture.width,
                                    child.rect.height / child.texture.height
                                    );//gets the x and y position in pixels of where the image is. Used later for display.

                                if (GUILayout.Button("", GUILayout.Width(34), GUILayout.Height(34)))
                                {
                                    //draw a clickable button
                                    if (cmSelectedTile != null && !e.control)
                                    {
                                        //empty the selected tile list if control isn't held. Allows multiselect of tiles.
                                        cmSelectedTile.Clear();
                                        cmCurSprite.Clear();
                                    }
                                    cmSelectedTile.Add(current); //Adds clicked on tile to list of selected tiles.
                                    cmCurSprite.Add(cmCurSprites[current]);
                                }

                                GUI.DrawTextureWithTexCoords(new Rect(5 + (j * 38), 4 + (i * 37), 32, 32), child.texture, newRect, true); //draws tile base on pixels gotten at the beginning of the loop
                                if (cmSelectedTile != null && cmSelectedTile.Contains(current))
                                {
                                    //if the current tile is inside of the list of selected tiles, draw a highlight indicator over the button.
                                    if (cmSelectedColor == null)
                                    {
                                        cmSelectedColor = new Texture2D(1, 1);
                                        cmSelectedColor.alphaIsTransparency = true;
                                        cmSelectedColor.filterMode = FilterMode.Point;
                                        cmSelectedColor.SetPixel(0, 0, new Color(.5f, .5f, 1f, .5f));
                                        cmSelectedColor.Apply();
                                    }
                                    GUI.DrawTexture(new Rect(5 + (j * 38), 4 + (i * 37), 32, 32), cmSelectedColor, ScaleMode.ScaleToFit, true);
                                }

                                if (j < columnCount)
                                {
                                    j++;
                                }
                                else
                                {
                                    // if we have enough columns to fill the scroll area, reset the column count and start a new line of buttons
                                    j = 0;
                                    i++;
                                    GUILayout.EndHorizontal();
                                    GUILayout.BeginHorizontal();
                                }
                                current++;
                            }
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message.StartsWith("IndexOutOfRangeException"))
                            {
                                Debug.Log("Tileset index was out of bounds, reloading and trying again.");
                                OnEnable();
                                return;
                            }
                        }
                    }
                    GUILayout.EndHorizontal(); //finish the drawing of tiles
                    EditorGUILayout.EndScrollView();
                    //Display all of the layers. May be put into a foldout for if there are too many layers. Haven't decided yet.
                    GUILayout.Label("Layers:");

                    if (GUILayout.Button("Add Layer"))
                    {
                        AddLayer();
                    }
                    string[] minusPlus = { "-", "+", "x", "r" };

                    ResetLayers();
                    layers = ResortLayers(layers);//Sort the layers based on their sorting order instead of name
                    var destroyFlag = -1;
                    for (i = 0; i < layers.Count; i++)
                    {
                        //iterates through layers and displays gui for options.
                        EditorGUILayout.BeginHorizontal();

                        var tmpPadding = GUI.skin.button.padding;
                        GUI.skin.button.padding = new RectOffset(3, 3, 3, 3);

                        if (layers[i].gameObject.activeSelf)
                        {
                            if (GUILayout.Button(texVisible, GUILayout.Width(15), GUILayout.Height(15)))
                            {
                                layers[i].gameObject.SetActive(false);
                            }
                        }
                        else
                        {
                            if (GUILayout.Button(texHidden, GUILayout.Width(15), GUILayout.Height(15)))
                            {
                                layers[i].gameObject.SetActive(true);
                            }
                        }
                        GUI.skin.button.padding = tmpPadding;

                        if (i == selectedLayer)
                        {
                            //if selected layer, draw checked checkbox to show it's selected
                            if (i != renameId)
                            {
                                EditorGUILayout.ToggleLeft(layers[i].name + " - " + layers[i].GetComponent<SpriteRenderer>().sortingOrder, true);
                            }
                            else
                            {
                                layers[i].name = EditorGUILayout.TextField(layers[i].name);
                            }
                        }
                        else
                        {
                            //if not the selected layer, and is clicked, set it as the selected layer
                            if (i != renameId)
                            {
                                if (EditorGUILayout.ToggleLeft(layers[i].name + " - " + layers[i].GetComponent<SpriteRenderer>().sortingOrder, false))
                                {
                                    selectedLayer = i;
                                }
                            }
                            else
                            {
                                layers[i].name = EditorGUILayout.TextField(layers[i].name);
                            }
                        }

                        //sets pressed value to -1 if nothing is pressed.
                        var pressed = GUILayout.Toolbar(-1, minusPlus);

                        switch (pressed)
                        {
                            case 0:
                                if (i > 0)
                                {
                                    //moves layer and all tiles in it to move away from the camera, and moves the layer above it toward the camera.
                                    layers[i - 1].GetComponent<SpriteRenderer>().sortingOrder += 1;
                                    var upLayer = layers[i - 1].GetComponent<SpriteRenderer>().sortingOrder;

                                    foreach (var sr in layers[i - 1].GetComponentsInChildren<SpriteRenderer>())
                                    {
                                        sr.sortingOrder = upLayer;
                                    }

                                    //layers[i].GetComponent<SpriteRenderer>().sortingOrder -= 1;
                                    var downLayer = layers[i].GetComponent<SpriteRenderer>().sortingOrder -= 1;

                                    foreach (var sr in layers[i].GetComponentsInChildren<SpriteRenderer>())
                                    {
                                        sr.sortingOrder = downLayer;
                                    }
                                    selectedLayer = i - 1;
                                }
                                layers = ResortLayers(layers);
                                break;
                            case 1:
                                if (i < layers.Count - 1)
                                {
                                    //moves layer and all tiles in it to move toward the camera, and moves the layer above it away from the camera.
                                    layers[i + 1].GetComponent<SpriteRenderer>().sortingOrder -= 1;
                                    var upLayer = layers[i + 1].GetComponent<SpriteRenderer>().sortingOrder;

                                    foreach (var sr in layers[i + 1].GetComponentsInChildren<SpriteRenderer>())
                                    {
                                        sr.sortingOrder = upLayer;
                                    }

                                    //layers[i].GetComponent<SpriteRenderer>().sortingOrder += 1;
                                    var downLayer = layers[i].GetComponent<SpriteRenderer>().sortingOrder += 1;

                                    foreach (var sr in layers[i].GetComponentsInChildren<SpriteRenderer>())
                                    {
                                        sr.sortingOrder = downLayer;
                                    }
                                    selectedLayer = i + 1;
                                }
                                layers = ResortLayers(layers);
                                break;
                            case 2:
                                //deletes the layer game object, which also deletes all the children
                                destroyFlag = i;
                                break;
                            case 3:
                                if (renameId == -1)
                                {
                                    renameId = i;
                                }
                                else
                                {
                                    renameId = -1;
                                }
                                break;
                            default:
                                //do nothing if a button wasn't pressed (required or I get errors T_T)
                                break;
                        }
                        EditorGUILayout.EndHorizontal(); //end the layer gui
                    }
                    if (selectedLayer <= layers.Count - 1 && selectedLayer > 0)
                    {
                        //double check to make certain a layer of some sort is selected and is in valid range
                        curLayer = layers[selectedLayer].gameObject;
                    }

                    if (selectedLayer <= layers.Count - 1 && layers[selectedLayer] != null)
                    {
                        ResetHighlight(layers[selectedLayer].gameObject, highlightLayer);
                        curLayer = layers[selectedLayer].gameObject;
                    }
                    else
                    {
                        if (layers.Count - 1 > 0 && layers[selectedLayer] != null)
                        {
                            curLayer = layers[selectedLayer].gameObject;
                        }
                        else
                        {

                        }
                    }
                    if (destroyFlag != -1)
                    {
                        DestroyImmediate(layers[destroyFlag].gameObject);
                        return; //Breaks method to not have errors down the line. Forces reload of tilesets to keep inside the bounds of the array.
                    }
                    destroyFlag = -1;
                }
                else if (curMode == 1)
                {
                    curMode = 0;
                    Application.OpenURL("https://www.youtube.com/watch?v=mxy9HdNM-is");
                }
                else if (curMode == 2)
                {
                    curMode = 0;
                    Close();
                }
            }
        }

        static bool IsFileInUse(string filePath)
        {
            try
            {
                //Try to open or create the file
                using (var fs = new FileStream(filePath, FileMode.OpenOrCreate))
                {
                    //We can check whether the file can be read or written by using fs.CanRead or fs.CanWrite here.
                }
                return false;
            }
            catch (IOException ex)
            {
                //check if the message is about file io.
                var message = ex.Message.ToString();
                //Check if the file is in use
                if (message.Contains("The process cannot access the file"))
                    return true;
                else
                    throw;
            }
        }

        static private List<Transform> ResortLayers(List<Transform> layers)
        {
            //sorts layers based on the sorting order
            layers.Sort(delegate (Transform x, Transform y)
            {
                var sortOrderX = x.GetComponent<SpriteRenderer>().sortingOrder;
                var sortOrderY = y.GetComponent<SpriteRenderer>().sortingOrder;
                return sortOrderX.CompareTo(sortOrderY);
            });
            return layers;
        }

        private void ResetHighlight(GameObject layer, bool status)
        {
            //highlights the layer in red if status is true, unhighlights if false
            foreach (var sr in cmquad.GetComponentsInChildren<SpriteRenderer>())
            {
                sr.color = new Color(1, 1, 1, 1);
            }
            foreach (var sr in layers[selectedLayer].GetComponentsInChildren<SpriteRenderer>())
            {
                if (status)
                {
                    sr.color = highlightColor;
                }
                else
                {
                    sr.color = new Color(1, 1, 1, 1);
                }
            }
        }

        private void OnSelectionChange()
        {

            //left over code for selecting a layer if selected in the heiarchy. Left in for if I want to do it again and need reference. Probably doesn't work atm.
            //if(Selection.activeObject != lastSelectedObject && Selection.activeObject != null)
            //{
            //			if(Selection.activeTransform != null)
            //{
            //				if(Selection.activeTransform.parent.name.StartsWith("TileMasterField"))
            //{
            //					string[] tmpStr = Selection.activeObject.name.Split('r');
            //lastSelectedObject = Selection.activeObject;
            //}
            //}
            //}
            Repaint();
        }


        static void OnSceneGUI(SceneView sceneview)
        {
            int i, j;
            if (cmquad != null)
            {
                if (cmquad.transform.childCount <= 0)
                {
                    //double checks there is at least 1 layer inside of cmquad.
                    AddLayer();
                    ResetLayers();
                }

                if (Event.current.type == EventType.layout)
                {
                    HandleUtility.AddDefaultControl(
                        GUIUtility.GetControlID(
                            window.GetHashCode(),
                            FocusType.Passive));
                }

                e = Event.current;

                var ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray.origin, ray.direction, out hit, Mathf.Infinity))
                {
                    //Draw gui elements in scene based on mouse position
                    Handles.BeginGUI();
                    Handles.color = Color.white;
                    Handles.Label(cmCurPos, " ", EditorStyles.boldLabel);

                    if ((cmCurPos.x != drawBox.x || cmCurPos.y != drawBox.y) && curTool == 2)
                    {
                        if (cmCurPos.x >= drawBox.x)
                        {
                            if (cmCurPos.y <= drawBox.y)
                            {
                                Handles.DrawLine(new Vector3(drawBox.x, drawBox.y + 1, 0), new Vector3(cmCurPos.x + 1, drawBox.y + 1, 0));
                                Handles.DrawLine(new Vector3(cmCurPos.x + 1, drawBox.y + 1, 0), new Vector3(cmCurPos.x + 1, cmCurPos.y, 0));
                                Handles.DrawLine(new Vector3(cmCurPos.x + 1, cmCurPos.y, 0), new Vector3(drawBox.x, cmCurPos.y, 0));
                                Handles.DrawLine(new Vector3(drawBox.x, cmCurPos.y, 0), new Vector3(drawBox.x, drawBox.y + 1, 0));
                            }
                            else
                            {
                                Handles.DrawLine(new Vector3(drawBox.x, drawBox.y, 0), new Vector3(cmCurPos.x + 1, drawBox.y, 0));
                                Handles.DrawLine(new Vector3(cmCurPos.x + 1, drawBox.y, 0), new Vector3(cmCurPos.x + 1, cmCurPos.y, 0));
                                Handles.DrawLine(new Vector3(cmCurPos.x + 1, cmCurPos.y, 0), new Vector3(drawBox.x, cmCurPos.y, 0));
                                Handles.DrawLine(new Vector3(drawBox.x, cmCurPos.y, 0), new Vector3(drawBox.x, drawBox.y, 0));
                            }
                        }
                        else
                        {
                            if (cmCurPos.y <= drawBox.y)
                            {
                                Handles.DrawLine(new Vector3(drawBox.x + 1, drawBox.y + 1, 0), new Vector3(cmCurPos.x + 1, drawBox.y + 1, 0));
                                Handles.DrawLine(new Vector3(cmCurPos.x + 1, drawBox.y + 1, 0), new Vector3(cmCurPos.x + 1, cmCurPos.y, 0));
                                Handles.DrawLine(new Vector3(cmCurPos.x + 1, cmCurPos.y, 0), new Vector3(drawBox.x + 1, cmCurPos.y, 0));
                                Handles.DrawLine(new Vector3(drawBox.x + 1, cmCurPos.y, 0), new Vector3(drawBox.x + 1, drawBox.y + 1, 0));
                            }
                            else
                            {
                                Handles.DrawLine(new Vector3(drawBox.x + 1, drawBox.y, 0), new Vector3(cmCurPos.x + 1, drawBox.y, 0));
                                Handles.DrawLine(new Vector3(cmCurPos.x + 1, drawBox.y, 0), new Vector3(cmCurPos.x + 1, cmCurPos.y, 0));
                                Handles.DrawLine(new Vector3(cmCurPos.x + 1, cmCurPos.y, 0), new Vector3(drawBox.x + 1, cmCurPos.y, 0));
                                Handles.DrawLine(new Vector3(drawBox.x + 1, cmCurPos.y, 0), new Vector3(drawBox.x + 1, drawBox.y, 0));
                            }
                        }
                    }
                    else
                    {
                        Handles.DrawLine(cmCurPos + new Vector3(0, 0, 0), cmCurPos + new Vector3(1, 0, 0));
                        Handles.DrawLine(cmCurPos + new Vector3(1, 0, 0), cmCurPos + new Vector3(1, 1, 0));
                        Handles.DrawLine(cmCurPos + new Vector3(1, 1, 0), cmCurPos + new Vector3(0, 1, 0));
                        Handles.DrawLine(cmCurPos + new Vector3(0, 1, 0), cmCurPos + new Vector3(0, 0, 0));
                    }
                    Handles.EndGUI();

                    if (e.isMouse)
                    {
                        if (e.button == 0 && (e.type == EventType.MouseUp || e.type == EventType.MouseDrag))
                        {
                            if (curTool == 0)
                            {
                                var tmpObj = GenerateTile(hit.point.x, hit.point.y);
                                Undo.RegisterCreatedObjectUndo(tmpObj, "Created Tile");
                                var tmpCurSprite = new Sprite[cmCurSprite.Count];
                                cmCurSprite.CopyTo(tmpCurSprite);

                                if (tmpCurSprite.Length > 0)
                                {
                                    tmpObj.GetComponent<SpriteRenderer>().sprite = tmpCurSprite[UnityEngine.Random.Range(0, tmpCurSprite.Length)];
                                    tmpObj.transform.localPosition = new Vector3(Mathf.Floor(hit.point.x) + .5f, Mathf.Floor(hit.point.y) + .5f, layers[selectedLayer].transform.position.z);
                                }
                                else
                                {
                                    Debug.LogWarning("Tile not selected for painting. Please select a tile to paint.");
                                }
                            }
                            else if (curTool == 1)
                            {
                                var tmpObj = layers[selectedLayer].Find("Tile|" + (Mathf.Floor(hit.point.x) + .5f) + "|" + (Mathf.Floor(hit.point.y) + .5f));
                                if (tmpObj != null)
                                {
                                    Undo.DestroyObjectImmediate(tmpObj.gameObject);
                                }
                            }
                            else if (curTool == 2)
                            {

                                if (e.type == EventType.MouseUp)
                                {
                                    Vector2 distance;
                                    bool drawLeft, drawUp;


                                    if (drawBox.x >= hit.point.x)
                                    {
                                        distance.x = drawBox.x - hit.point.x;
                                        drawLeft = true;
                                    }
                                    else
                                    {
                                        distance.x = hit.point.x - drawBox.x;
                                        drawLeft = false;
                                    }

                                    if (drawBox.y >= hit.point.y)
                                    {
                                        distance.y = drawBox.y - hit.point.y;
                                        drawUp = false;
                                    }
                                    else
                                    {
                                        distance.y = hit.point.y - drawBox.y;

                                        distance.y -= 1;
                                        drawUp = true;
                                    }

                                    if (cmCurPos.y > drawBox.y)
                                    {
                                        distance.y -= 1;
                                    }


                                    for (i = 0; i <= distance.x; i++)
                                    {
                                        for (j = 0; j <= Mathf.Ceil(distance.y); j++)
                                        {
                                            var curI = i;
                                            var curJ = j;
                                            if (drawLeft)
                                            {
                                                curI = -curI;
                                            }
                                            if (drawUp)
                                            {
                                                curJ = -curJ;
                                            }
                                            if (cmCurSprite != null)
                                            {
                                                var tmpObj = GenerateTile(drawBox.x + curI, drawBox.y - curJ);
                                                Undo.RegisterCreatedObjectUndo(tmpObj, "Created Tiles in Box");
                                                var tmpCurSprite = new Sprite[cmCurSprite.Count];
                                                cmCurSprite.CopyTo(tmpCurSprite);
                                                tmpObj.GetComponent<SpriteRenderer>().sprite = tmpCurSprite[UnityEngine.Random.Range(0, tmpCurSprite.Length)];
                                                tmpObj.transform.localPosition = new Vector3(Mathf.Floor(drawBox.x + curI) + .5f, Mathf.Floor(drawBox.y - curJ) + .5f, layers[selectedLayer].transform.position.z);
                                            }
                                            else
                                            {
                                                Debug.LogWarning("No tiles selected.");
                                            }
                                        }
                                    }

                                }
                            }
                        }
                        else if (e.button == 0 && e.type == EventType.MouseDown)
                        {
                            drawBox.x = Mathf.Floor(hit.point.x);
                            drawBox.y = Mathf.Floor(hit.point.y);
                        }
                        else if (e.type == EventType.MouseMove)
                        {
                            drawBox.x = Mathf.Floor(hit.point.x);
                            drawBox.y = Mathf.Floor(hit.point.y);
                        }

                    }
                    cmCurPos.x = Mathf.Floor(hit.point.x);
                    cmCurPos.y = Mathf.Floor(hit.point.y);
                    if (curLayer != null)
                    {
                        cmCurPos.z = curLayer.transform.position.z - 1;
                    }
                    else
                    {
                        cmCurPos.z = 0;
                    }
                }
            }
            else
            {
                ResetManager();
            }
            SceneView.RepaintAll();
        }

        void OnDisable()
        {
            SceneView.onSceneGUIDelegate -= OnSceneGUI;
        }

        static GameObject GenerateTile(float x, float y)
        {
            GameObject tmpObj = null;
            if (curLayer != null)
            {
                var children = curLayer.GetComponentsInChildren<Transform>();
                if (children != null)
                {
                    foreach (var current in children)
                    {
                        if (current.name == "Tile|" + (Mathf.Floor(x) + .5f) + "|" + (Mathf.Floor(y) + .5f) && current.parent == curLayer.transform)
                        {
                            tmpObj = current.gameObject;
                        }
                    }
                }
            }
            if (tmpObj == null)
            {
                tmpObj = new GameObject("Tile|" + (Mathf.Floor(x) + .5f) + "|" + (Mathf.Floor(y) + .5f));
                tmpObj.AddComponent<SpriteRenderer>();
            }
            if (selectedLayer > layers.Count - 1)
            {
                selectedLayer = layers.Count - 1;
                ResetLayers();
                layers = ResortLayers(layers);
            }
            tmpObj.transform.parent = layers[selectedLayer];
            tmpObj.GetComponent<SpriteRenderer>().sortingOrder = layers[selectedLayer].GetComponent<SpriteRenderer>().sortingOrder;
            tmpObj.transform.localScale = new Vector3(1.02f, 1.02f, 1);
            var tmpCol = tmpObj.GetComponent<PolygonCollider2D>();
            if (tmpCol == null && makeCollider)
            {
                tmpCol = tmpObj.AddComponent<PolygonCollider2D>();
                Vector2[] points =
                {
                    new Vector2(-.5f,.49f),
                    new Vector2(-.45f,.5f),
                    new Vector2(.45f,.5f),
                    new Vector2(.5f,.49f),
                    new Vector2(.5f,-.45f),
                    new Vector2(.45f,-.5f),
                    new Vector2(-.45f,-.5f),
                    new Vector2(-.5f,-.45f)
                };
                tmpCol.points = points;
            }
            if (tmpCol != null && !makeCollider)
            {
                Undo.DestroyObjectImmediate(tmpCol);
            }
            //Repaint();
            SceneView.RepaintAll();
            return tmpObj;
        }
    }
}