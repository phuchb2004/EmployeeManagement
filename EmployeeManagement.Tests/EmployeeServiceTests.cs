using EmployeeManagement.Controllers;
using EmployeeManagement.DTOs;
using EmployeeManagement.Models;
using EmployeeManagement.Models.Enums;
using EmployeeManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Threading.Tasks;
using Xunit;

namespace EmployeeManagement.Tests;

public class EmployeeServiceTests
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

        for (int i = 1; i <= 15; i++)
        {
            employees.Add(new Employee
            {
                Id = i,
                Name = $"Employee {i}",
                Email = $"employee{i}@gmail.com",
                Department = "IT",
                IsDeleted = (i == 5),
                CreatedAt = DateTime.Now.AddMinutes(i)
            });
        }
        context.Employees.AddRange(employees);
        await context.SaveChangesAsync();

        var service = new EmployeeService(context);
        var result = await service.GetAllEmployee(1);
        Assert.Equal(14, result.TotalItems);
        Assert.Equal(10, result.Items.Count);
        Assert.DoesNotContain(result.Items, e => e.Name == "Employee 5");
        Assert.Equal("Employee 15", result.Items.First().Name);
    }

    //Create Employee
    [Fact]
    public async Task CreateEmployee_WhenNameIsEmpty()
    {
        using var context = CreateDbContext();
        var service = new EmployeeService(context);
        var invalidEmployee = new Employee
        {
            Name = "",
            Email = "test@mail.com",
            Department = "IT"
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateEmployee(invalidEmployee));
        Assert.Equal("Name is required!", exception.Message);
    }

    [Fact]
    public async Task CreateEmployee_WhenNameIsExceedMaxLength()
    {
        using var context = CreateDbContext();
        var service = new EmployeeService(context);
        var invalidEmployee = new Employee
        {
            Name = new string('a', 101),
            Email = "phuc@gmail.com",
            Department = "IT"
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateEmployee(invalidEmployee));
        Assert.Equal("Employee name cannot exceed 100 characters", exception.Message);
    }

    [Fact]
    public async Task CreateEmployee_WhenEmailIsEmpty()
    {
        using var context = CreateDbContext();
        var service = new EmployeeService(context);
        var invalidEmployee = new Employee
        {
            Name = "Phuc",
            Email = "",
            Department = "IT"
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateEmployee(invalidEmployee));
        Assert.Equal("Email is required!", exception.Message);
    }

    [Fact]
    public async Task CreateEmployee_WhenEmailIsInvalid()
    {
        using var context = CreateDbContext();
        var service = new EmployeeService(context);
        var invalidEmployee = new Employee
        {
            Name = "Phuc",
            Email = "test.gmail.com",
            Department = "HR"
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateEmployee(invalidEmployee));
        Assert.Equal("Invalid email, please input the correct format!", exception.Message);
    }

    [Fact]
    public async Task CreateEmployee_WhenDepartmentIsEmpty()
    {
        using var context = CreateDbContext();
        var service = new EmployeeService(context);
        var invalidEmployee = new Employee
        {
            Name = "Phuc",
            Email = "phuc@gmail.com",
            Department = ""
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateEmployee(invalidEmployee));
        Assert.Equal("Department is required!", exception.Message);
    }

    [Fact]
    public async Task CreateEmployee_WhenDepartmentIsInvalid()
    {
        using var context = CreateDbContext();
        var service = new EmployeeService(context);
        var invalidEmployee = new Employee
        {
            Name = "Phuc",
            Email = "phuchb@gmail.com",
            Department = "Customer Service",
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateEmployee(invalidEmployee));
        Assert.Contains("Invalid department", exception.Message);
    }

    [Fact]
    public async Task CreateEmployee_WhenDataIsValid()
    {
        using var context = CreateDbContext();
        var service = new EmployeeService(context);
        var newEmployee = new Employee
        {
            Name = "Joey",
            Email = "joey1@gmail.com",
            Department = "IT",
            DateOfBirth = new DateTime(2000, 6, 7)
        };

        var result = await service.CreateEmployee(newEmployee);
        Assert.NotNull(result);
        Assert.NotEqual(0, result.Id);
        Assert.Equal("Joey", result.Name);

        var inDB = await context.Employees.FindAsync(result.Id);
        Assert.False(inDB.IsDeleted);
        Assert.NotNull(inDB.CreatedAt);
    }

    //Update Employee
    [Fact]
    public async Task UpdateEmployee_WhenEmployeeIsNotFound()
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
        Assert.Contains($"Update fail, employee with ID {id} is not found!", notFound.Value!.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateEmployee_WhenNameIsEmpty()
    {
        using var context = CreateDbContext();
        context.Employees.Add(new Employee
        {
            Id = 2,
            Name = "phuchb",
            Email = "phuc@gmail.com",
            Department = "IT",
        });
        await context.SaveChangesAsync();

        var service = new EmployeeService(context);
        var controller = new EmployeeController(service);

        var updatedEmployee = new UpdateEmployeeDto
        {
            Name = "",
            Email = "joey@gmail.com",
            Department = "HR"
        };

        var actionResult = await controller.UpdateEmployee(2, updatedEmployee);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var actualMessage = badRequestResult.Value.GetType().GetProperty("message")?.GetValue(badRequestResult.Value, null) as string;
        Assert.Equal("Name is required!", actualMessage);
    }

    [Fact]
    public async Task UpdateEmployee_WhenNameIsExceedMaxLength()
    {
        using var context = CreateDbContext();
        context.Employees.Add(new Employee
        {
            Id = 2,
            Name = "phuchb",
            Email = "phuchb@gmail.com",
            Department = "IT",
        });
        await context.SaveChangesAsync();

        var service = new EmployeeService(context);
        var controller = new EmployeeController(service);

        var updatedEmployee = new UpdateEmployeeDto
        {
            Name = new string('b', 101),
            Email = "joey@gmail.com",
            Department = "HR"
        };

        var actionResult = await controller.UpdateEmployee(2, updatedEmployee);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var actualMessage = badRequestResult.Value.GetType().GetProperty("message")?.GetValue(badRequestResult.Value, null) as string;
        Assert.Equal("Employee name cannot exceed 100 characters", actualMessage);
    }
    
    [Fact]
    public async Task UpdateEmployee_WhenEmailIsEmpty()
    {
        using var context = CreateDbContext();
        context.Employees.Add(new Employee
        {
            Id = 2,
            Name = "phuchb",
            Email = "phuc@gmail.com",
            Department = "IT",
        });
        await context.SaveChangesAsync();

        var service = new EmployeeService(context);
        var controller = new EmployeeController(service);

        var updatedEmployee = new UpdateEmployeeDto
        {
            Name = "Joey",
            Email = "",
            Department = "HR"
        };

        var actionResult = await controller.UpdateEmployee(2, updatedEmployee);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var actualMessage = badRequestResult.Value.GetType().GetProperty("message")?.GetValue(badRequestResult.Value, null) as string;
        Assert.Equal("Email is required!", actualMessage);
    }

    [Fact]
    public async Task UpdateEmployee_WhenEmailIsInvalid()
    {
        using var context = CreateDbContext();
        context.Employees.Add(new Employee
        {
            Id = 2,
            Name = "phuchb",
            Email = "phuc@gmail.com",
            Department = "IT",
        });
        await context.SaveChangesAsync();

        var service = new EmployeeService(context);
        var controller = new EmployeeController(service);

        var updatedEmployee = new UpdateEmployeeDto
        {
            Name = "Joey",
            Email = "test.gmail.com",
            Department = "HR"
        };

        var actionResult = await controller.UpdateEmployee(2, updatedEmployee);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var actualMessage = badRequestResult.Value.GetType().GetProperty("message")?.GetValue(badRequestResult.Value, null) as string;
        Assert.Equal("Invalid email, please input the correct format!", actualMessage);
    }

    [Fact]
    public async Task UpdateEmployee_WhenDepartmentIsEmpty()
    {
        using var context = CreateDbContext();
        context.Employees.Add(new Employee
        {
            Id = 2,
            Name = "phuchb",
            Email = "phuc@gmail.com",
            Department = "IT",
        });
        await context.SaveChangesAsync();

        var service = new EmployeeService(context);
        var controller = new EmployeeController(service);

        var updatedEmployee = new UpdateEmployeeDto
        {
            Name = "Joey",
            Email = "joey@gmail.com",
            Department = ""
        };

        var actionResult = await controller.UpdateEmployee(2, updatedEmployee);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var actualMessage = badRequestResult.Value.GetType().GetProperty("message")?.GetValue(badRequestResult.Value, null) as string;
        Assert.Equal("Department is required!", actualMessage);
    }

    [Fact]
    public async Task UpdateEmployee_WhenDepartmentIsInvalid()
    {
        using var context = CreateDbContext();
        context.Employees.Add(new Employee
        {
            Id = 2,
            Name = "phuchb",
            Email = "phuc@gmail.com",
            Department = "IT",
        });
        await context.SaveChangesAsync();

        var service = new EmployeeService(context);
        var controller = new EmployeeController(service);

        var updatedEmployee = new UpdateEmployeeDto
        {
            Name = "Joey",
            Email = "joey@gmail.com",
            Department = "Customer Service"
        };

        var actionResult = await controller.UpdateEmployee(2, updatedEmployee);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var actualMessage = badRequestResult.Value.GetType().GetProperty("message")?.GetValue(badRequestResult.Value, null) as string;
        string validDepartments = string.Join(", ", Enum.GetNames(typeof(DepartmentType)));
        Assert.Equal($"Invalid department, please select the following department: {validDepartments}", actualMessage);
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
            Name = "Updated",
            Email = "updated@mail.com",
            Department = "HR",
            DateOfBirth = new DateTime(1992, 2, 2)
        };

        var actionResult = await controller.UpdateEmployee(5, updateDto);

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var value = ok.Value!;
        Assert.NotNull(value);
        Assert.Contains($"Employee {updateDto.Name} is updated", value.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    //Delete Employee
    [Fact]
    public async Task DeleteEmployee_WhenSuccess()
    {
        using var context = CreateDbContext();
        context.Employees.Add(new Employee
        {
            Id = 29,
            Name = "omen",
            Email = "omen@gmail.com",
            Department = "HR",
            IsDeleted = false
        });
        await context.SaveChangesAsync();

        var service = new EmployeeService(context);
        var isSuccess = await service.DeleteEmployee(29);
        Assert.True(isSuccess);

        var inDB = await context.Employees.FindAsync(29);
        Assert.NotNull(inDB);
        Assert.True(inDB!.IsDeleted);
        Assert.NotNull(inDB.DeletedAt);
    }

    [Fact]
    public async Task DeleteEmployee_WhenNotFound()
    {
        using var context = CreateDbContext();
        var service = new EmployeeService(context);
        var isSuccess = await service.DeleteEmployee(9999);
        Assert.False(isSuccess);
    }

    //Get Employee By Id
    [Fact]
    public async Task GetEmployeeById_ReturnNullIfSoftDelete()
    {
        using var context = CreateDbContext();
        context.Employees.Add(new Employee
        {
            Id = 14,
            Name = "hbphuc",
            Email = "hbphuc@gmail.com",
            Department = "Sales",
            IsDeleted = true
        });
        await context.SaveChangesAsync();

        var service = new EmployeeService(context);
        var result = await service.GetEmployeeById(14);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetEmployeeById_WhenNotFound()
    {
        using var context = CreateDbContext();
        var service = new EmployeeService(context);
        var controller = new EmployeeController(service);

        int id = 9999;
        var actionResult = await controller.GetEmployeeById(id);

        var notFound = Assert.IsType<NotFoundObjectResult>(actionResult.Result);
        Assert.Contains($"Employee with ID {id} is not found", notFound.Value!.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetEmployeeById_WhenExist()
    {
        using var context = CreateDbContext();
        context.Employees.Add(new Employee
        {
            Id = 42,
            Name = "Test",
            Email = "test@mail.com",
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
        Assert.Equal("Test", dto.Name);
    }

    //Get Employee By Department
    [Fact]
    public async Task GetEmployeeByDepartment_ReturnOkWithPagination()
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
}
