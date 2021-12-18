using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Water : MonoBehaviour
{
    //settings
    [SerializeField, Range(0, 30000)]
    private int targetWaterdrops = 5000;
    [SerializeField, Range(1f, 50f)]
    private float waterdropSize = 5f; //waterdropSize in mm
    [SerializeField, Range(0f, 0.1f)]
    private float evaporationRate = 0.01f; //mm per iteration
    [SerializeField]
    private Terrain terrain;



    
    [SerializeField]
    private Color waterColor;
    [SerializeField]
    private Color nonWater;
    [SerializeField]
    private Material waterMaterial;

    private bool autorun = false;
    public List<TerrainPoint> terrainPoints;
    private int terrainSize;
    private Texture2D waterTexture;
    private Color[] basePixels;
    private float[,] heights;
    private float heightScale;
    // Start is called before the first frame update
    void Start()
    {
        loadTerrain();
        CreateTexture();
        AddWater(targetWaterdrops);
        DrawWater();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void loadTerrain()
    {
        terrainPoints = new List<TerrainPoint>();
        // load terrainHeighMap

        terrainSize = terrain.terrainData.heightmapResolution;
        Debug.Log($"resolution = {terrainSize}");
        heights = terrain.terrainData.GetHeights(0, 0, terrainSize, terrainSize);
        heightScale = terrain.terrainData.heightmapScale.y;
        for (int y = 0; y < terrainSize; y++)
        {
            for (int x = 0; x < terrainSize; x++)
            {
                TerrainPoint terrainPoint = new TerrainPoint();
                terrainPoint.x = x;
                terrainPoint.y = y;
                terrainPoint.terrainElevation = heights[x, y] * heightScale;
                terrainPoint.waterElevation = terrainPoint.terrainElevation;
                terrainPoints.Add(terrainPoint);
            }

        }
    }
    private void CreateTexture()
    {
        int textureSize = terrain.terrainData.baseMapResolution;
        waterTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        for (int x = 0; x < textureSize; x++)
        {
            for (int y = 0; y < textureSize; y++)
            {
                waterTexture.SetPixel(x, y, nonWater);
            }
        }
        waterTexture.Apply();
        waterMaterial.mainTexture = waterTexture;
        basePixels = waterTexture.GetPixels();
    }

    public void AddWater(int dropCount)
    {
        for (int i = 0; i < dropCount; i++)
        {
            int randomTerrainPoint = Random.Range(0, terrainPoints.Count-1);
            TerrainPoint terrainPoint = terrainPoints[randomTerrainPoint];
            terrainPoint.waterElevation+= (waterdropSize / 1000);
            terrainPoints[randomTerrainPoint] = terrainPoint;
        }
    }

    private void DrawWater()
    {
        waterTexture.SetPixels(basePixels);
        TerrainPoint[] waterPoints = terrainPoints.Where(x => x.waterElevation > x.terrainElevation).ToArray();
        for (int i = 0; i < waterPoints.Count(); i++)
        {
            waterTexture.SetPixel(waterPoints[i].y, waterPoints[i].x, waterColor);
        }
        waterTexture.Apply();
    }

    private void MoveWater()
    {
        // find all the points with water at the start of the iteration and order them from low to high;
        TerrainPoint[] waterPoints = terrainPoints.Where(x => x.waterElevation > x.terrainElevation).OrderBy(x=>x.waterElevation).ToArray();
        waterPoints.Reverse();
        foreach (TerrainPoint terrainPoint in waterPoints)
        {
            int lowestNeighbourIndex = GetLowestNeighbourIndex(terrainPoint);
            int currentTerrainIndex = GetTerrainPointsIndex(terrainPoint.x, terrainPoint.y);
            if (lowestNeighbourIndex== currentTerrainIndex)
            {
                continue;
            }
            if (lowestNeighbourIndex==-1)
            {
                terrainPoint.waterElevation = terrainPoint.terrainElevation;
                terrainPoints[currentTerrainIndex] = terrainPoint;
                continue;
            }
            ShareWater(terrainPoint, terrainPoints[lowestNeighbourIndex]);
        }

    }

    int GetLowestNeighbourIndex(TerrainPoint terrainPoint)
    {
        
        int centerpointX = terrainPoint.x;
        int centerpointY = terrainPoint.y;
        float centerElevation = terrainPoint.waterElevation;
        float steepestSlope = 0;
        float lowestpoint = terrainPoint.waterElevation;

        int lowestNeighbourX = terrainPoint.x;
        int lowestNeighbourY = terrainPoint.y;
        float distance = 1;

        float DistanceX = 0;
        float DistanceY = 0;
        for (int pointX = terrainPoint.x - 1; pointX < terrainPoint.x + 2; pointX++)
        {
            DistanceX = (pointX - centerpointX) * (pointX - centerpointX);
            for (int pointY = terrainPoint.y - 1; pointY < terrainPoint.y + 2; pointY++)
            {
                if (pointX == centerpointX && pointY == centerpointY)
                {
                    continue;
                }
                float pointElevation = getWaterElevation(pointX, pointY);

                DistanceY = (pointY - centerpointY) * (pointY - centerpointY);
                distance = Mathf.Sqrt(DistanceX + DistanceY);
                float slope = (centerElevation - pointElevation) / distance;
                if (slope > steepestSlope)
                {
                    lowestpoint = pointElevation;
                    lowestNeighbourX = pointX;
                    lowestNeighbourY = pointY;
                    steepestSlope = slope;
                }
                if (pointX < 1 || pointX >= terrainSize || pointY < 1 || pointY >= terrainSize)
                {
                    return -1;
                }
            }
            
        }

        return GetTerrainPointsIndex(lowestNeighbourX, lowestNeighbourY);
    }

    private float getWaterElevation(int x, int y)
    {
        if (x > -1 && x < terrainSize && y > -1 && y < terrainSize)
        {
            return terrainPoints[GetTerrainPointsIndex(x, y)].waterElevation;
        }

        return float.MinValue;
    }
    private int GetTerrainPointsIndex(int x, int y)
    {
        return (y * terrainSize) + x;
    }

    private void ShareWater(TerrainPoint firstTerrainPoint, TerrainPoint secondTerrainPoint)
    {

        TerrainPoint lowestTerrain = firstTerrainPoint;
        TerrainPoint highestTerrain = secondTerrainPoint;
        if (lowestTerrain.terrainElevation>highestTerrain.terrainElevation)
        {
            lowestTerrain = secondTerrainPoint;
            highestTerrain = firstTerrainPoint;
        }

        float lowestTileCapacity = highestTerrain.terrainElevation - lowestTerrain.terrainElevation;
        float combinedVolume = (firstTerrainPoint.waterElevation - firstTerrainPoint.terrainElevation) + (secondTerrainPoint.waterElevation - secondTerrainPoint.terrainElevation);
        float volumeForSharing = combinedVolume - lowestTileCapacity;
        if (volumeForSharing<0) // it all fits in the second tile below the terrain of the first tile
        {
            lowestTerrain.waterElevation = lowestTerrain.terrainElevation + ( combinedVolume);
            highestTerrain.waterElevation = highestTerrain.terrainElevation;
        }
        else
        {
            lowestTerrain.waterElevation = lowestTerrain.terrainElevation + lowestTileCapacity + (0.5f * volumeForSharing);
            highestTerrain.waterElevation = highestTerrain.terrainElevation+ (0.5f * volumeForSharing);
        }
        int lowestTerrainIndex = GetTerrainPointsIndex(lowestTerrain.x, lowestTerrain.y);
        int highestTerrainIndex = GetTerrainPointsIndex(highestTerrain.x, highestTerrain.y);
        terrainPoints[lowestTerrainIndex] = lowestTerrain;
        terrainPoints[highestTerrainIndex] = highestTerrain;

    }

    public void NextIteration()
    {
        MoveWater();
        DrawWater();
        Evaporate();
        UpdateTerrain();
    }

    public void AutoRun()
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
            NextIteration();
            yield return null ;
        }
    }
    private void UpdateTerrain()
    {
        foreach (TerrainPoint terrainPoint in terrainPoints)
        {
            heights[terrainPoint.x, terrainPoint.y] = terrainPoint.waterElevation / heightScale;
        }
        terrain.terrainData.SetHeights(0, 0, heights);
    }

    private void Evaporate()
    {
        TerrainPoint[] waterPoints = terrainPoints.Where(x => x.waterElevation > x.terrainElevation).ToArray();
        foreach (TerrainPoint terrainPoint in waterPoints)
        {
            terrainPoint.waterElevation -= (evaporationRate/1000);
            if (terrainPoint.waterElevation<terrainPoint.terrainElevation)
            {
                terrainPoint.waterElevation = terrainPoint.terrainElevation;
            }
            int currentTerrainIndex = GetTerrainPointsIndex(terrainPoint.x, terrainPoint.y);
            terrainPoints[currentTerrainIndex] = terrainPoint;
        }
    }
}
