namespace UrbanInfraSystem.Application.DTOs.Staff;

public class StaffDashboardSummaryResponse
{
    public int NewIncidentsInDepartment { get; set; }
    public int MyInProgressTasks { get; set; }
    public int MyPendingAssignments { get; set; }
}