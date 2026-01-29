using System.Text.Json.Serialization;

namespace EmployeeManagement.Models.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DepartmentType
    {
        IT,
        HR,
        Sales,
        Manager
    }
}
