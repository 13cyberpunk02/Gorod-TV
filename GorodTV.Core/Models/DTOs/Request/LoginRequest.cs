namespace GorodTV.Core.Models.DTOs.Request;

/// <summary>Данные для авторизации: номер договора + пароль.</summary>
public record LoginRequest(string ContractNumber, string Password);
