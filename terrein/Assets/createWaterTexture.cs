using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Threading;

public class createWaterTexture : MonoBehaviour
{
    //settings
    [SerializeField, Range(0, 30000)]
    private int targetWaterdrops=50;
    [SerializeField, Range(1f, 50f)]
    private float waterdropSize=5f; //waterdropSize in mm

    [SerializeField,Range(0f, 0.1f)]
    private float evaporationRate=0.01f; //percentage per iteration
    [SerializeField]
    private Terrain terrain;

    private Texture2D waterTexture;
    [SerializeField]
    private Color waterColor;
    [SerializeField]
    private Color nonWater;
    [SerializeField]
    private Material waterMaterial;

    private bool autorun = false;
    public struct waterPoint
    {
        public int x;
        public int y;
        public float groundElevation;
        public float waterElevation;
        public float volume;
        public bool HasWater;
    }
    private float[,] heights;

    private List<waterPoint> waterpoints;
    private int textureSize;
    // Start is called before the first frame update
    void Start()
    {
        // load terrainHeighMap
        int resolution = terrain.terrainData.heightmapResolution;
        Debug.Log($"resolution = {resolution}");
        heights = terrain.terrainData.GetHeights(0, 0, resolution, resolution);
        float heightscale = terrain.terrainData.heightmapScale.y;
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                heights[x, y] = heights[x,y]*heightscale;
            }

        }

        //set up the texture
        textureSize = resolution-1;
        waterTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32,false);
        for (int x = 0; x < textureSize; x++)
        {
            for (int y = 0; y < textureSize; y++)
            {
                waterTexture.SetPixel(x, y, nonWater);
            }
        }
        waterpoints = new List<waterPoint>();
        CreateRandomWaterPoints(targetWaterdrops);
        DrawWaterpoints();

       
       
        
    }

    private void CreateRandomWaterPoints(int count)
    {
        
        for (int i = 0; i < count; i++)
        {
            waterPoint waterpoint = new waterPoint();
            int pointX = (int)(Random.value * (textureSize - 1));
            int pointY = (int)(Random.value * (textureSize - 1));
            waterpoint.x = pointX;
            waterpoint.y = pointY;
            waterpoint.HasWater = true;
            waterpoint.volume = waterdropSize;
            waterpoint.groundElevation = heights[pointX, pointY];
            waterpoints.Add(waterpoint);
        }
    }

    private void DrawWaterpoints()
    {
        waterPoint waterpoint;
        for (int i = 0; i < waterpoints.Count; i++)
        {
            waterpoint = waterpoints[i];
            Color pixelColor = waterColor;
            if (waterpoint.HasWater==false)
            {
                pixelColor = nonWater;
            }
            waterTexture.SetPixel(waterpoint.y, waterpoint.x, pixelColor);
        }
        waterTexture.Apply();
        waterMaterial.mainTexture = waterTexture;
    }

    public void MoveWater()
    {
        waterpoints = waterpoints.Where(x => x.HasWater == true).ToList();
        //spawn in new waterdroplets
        float currentVolume = 0;
        if (waterpoints.Count > 0)
        {
            currentVolume = waterpoints.Average(x => x.volume) * waterpoints.Count;
        }
        float maxVolume = targetWaterdrops * waterdropSize;
        int waterspawncount = (int)((maxVolume - currentVolume) / waterdropSize);
        CreateRandomWaterPoints(waterspawncount);

        waterpoints = waterpoints.OrderBy(x => x.groundElevation).ToList();
        waterpoints.Reverse();
        for (int i = waterpoints.Count - 1; i >= 0; i--)
        {

            waterPoint waterpoint = waterpoints[i];
            int centerpointX = waterpoint.x;
            int centerpointY = waterpoint.y;
            float centerElevation = waterpoint.groundElevation;
            float steepestSlope = 0;
            float lowestpoint = waterpoint.groundElevation;

            int lowestNeighbourX = waterpoint.x;
            int lowestNeighbourY = waterpoint.y;
            float distance = 1;

            float DistanceX = 0;
            float DistanceY = 0;
            for (int pointX = waterpoint.x - 1; pointX < waterpoint.x + 2; pointX++)
            {
                DistanceX = (pointX - centerpointX) * (pointX - centerpointX);
                for (int pointY = waterpoint.y - 1; pointY < waterpoint.y + 2; pointY++)
                {
                    if (pointX == centerpointX && pointY==centerpointY)
                    {
                        continue;
                    }
                    float pointElevation = getElevation(pointX, pointY);
                    DistanceY = (pointY - centerpointY) * (pointY - centerpointY);
                    distance = Mathf.Sqrt(DistanceX+DistanceY);
                    float slope = (centerElevation - pointElevation) / distance;
                    if (slope > steepestSlope)
                    {
                        lowestpoint = pointElevation;
                        lowestNeighbourX = pointX;
                        lowestNeighbourY = pointY;
                        steepestSlope = slope;
                    } 
                 }
            }
            waterpoint.HasWater = false;
            waterpoints[i] = waterpoint;

            if (waterpoint.volume>evaporationRate&& waterpoint.x>0 && waterpoint.x<textureSize && waterpoint.y>0 && waterpoint.y<textureSize)
            {
                waterPoint newWaterpoint = new waterPoint();
                newWaterpoint.x = lowestNeighbourX;
                newWaterpoint.y = lowestNeighbourY;
                newWaterpoint.groundElevation = lowestpoint;
                newWaterpoint.volume = waterpoint.volume - evaporationRate;
                newWaterpoint.HasWater = true;
                waterpoints.Add(newWaterpoint);
            }
           
            
        }
        DrawWaterpoints();

    }

    private float getElevation (int x, int y)
    {
        if (x>-1 && x<textureSize && y>-1 && y< textureSize)
        {
            return heights[x, y];
        }
        return float.MaxValue;
    }

 

    public void Autorun()
    {
        autorun = !autorun;
        if (autorun)
        {
            StartCoroutine(runAutomatically());
        }

    }
    private IEnumerator runAutomatically()
    {
        while (autorun)
        {
            MoveWater();
            yield return new WaitForSeconds(0.1f);
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
