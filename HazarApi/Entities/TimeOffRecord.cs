namespace HazarApi.Entities;

/// <summary>
/// يمثل سجل زمنية مستخدمة من قبل موظف.
/// </summary>
public class TimeOffRecord : FullBaseEntity
{
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public DateOnly TimeOffDate { get; set; }
    public int MinutesUsed { get; set; } // عدد الدقائق المستخدمة
    public required string Reason { get; set; } // سبب الزمنية
    public bool IsUsedForDelay { get; set; } // هل استُخدمت لتغطية تأخير
}

