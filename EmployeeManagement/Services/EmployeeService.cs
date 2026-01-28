using EmployeeManagement.Models;
using EmployeeManagement.DTOs;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Services
{
    public class EmployeeService
    {

        private readonly AppDbContext _context;

        public EmployeeService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<EmployeeDto>> GetAllEmployee()
        {
            return await _context.Employees
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

            if (employee == null) 
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

        public async Task<EmployeeDto> CreateEmployee(Employee createdEmployee)
        {
            var newEmployee = new Employee
            {
                Name = createdEmployee.Name,
                Email = createdEmployee.Email,
                Department = createdEmployee.Department,
                DateOfBirth = createdEmployee.DateOfBirth
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
            if (employee == null)
            {
                return null;
            }

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
            if (isExistedEmployee == null)
            {
                return false;
            }

            _context.Employees.Remove(isExistedEmployee);
            await _context.SaveChangesAsync();
            return true;
        }

    }
}
