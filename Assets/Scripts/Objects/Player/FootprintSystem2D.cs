using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(PlayerExploring))]
public class FootprintsFromPlayerExploring : MonoBehaviour
{
    [Header("Dependencies")]
    public PlayerExploring player;          // auto-filled if left empty
    public Tilemap footprintsTilemap;       // overlay tilemap for footprints
    public LayerMask snowColliderMask;      // Snow layer, requires TilemapCollider2D on snow

    [Header("Footprint Tiles (sprite default faces UP)")]
    public TileBase leftFootTile;
    public TileBase rightFootTile;

    [Header("Step Timing")]
    public float stepInterval = 0.18f;      // seconds between footprints while moving

    [Header("Offsets in CELL units (fractions of a cell)")]
    public float lateralOffsetCells = 0.5f;
    public float forwardOffsetCells = 0.5f;
    [Header("Model/Art Corrections")]
    public float yCorrectionOnHorizontalCells = 1.2f; // in cell units; tune 0.04–0.12


    [Header("Fade")]
    public float footprintLifetime = 2.5f;  // seconds to fade out
    public bool clearWhenGone = true;

    // deterministic alternation
    private int stepIndex = 0;
    private float movingTime = 0f;

    // avoid clearing newer stamp in same cell
    private int nextStampId = 1;

    private struct Stamp
    {
        public int id;
        public Vector3Int cell;
        public float time;
    }

    private readonly List<Stamp> activeStamps = new List<Stamp>(256);
    private readonly Dictionary<Vector3Int, int> latestStampIdAtCell = new Dictionary<Vector3Int, int>();

    // optional: avoid spamming exact same cell (helps if footprints tilemap has same cell size as snow)
    private Vector3Int lastStampedCell;
    private bool hasLastStampedCell = false;

    void Awake()
    {
        if (player == null) player = GetComponent<PlayerExploring>();
    }

    void Update()
    {
        // Only count time while actually moving in your movement system
        // (PlayerExploring sets isMoving true/false in UpdateAnimationAndMove). :contentReference[oaicite:1]{index=1}
        if (player != null && player.isMoving)
        {
            movingTime += Time.deltaTime;

            // Stamp every interval while moving
            while (movingTime >= stepInterval)
            {
                TryStamp();
                stepIndex++;
                movingTime -= stepInterval;
            }
        }
        else
        {
            movingTime = 0f;
        }

        FadeAndCleanup();
    }

    private void TryStamp()
    {
        Vector2 pos = transform.position;
        //Debug.Log("Trying to stamp footprint at " + pos);

        // Only stamp on snow
        if (Physics2D.OverlapPoint(pos, snowColliderMask) == null) return;

        // Direction comes from PlayerExploring.change (already normalized there). :contentReference[oaicite:2]{index=2}
        Vector2 moveDir = player != null ? new Vector2(player.change.x, player.change.y) : Vector2.up;
        if (moveDir.sqrMagnitude < 0.001f) moveDir = Vector2.up;

        // Stamp cell on footprints tilemap (recommended: footprints tilemap has finer cell size than snow)
        Vector3Int cell = footprintsTilemap.WorldToCell(pos);

        // Avoid repeated overwrites in same cell
        if (hasLastStampedCell && cell == lastStampedCell) return;
        lastStampedCell = cell;
        hasLastStampedCell = true;

        Dir4 d = GetDir4(moveDir);
        float rotDeg = RotationDegFromDir(d);

        bool isLeft = (stepIndex % 2 == 0);
        TileBase tile = isLeft ? leftFootTile : rightFootTile;

        footprintsTilemap.SetTile(cell, tile);
        footprintsTilemap.SetTileFlags(cell, TileFlags.None);

        // Offsets: left/right (perp) + slight forward (along travel)
        Vector2 leftPerp = LeftPerpFromDir(d);
        Vector2 fwd = ForwardFromDir(d);
        float lateralSign = isLeft ? 1f : -1f;

        Vector3 offset =
            (Vector3)(leftPerp * (lateralOffsetCells * lateralSign) +
                      fwd * forwardOffsetCells);

        // Direction-specific Y correction when moving horizontally (Left/Right)
        if (d == Dir4.Left || d == Dir4.Right)
        {
            offset.y -= yCorrectionOnHorizontalCells;
        }


        Matrix4x4 m = Matrix4x4.TRS(offset, Quaternion.Euler(0, 0, rotDeg), Vector3.one);
        footprintsTilemap.SetTransformMatrix(cell, m);

        // Full opacity at birth
        footprintsTilemap.SetColor(cell, Color.white);

        // Record for fade, keyed by most recent stamp in this cell
        int id = nextStampId++;
        latestStampIdAtCell[cell] = id;
        activeStamps.Add(new Stamp { id = id, cell = cell, time = Time.time });
    }

    private void FadeAndCleanup()
    {
        float now = Time.time;

        for (int i = activeStamps.Count - 1; i >= 0; i--)
        {
            Stamp s = activeStamps[i];

            // If overwritten by a newer stamp in same cell, stop tracking this one
            if (!latestStampIdAtCell.TryGetValue(s.cell, out int latestId) || latestId != s.id)
            {
                activeStamps.RemoveAt(i);
                continue;
            }

            float t = (now - s.time) / Mathf.Max(footprintLifetime, 0.001f);

            if (t >= 1f)
            {
                if (clearWhenGone)
                {
                    footprintsTilemap.SetTile(s.cell, null);
                    footprintsTilemap.SetTransformMatrix(s.cell, Matrix4x4.identity);
                    footprintsTilemap.SetColor(s.cell, Color.white);
                    latestStampIdAtCell.Remove(s.cell);
                }
                activeStamps.RemoveAt(i);
                continue;
            }

            float alpha = 1f - t;
            footprintsTilemap.SetColor(s.cell, new Color(1f, 1f, 1f, alpha));
        }
    }

    private enum Dir4 { Up, Right, Down, Left }

    private Dir4 GetDir4(Vector2 dir)
    {
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            return (dir.x >= 0) ? Dir4.Right : Dir4.Left;
        else
            return (dir.y >= 0) ? Dir4.Up : Dir4.Down;
    }

    private float RotationDegFromDir(Dir4 d)
    {
        // sprite faces UP by default
        return d switch
        {
            Dir4.Up => 0f,
            Dir4.Right => -90f,
            Dir4.Down => 180f,
            Dir4.Left => 90f,
            _ => 0f
        };
    }

    private Vector2 LeftPerpFromDir(Dir4 d)
    {
        return d switch
        {
            Dir4.Up => Vector2.left,
            Dir4.Right => Vector2.up,
            Dir4.Down => Vector2.right,
            Dir4.Left => Vector2.down,
            _ => Vector2.left
        };
    }

    private Vector2 ForwardFromDir(Dir4 d)
    {
        return d switch
        {
            Dir4.Up => Vector2.up,
            Dir4.Right => Vector2.right,
            Dir4.Down => Vector2.down,
            Dir4.Left => Vector2.left,
            _ => Vector2.up
        };
    }
}
