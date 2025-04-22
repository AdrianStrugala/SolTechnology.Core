// DreamTravel.Ui/Models/ApiResponse.cs
public class ApiResponse<T>
{
    public List<T> Data { get; set; } = new();
    public bool IsSuccess { get; set; }
}
