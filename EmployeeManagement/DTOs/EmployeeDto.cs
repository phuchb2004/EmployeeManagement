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
        [Required(ErrorMessage = "Name is required")]
        [MaxLength(100)]
        public string Name { get; init; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Email incorrect format")]
        public string Email { get; init; } = string.Empty;

        public string Department { get; init; } = string.Empty;

        public DateTime DateOfBirth { get; init; }
    }

    public record UpdateEmployeeDto : CreateEmployeeDto
    {

    }

}
