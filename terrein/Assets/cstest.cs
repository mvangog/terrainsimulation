using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cstest : MonoBehaviour
{
    [SerializeField]
    private ComputeShader computeshader;
    [SerializeField]
    private RenderTexture rendertexture;
    // Start is called before the first frame update
    void Start()
    {
        //rendertexture = new RenderTexture(256, 256, 24);
        //rendertexture.enableRandomWrite = true;
        //rendertexture.Create();

        //computeshader.SetTexture(0, "Result", rendertexture);
        //computeshader.Dispatch(0, rendertexture.width / 8, rendertexture.height / 8, 1);

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
