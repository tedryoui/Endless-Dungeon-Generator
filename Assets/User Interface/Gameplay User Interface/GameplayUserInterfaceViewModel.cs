using System;
using System.Collections.Generic;
using Attributes;
using Service.Concrete;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class GameplayUserInterfaceViewModel : MonoBehaviour
{
    [SerializeField]                   private UIDocument _document;
    [SerializeField, DisabledProperty] private Texture2D  _texture2D;
    
    private Image _minimapTexture;

    public void Initialize()
    {
        _minimapTexture = _document.rootVisualElement.Q<Image>("Texture");
    }

    public void RefreshMap(Dictionary<int3, uint> matrix, int2 size)
    {
        _texture2D            = CreateMapTexture(matrix, size);
        _minimapTexture.image = _texture2D;
    }
    
    private Texture2D CreateMapTexture(Dictionary<int3, uint> matrix, int2 size)
    {
        Texture2D texture = new Texture2D(size.x, size.y, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point; 
    
        Color[] pixels = new Color[size.x * size.y];

        int x = 0;
        int y = 0;
        
        foreach (var element in matrix)
        {
            int  index = y * size.x + x;
            uint value = element.Value;
            
            pixels[index] = value == 0 ? Color.clear : Color.white;

            x++;
            if (x >= size.x)
            {
                x = 0;
                y++;
            }
        }
    
        texture.SetPixels(pixels);
        texture.Apply();
    
        return texture;
    }
}
