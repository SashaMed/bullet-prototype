using System;
using System.Collections.Generic;
using UnityEngine;

public static class Wave
{

    public static int Infect(IReadOnlyList<Obstacle> obstacles, Obstacle target, float energy,
        float costPerMeter, float linkDistance, float delayPerMeter)
    {
        int kills = 0;
        var alive = new List<Obstacle>();
        foreach (var obstacle in obstacles)
        {
            if (obstacle != null && obstacle.IsAlive)
                alive.Add(obstacle);
        }

        var positions = alive.ConvertAll(o => o.Position);
        Spread(positions, alive.IndexOf(target), linkDistance, (i, pathLength) =>
        {
            bool killed = alive[i].Hit(energy - costPerMeter * pathLength, pathLength * delayPerMeter);
            if (killed)
                kills++;
            return killed && alive[i].Conducts;
        });
        return kills;
    }


    public static void Spread(IReadOnlyList<Vector3> positions, int source, float linkDistance, Func<int, float, bool> visit)
    {
        int n = positions.Count;
        var dist = new float[n];
        var done = new bool[n];
        for (int i = 0; i < n; i++)
            dist[i] = float.PositiveInfinity;
        dist[source] = 0f;

        while (true)
        {
            int u = -1;
            for (int i = 0; i < n; i++)
            {
                if (!done[i] && !float.IsPositiveInfinity(dist[i]) && (u < 0 || dist[i] < dist[u]))
                    u = i;
            }
            if (u < 0)
                return;

            done[u] = true;
            if (!visit(u, dist[u]))
                continue;

            for (int v = 0; v < n; v++)
            {
                if (done[v])
                    continue;
                float d = Vector3.Distance(positions[u], positions[v]);
                if (d <= linkDistance && dist[u] + d < dist[v])
                    dist[v] = dist[u] + d;
            }
        }
    }
}
