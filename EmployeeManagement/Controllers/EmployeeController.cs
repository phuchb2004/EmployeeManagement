using EmployeeManagement.DTOs;
using EmployeeManagement.Services;
using EmployeeManagement.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.Controllers
{
    [Route("api/employees")]
    [ApiController]
    public class EmployeeController : ControllerBase
    {
        private readonly EmployeeService _service;

        public EmployeeController(EmployeeService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<EmployeeDto>>> GetAllEmployee()
        {
            var employees = await _service.GetAllEmployee();
            return Ok(employees);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<EmployeeDto>> GetEmployeeById(int id)
        {
            var employee = await _service.GetEmployeeById(id);
            if (employee == null)
            {
                return NotFound(new {
                    message = $"Employee with ID {id} is not found"
                });
            }

            return Ok(employee);
        }

        [HttpPost]
        public async Task<ActionResult<EmployeeDto>> CreateEmployee([FromBody] CreateEmployeeDto requestDto)
        {
            try
            {
                var newEmployee = new Employee
                {
                    Name = requestDto.Name,
                    Email = requestDto.Email,
                    Department = requestDto.Department,
                    DateOfBirth = requestDto.DateOfBirth
                };

                var createdEmployee = await _service.CreateEmployee(newEmployee);

                return CreatedAtAction(nameof(GetEmployeeById), new { id = createdEmployee.Id }, new
                {
                    message = "Employee Created!",
                    createdEmployee
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }

        }

        [HttpPut("{id}")]
        public async Task<ActionResult<EmployeeDto>> UpdateEmployee(int id, [FromBody] UpdateEmployeeDto requestDto)
        {
            try
            {
                var existedEmployee = new Employee
                {
                    Name = requestDto.Name,
                    Email = requestDto.Email,
                    Department = requestDto.Department,
                    DateOfBirth = requestDto.DateOfBirth
                };

                var updatedEmployee = await _service.UpdateEmployee(id, existedEmployee);
                if (updatedEmployee == null)
                {
                    return NotFound(new
                    {
                        message = $"Update fail, employee with ID {id} is not found!"
                    });
                }

                return Ok(new
                {
                    message = $"Employee {updatedEmployee.Name} is updated",
                    updatedEmployee
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<EmployeeDto>> DeleteEmployee(int id)
        {
            var employee = await _service.DeleteEmployee(id);
            if (employee == null)
            {
                return NotFound(new {
                    message = $"Delete fail, employee with ID {id} is not found!"
                });
            }

            return Ok(new {
                message = "Employee deleted!"
            });
        }
    }
}
