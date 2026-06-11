using System.ComponentModel.DataAnnotations;

public class RegisterViewModel
{
    [Required]
    [Display(Name = "Display Name")]
    [MaxLength(50)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirm Passwor")]
    [Compare("Password", ErrorMessage = "Пароль не совпадает.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}