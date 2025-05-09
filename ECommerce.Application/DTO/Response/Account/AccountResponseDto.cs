﻿namespace ECommerce.Application.DTO.Response.Account;

public record AccountResponseDto
{
    public int Id { get; set; }
    public required string Role { get; set; }
    public required string Name { get; set; }
    public required string Surname { get; set; }
    public required string Email { get; set; }
    public required string Address { get; set; }
    public required string PhoneNumber { get; set; }
    public required DateTime DateOfBirth { get; init; }
}