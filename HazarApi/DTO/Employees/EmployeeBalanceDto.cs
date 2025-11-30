namespace HazarApi.DTO.Employees;

/// <summary>
/// يعرض رصيد الموظف من الزمنيات والـ90 دقيقة.
/// </summary>
public class EmployeeBalanceDto
{
    public int EmployeeId { get; set; }
    public required string EmployeeName { get; set; }
    public int MonthlyTimeOffBalance { get; set; } // الرصيد الحالي من الزمنيات (بالدقائق)
    public int NinetyMinutesBalance { get; set; } // الرصيد الحالي من الـ90 دقيقة
    public int Year { get; set; }
    public int Month { get; set; }
    public int TotalTimeOffUsed { get; set; } // إجمالي الزمنيات المستخدمة هذا الشهر
    public int RemainingTimeOffMinutes { get; set; } // المتبقي من الزمنيات
}

