using System.Collections.Generic;
using PixelFlow.Data;
using PixelFlow.Models;
using UnityEngine;

namespace PixelFlow.Services
{
    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error
    }

    public struct ValidationIssue
    {
        public ValidationSeverity Severity;
        public string Message;
        public Vector2Int CellPosition;

        public ValidationIssue(ValidationSeverity severity, string message, Vector2Int cellPosition = default)
        {
            Severity = severity;
            Message = message;
            CellPosition = cellPosition;
        }
    }

    public class ValidationResult
    {
        public bool IsValid => Issues.FindIndex(i => i.Severity == ValidationSeverity.Error) < 0;
        public List<ValidationIssue> Issues { get; } = new List<ValidationIssue>();
        public int ComplexityScore { get; set; }
        public bool IsSolvable { get; set; }

        public void AddError(string message, Vector2Int position = default)
        {
            Issues.Add(new ValidationIssue(ValidationSeverity.Error, message, position));
        }

        public void AddWarning(string message, Vector2Int position = default)
        {
            Issues.Add(new ValidationIssue(ValidationSeverity.Warning, message, position));
        }

        public void AddInfo(string message, Vector2Int position = default)
        {
            Issues.Add(new ValidationIssue(ValidationSeverity.Info, message, position));
        }
    }

    public interface ILevelValidator
    {
        ValidationResult Validate(LevelData level);
        ValidationResult ValidateRuntimeState(GridModel gridModel);
    }
}
