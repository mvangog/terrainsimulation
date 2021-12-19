using UnityEngine;

public class TerrainPoint
{
    public int x;
    public int y;
    public float terrainElevation;
    public float waterElevation;
    public Vector2 waterSpeed;

    public TerrainPoint()
    {
        waterSpeed = Vector2.zero;
    }

}
