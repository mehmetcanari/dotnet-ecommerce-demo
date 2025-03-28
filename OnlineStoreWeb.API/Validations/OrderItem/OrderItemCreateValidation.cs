﻿using FluentValidation;
using OnlineStoreWeb.API.DTO.Request.OrderItem;

namespace OnlineStoreWeb.API.Validations.OrderItem;

public class OrderItemCreateValidation : AbstractValidator<CreateOrderItemDto>
{
    public OrderItemCreateValidation()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithMessage("Account Id is required.")
            .GreaterThan(0)
            .WithMessage("Account Id must be greater than 0.");
        
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Product Id is required.")
            .GreaterThan(0)
            .WithMessage("Product Id must be greater than 0.");
        
        RuleFor(x => x.Quantity)
            .NotEmpty()
            .WithMessage("Quantity is required.")
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0.");
    }
}