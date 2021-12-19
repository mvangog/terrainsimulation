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

    private void MoveWaterOld()
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
            ShareWaterOld(terrainPoint, terrainPoints[lowestNeighbourIndex]);
        }

    }


    private void MoveWater()
    {
        List<Vector2Int> directions = new List<Vector2Int>();
        directions.Add(new Vector2Int(0, 1)); //up
        directions.Add(new Vector2Int(1, 1)); //topright
        directions.Add(new Vector2Int(1, 0)); //right
        directions.Add(new Vector2Int(1, -1)); //bottomright
        directions.Add(new Vector2Int(0, -1)); //bottom
        directions.Add(new Vector2Int(-1, -1)); //bottomleft
        directions.Add(new Vector2Int(-1, 0)); //left
        directions.Add(new Vector2Int(-1, 1)); //topleft

        // find all the points with water at the start of the iteration and order them from low to high;
        TerrainPoint[] waterPoints = terrainPoints.Where(x => x.waterElevation > x.terrainElevation).OrderBy(x => x.waterElevation).ToArray();
        waterPoints.Reverse();
        foreach (TerrainPoint terrainPoint in waterPoints)
        {
            Vector2Int finalDirection = new Vector2Int();
            float maxForce = float.MinValue;

            float force;
           
            for (int i = 0; i < directions.Count; i++)
            {
                force = getForceInDirection(terrainPoint, directions[i]);
                if (force > maxForce)
                {
                    maxForce = force;
                    finalDirection = directions[i];
                }
            }
            if (maxForce>0)
            {
                ExchangeWater(terrainPoint, finalDirection,maxForce);
            }

        }
    }

    private void ExchangeWater(TerrainPoint terrainPoint, Vector2Int direction, float Force)
    {
        float deltaTime = Time.deltaTime;

        int pointX = terrainPoint.x;
        int pointY = terrainPoint.y;
        int terrainpointIndex = GetTerrainPointsIndex(pointX, pointY);

        int otherTerrainPointIndex = GetTerrainPointsIndex(pointX - direction.x, pointY - direction.y);
        if (otherTerrainPointIndex ==-1)
        {
            terrainPoint.waterElevation = terrainPoint.terrainElevation;
            terrainPoints[terrainpointIndex] = terrainPoint;
            return;
        }

        TerrainPoint otherTerrainPoint = terrainPoints[otherTerrainPointIndex];
        float mass = 10 * (terrainPoint.waterElevation - terrainPoint.terrainElevation);
        float acceleration = mass / Force;
        Vector2 normalizedDirection = direction;
        normalizedDirection.Normalize();
        float speedInDirection = normalizedDirection.x * terrainPoint.waterSpeed.x + normalizedDirection.y * terrainPoint.waterSpeed.y;
        float speed = speedInDirection + acceleration * deltaTime*10;
        if (speed >1)
        {
            speed = 1;
        }
        float volume = (terrainPoint.waterElevation - terrainPoint.terrainElevation)*speed;

        // update the speed of the other point
        float otherTerrainpointVolume = otherTerrainPoint.waterElevation - otherTerrainPoint.terrainElevation;
        Vector2 otherspeed = (((otherTerrainpointVolume) * otherTerrainPoint.waterSpeed) + (terrainPoint.waterSpeed * volume)) / (otherTerrainpointVolume + volume);
        otherTerrainPoint.waterSpeed = otherspeed;
        otherTerrainPoint.waterElevation += volume;
        terrainPoint.waterElevation -= volume;
        terrainPoints[terrainpointIndex] = terrainPoint;
        terrainPoints[otherTerrainPointIndex] = otherTerrainPoint;
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
        if (x<0)
        {
            return -1;
        }
        if (y<0)
        {
            return -1;
        }
        if (x>terrainSize-1)
        {
            return -1;
        }
        if (y > terrainSize-1)
        {
            return -1;
        }

        return (y * terrainSize) + x;
    }

    private void ShareWaterOld(TerrainPoint firstTerrainPoint, TerrainPoint secondTerrainPoint)
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

    private float getForceInDirection( TerrainPoint terrainPoint, Vector2Int direction)
    {

        int pointX = terrainPoint.x;
        int pointY = terrainPoint.y;
        int terrainpointIndex = GetTerrainPointsIndex(pointX, pointY);

        int firstTerrainPointIndex = GetTerrainPointsIndex(pointX - direction.x, pointY - direction.y);
        
        int thirdTerrainPointIndex = GetTerrainPointsIndex(pointX + direction.x, pointY + direction.y);
        


        TerrainPoint secondTerrainPoint = terrainPoint;
        // calcute the force coming from the first point
        float TotalForce = 0;
        float heightDifference = 100;
        if (firstTerrainPointIndex!=-1)
        {
            TerrainPoint firstTerrainPoint = terrainPoints[firstTerrainPointIndex];
            TotalForce += getOutsideForce(firstTerrainPoint,direction);
            // include the hydrostatic pressure at the border
            TotalForce += 5 * (firstTerrainPoint.waterElevation - firstTerrainPoint.terrainElevation);
        }
        // calculate the resistance form the third point
        if (thirdTerrainPointIndex != -1)
        {
            TerrainPoint thirdTerrainPoint = terrainPoints[thirdTerrainPointIndex];
            TotalForce += getOutsideForce(thirdTerrainPoint, direction);
            heightDifference = secondTerrainPoint.terrainElevation - thirdTerrainPoint.terrainElevation;
            // include the hydrostatic pressure at the border
            TotalForce -= 5 * (thirdTerrainPoint.waterElevation - thirdTerrainPoint.terrainElevation);
        }
        // calculate the forces ffrom the second point
        TotalForce += getInsideForce(secondTerrainPoint, direction, heightDifference);
        // include the hydrostatic pressure at the border
        TotalForce += 5 * (secondTerrainPoint.waterElevation - secondTerrainPoint.terrainElevation);
        return TotalForce;

    }

    private float getInsideForce(TerrainPoint terrainPoint, Vector2 direction, float heightDifference)
    {
        float TotalForce = 0;
        Vector2 normalizedDirection = direction.normalized;
        // add the force from the speed of the watercolumn
        float waterColumnHeight = terrainPoint.waterElevation - terrainPoint.terrainElevation;
        float speedInDirection = normalizedDirection.x * terrainPoint.waterSpeed.x + normalizedDirection.y * terrainPoint.waterSpeed.y;
        TotalForce += 10 * waterColumnHeight * speedInDirection;
        // add the force fromthe sloping of the terrain
        float slopeFactor = Mathf.Sqrt((direction.magnitude * direction.magnitude) + (heightDifference* heightDifference));
        TotalForce += slopeFactor * heightDifference / 10;

        return TotalForce;
    }

    private float getOutsideForce(TerrainPoint terrainPoint, Vector2 direction)
    {
        Vector2 normalizedDirection = direction.normalized;
        float TotalForce = 0;
        // get the hydrostatic force from the watercolumn
        float waterColumnHeight = terrainPoint.waterElevation - terrainPoint.terrainElevation;
        TotalForce += 5 * waterColumnHeight;
        // add the force from the speed of the watercolumn
        float speedInDirection = normalizedDirection.x * terrainPoint.waterSpeed.x + normalizedDirection.y * terrainPoint.waterSpeed.y;
        TotalForce += 10 * waterColumnHeight * speedInDirection;

        return TotalForce;
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
