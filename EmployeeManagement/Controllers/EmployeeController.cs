using EmployeeManagement.DTOs;
using EmployeeManagement.Services;
using EmployeeManagement.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using EmployeeManagement.Models.Enums;

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
        public async Task<ActionResult<Pagination<EmployeeDto>>> GetAllEmployee([FromQuery] int page = 1)
        {
            if (page < 1)
            {
                page = 1;
            }

            var result = await _service.GetAllEmployee(page);
            return Ok(result);
        }

        [HttpGet("get-employee-by-id/{id}")]
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

        [HttpGet("get-employee-by-department")]
        public async Task<ActionResult<Pagination<EmployeeDto>>> GetEmployeeByDepartment([FromQuery] DepartmentType department, int page = 1)
        {
            if (page < 1)
            {
                page = 1;
            }

            string departmentSelected = department.ToString();

            var result = await _service.GetEmployeeByDepartment(departmentSelected, page);

            return Ok(result);
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
            var IsExistedEmployee = await _service.DeleteEmployee(id);
            if (!IsExistedEmployee)
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
