using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class GraphicsBlitTest : MonoBehaviour
{
    [SerializeField]
    private Texture sourceTexture;

    public bool clear;
    

    [SerializeField] private CustomRenderTexture destRenderTexture;
    [SerializeField] private Material mat;
    
    
    private void Awake()
    {
        ClearTexture();
    }

    private void Update()
    {
        
        Graphics.Blit( sourceTexture, destRenderTexture, mat );
        
        if ( clear )
        {
            ClearTexture();
            clear = false;
        }
    }

    private void ClearTexture()
    {
        RenderTexture rt = RenderTexture.active;
        RenderTexture.active = destRenderTexture;
        GL.Clear(false, true, Color.black);
        RenderTexture.active = rt;


    }
}
