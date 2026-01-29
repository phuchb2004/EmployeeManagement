using EmployeeManagement.Models;
using EmployeeManagement.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using EmployeeManagement.Models.Enums;

namespace EmployeeManagement.Services
{
    public class EmployeeService
    {
        private readonly AppDbContext _context;
        private static readonly Regex emailRegex = new Regex(@"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$");
        private const int MAX_NAME_LENGTH = 100;

        public EmployeeService(AppDbContext context)
        {
            _context = context;
        }

        private static void ValidateEmployee(Employee employee)
        {
            //Name
            if (string.IsNullOrEmpty(employee.Name))
            {
                throw new ArgumentException("Name is required!");
            }

            if (employee.Name.Length > MAX_NAME_LENGTH)
            {
                throw new ArgumentException($"Employee name cannot exceed {MAX_NAME_LENGTH} characters");
            }

            //Email
            if (string.IsNullOrEmpty(employee.Email))
            {
                throw new ArgumentException("Email is required!");
            }

            if (!emailRegex.IsMatch(employee.Email))
            {
                throw new ArgumentException("Invalid email, please input the correct format!");
            }

            //Department
            if (string.IsNullOrEmpty(employee.Department))
            {
                throw new ArgumentException("Department is required!");
            }

            bool isValidDepartment = Enum.TryParse<DepartmentType>(employee.Department, true, out _);

            if (!isValidDepartment)
            {
                string validDepartments = string.Join(", ", Enum.GetNames(typeof(DepartmentType)));
                throw new ArgumentException($"Invalid department, please select the following department: {validDepartments}");
            }
        }

        public async Task<List<EmployeeDto>> GetAllEmployee()
        {
            return await _context.Employees
                .Where(e => !e.IsDeleted)
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new EmployeeDto(
                    e.Id,
                    e.Name,
                    e.Email,
                    e.Department,
                    e.DateOfBirth
                ))
                .ToListAsync();
        }

        public async Task<EmployeeDto?> GetEmployeeById(int id)
        {
            var employee = await _context.Employees.FindAsync(id);

            if (employee == null || employee.IsDeleted) 
            {
                return null;
            }

            return new EmployeeDto(
                employee.Id,
                employee.Name,
                employee.Email,
                employee.Department,
                employee.DateOfBirth
            );
        }

        public async Task<List<EmployeeDto>> GetEmployeeByDepartment(string department)
        {
            return await _context.Employees
                .Where(e => e.Department == department && !e.IsDeleted)
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new EmployeeDto(
                    e.Id,
                    e.Name,
                    e.Email,
                    e.Department,
                    e.DateOfBirth
                ))
                .ToListAsync();
        }

        public async Task<EmployeeDto> CreateEmployee(Employee createdEmployee)
        {
            ValidateEmployee(createdEmployee);

            var newEmployee = new Employee
            {
                Name = createdEmployee.Name,
                Email = createdEmployee.Email,
                Department = createdEmployee.Department,
                DateOfBirth = createdEmployee.DateOfBirth,
                CreatedAt = DateTime.Now,
                IsDeleted = false
            };

            _context.Employees.Add(newEmployee);
            await _context.SaveChangesAsync();

            return new EmployeeDto(
                newEmployee.Id,
                newEmployee.Name,
                newEmployee.Email,
                newEmployee.Department,
                newEmployee.DateOfBirth
            );
        }

        public async Task<EmployeeDto?> UpdateEmployee(int id, Employee updatedEmployee)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null || employee.IsDeleted)
            {
                return null;
            }

            ValidateEmployee(updatedEmployee);

            employee.Name = updatedEmployee.Name;
            employee.Email = updatedEmployee.Email;
            employee.Department = updatedEmployee.Department;
            employee.DateOfBirth = updatedEmployee.DateOfBirth;

            await _context.SaveChangesAsync();
            return new EmployeeDto(
                employee.Id,
                employee.Name,
                employee.Email,
                employee.Department,
                employee.DateOfBirth
            );
        }

        public async Task<bool> DeleteEmployee(int id)
        {
            var isExistedEmployee = await _context.Employees.FindAsync(id);
            if (isExistedEmployee == null || isExistedEmployee.IsDeleted)
            {
                return false;
            }

            isExistedEmployee.IsDeleted = true;
            isExistedEmployee.DeletedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
