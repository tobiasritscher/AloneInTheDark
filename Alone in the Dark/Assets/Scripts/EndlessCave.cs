using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds the cave a bit ahead of the player and forgets it behind them. Deterministic per seed,
/// so the daily cave is the same cave for everyone on the same day.
/// </summary>
public class EndlessCave : MonoBehaviour
{
    public Transform player;
    public int seed;

    const float Step = 1.8f;
    const float SpawnAhead = 45f;
    const float DespawnBehind = 30f;
    const float WallThickness = 2.2f;

    readonly Queue<KeyValuePair<float, GameObject>> pieces = new Queue<KeyValuePair<float, GameObject>>();
    float nextX;
    float nextBonusX = 70f;
    float nextWindX = 130f;
    int windCount;
    float noiseOffset;
    System.Random rng;

    public void Init(Transform playerTransform, int caveSeed)
    {
        player = playerTransform;
        seed = caveSeed;
        rng = new System.Random(seed);
        noiseOffset = (float)rng.NextDouble() * 1000f;
        nextX = -10f;
        Fill();
    }

    void Update()
    {
        if (player == null)
        {
            return;
        }

        Fill();
        while (pieces.Count > 0 && pieces.Peek().Key < player.position.x - DespawnBehind)
        {
            var old = pieces.Dequeue().Value;
            if (old != null)
            {
                Destroy(old);
            }
        }
    }

    /// <summary>Gravity strength at a given distance. Starts weightless, then the deep pulls.</summary>
    public static float GravityAt(float x)
    {
        return x < 60f ? 0f : Mathf.Lerp(0.8f, 1.8f, Mathf.Clamp01((x - 60f) / 400f));
    }

    /// <summary>Forward pull. Ramps up with distance, which is the whole difficulty curve.</summary>
    public static float DriftAt(float x)
    {
        return Mathf.Lerp(0.9f, 3.2f, Mathf.Clamp01(x / 600f));
    }

    void Fill()
    {
        float limit = player.position.x + SpawnAhead;
        while (nextX < limit)
        {
            Column(nextX);
            nextX += Step;
        }
    }

    float Ramp(float x)
    {
        return Mathf.Clamp01((x - 10f) / 25f);
    }

    public float CenterAt(float x)
    {
        float amplitude = Mathf.Lerp(5f, 11f, Mathf.Clamp01(x / 500f));
        float n = Mathf.PerlinNoise(x * 0.03f + noiseOffset, 0.37f) - 0.5f;
        return n * 2f * amplitude * Ramp(x);
    }

    public float HalfWidthAt(float x)
    {
        float baseWidth = Mathf.Lerp(3.3f, 1.9f, Mathf.Clamp01((x - 20f) / 450f));
        float n = Mathf.PerlinNoise(x * 0.09f + noiseOffset, 7.1f) - 0.5f;
        return baseWidth + n * 0.8f * Ramp(x);
    }

    void Column(float x)
    {
        float center = CenterAt(x);
        float half = HalfWidthAt(x);

        var top = LevelBuilder.Wall(transform, new Vector3(x, center + half + WallThickness * 0.5f, 0f), rng);
        var bottom = LevelBuilder.Wall(transform, new Vector3(x, center - half - WallThickness * 0.5f, 0f), rng);
        pieces.Enqueue(new KeyValuePair<float, GameObject>(x, top));
        pieces.Enqueue(new KeyValuePair<float, GameObject>(x, bottom));

        if (x >= nextBonusX)
        {
            var bonus = LevelBuilder.Bonus(transform, new Vector3(x, center - 1f, 0f));
            pieces.Enqueue(new KeyValuePair<float, GameObject>(x, bonus));
            nextBonusX += 80f + (float)rng.NextDouble() * 40f;
        }

        if (x >= nextWindX)
        {
            float length = 14f;
            float mid = x + length * 0.5f;
            float dir = windCount % 2 == 0 ? -1f : 1f;
            var wind = LevelBuilder.Wind(transform, new Vector3(mid, CenterAt(mid), 0f), new Vector2(length, HalfWidthAt(mid) * 2f + 3f), new Vector2(1.5f * dir, 0f));
            pieces.Enqueue(new KeyValuePair<float, GameObject>(x + length, wind));
            windCount++;
            nextWindX += 70f + (float)rng.NextDouble() * 40f;
        }
    }
}
