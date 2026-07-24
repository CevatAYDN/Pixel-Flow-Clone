using System.Collections.Generic;
using PixelFlow.Data;
using UnityEngine;

namespace PixelFlow.Services
{
    public enum SolverStrategyType
    {
        StandardDFS,
        PhaseBased,
        DynamicDifficulty
    }

    public interface IPathSolverStrategy
    {
        SolverStrategyType StrategyType { get; }
        
        bool Solve(LevelData level, out Dictionary<ColorType, List<Vector2Int>> solutions);
        
        bool SolvePartial(LevelData level, ColorType color, int steps, out List<Vector2Int> partialPath);
    }
}
