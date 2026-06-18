namespace Vali.Core.Generation;

public interface IGenerationProgressView
{
    /// <summary>Called from the scheduler body when a work item finishes (worker thread).</summary>
    void OnCompleted(string countryCode, string? subdivisionCode, (IList<Location> locations, int regionGoalCount, int minDistance)[] results);

    /// <summary>Runs the display until the scheduler finishes.</summary>
    Task RunUntil(Task schedulerTask);
}
