using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FaceGuardPro.Shared.Enums;

namespace FaceGuardPro.Data.Entities;

[Table("Employees")]
public class Employee
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    [Column("EmployeeId")]
    public string EmployeeId { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Position { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Department { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public DateTime JoinDate { get; set; }

    [Required]
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;

    [StringLength(500)]
    public string? PhotoPath { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<FaceTemplate> FaceTemplates { get; set; } = new List<FaceTemplate>();
    public virtual ICollection<AuthenticationLog> AuthenticationLogs { get; set; } = new List<AuthenticationLog>();

    // Computed properties
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";

    [NotMapped]
    public bool HasFaceTemplate => FaceTemplates.Any();

    [NotMapped]
    public int YearsOfService => DateTime.Now.Year - JoinDate.Year;
}