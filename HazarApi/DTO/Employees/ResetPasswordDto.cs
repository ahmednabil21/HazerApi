namespace HazarApi.DTO.Employees;

/// <summary>
/// يعبر عن طلب إعادة تعيين كلمة مرور لموظف.
/// </summary>
public class ResetPasswordDto
{
    public required string NewPassword { get; set; }
}

