using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Opt-in menu-only editor operations. Never enters Play Mode or saves the original scene automatically.</summary>
public static class MainMenuDesignTools
{
    const string ScenePath = "Assets/Scenes/BirlesikOyun.unity";
    const string SnapshotPath = "Assets/UI/Generated/MainMenuMapBlur.png";
    const string ApplyMenu = "Protocol Escape/Main Menu/Apply Cybersecurity Design";
    const string RefreshMenu = "Protocol Escape/Main Menu/Refresh Real Map Backdrop";

    [MenuItem(ApplyMenu)]
    public static void Apply()
    {
        MainMenuUILayout layout;
        try { layout = ValidateScene(); }
        catch(Exception e) { Debug.LogError("Main menu: " + e.Message); return; }

        int undoGroup=-1;
        try
        {
            string backup=BackupScene(layout.gameObject.scene);
            // A real camera capture must succeed before the existing UI is changed.
            Sprite backdrop=CaptureMap(layout.gameObject.scene);
            string[] listeners=PersistentListeners(layout);
            Undo.IncrementCurrentGroup();undoGroup=Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Apply Protocol Escape main menu");
            Undo.RecordObject(layout.mainMenuPanel.parent,"Menu stacking order");
            Undo.RegisterFullObjectHierarchyUndo(layout.mainMenuPanel.gameObject,"Style main menu");
            Undo.RecordObject(layout,"Keep menu references");

            var style=layout.mainMenuPanel.GetComponent<MainMenuCyberStyle>();
            if(style==null)style=Undo.AddComponent<MainMenuCyberStyle>(layout.mainMenuPanel.gameObject);
            Undo.RecordObject(style,"Configure menu style");
            style.layout=layout;
            style.cardSurface=Graphic("CyberMenuCardSurface",layout.menuCard);
            style.playSurface=Graphic("CyberMenuButtonSurface",layout.playButton.transform);
            style.infoSurface=Graphic("CyberMenuButtonSurface",layout.infoButton.transform);
            style.exitSurface=Graphic("CyberMenuButtonSurface",layout.exitButton.transform);

            var veil=Image("CyberMenuDarken",layout.mainMenuPanel);
            style.darkenOverlay=veil;
            if(layout.menuBackground!=null)
            {
                Undo.RecordObject(layout.menuBackground,"Set real map backdrop");
                layout.menuBackground.sprite=backdrop;
            }

            Decor("CyberMenuTopSlashes",layout.menuCard,MainMenuChromeGraphic.Shape.Slashes,
                new Vector2(-300f,379f),new Vector2(145f,18f),new Color32(42,200,223,255));
            Decor("CyberMenuBottomSlashes",layout.menuCard,MainMenuChromeGraphic.Shape.Slashes,
                new Vector2(300f,-379f),new Vector2(145f,18f),new Color32(42,200,223,255));
            Decor("CyberMenuTitleCornerLeft",layout.menuCard,MainMenuChromeGraphic.Shape.Corner,
                new Vector2(-330f,263f),new Vector2(29f,29f),new Color32(20,230,126,255));
            var corner=Decor("CyberMenuTitleCornerRight",layout.menuCard,MainMenuChromeGraphic.Shape.Corner,
                new Vector2(330f,165f),new Vector2(29f,29f),new Color32(20,230,126,255));
            corner.rectTransform.localRotation=Quaternion.Euler(0f,0f,180f);
            Decor("CyberMenuPlayIcon",layout.playButton.transform,MainMenuChromeGraphic.Shape.PlayIcon,
                new Vector2(-218f,0f),new Vector2(30f,34f),new Color32(2,67,34,255));
            Decor("CyberMenuInfoIcon",layout.infoButton.transform,MainMenuChromeGraphic.Shape.BookIcon,
                new Vector2(-218f,0f),new Vector2(40f,38f),new Color32(227,242,247,255));
            Decor("CyberMenuExitIcon",layout.exitButton.transform,MainMenuChromeGraphic.Shape.PowerIcon,
                new Vector2(-218f,0f),new Vector2(38f,38f),new Color32(227,242,247,255));

            style.ApplyVisuals();
            if(!listeners.SequenceEqual(PersistentListeners(layout)))
                throw new InvalidOperationException("Button OnClick references changed unexpectedly.");
            EditorUtility.SetDirty(style);
            EditorSceneManager.MarkSceneDirty(layout.gameObject.scene);
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject=layout.mainMenuPanel.gameObject;
            Debug.Log("Main menu ready. Real map snapshot: "+SnapshotPath+
                "\nSaved-scene backup: "+backup+
                "\nThe original scene has NOT been saved automatically. Inspect the menu, then save the scene.");
        }
        catch(Exception e)
        {
            if(undoGroup>=0)Undo.RevertAllDownToGroup(undoGroup);
            Debug.LogError("Main menu design was not completed; UI changes reverted.\n"+e);
        }
    }

    [MenuItem(RefreshMenu)]
    public static void RefreshBackdrop()
    {
        try
        {
            var layout=ValidateScene();
            var style=layout.mainMenuPanel.GetComponent<MainMenuCyberStyle>();
            if(style==null||!style.IsConfigured)
                throw new InvalidOperationException("Apply Cybersecurity Design first.");
            string backup=BackupScene(layout.gameObject.scene);
            Sprite sprite=CaptureMap(layout.gameObject.scene);
            Undo.RecordObject(layout.menuBackground,"Refresh real map backdrop");
            layout.menuBackground.sprite=sprite;
            EditorSceneManager.MarkSceneDirty(layout.gameObject.scene);
            Debug.Log("Real map backdrop refreshed. Scene remains unsaved. Backup: "+backup);
        }
        catch(Exception e){Debug.LogError("Map backdrop was not refreshed.\n"+e);}
    }

    [MenuItem(ApplyMenu,true)]
    [MenuItem(RefreshMenu,true)]
    static bool CanRun(){return !EditorApplication.isPlayingOrWillChangePlaymode;}

    static MainMenuUILayout ValidateScene()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode manually before using this menu tool.");
        Scene scene=SceneManager.GetActiveScene();
        if(!scene.IsValid()||scene.path!=ScenePath)
            throw new InvalidOperationException("Open Assets/Scenes/BirlesikOyun.unity as the active scene.");
        var layouts=Resources.FindObjectsOfTypeAll<MainMenuUILayout>()
            .Where(x=>x.gameObject.scene==scene&&!EditorUtility.IsPersistent(x)).ToArray();
        if(layouts.Length!=1)throw new InvalidOperationException("Exactly one MainMenuUILayout is required in the active scene.");
        var layout=layouts[0];
        if(layout.mainMenuPanel==null||layout.menuBackground==null||layout.menuCard==null||
            layout.gameTitleText==null||layout.subtitleText==null||layout.playButton==null||
            layout.infoButton==null||layout.exitButton==null||layout.howToPlayPanel==null)
            throw new InvalidOperationException("Existing menu references are incomplete; no UI has been changed.");
        foreach(var button in new[]{layout.playButton,layout.infoButton,layout.exitButton})
            if(!button.transform.IsChildOf(layout.mainMenuPanel))
                throw new InvalidOperationException("All three existing buttons must belong to MainMenuPanel.");
        if(!layout.menuCard.IsChildOf(layout.mainMenuPanel)||!layout.menuBackground.transform.IsChildOf(layout.mainMenuPanel))
            throw new InvalidOperationException("Menu card and backdrop must belong to MainMenuPanel.");
        if(GraphicsSettings.currentRenderPipeline!=null)
            throw new InvalidOperationException("This capture command targets the current Built-in pipeline; no render package will be installed.");
        return layout;
    }

    static string BackupScene(Scene scene)
    {
        string folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments),
            "ProtocolEscape_Menu_Backups",DateTime.Now.ToString("yyyyMMdd_HHmmss_fff"));
        Directory.CreateDirectory(folder);
        string source=ProjectPath(scene.path);
        File.Copy(source,Path.Combine(folder,Path.GetFileName(source)),false);
        if(File.Exists(ProjectPath(SnapshotPath)))
            File.Copy(ProjectPath(SnapshotPath),Path.Combine(folder,"MainMenuMapBlur.previous.png"),false);
        File.WriteAllText(Path.Combine(folder,"README.txt"),
            "Backup of the saved scene before the menu command. Unsaved scene UI is protected by Unity Undo.\r\n"+
            "Project: "+Path.GetFullPath(Path.Combine(Application.dataPath,".."))+"\r\nScene: "+scene.path);
        return folder;
    }

    static Sprite CaptureMap(Scene scene)
    {
        var cameras=Resources.FindObjectsOfTypeAll<Camera>()
            .Where(c=>c.gameObject.scene==scene&&!EditorUtility.IsPersistent(c)&&c.CompareTag("MainCamera")).ToArray();
        if(cameras.Length!=1)
            throw new InvalidOperationException("Exactly one existing MainCamera is required for a real map image.");
        Camera source=cameras[0]; GameObject temporary=null; RenderTexture rt=null; Texture2D pixels=null;
        RenderTexture previous=RenderTexture.active;
        byte[] png;
        try
        {
            temporary=new GameObject("ProtocolEscapeMenuCapture");
            temporary.hideFlags=HideFlags.HideAndDontSave;
            var capture=temporary.AddComponent<Camera>();
            capture.CopyFrom(source);capture.enabled=false;
            capture.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);
            capture.aspect=16f/9f;capture.rect=new Rect(0f,0f,1f,1f);
            int uiLayer=LayerMask.NameToLayer("UI");
            if(uiLayer>=0)capture.cullingMask&=~(1<<uiLayer);
            rt=RenderTexture.GetTemporary(960,540,24,RenderTextureFormat.ARGB32);
            capture.targetTexture=rt;
            capture.Render(); // Copies the real scene view without enabling or modifying the source camera.
            RenderTexture.active=rt;
            pixels=new Texture2D(960,540,TextureFormat.RGBA32,false);
            pixels.ReadPixels(new Rect(0,0,960,540),0,0,false);
            Color32[] originalPixels=pixels.GetPixels32();
            if(!MainMenuMapBlur.HasVisibleContent(originalPixels))
                throw new InvalidOperationException("The scene camera produced a blank image. Make the real map visible and retry; no placeholder will be generated.");
            pixels.SetPixels32(MainMenuMapBlur.Blur(originalPixels,960,540,5,3));
            pixels.Apply(false,false);png=pixels.EncodeToPNG();
            capture.targetTexture=null;
        }
        finally
        {
            RenderTexture.active=previous;
            if(temporary!=null)Object.DestroyImmediate(temporary);
            if(pixels!=null)Object.DestroyImmediate(pixels);
            if(rt!=null)RenderTexture.ReleaseTemporary(rt);
        }
        if(png==null||png.Length==0)throw new InvalidOperationException("Real map capture failed; no placeholder was generated.");
        EnsureFolder("Assets/UI/Generated");
        File.WriteAllBytes(ProjectPath(SnapshotPath),png);
        AssetDatabase.ImportAsset(SnapshotPath,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
        var importer=(TextureImporter)AssetImporter.GetAtPath(SnapshotPath);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.mipmapEnabled=false;importer.alphaIsTransparency=false;importer.sRGBTexture=true;
        importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(SnapshotPath);
        if(sprite==null)throw new InvalidOperationException("Captured map could not be imported as a UI sprite.");
        return sprite;
    }

    static string ProjectPath(string assetPath)
    { return Path.GetFullPath(Path.Combine(Application.dataPath,"..",assetPath)); }

    static void EnsureFolder(string path)
    {
        string[] parts=path.Split('/');string parent=parts[0];
        for(int i=1;i<parts.Length;i++)
        {
            string next=parent+"/"+parts[i];
            if(!AssetDatabase.IsValidFolder(next))AssetDatabase.CreateFolder(parent,parts[i]);
            parent=next;
        }
    }
    static GameObject Child(string name,Transform parent)
    {
        Transform found=parent.Find(name);
        if(found!=null)return found.gameObject;
        var go=new GameObject(name,typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go,"Create menu decoration");
        go.layer=parent.gameObject.layer;go.transform.SetParent(parent,false);return go;
    }
    static MainMenuChromeGraphic Graphic(string name,Transform parent)
    {
        var go=Child(name,parent);var graphic=go.GetComponent<MainMenuChromeGraphic>();
        if(graphic==null)graphic=Undo.AddComponent<MainMenuChromeGraphic>(go);
        Undo.RecordObject(graphic,"Style menu geometry");
        Undo.RecordObject(graphic.rectTransform,"Place menu geometry");
        graphic.raycastTarget=false;return graphic;
    }
    static Image Image(string name,Transform parent)
    {
        var go=Child(name,parent);var image=go.GetComponent<Image>();
        if(image==null)image=Undo.AddComponent<Image>(go);
        Undo.RecordObject(image,"Style menu veil");
        Undo.RecordObject(image.rectTransform,"Place menu veil");image.raycastTarget=false;return image;
    }
    static MainMenuChromeGraphic Decor(string name,Transform parent,MainMenuChromeGraphic.Shape shape,Vector2 position,Vector2 size,Color tint)
    {
        var g=Graphic(name,parent);g.shape=shape;g.color=Color.white;g.edge=tint;
        g.stroke=shape==MainMenuChromeGraphic.Shape.PowerIcon?3f:2.5f;g.glow=false;
        MainMenuCyberStyle.Center(g.rectTransform,position,size);g.raycastTarget=false;g.SetVerticesDirty();return g;
    }
    static string[] PersistentListeners(MainMenuUILayout layout)
    {
        var result=new List<string>();
        foreach(var button in new[]{layout.playButton,layout.infoButton,layout.exitButton})
            for(int i=0;i<button.onClick.GetPersistentEventCount();i++)
            {
                Object target=button.onClick.GetPersistentTarget(i);
                result.Add(button.GetInstanceID()+":"+i+":"+(target!=null?target.GetInstanceID():0)+":"+
                    button.onClick.GetPersistentMethodName(i)+":"+button.onClick.GetPersistentListenerState(i));
            }
        return result.ToArray();
    }
}

/// <summary>Separable box blur on genuine captured pixels, editor-only and bounded to one snapshot.</summary>
public static class MainMenuMapBlur
{
    public static bool HasVisibleContent(Color32[] pixels)
    {
        if(pixels==null||pixels.Length==0)return false;
        int min=255,max=0;
        foreach(Color32 p in pixels)
        {
            int value=(p.r+p.g+p.b)/3;
            min=Math.Min(min,value);max=Math.Max(max,value);
        }
        return max-min>=8;
    }
    public static Color32[] Blur(Color32[] input,int width,int height,int radius,int passes)
    {
        if(input==null||width<1||height<1||input.Length!=width*height||radius<0||radius>32||passes<1||passes>4)
            throw new ArgumentException("Invalid bounded menu blur input.");
        var a=(Color32[])input.Clone();var b=new Color32[a.Length];
        for(int p=0;p<passes;p++){Pass(a,b,width,height,radius,true);Pass(b,a,width,height,radius,false);}
        return a;
    }
    static void Pass(Color32[] source,Color32[] target,int width,int height,int radius,bool horizontal)
    {
        int length=horizontal?width:height,lines=horizontal?height:width,kernel=radius*2+1;
        for(int line=0;line<lines;line++)
        {
            int r=0,g=0,b=0,a=0;
            for(int k=-radius;k<=radius;k++)
            {
                Color32 c=source[Index(line,Clamp(k,0,length-1),width,horizontal)];
                r+=c.r;g+=c.g;b+=c.b;a+=c.a;
            }
            for(int i=0;i<length;i++)
            {
                target[Index(line,i,width,horizontal)]=new Color32((byte)(r/kernel),(byte)(g/kernel),(byte)(b/kernel),(byte)(a/kernel));
                Color32 leave=source[Index(line,Clamp(i-radius,0,length-1),width,horizontal)];
                Color32 enter=source[Index(line,Clamp(i+radius+1,0,length-1),width,horizontal)];
                r+=enter.r-leave.r;g+=enter.g-leave.g;b+=enter.b-leave.b;a+=enter.a-leave.a;
            }
        }
    }
    static int Index(int line,int index,int width,bool horizontal){return horizontal?line*width+index:index*width+line;}
    static int Clamp(int value,int min,int max){return value<min?min:value>max?max:value;}
}
