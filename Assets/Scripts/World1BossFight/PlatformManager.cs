using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace World1BossFight
{
    [Serializable]
    public struct PlatformData
    {
        public bool IsValid;
        
        public Vector3 StartPosition;
        public List<Vector2Int> Positions;
        public float TileSize;
    }

    [Serializable]
    public struct BranchStrikeData
    {
        public PlatformData PlatformData;
        public int Distance;
        public Vector2Int Direction;
    }
    
    public class PlatformManager : MonoBehaviour
    {
        public static PlatformManager Instance { get; private set; }
        
        [SerializeField] private Vector2Int gridSize;
        [SerializeField] private float tileSize;

        private bool[][] _reservedTiles;
        
        private List<Vector2Int> _reservedPositions;
        private List<Vector2Int> _positions;
        
        private void OnEnable()
        {
            Instance = this;
        }

        private void Awake()
        {
            _reservedPositions = new List<Vector2Int>();
            _positions = new List<Vector2Int>();
            
            for (var x = 0; x < gridSize.x; x++)
            {
                for (var y = 0; y < gridSize.y; y++)
                {
                    _positions.Add(new Vector2Int(x, y));
                }
            }
        }

        public PlatformData FindAndReservePositions(Vector2Int bounds)
        {
            var unvisitedPositions = new List<Vector2Int>();
            var positions = new List<Vector2Int>();
            
            unvisitedPositions.AddRange(_positions);
            var invalidPosition = false;

            while (unvisitedPositions.Count > 0)
            {
                positions.Clear();
                var index = Random.Range(0, unvisitedPositions.Count);
                var position = unvisitedPositions[index];
                unvisitedPositions.Remove(position);
                
                if (position.x + bounds.x > gridSize.x ||
                    position.y + bounds.y > gridSize.y)
                    continue;
                
                invalidPosition = false;
                for (var x = 0; x < bounds.x; x++)
                {
                    for (var y = 0; y < bounds.y; y++)
                    {
                        var offsetPosition = new Vector2Int(x, y) + position;
                        positions.Add(offsetPosition);
                        invalidPosition = _reservedPositions.Contains(offsetPosition);
                        if (invalidPosition) break;
                    }
                    if (invalidPosition) break;
                }
                
                if (!invalidPosition) break;
            }

            if (invalidPosition) return new PlatformData { IsValid = false };
            
            ReservePositions(positions);
            return new PlatformData { IsValid = true, StartPosition = (Vector2)positions[0] * tileSize + (Vector2)transform.position - (Vector2)transform.localScale / 2f, Positions = positions, TileSize = tileSize };
        }

        public BranchStrikeData FindAndReserveBranchStrikePositions(bool randomizeMaxDistance = false)
        {
            var branchStrikeData = new BranchStrikeData();
            Vector2Int startPosition;
            Vector2Int direction;
            int maxDistance;
            
            var isVertical = Random.Range(0, 2) == 0;
            if (isVertical)
            {
                var isNorth = Random.Range(0, 2) == 0;
                var x = Random.Range(0, gridSize.x);
                var y = isNorth ? gridSize.y : -1;
                
                startPosition = new Vector2Int(x, y);
                direction = new Vector2Int(0, isNorth ? -1 : 1);
                maxDistance = randomizeMaxDistance ? Random.Range(3, gridSize.y) : gridSize.y;
            }
            else
            {
                var isEast = Random.Range(0, 2) == 0;
                var x = isEast ? gridSize.x : -1;
                var y = Random.Range(0, gridSize.y);
                
                startPosition = new Vector2Int(x, y);
                direction = new Vector2Int(isEast ? -1 : 1, 0);
                maxDistance = randomizeMaxDistance ? Random.Range(3, gridSize.x) : gridSize.x;
            }

            var positions = GetPositionsInConnectedLine(startPosition, direction, maxDistance);
            if (positions.Count < 2)
            {
                branchStrikeData.PlatformData = new PlatformData { IsValid = false };
                return branchStrikeData;
            }
            
            ReservePositions(positions);
            branchStrikeData.PlatformData = new PlatformData
            {
                IsValid = true, Positions = positions,
                StartPosition = (Vector2)startPosition * tileSize + (Vector2)transform.position - (Vector2)transform.localScale / 2f,
                TileSize = tileSize
            };
            branchStrikeData.Distance = positions.Count;
            branchStrikeData.Direction = direction;
            return branchStrikeData;
        }

        public bool CanReservePositions(List<Vector2Int> positions)
        {
            return positions.All(position => !_reservedPositions.Contains(position) && IsInsideGrid(position));
        }

        public List<Vector2Int> ReserveHedgeSplitPositions()
        {
            var center = gridSize / 2;
            var positions = new List<Vector2Int>();
            positions.AddRange(GetPositionsInConnectedLine(new Vector2Int(center.x, 0), new Vector2Int(0, 1), gridSize.y, false));
            positions.AddRange(GetPositionsInConnectedLine(new Vector2Int(center.x + 1, 0), new Vector2Int(0, 1), gridSize.y, false));
            positions.AddRange(GetPositionsInConnectedLine(new Vector2Int(0, center.y), new Vector2Int(1, 0), gridSize.x, false));
            positions.AddRange(GetPositionsInConnectedLine(new Vector2Int(0, center.y + 1), new Vector2Int(1, 0), gridSize.x, false));
            ReservePositions(positions);
            return positions;
        }

        private List<Vector2Int> GetPositionsInConnectedLine(Vector2Int startPosition, Vector2Int direction, int maxDistance, bool cancelOnHitReserved = true)
        {
            var position = IsInsideGrid(startPosition) ? startPosition : startPosition + direction;
            var positions = new List<Vector2Int>();
            
            do
            {
                if (cancelOnHitReserved && _reservedPositions.Contains(position)) break;
                positions.Add(position);
                if (positions.Count > maxDistance) break;
                
                position += direction;
            } while (IsInsideGrid(position));
            
            return positions;
        }

        private bool IsInsideGrid(Vector2Int position)
        {
            return Mathf.Abs(position.x) < gridSize.x && position.x >= 0 && Mathf.Abs(position.y) < gridSize.y && position.y >= 0;
        }

        public void ReservePositions(List<Vector2Int> positions)
        {
            //Debug.Log("Reserved Positions");
            foreach (var position in positions)
            {
                //Debug.Log("\t" + position);
                _positions.Remove(position);
                _reservedPositions.Add(position);
            }
        }

        public void UnreservePositions(List<Vector2Int> positions)
        {
            foreach (var position in positions)
            {
                _reservedPositions.Remove(position);
                _positions.Add(position);
            }
        }
    }
}
