using System.Collections.Generic;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [SerializeField] private Obstacle prefab;
    [SerializeField] private Transform root;
    [SerializeField] private Transform start;
    [SerializeField] private Transform finish;

    [Header("Path")]
    [SerializeField] private float corridorWidth = 1.4f;
    [SerializeField] private float clearRadius = 2.5f;

    [Header("Clusters")]
    [SerializeField] private Vector2Int clusterSize = new Vector2Int(2, 7);
    [SerializeField, Range(0f, 1f)] private float singleChance = 0.3f;
    [SerializeField] private Vector2 clusterGap = new Vector2(0.3f, 1.5f);

    [Header("Forest")]
    [SerializeField] private Vector2 forestSize = new Vector2(30f, 24f);
    [SerializeField] private float forestSpacing = 1.1f;
    [SerializeField] private float forestGap = 0.3f;
    [SerializeField] private int forestAttempts = 4000;

    [Header("Balance")]
    [SerializeField] private float hp = 1f;
    [SerializeField] private float costPerMeter = 0.5f;
    [SerializeField] private float linkDistance = 1.2f;
    [SerializeField] private float energyPerVolume = 25f;
    [SerializeField] private float minPlayerVolume = 0.03f;
    [SerializeField] private float margin = 0.2f;
    [SerializeField] private float clusterTolerance = 0.1f;

    private readonly List<Obstacle> obstacles = new List<Obstacle>();
    private readonly List<Transform> groups = new List<Transform>();

    public IReadOnlyList<Obstacle> Obstacles => obstacles;
    public float CorridorWidth => corridorWidth;
    public float CostPerMeter => costPerMeter;
    public float LinkDistance => linkDistance;
    public float EnergyPerVolume => energyPerVolume;
    public float MinPlayerVolume => minPlayerVolume;
    public float PlayerStartVolume { get; private set; }

    private float ObstacleRadius => prefab.Radius;
    private Vector3 Forward => Vector3.ProjectOnPlane(finish.position - start.position, Vector3.up).normalized;
    private float PathLength => Vector3.ProjectOnPlane(finish.position - start.position, Vector3.up).magnitude;

    public void Generate(int seed)
    {
        Random.InitState(seed);

        var clusters = PlaceClusters();
        var occupied = new List<Vector2>();
        foreach (var cluster in clusters)
            occupied.AddRange(cluster);
        var forest = PlaceForest(occupied);

        Clear();
        for (int i = 0; i < clusters.Count; i++)
        {
            var parent = CreateGroup($"Cluster {i}");
            foreach (var p in clusters[i])
                Spawn(p, hp, true, parent);
        }
        var forestParent = CreateGroup("Forest");
        foreach (var p in forest)
            Spawn(p, 0f, false, forestParent);

        Balance(clusters);
    }

    private void Clear()
    {
        foreach (var group in groups)
        {
            if (group != null)
                Destroy(group.gameObject);
        }
        groups.Clear();
        obstacles.Clear();
    }

    private Transform CreateGroup(string groupName)
    {
        var group = new GameObject(groupName).transform;
        group.SetParent(root, false);
        groups.Add(group);
        return group;
    }

    private void Spawn(Vector2 p, float obstacleHp, bool conducts, Transform parent)
    {
        var obstacle = Instantiate(prefab, ToWorld(p), Quaternion.identity, parent);
        obstacle.Configure(obstacleHp, conducts);
        obstacles.Add(obstacle);
    }

    private Vector3 ToWorld(Vector2 p)
    {
        Vector3 side = Vector3.Cross(Vector3.up, Forward);
        Vector3 origin = new Vector3(start.position.x, root.position.y, start.position.z);
        return origin + Forward * p.x + side * p.y;
    }


    private List<List<Vector2>> PlaceClusters()
    {
        var clusters = new List<List<Vector2>>();
        float end = PathLength - clearRadius;
        float cursor = clearRadius;

        while (cursor < end)
        {
            int count = Random.value < singleChance ? 1 : Random.Range(clusterSize.x, clusterSize.y + 1);
            var cluster = PlaceCluster(cursor, count, end);
            clusters.Add(cluster);

            float lastX = cursor;
            foreach (var p in cluster)
                lastX = Mathf.Max(lastX, p.x);
            cursor = lastX + linkDistance + Random.Range(clusterGap.x, clusterGap.y);
        }
        return clusters;
    }

    private List<Vector2> PlaceCluster(float along, int count, float end)
    {
        float minDist = ObstacleRadius * 2f + 0.1f;
        float halfWidth = corridorWidth * 0.5f;
        var points = new List<Vector2> { new Vector2(along, 0f) };

        for (int guard = 0; points.Count < count && guard < 1000; guard++)
        {
            Vector2 from = points[Random.Range(0, points.Count)];
            float angle = Random.Range(-90f, 90f) * Mathf.Deg2Rad;
            float dist = Random.Range(minDist, linkDistance * 0.95f);
            Vector2 p = from + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;

            if (p.x < along || p.x > end || Mathf.Abs(p.y) > halfWidth || IsTooClose(points, p, minDist))
                continue;
            points.Add(p);
        }
        return points;
    }

    private List<Vector2> PlaceForest(List<Vector2> occupied)
    {
        var forest = new List<Vector2>();
        float pathFree = corridorWidth * 0.5f + forestGap + ObstacleRadius;
        float length = PathLength;

        for (int i = 0; i < forestAttempts; i++)
        {
            var p = new Vector2(
                length * 0.5f + Random.Range(-forestSize.x, forestSize.x) * 0.5f,
                Random.Range(-forestSize.y, forestSize.y) * 0.5f);

            bool onPath = Mathf.Abs(p.y) < pathFree && p.x > -clearRadius && p.x < length + clearRadius;
            bool nearEnds = p.magnitude < clearRadius || (p - new Vector2(length, 0f)).magnitude < clearRadius;
            if (onPath || nearEnds || IsTooClose(forest, p, forestSpacing) || IsTooClose(occupied, p, forestSpacing))
                continue;
            forest.Add(p);
        }
        return forest;
    }

    private void Balance(List<List<Vector2>> clusters)
    {
        float optimal = 0f, oneByOne = 0f;

        foreach (var cluster in clusters)
        {
            var positions = cluster.ConvertAll(p => new Vector3(p.x, 0f, p.y));
            float ecc = 0f;
            Wave.Spread(positions, 0, linkDistance, (i, pathLength) =>
            {
                ecc = Mathf.Max(ecc, pathLength);
                return true;
            });

            optimal += (hp + costPerMeter * ecc) * (1f + clusterTolerance);
            oneByOne += cluster.Count * hp;
        }

        float budget = optimal * (1f + margin);
        PlayerStartVolume = minPlayerVolume + budget / energyPerVolume;
        float finalVolume = minPlayerVolume + (budget - optimal) / energyPerVolume;
        float finalDiameter = 2f * Mathf.Pow(finalVolume, 1f / 3f);
        float startDiameter = 2f * Mathf.Pow(PlayerStartVolume, 1f / 3f);

        Debug.Log($"Level: {clusters.Count} clusters | required {optimal:F2}, budget {budget:F2} | " +
                  $"one by one {oneByOne:F2} | " +
                  $"ball diameter {startDiameter:F2} -> {finalDiameter:F2} (corridor {corridorWidth:F2})");
    }

    private static bool IsTooClose(List<Vector2> points, Vector2 p, float minDist)
    {
        float sqr = minDist * minDist;
        foreach (var q in points)
        {
            if ((q - p).sqrMagnitude < sqr)
                return true;
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (start == null || finish == null || root == null)
            return;

        Gizmos.color = Color.cyan;
        float half = corridorWidth * 0.5f;
        float length = PathLength;
        Gizmos.DrawLine(ToWorld(new Vector2(0f, -half)), ToWorld(new Vector2(length, -half)));
        Gizmos.DrawLine(ToWorld(new Vector2(0f, half)), ToWorld(new Vector2(length, half)));
    }
}
