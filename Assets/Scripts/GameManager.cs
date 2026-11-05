using UnityEngine;

public class GameManager : MonoBehaviour
{

    public enum State { Idle, Shot, Preparing, Moving, Finished }
    [Space]
    [Tooltip("Volume moved from the player to the shot per second of holding")]
    public float transferSpeed = 0.15f;
    [Tooltip("Free visual volume of the shot, deals no damage")]
    public float shotSeedVolume = 0.01f;
    public float shotSpeed = 10f;
    public float waveDelayPerMeter = 0.15f;
    public float doorOpenDistance = 5f;

    [Space]
    public Bullet shotBullet;
    public Bullet playerBullet;
    public Door door;
    public LevelGenerator level;
    public PathRunner pathRunner;
    public Transform pathStrip;

    public UIController ui;
    public int seed = 1;

    private State state;

    private Vector3 initialBulletPos;
    private Vector3 initialPlayerPos;
    private float playerVolume;
    private float transferred;

    private Vector3 ShotDirection => Vector3.ProjectOnPlane(door.transform.position - initialBulletPos, Vector3.up).normalized;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        initialBulletPos = shotBullet.transform.position;
        initialPlayerPos = playerBullet.transform.position;
        LoadLevel();
    }

    public void Refresh()
    {
        state = State.Finished;
        seed++;
        ui.Fade(LoadLevel);
    }

    private void LoadLevel()
    {
        pathRunner.Stop();
        playerBullet.transform.position = initialPlayerPos;
        door.Close();
        level.Generate(seed);
        playerVolume = level.PlayerStartVolume;
        playerBullet.SetInitialSize(Diameter(playerVolume));
        ResetShot();
        state = State.Idle;
    }

    private void ResetShot()
    {
        transferred = 0f;
        shotBullet.transform.position = initialBulletPos;
        shotBullet.SetInitialSize(Diameter(shotSeedVolume));
    }

    private static float Diameter(float volume) => 2f * Mathf.Pow(volume, 1f / 3f);

    // Update is called once per frame
    void Update()
    {
        if (ui == null)
        {
            return;
        }
        var input = ui.InputButton.Input;

        var stripScale = pathStrip.localScale;
        pathStrip.localScale = new Vector3(stripScale.x, stripScale.y, playerBullet.Radius * 2f);


        switch (state)
        {
            case State.Finished: return;
            case State.Moving:
                if (Flat(door.transform.position - playerBullet.transform.position).sqrMagnitude <= doorOpenDistance * doorOpenDistance)
                    door.Open();
                break;
            case State.Idle:
                if (input)
                    state = State.Preparing;
                break;
            case State.Shot:
                Shot();
                break;
            case State.Preparing:
                if (!input)
                {
                    state = State.Shot;
                    shotBullet.DisableShake();
                    playerBullet.DisableShake();
                    break;
                }
                Charge();
                break;
        }
    }

    private void Charge()
    {
        float amount = transferSpeed * Time.deltaTime;
        transferred += amount;
        playerVolume -= amount;
        shotBullet.Resize(Diameter(shotSeedVolume + transferred));
        playerBullet.Resize(Diameter(Mathf.Max(playerVolume, 0f)));

        if (playerVolume < level.MinPlayerVolume)
        {
            state = State.Finished;
            ui.ShowPopup("You lose", Refresh);
        }
    }

    private void Shot()
    {
        Vector3 dir = ShotDirection;
        shotBullet.transform.position += dir * (shotSpeed * Time.deltaTime);

        var target = FindHit(shotBullet.transform.position, shotBullet.Radius);
        if (target != null)
        {
            float energy = transferred * level.EnergyPerVolume;
            int kills = Wave.Infect(level.Obstacles, target, energy,
                level.CostPerMeter, level.LinkDistance, waveDelayPerMeter);
            float energyLeft = (playerVolume - level.MinPlayerVolume) * level.EnergyPerVolume;
            Debug.Log($"Shot: energy {energy:F2}, killed {kills} (conducting: {target.Conducts}), energy left {energyLeft:F2}");
            EndShot();
            return;
        }

        // Missed everything and flew past the door: the shot is wasted
        if (Vector3.Dot(door.transform.position - shotBullet.transform.position, dir) < 0f)
        {
            Debug.Log($"Shot missed: energy {transferred * level.EnergyPerVolume:F2} wasted");
            EndShot();
        }
    }

    private void EndShot()
    {
        ResetShot();
        if (!IsPathClear())
        {
            state = State.Idle;
            return;
        }

        state = State.Moving;
        pathRunner.Run(playerBullet.transform, door.transform.position, () =>
        {
            state = State.Finished;
            ui.ShowPopup("You win", Refresh);
        });
    }

    private bool IsPathClear()
    {
        Vector3 from = Flat(playerBullet.transform.position);
        Vector3 path = Flat(door.transform.position) - from;

        foreach (var obstacle in level.Obstacles)
        {
            if (obstacle == null || !obstacle.IsAlive)
                continue;

            Vector3 p = Flat(obstacle.Position);
            float t = Mathf.Clamp01(Vector3.Dot(p - from, path) / path.sqrMagnitude);
            float clearance = playerBullet.Radius + obstacle.Radius;
            if ((from + path * t - p).sqrMagnitude < clearance * clearance)
                return false;
        }
        return true;
    }

    private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

    private Obstacle FindHit(Vector3 position, float radius)
    {
        foreach (var obstacle in level.Obstacles)
        {
            if (obstacle == null || !obstacle.IsAlive)
                continue;

            Vector3 offset = Vector3.ProjectOnPlane(obstacle.Position - position, Vector3.up);
            float hitDistance = radius + obstacle.Radius;
            if (offset.sqrMagnitude < hitDistance * hitDistance)
                return obstacle;
        }
        return null;
    }
}
