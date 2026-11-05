using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Visual settings only. Never changes button listeners, game state, cameras or gameplay objects.</summary>
[ExecuteAlways]
public sealed class MainMenuCyberStyle : MonoBehaviour
{
    public MainMenuUILayout layout;
    public MainMenuChromeGraphic cardSurface;
    public MainMenuChromeGraphic playSurface;
    public MainMenuChromeGraphic infoSurface;
    public MainMenuChromeGraphic exitSurface;
    public Image darkenOverlay;
    public bool IsConfigured => layout != null && layout.mainMenuPanel == transform &&
        cardSurface != null && playSurface != null && infoSurface != null && exitSurface != null;

    static readonly Color Cyan=new Color32(91,225,242,255);
    static readonly Color Green=new Color32(30,239,126,255);
    bool applying;
    Vector2 lastSize;

    private void OnEnable(){if(IsConfigured)ApplyVisuals();}
    private void OnRectTransformDimensionsChange(){if(IsConfigured&&!applying)FitCard();}
    private void Update()
    {
        if(!IsConfigured||applying)return;
        if(lastSize!=layout.mainMenuPanel.rect.size)FitCard();
    }
    public void ApplyVisuals()
    {
        if(!IsConfigured||applying)return;
        applying=true;
        try
        {
            Stretch(layout.mainMenuPanel);
            layout.mainMenuPanel.SetAsLastSibling();
            if(layout.menuBackground!=null)
            {
                Stretch(layout.menuBackground.rectTransform);
                layout.menuBackground.color=Color.white;
                layout.menuBackground.raycastTarget=false;
                layout.menuBackground.preserveAspect=false;
                layout.menuBackground.transform.SetAsFirstSibling();
            }
            if(darkenOverlay!=null)
            {
                Stretch(darkenOverlay.rectTransform);
                darkenOverlay.color=new Color(0.015f,0.04f,0.09f,0.62f);
                darkenOverlay.raycastTarget=false;
                darkenOverlay.transform.SetSiblingIndex(1);
            }
            Center(layout.menuCard,Vector2.zero,new Vector2(880f,828f));
            layout.menuCard.SetAsLastSibling();
            var legacyCard=layout.menuCard.GetComponent<Image>();
            if(legacyCard!=null){legacyCard.color=Color.clear;legacyCard.raycastTarget=false;}
            var legacyOutline=layout.menuCard.GetComponent<Outline>();
            if(legacyOutline!=null)legacyOutline.enabled=false;
            Configure(cardSurface,new Color32(5,21,33,240),new Color32(4,15,24,248),Cyan,40f,1.8f,true);
            cardSurface.raycastTarget=false;Stretch(cardSurface.rectTransform);cardSurface.transform.SetAsFirstSibling();

            var title=layout.gameTitleText;
            if(title!=null)
            {
                Center(title.rectTransform,new Vector2(0f,230f),new Vector2(770f,200f));
                title.text="PROTOCOL:\n<size=120%><i><color=#1EEF7E>ESCAPE</color></i></size>";
                title.richText=true;title.fontSize=72f;title.enableAutoSizing=false;
                title.fontStyle=FontStyles.Bold;title.alignment=TextAlignmentOptions.Center;
                title.enableWordWrapping=false;title.overflowMode=TextOverflowModes.Overflow;
                title.characterSpacing=5f;title.lineSpacing=0f;title.color=Color.white;
                title.margin=Vector4.zero;title.raycastTarget=false;title.extraPadding=true;
                var oldOutline=title.GetComponent<Outline>();if(oldOutline!=null)oldOutline.enabled=false;
            }
            var subtitle=layout.subtitleText;
            if(subtitle!=null)
            {
                Center(subtitle.rectTransform,new Vector2(0f,96f),new Vector2(650f,40f));
                subtitle.text="LABYRINTH OVERRIDE SYSTEM";subtitle.fontSize=18f;
                subtitle.fontStyle=FontStyles.Bold;subtitle.alignment=TextAlignmentOptions.Center;
                subtitle.characterSpacing=5f;subtitle.enableAutoSizing=false;subtitle.enableWordWrapping=false;
                subtitle.color=new Color32(118,211,226,255);subtitle.raycastTarget=false;subtitle.margin=Vector4.zero;
            }
            StyleButton(layout.playButton,playSurface,"OYUNA BAŞLA",new Vector2(0f,-42f),new Vector2(620f,100f),true);
            StyleButton(layout.infoButton,infoSurface,"NASIL OYNANIR?",new Vector2(0f,-170f),new Vector2(620f,94f),false);
            StyleButton(layout.exitButton,exitSurface,"ÇIKIŞ",new Vector2(0f,-290f),new Vector2(620f,94f),false);
            FitCard();
        }
        finally{applying=false;}
    }
    void FitCard()
    {
        if(layout==null||layout.mainMenuPanel==null||layout.menuCard==null)return;
        Vector2 size=layout.mainMenuPanel.rect.size;lastSize=size;
        if(size.x<1f||size.y<1f)return;
        if(layout.menuBackground!=null&&layout.menuBackground.sprite!=null)
        {
            Rect source=layout.menuBackground.sprite.rect;
            float aspect=source.width/Mathf.Max(1f,source.height);
            Vector2 cover=size;
            if(size.x/size.y>aspect)cover.y=size.x/aspect;else cover.x=size.y*aspect;
            Center(layout.menuBackground.rectTransform,Vector2.zero,cover);
        }
        float scale=Mathf.Min(1f,Mathf.Min(Mathf.Max(1f,size.x-48f)/880f,Mathf.Max(1f,size.y-48f)/828f));
        layout.menuCard.localScale=new Vector3(scale,scale,1f);
    }
    static void StyleButton(Button button,MainMenuChromeGraphic surface,string text,Vector2 pos,Vector2 size,bool primary)
    {
        if(button==null||surface==null)return;
        Center((RectTransform)button.transform,pos,size);
        var originalImage=button.GetComponent<Image>();
        if(originalImage!=null){originalImage.color=Color.clear;originalImage.raycastTarget=false;}
        Stretch(surface.rectTransform);surface.transform.SetAsFirstSibling();
        Configure(surface,primary?new Color32(36,255,146,255):new Color32(19,43,59,250),
            primary?new Color32(5,180,79,255):new Color32(8,25,37,253),
            primary?new Color32(170,255,215,255):new Color32(76,137,158,255),
            primary?24f:22f,primary?2.4f:1.8f,primary);
        surface.raycastTarget=true;button.targetGraphic=surface;
        var label=button.GetComponentInChildren<TMP_Text>(true);
        if(label!=null)
        {
            Stretch(label.rectTransform);
            label.rectTransform.offsetMin=new Vector2(82f,5f);label.rectTransform.offsetMax=new Vector2(-18f,-5f);
            label.text=text;label.fontStyle=FontStyles.Bold;label.fontSize=primary?36f:31f;
            label.alignment=TextAlignmentOptions.Center;label.color=primary?new Color32(2,40,21,255):new Color32(227,242,247,255);
            label.enableAutoSizing=true;label.fontSizeMin=24f;label.fontSizeMax=primary?36f:31f;
            label.enableWordWrapping=false;label.margin=Vector4.zero;label.raycastTarget=false;label.extraPadding=true;
        }
        ColorBlock colors=button.colors;
        colors.normalColor=Color.white;
        colors.highlightedColor=new Color(1.22f,1.32f,1.26f,1f);
        colors.selectedColor=new Color(1.12f,1.22f,1.18f,1f);
        colors.pressedColor=new Color(0.68f,0.86f,0.78f,1f);
        colors.disabledColor=new Color(0.42f,0.48f,0.5f,0.6f);
        colors.colorMultiplier=1f;colors.fadeDuration=0.12f;
        button.colors=colors;button.transition=Selectable.Transition.ColorTint;
    }
    public static void Configure(MainMenuChromeGraphic g,Color top,Color bottom,Color border,float cut,float width,bool glow)
    {
        if(g==null)return;g.shape=MainMenuChromeGraphic.Shape.Surface;
        g.color=Color.white;g.fillTop=top;g.fillBottom=bottom;g.edge=border;g.corner=cut;g.stroke=width;g.glow=glow;g.SetVerticesDirty();
    }
    public static void Stretch(RectTransform r)
    {if(r==null)return;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.pivot=new Vector2(0.5f,0.5f);r.offsetMin=Vector2.zero;r.offsetMax=Vector2.zero;r.localScale=Vector3.one;}
    public static void Center(RectTransform r,Vector2 pos,Vector2 size)
    {if(r==null)return;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0.5f,0.5f);r.anchoredPosition=pos;r.sizeDelta=size;r.localScale=Vector3.one;}
}
