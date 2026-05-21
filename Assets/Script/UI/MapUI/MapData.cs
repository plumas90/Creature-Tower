using System.Collections.Generic;

[System.Serializable]
public class MapNode
{
    public int floorIndex;
    public int nodeIndex;
    public bool isBoss;
    public NormalStage.RoomTheme roomTheme;
    public int prefabIndex; // Index in the active candidate pool
}

[System.Serializable]
public class MapFloor
{
    public int floorIndex;
    public bool isBossFloor;
    public List<MapNode> nodes = new List<MapNode>();
}
