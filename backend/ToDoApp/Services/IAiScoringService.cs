namespace ToDoApp.Services
{
    public interface IAiScoringService
    {
        Task<int> EstimateTaskComplexityAsync(string title, string? description);
    }

}
