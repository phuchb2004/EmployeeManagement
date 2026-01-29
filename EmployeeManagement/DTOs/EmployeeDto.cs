using System.ComponentModel.DataAnnotations;

namespace EmployeeManagement.DTOs
{

    public record EmployeeDto(
        int Id,
        string Name,
        string Email,
        string Department,
        DateTime DateOfBirth
    );

    public record CreateEmployeeDto
    {
        public string Name { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Department { get; init; } = string.Empty;

        public DateTime DateOfBirth { get; init; }
    }

    public record UpdateEmployeeDto : CreateEmployeeDto
    {

    }

}
