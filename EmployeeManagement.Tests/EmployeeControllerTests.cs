using EmployeeManagement.Controllers;
using EmployeeManagement.DTOs;
using EmployeeManagement.Models;
using EmployeeManagement.Models.Enums;
using EmployeeManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EmployeeManagement.Tests
{
    public class EmployeeControllerTests
    {
        private static AppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new AppDbContext(options);
            return context;
        }

        //Get All Employee
        [Fact]
        public async Task GetAllEmployee_ReturnCorrectPagination()
        {
            using var context = CreateDbContext();
            var employees = new List<Employee>();
            for (int i = 1; i <= 12; i++)
            {
                employees.Add(new Employee
                {
                    Id = i,
                    Name = $"Employee {i}",
                    Email = $"employee{i}@mail.com",
                    Department = "IT",
                    IsDeleted = (i == 3),
                    CreatedAt = DateTime.Now.AddMinutes(i)
                });
            }
            context.Employees.AddRange(employees);
            await context.SaveChangesAsync();

            var service = new EmployeeService(context);
            var controller = new EmployeeController(service);

            var actionResult = await controller.GetAllEmployee(1);
            var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            var pagination = Assert.IsType<Pagination<EmployeeDto>>(ok.Value);
            Assert.Equal(11, pagination.TotalItems);
            Assert.Equal(10, pagination.Items.Count);
            Assert.DoesNotContain(pagination.Items, e => e.Name == "Employee 3");
            Assert.Equal("Employee 12", pagination.Items.First().Name);
        }

        [Fact]
        public async Task GetAllEmployee_PageLessThanOne_DefaultsToFirstPage()
        {
            using var context = CreateDbContext();
            for (int i = 1; i <= 5; i++)
            {
                context.Employees.Add(new Employee
                {
                    Id = i,
                    Name = $"E{i}",
                    Email = $"e{i}@mail.com",
                    Department = "HR",
                    CreatedAt = DateTime.Now.AddMinutes(i)
                });
            }
            await context.SaveChangesAsync();

            var service = new EmployeeService(context);
            var controller = new EmployeeController(service);

            var actionResult = await controller.GetAllEmployee(0);
            var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            var pagination = Assert.IsType<Pagination<EmployeeDto>>(ok.Value);
            Assert.Equal(5, pagination.TotalItems);
            Assert.Equal(5, pagination.Items.Count);
            Assert.Equal(1, pagination.PageIndex);
        }

        //Get Employee By Id
        [Fact]
        public async Task GetEmployeeById_WhenNotFound()
        {
            using var context = CreateDbContext();
            var service = new EmployeeService(context);
            var controller = new EmployeeController(service);

            int id = 9999;
            var actionResult = await controller.GetEmployeeById(id);

            var notFound = Assert.IsType<NotFoundObjectResult>(actionResult.Result);
            var msg = notFound.Value!.ToString() ?? string.Empty;
            Assert.Contains($"Employee with ID {id} is not found", msg, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GetEmployeeById_WhenExist()
        {
            using var context = CreateDbContext();
            context.Employees.Add(new Employee
            {
                Id = 42,
                Name = "TestUser",
                Email = "testuser@mail.com",
                Department = "HR",
                DateOfBirth = new DateTime(1990, 1, 1)
            });
            await context.SaveChangesAsync();

            var service = new EmployeeService(context);
            var controller = new EmployeeController(service);

            var actionResult = await controller.GetEmployeeById(42);
            var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            var dto = Assert.IsType<EmployeeDto>(ok.Value);
            Assert.Equal(42, dto.Id);
            Assert.Equal("TestUser", dto.Name);
            Assert.Equal("HR", dto.Department);
        }

        //Get Employee By Department
        [Fact]
        public async Task GetEmployeeByDepartment_ReturnsOkWithPagination()
        {
            using var context = CreateDbContext();
            context.Employees.AddRange(
                new Employee { Id = 1, Name = "A", Email = "a@mail.com", Department = "IT", CreatedAt = DateTime.Now },
                new Employee { Id = 2, Name = "B", Email = "b@mail.com", Department = "HR", CreatedAt = DateTime.Now },
                new Employee { Id = 3, Name = "C", Email = "c@mail.com", Department = "IT", CreatedAt = DateTime.Now }
            );
            await context.SaveChangesAsync();

            var service = new EmployeeService(context);
            var controller = new EmployeeController(service);

            var actionResult = await controller.GetEmployeeByDepartment(DepartmentType.IT, 1);

            var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            var pagination = Assert.IsType<Pagination<EmployeeDto>>(ok.Value);
            Assert.Equal(2, pagination.TotalItems);
            Assert.All(pagination.Items, item => Assert.Equal("IT", item.Department));
        }

        [Fact]
        public async Task GetEmployeeByDepartment_LessThanOnePage()
        {
            using var context = CreateDbContext();
            context.Employees.Add(new Employee { Id = 1, Name = "A", Email = "a@m.com", Department = "IT" });
            await context.SaveChangesAsync();

            var service = new EmployeeService(context);
            var controller = new EmployeeController(service);

            var actionResult = await controller.GetEmployeeByDepartment(DepartmentType.IT, -5);

            var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            var pagination = Assert.IsType<Pagination<EmployeeDto>>(ok.Value);

            Assert.Equal(1, pagination.PageIndex);
            Assert.Equal(1, pagination.TotalItems);
        }

        //Create Employee
        [Fact]
        public async Task CreateEmployee_WhenNameIsEmpty()
        {
            using var context = CreateDbContext();
            var controller = new EmployeeController(new EmployeeService(context));

            var createDto = new CreateEmployeeDto
            {
                Name = "",
                Email = "test@mail.com",
                Department = "IT",
                DateOfBirth = new DateTime(2000, 1, 1)
            };

            var actionResult = await controller.CreateEmployee(createDto);

            var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            var message = badRequest.Value!.GetType().GetProperty("message")?.GetValue(badRequest.Value, null) as string;
            Assert.Equal("Name is required!", message);
        }

        [Fact]
        public async Task CreateEmployee_WhenNameExceedsMaxLength()
        {
            using var context = CreateDbContext();
            var controller = new EmployeeController(new EmployeeService(context));

            var createDto = new CreateEmployeeDto
            {
                Name = new string('a', 101),
                Email = "test@mail.com",
                Department = "IT",
                DateOfBirth = new DateTime(2000, 1, 1)
            };

            var actionResult = await controller.CreateEmployee(createDto);

            var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            var message = badRequest.Value!.GetType().GetProperty("message")?.GetValue(badRequest.Value, null) as string;
            Assert.Equal("Employee name cannot exceed 100 characters", message);
        }

        [Fact]
        public async Task CreateEmployee_WhenEmailIsEmpty()
        {
            using var context = CreateDbContext();
            var controller = new EmployeeController(new EmployeeService(context));

            var createDto = new CreateEmployeeDto
            {
                Name = "Test",
                Email = "",
                Department = "IT",
                DateOfBirth = new DateTime(2000, 1, 1)
            };

            var actionResult = await controller.CreateEmployee(createDto);

            var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            var message = badRequest.Value!.GetType().GetProperty("message")?.GetValue(badRequest.Value, null) as string;
            Assert.Equal("Email is required!", message);
        }

        [Fact]
        public async Task CreateEmployee_WhenEmailIsInvalid()
        {
            using var context = CreateDbContext();
            var service = new EmployeeService(context);
            var controller = new EmployeeController(service);

            var createDto = new CreateEmployeeDto
            {
                Name = "New",
                Email = "invalid-email",
                Department = "IT",
                DateOfBirth = new DateTime(1995, 5, 5)
            };

            var actionResult = await controller.CreateEmployee(createDto);
            var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            var message = badRequest.Value!.GetType().GetProperty("message")?.GetValue(badRequest.Value, null) as string;
            Assert.Equal("Invalid email, please input the correct format!", message);
        }

        [Fact]
        public async Task CreateEmployee_WhenDepartmentIsEmpty()
        {
            using var context = CreateDbContext();
            var controller = new EmployeeController(new EmployeeService(context));

            var createDto = new CreateEmployeeDto
            {
                Name = "Test",
                Email = "test@mail.com",
                Department = "",
                DateOfBirth = new DateTime(2000, 1, 1)
            };

            var actionResult = await controller.CreateEmployee(createDto);

            var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            var message = badRequest.Value!.GetType().GetProperty("message")?.GetValue(badRequest.Value, null) as string;
            Assert.Equal("Department is required!", message);
        }

        [Fact]
        public async Task CreateEmployee_WhenDepartmentIsInvalid()
        {
            using var context = CreateDbContext();
            var controller = new EmployeeController(new EmployeeService(context));

            var createDto = new CreateEmployeeDto
            {
                Name = "Test",
                Email = "test@mail.com",
                Department = "Customer Service",
                DateOfBirth = new DateTime(2000, 1, 1)
            };

            var actionResult = await controller.CreateEmployee(createDto);

            var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            var message = badRequest.Value!.GetType().GetProperty("message")?.GetValue(badRequest.Value, null) as string;
            Assert.Contains("Invalid department", message);
        }

        [Fact]
        public async Task CreateEmployee_WhenDataIdValid()
        {
            using var context = CreateDbContext();
            var service = new EmployeeService(context);
            var controller = new EmployeeController(service);

            var createDto = new CreateEmployeeDto
            {
                Name = "Created",
                Email = "created@mail.com",
                Department = "IT",
                DateOfBirth = new DateTime(2000, 1, 1)
            };

            var actionResult = await controller.CreateEmployee(createDto);
            var created = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
            Assert.Equal(nameof(controller.GetEmployeeById), created.ActionName);

            var value = created.Value!;
            var createdMessage = value.GetType().GetProperty("message")?.GetValue(value, null) as string;
            Assert.Equal("Employee Created!", createdMessage);

            var createdEmployee = value.GetType().GetProperty("createdEmployee")?.GetValue(value, null);
            Assert.NotNull(createdEmployee);
            var dto = Assert.IsType<EmployeeDto>(createdEmployee);
            Assert.Equal("Created", dto.Name);
            Assert.Equal("created@mail.com", dto.Email);
        }

        //Update Employee
        [Fact]
        public async Task UpdateEmployee_WhenNotFound()
        {
            using var context = CreateDbContext();
            var service = new EmployeeService(context);
            var controller = new EmployeeController(service);

            var updateDto = new UpdateEmployeeDto
            {
                Name = "X",
                Email = "x@mail.com",
                Department = "IT"
            };

            int id = 99999;
            var actionResult = await controller.UpdateEmployee(id, updateDto);

            var notFound = Assert.IsType<NotFoundObjectResult>(actionResult.Result);
            var msg = notFound.Value!.ToString() ?? string.Empty;
            Assert.Contains($"Update fail, employee with ID {id} is not found!", msg, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task UpdateEmployee_WhenNameExceedsMaxLength()
        {
            using var context = CreateDbContext();
            context.Employees.Add(new Employee { Id = 1, Name = "Old", Email = "old@m.com", Department = "IT" });
            await context.SaveChangesAsync();

            var controller = new EmployeeController(new EmployeeService(context));

            var updateDto = new UpdateEmployeeDto
            {
                Name = new string('a', 101),
                Email = "test@mail.com",
                Department = "IT",
                DateOfBirth = new DateTime(2000, 1, 1)
            };

            var actionResult = await controller.UpdateEmployee(1, updateDto);

            var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            var message = badRequest.Value!.GetType().GetProperty("message")?.GetValue(badRequest.Value, null) as string;
            Assert.Equal("Employee name cannot exceed 100 characters", message);
        }

        [Fact]
        public async Task UpdateEmployee_WhenNameIsInvalid()
        {
            using var context = CreateDbContext();
            context.Employees.Add(new Employee
            {
                Id = 2,
                Name = "phuchb",
                Email = "phuc@mail.com",
                Department = "IT",
            });
            await context.SaveChangesAsync();

            var service = new EmployeeService(context);
            var controller = new EmployeeController(service);

            var updatedEmployee = new UpdateEmployeeDto
            {
                Name = "",
                Email = "joey@mail.com",
                Department = "HR"
            };

            var actionResult = await controller.UpdateEmployee(2, updatedEmployee);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            var actualMessage = badRequestResult.Value.GetType().GetProperty("message")?.GetValue(badRequestResult.Value, null) as string;
            Assert.Equal("Name is required!", actualMessage);
        }

        [Fact]
        public async Task UpdateEmployee_WhenNameIsEmpty()
        {
            using var context = CreateDbContext();
            context.Employees.Add(new Employee { Id = 1, Name = "", Email = "old@m.com", Department = "IT" });
            await context.SaveChangesAsync();

            var controller = new EmployeeController(new EmployeeService(context));

            var updateDto = new UpdateEmployeeDto
            {
                Name = "Test",
                Email = "",
                Department = "IT",
                DateOfBirth = new DateTime(2000, 1, 1)
            };

            var actionResult = await controller.UpdateEmployee(1, updateDto);

            var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            var message = badRequest.Value!.GetType().GetProperty("message")?.GetValue(badRequest.Value, null) as string;
            Assert.Equal("Email is required!", message);
        }

        [Fact]
        public async Task UpdateEmployee_WhenEmailIsEmpty()
        {
            using var context = CreateDbContext();
            context.Employees.Add(new Employee 
            {
                Id = 2,
                Name = "Old",
                Email = "old@m.com",
                Department = "IT" 
            });
            await context.SaveChangesAsync();

            var controller = new EmployeeController(new EmployeeService(context));

            var updateDto = new UpdateEmployeeDto
            {
                Name = "Test",
                Email = "",
                Department = "IT",
                DateOfBirth = new DateTime(2000, 1, 1)
            };

            var actionResult = await controller.UpdateEmployee(2, updateDto);
            var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            var message = badRequest.Value!.GetType().GetProperty("message")?.GetValue(badRequest.Value, null) as string;
            Assert.Equal("Email is required!", message);
        }

        [Fact]
        public async Task UpdateEmployee_WhenEmailIsInvalid()
        {
            using var context = CreateDbContext();
            context.Employees.Add(new Employee
            {
                Id = 3,
                Name = "Phuc",
                Email = "phuc@gmail.com",
                Department = "Sales",
                DateOfBirth = new DateTime(1995, 5, 2)
            });
            await context.SaveChangesAsync();

            var controller = new EmployeeController(new EmployeeService(context));

            var updateDto = new UpdateEmployeeDto
            {
                Name = "Joey",
                Email = "phuc.gmail.com",
                Department = "HR",
                DateOfBirth = new DateTime(1995, 5, 2)
            };

            var actionResult = await controller.UpdateEmployee(3, updateDto);
            var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            var message = badRequest.Value!.GetType().GetProperty("message")?.GetValue(badRequest.Value, null) as string;
            Assert.Equal("Invalid email, please input the correct format!", message);
        }

        [Fact]
        public async Task UpdateEmployee_WhenDepartmentIsEmpty()
        {
            using var context = CreateDbContext();
            context.Employees.Add(new Employee
            {
                Id = 4,
                Name = "Chamber",
                Email = "cham@gmail.com",
                Department = "Manager",
                DateOfBirth = new DateTime(1990, 3, 3),
            });
            await context.SaveChangesAsync();

            var controller = new EmployeeController(new EmployeeService(context));

            var updateDto = new UpdateEmployeeDto
            {
                Name = "Chamber",
                Email = "cham@gmail.com",
                Department = "",
                DateOfBirth = new DateTime(1990, 3, 3)
            };

            var actionResult = await controller.UpdateEmployee(4, updateDto);
            var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            var message = badRequest.Value!.GetType().GetProperty("message")?.GetValue(badRequest.Value, null) as string;
            Assert.Equal("Department is required!", message);
        }

        [Fact]
        public async Task UpdateEmployee_WhenDepartmentIsInvalid()
        {
            using var context = CreateDbContext();
            context.Employees.Add(new Employee
            {
                Id = 2,
                Name = "Garou",
                Email = "garou@gmail.com",
                Department = "Customer Service",
                DateOfBirth = new DateTime(1988, 4, 4),
            });
            await context.SaveChangesAsync();

            var controller = new EmployeeController(new EmployeeService(context));

            var updateDto = new UpdateEmployeeDto
            {
                Name = "Garou",
                Email = "garou@gmail.com",
                Department = "Customer Service",
                DateOfBirth = new DateTime(1988, 4, 4)
            };

            var actionResult = await controller.UpdateEmployee(2, updateDto);
            var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            var message = badRequest.Value!.GetType().GetProperty("message")?.GetValue(badRequest.Value, null) as string;
            Assert.Contains("Invalid department", message);
        }

        [Fact]
        public async Task UpdateEmployee_WhenDataIsValid()
        {
            using var context = CreateDbContext();
            context.Employees.Add(new Employee
            {
                Id = 5,
                Name = "Old",
                Email = "old@mail.com",
                Department = "HR",
                DateOfBirth = new DateTime(1992, 2, 2)
            });
            await context.SaveChangesAsync();

            var service = new EmployeeService(context);
            var controller = new EmployeeController(service);

            var updateDto = new UpdateEmployeeDto
            {
                Name = "UpdatedName",
                Email = "updated@mail.com",
                Department = "HR",
                DateOfBirth = new DateTime(1992, 2, 2)
            };

            var actionResult = await controller.UpdateEmployee(5, updateDto);
            var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            var value = ok.Value!;
            Assert.Contains($"Employee {updateDto.Name} is updated", value.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        // Delete Employee
        [Fact]
        public async Task DeleteEmployee_WhenNotFound()
        {
            using var context = CreateDbContext();
            var service = new EmployeeService(context);
            var controller = new EmployeeController(service);

            var actionResult = await controller.DeleteEmployee(9999);
            var notFound = Assert.IsType<NotFoundObjectResult>(actionResult.Result);
            var msg = notFound.Value!.ToString() ?? string.Empty;
            Assert.Contains($"Delete fail, employee with ID 9999 is not found!", msg, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task DeleteEmployee_Success_ReturnsOk()
        {
            using var context = CreateDbContext();
            context.Employees.Add(new Employee
            {
                Id = 29,
                Name = "ToDelete",
                Email = "delete@mail.com",
                Department = "HR",
                IsDeleted = false
            });
            await context.SaveChangesAsync();

            var service = new EmployeeService(context);
            var controller = new EmployeeController(service);

            var actionResult = await controller.DeleteEmployee(29);
            var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            var message = ok.Value!.GetType().GetProperty("message")?.GetValue(ok.Value, null) as string;
            Assert.Equal("Employee deleted!", message);

            var inDb = await context.Employees.FindAsync(29);
            Assert.True(inDb!.IsDeleted);
        }
    }
}
