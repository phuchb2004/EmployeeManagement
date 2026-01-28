namespace EmployeeManagement.Models
{
    public class Employee
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Department { get; set; }
        public DateTime DateOfBirth { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
