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
    
    [SerializeField]
    private Texture sourceTexture2DArray;

    [SerializeField] private CustomRenderTexture destRenderTexture;
    [SerializeField] private Material mat;

    [SerializeField] private ComputeShader compute;
    
    private void Awake()
    {
        Graphics.Blit( sourceTexture, destRenderTexture );
    }

    private void Update()
    {
        
        Graphics.Blit( sourceTexture, destRenderTexture, mat );
        
        if ( clear )
        {
            GL.Clear( true, true, Color.black );
            clear = false;
        }
    }

/*
    private void OnEnable()
    {
        RenderPipelineManager.endFrameRendering += Screen_EndFrameRendering;
    }

    private void OnDisable()
    {
        RenderPipelineManager.endFrameRendering -= Screen_EndFrameRendering;
    }

    private void Screen_EndFrameRendering( ScriptableRenderContext context, Camera[] cams )
    {
        Graphics.Blit( sourceTexture, mat, 0 );
    }
    */
}
