using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    [SerializeField]
    private Terrain terrain;
    [SerializeField]
    ComputeShader sinewaveShader;
    [SerializeField]
    private int waveSpeedX = 10;
    [SerializeField]
    private int waveSpeedY = 10;
    // Start is called before the first frame update
    private float[,] heights;
    private int resolution;
    private Point[] points;
    struct Point
    {
        public int x;
        public int y;
        public float value;
    };

    void Start()
    {
        resolution = terrain.terrainData.heightmapResolution;
        Debug.Log($"resolution = {resolution}");
        heights = new float[resolution, resolution];
        float heightscale = terrain.terrainData.heightmapScale.y;
        heights = terrain.terrainData.GetHeights(0, 0, resolution, resolution);
        points = new Point[resolution * resolution];
        Debug.Log(heightscale);
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                Point point = new Point();
                point.x = x;
                point.y = y;
                point.value = 0;
                points[x * resolution + y] = point;
                heights[x,y] = 0f;
            }
            
        }
        //SineWave();
        StartCoroutine(makeWaves());
        terrain.terrainData.SetHeights(0, 0, heights);
    }

    private IEnumerator makeWaves()
    {
        WaitForSeconds waitTime = new WaitForSeconds(1/60);
        int offsetX = 0;
        int offsetY = 0;
        while (true)
        {
            //SineWave(offsetX+=waveSpeedX, offsetY+=waveSpeedY);
            SineWaveShader(offsetX += waveSpeedX, offsetY += waveSpeedY);
            terrain.terrainData.SetHeights(0, 0, heights);
            yield return waitTime;
        }

       
    }


    void SineWave(int offsetX = 0, int offsetY=0)
    {
        float Yheight;
        float Xheight;
        for (int y = 0; y < resolution; y++)
        {
            Yheight = getWaveHeight(y + offsetY);

            for (int x = 0; x < resolution; x++)
            {
                Xheight = getWaveHeight(x + offsetX);
                heights[x, y] = Yheight + Xheight;
            }

        }
    }

    void SineWaveShader(int offsetX=0,int offsetY=0)
    {
        int kernelindex = sinewaveShader.FindKernel("CSMain");
        int intsize = sizeof(int);
        int floatsize = sizeof(float);
        int totalsize = intsize + intsize + floatsize;
        ComputeBuffer pointsbuffer = new ComputeBuffer(points.Length, totalsize);
        pointsbuffer.SetData(points);
        sinewaveShader.SetBuffer(kernelindex, "points", pointsbuffer);
        sinewaveShader.SetInt("offsetX",offsetX);
        sinewaveShader.SetInt("offsetY", offsetY);

        sinewaveShader.Dispatch(kernelindex, points.Length/256, 1, 1);

        pointsbuffer.GetData(points);
        pointsbuffer.Dispose();

        //read back the data
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                heights[x, y] = points[x * resolution + y].value;
            }

        }
    }

    float getWaveHeight (int position)
    {
        float height;
        height = Mathf.Cos(((float)position / 45f)) * 0.5f;
        height /= 4;
        height += 0.25f;
        return height;
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
