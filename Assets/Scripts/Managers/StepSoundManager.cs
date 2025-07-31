using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class StepSoundManager : MonoBehaviour
{
    [System.Serializable]
    public class GroundTypeSound
    {
        public string groundTypeName;
        public AudioClip[] stepSounds;
    }

    public List<GroundTypeSound> groundSounds;
    public AudioSource audioSource;

    public Tilemap grassTilemap;
    public Tilemap stoneTilemap;
    public Tilemap woodTilemap;

    public void PlayStepSound(Vector3 playerWorldPos)
    {
        Vector3Int gridPos = grassTilemap.WorldToCell(playerWorldPos);

        string groundType = DetectGroundType(gridPos);


        if (!string.IsNullOrEmpty(groundType))
        {
            GroundTypeSound soundSet = groundSounds.Find(g => g.groundTypeName == groundType);
            if (soundSet != null && soundSet.stepSounds.Length > 0)
            {
                AudioClip clip = soundSet.stepSounds[Random.Range(0, soundSet.stepSounds.Length)];
                audioSource.PlayOneShot(clip);
            }
        }
    }

    private string DetectGroundType(Vector3Int gridPos)
    {
        if (grassTilemap.HasTile(gridPos)) return "Grass";
        if (stoneTilemap.HasTile(gridPos)) return "Stone";
        if (woodTilemap.HasTile(gridPos)) return "Wood";

        return null;  // No known ground type
    }
}
