using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds cave pieces from code so new levels never need scene surgery.
/// Handcrafted levels are ASCII maps in Assets/Resources/Levels; Tools/generate_levels.py
/// writes them and verifies each one is solvable. The legend is in each file's header.
/// </summary>
public static class LevelBuilder
{
    public const float Cell = 2f;
    public const string TagWall = "Cave";
    public const string TagTarget = "Target";
    public const string TagFinish = "Finish";
    public const string TagBonus = "BonusPoints";

    static GameObject cubePrefab;
    static GameObject cupPrefab;
    static GameObject windPrefab;
    static Material targetMaterial;

    public class Built
    {
        public GameObject root;
        public float gravity = 1.5f;
        public string name = "";
    }

    /// <summary>An ASCII map plus the lookups the builder needs. Out of bounds counts as rock.</summary>
    class Map
    {
        public readonly List<string> rows = new List<string>();
        public int width;
        public int startCol;
        public int startRow;

        public int Height { get { return rows.Count; } }

        public char At(int col, int row)
        {
            if (row < 0 || row >= rows.Count || col < 0 || col >= rows[row].Length)
            {
                return '#';
            }

            return rows[row][col];
        }

        public bool Open(int col, int row)
        {
            return At(col, row) != '#';
        }

        /// <summary>How many open cells continue from this one in a direction, excluding itself.</summary>
        public int Run(int col, int row, int dCol, int dRow)
        {
            int n = 0;
            while (Open(col + dCol * (n + 1), row + dRow * (n + 1)))
            {
                n++;
            }

            return n;
        }

        /// <summary>World position of a cell, with the start cell at the origin.</summary>
        public Vector3 World(float col, float row)
        {
            return new Vector3((col - startCol) * Cell, -(row - startRow) * Cell, 0f);
        }
    }

    /// <summary>Parses an ASCII map and instantiates it so that the 'S' cell sits at the world origin.</summary>
    public static Built BuildAscii(string text)
    {
        var built = new Built();
        var map = Parse(text, built);

        built.root = new GameObject("GeneratedLevel");
        var root = built.root.transform;
        var rng = new System.Random(text.GetHashCode());
        var windSeen = new bool[map.width, map.Height];

        for (int row = 0; row < map.Height; row++)
        {
            for (int col = 0; col < map.width; col++)
            {
                char ch = map.At(col, row);
                switch (ch)
                {
                    case '#':
                        if (HasOpenNeighbour(map, col, row))
                        {
                            Wall(root, map.World(col, row), rng);
                        }

                        break;

                    case 'T':
                    case 'F':
                        Gate(root, map, col, row, ch == 'T' ? TagTarget : TagFinish);
                        break;

                    case 'B':
                        Bonus(root, map.World(col, row));
                        break;

                    case '<':
                    case '>':
                    case '^':
                    case 'v':
                        if (!windSeen[col, row])
                        {
                            WindZone(root, map, col, row, ch, windSeen);
                        }

                        break;
                }
            }
        }

        return built;
    }

    static Map Parse(string text, Built built)
    {
        var map = new Map();
        foreach (var line in text.Replace("\r", "").Split('\n'))
        {
            if (line.StartsWith("@"))
            {
                ParseHeader(line, built);
                continue;
            }

            map.rows.Add(line);
        }

        // Trailing blank lines are padding; interior ones are open space.
        while (map.rows.Count > 0 && map.rows[map.rows.Count - 1].Trim().Length == 0)
        {
            map.rows.RemoveAt(map.rows.Count - 1);
        }

        foreach (var row in map.rows)
        {
            map.width = Mathf.Max(map.width, row.Length);
        }

        map.startCol = -1;
        map.startRow = -1;
        for (int row = 0; row < map.rows.Count && map.startRow < 0; row++)
        {
            int col = map.rows[row].IndexOf('S');
            if (col >= 0)
            {
                map.startCol = col;
                map.startRow = row;
            }
        }

        if (map.startRow < 0)
        {
            Debug.LogError("Level '" + built.name + "' has no 'S' start cell; starting at the top left");
            map.startCol = 0;
            map.startRow = 0;
        }

        return map;
    }

    static void ParseHeader(string line, Built built)
    {
        var parts = line.Substring(1).Trim().Split(new[] { ' ' }, 2);
        if (parts.Length < 2)
        {
            return;
        }

        if (parts[0] == "gravity")
        {
            float parsed;
            if (float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out parsed))
            {
                built.gravity = parsed;
            }
        }
        else if (parts[0] == "name")
        {
            built.name = parts[1];
        }
    }

    /// <summary>Only rock touching open space becomes a cube, so a thick map stays cheap to build.</summary>
    static bool HasOpenNeighbour(Map map, int col, int row)
    {
        for (int dRow = -1; dRow <= 1; dRow++)
        {
            for (int dCol = -1; dCol <= 1; dCol++)
            {
                if ((dRow != 0 || dCol != 0) && map.Open(col + dCol, row + dRow))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>Spans the full open cross-section, so a gate cannot be slipped past.</summary>
    static void Gate(Transform parent, Map map, int col, int row, string tag)
    {
        int up = map.Run(col, row, 0, -1);
        int down = map.Run(col, row, 0, 1);
        int left = map.Run(col, row, -1, 0);
        int right = map.Run(col, row, 1, 0);
        int vertical = up + down + 1;
        int horizontal = left + right + 1;

        Vector3 center;
        Vector3 scale;
        if (vertical <= horizontal)
        {
            // A horizontal corridor needs a vertical bar across it.
            center = map.World(col, row - (up - down) * 0.5f);
            scale = new Vector3(0.6f, vertical * Cell, 2f);
        }
        else
        {
            center = map.World(col + (right - left) * 0.5f, row);
            scale = new Vector3(horizontal * Cell, 0.6f, 2f);
        }

        Gate(parent, center, tag, scale);
    }

    /// <summary>Covers a connected run of one wind character with a single trigger box.</summary>
    static void WindZone(Transform parent, Map map, int col, int row, char ch, bool[,] seen)
    {
        var pending = new Stack<int>();
        pending.Push(row * map.width + col);
        int minCol = col, maxCol = col, minRow = row, maxRow = row;

        while (pending.Count > 0)
        {
            int packed = pending.Pop();
            int c = packed % map.width;
            int r = packed / map.width;
            if (r >= map.Height || seen[c, r] || map.At(c, r) != ch)
            {
                continue;
            }

            seen[c, r] = true;
            minCol = Mathf.Min(minCol, c);
            maxCol = Mathf.Max(maxCol, c);
            minRow = Mathf.Min(minRow, r);
            maxRow = Mathf.Max(maxRow, r);

            if (c + 1 < map.width)
            {
                pending.Push(r * map.width + c + 1);
            }

            if (c - 1 >= 0)
            {
                pending.Push(r * map.width + c - 1);
            }

            if (r + 1 < map.Height)
            {
                pending.Push((r + 1) * map.width + c);
            }

            if (r - 1 >= 0)
            {
                pending.Push((r - 1) * map.width + c);
            }
        }

        Vector2 force;
        if (ch == '<')
        {
            force = new Vector2(-1.5f, 0f);
        }
        else if (ch == '>')
        {
            force = new Vector2(1.5f, 0f);
        }
        else if (ch == '^')
        {
            force = new Vector2(0f, 1.5f);
        }
        else
        {
            force = new Vector2(0f, -1.5f);
        }

        var center = map.World((minCol + maxCol) * 0.5f, (minRow + maxRow) * 0.5f);
        var size = new Vector2((maxCol - minCol + 1) * Cell, (maxRow - minRow + 1) * Cell);
        Wind(parent, center, size, force);
    }

    public static GameObject Wall(Transform parent, Vector3 pos, System.Random rng)
    {
        float scale = 2.0f + (float)rng.NextDouble() * 0.4f;
        float rotationZ = ((float)rng.NextDouble() - 0.5f) * 20f;
        const float jitter = 0.15f;
        pos += new Vector3(((float)rng.NextDouble() - 0.5f) * jitter, ((float)rng.NextDouble() - 0.5f) * jitter, 0f);
        return Wall(parent, pos, scale, rotationZ);
    }

    public static GameObject Wall(Transform parent, Vector3 pos, float scale, float rotationZ)
    {
        var prefab = CubePrefab();
        if (prefab == null)
        {
            return null;
        }

        var go = Object.Instantiate(prefab, pos, Quaternion.Euler(0f, 0f, rotationZ), parent);
        go.transform.localScale = Vector3.one * scale;
        go.tag = TagWall;
        return go;
    }

    public static GameObject Gate(Transform parent, Vector3 pos, string tag, Vector3 scale)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = tag;
        go.tag = tag;
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.GetComponent<Collider>().isTrigger = true;

        var mat = TargetMaterial();
        if (mat != null)
        {
            go.GetComponent<Renderer>().sharedMaterial = mat;
        }

        return go;
    }

    public static GameObject Bonus(Transform parent, Vector3 pos)
    {
        GameObject go;
        var prefab = CupPrefab();
        if (prefab != null)
        {
            go = Object.Instantiate(prefab, pos, Quaternion.identity, parent);
            go.transform.localScale = Vector3.one * 0.5f;
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.8f;
        }

        go.name = "Bonus";
        go.tag = TagBonus;
        EnsurePickupCollider(go);
        go.AddComponent<Spinner>();
        return go;
    }

    /// <summary>
    /// The trophy prefab ships without a collider, which is why the bonus cup in the handmade
    /// levels could never actually be picked up.
    /// </summary>
    public static void EnsurePickupCollider(GameObject go)
    {
        if (go.GetComponent<Collider>() != null)
        {
            return;
        }

        var col = go.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 2.5f;
        col.center = new Vector3(0f, 1.5f, 0f);
    }

    public static GameObject Wind(Transform parent, Vector3 center, Vector2 size, Vector2 force)
    {
        var go = new GameObject("Wind");
        go.transform.SetParent(parent, false);
        go.transform.position = center;

        var box = go.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(size.x, size.y, 4f);
        go.AddComponent<CaveWind>().force = force;

        var prefab = WindPrefab();
        if (prefab != null)
        {
            var dir = force.normalized;
            bool vertical = Mathf.Abs(dir.y) > 0.5f;
            // The effect emits along its local z, matching how the scene's own wind zones are set up.
            var look = Quaternion.LookRotation(new Vector3(dir.x, dir.y, 0f), vertical ? Vector3.back : Vector3.up);
            var fx = Object.Instantiate(prefab, center, look, go.transform);
            var ps = fx.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = vertical ? new Vector3(size.x, 4f, size.y) : new Vector3(4f, size.y, size.x);
                var emission = ps.emission;
                emission.rateOverTime = 12f;
            }
        }

        return go;
    }

    static GameObject CubePrefab()
    {
        if (cubePrefab == null)
        {
            cubePrefab = Resources.Load<GameObject>("Prefabs/Cube");
        }

        return cubePrefab;
    }

    static GameObject CupPrefab()
    {
        if (cupPrefab == null)
        {
            cupPrefab = Resources.Load<GameObject>("TrophyCups/Prefabs/Gold/Gold Cup (High Poly");
        }

        return cupPrefab;
    }

    static GameObject WindPrefab()
    {
        if (windPrefab == null)
        {
            windPrefab = Resources.Load<GameObject>("WindTrailsEffect/WindTrailsEffect");
        }

        return windPrefab;
    }

    static Material TargetMaterial()
    {
        if (targetMaterial == null)
        {
            targetMaterial = Resources.Load<Material>("Materials/Target");
        }

        return targetMaterial;
    }
}
