using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SportCourtManagement_FrontEnd.Models.Api;

namespace SportCourtManagement_FrontEnd.Models.Promotions;

/// <summary>
/// Promotion data transfer object.
/// </summary>
public class PromotionDto
{
  public int PromotionId { get; set; }
  public string PromoCode { get; set; } = string.Empty;
  public string PromoName { get; set; } = string.Empty;
  public string? Description { get; set; }
  public DiscountType DiscountType { get; set; }
  public decimal DiscountValue { get; set; }
  public decimal MinOrderAmount { get; set; }
  public decimal? MaxDiscount { get; set; }
  public int? UsageLimit { get; set; }
  public int UsedCount { get; set; }
  public DateTime StartDate { get; set; }
  public DateTime EndDate { get; set; }
  public bool IsActive { get; set; }
}

public enum DiscountType
{
  Percent = 0,
  FixedAmount = 1
}

/// <summary>
/// Request DTO for creating/updating promotions.
/// </summary>
public class PromotionFormDto : IValidatableObject
{
  public int PromotionId { get; set; }

  [Required(ErrorMessage = "Vui lòng nhập mã khuyến mãi")]
  public string PromoCode { get; set; } = string.Empty;

  [Required(ErrorMessage = "Vui lòng nhập tên chương trình")]
  public string PromoName { get; set; } = string.Empty;

  public string? Description { get; set; }
  public DiscountType DiscountType { get; set; }

  [Range(0.01, 1000000000, ErrorMessage = "Giá trị giảm giá phải lớn hơn 0")]
  public decimal DiscountValue { get; set; }

  [Range(0, 1000000000, ErrorMessage = "Đơn tối thiểu không được âm")]
  public decimal MinOrderAmount { get; set; }

  [Range(0.01, 1000000000, ErrorMessage = "Số tiền giảm tối đa phải lớn hơn 0")]
  public decimal? MaxDiscount { get; set; }

  [Range(1, int.MaxValue, ErrorMessage = "Giới hạn sử dụng phải từ 1 lượt trở lên")]
  public int? UsageLimit { get; set; }

  public DateTime StartDate { get; set; } = DateTime.Today;
  public DateTime EndDate { get; set; } = DateTime.Today.AddMonths(1);
  public bool IsActive { get; set; } = true;

  public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
  {
    if (EndDate < StartDate)
    {
      yield return new ValidationResult("Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.", new[] { nameof(EndDate) });
    }
    if (DiscountType == DiscountType.Percent && DiscountValue > 100)
    {
      yield return new ValidationResult("Giảm giá theo phần trăm không được vượt quá 100%.", new[] { nameof(DiscountValue) });
    }
  }
}

/// <summary>
/// View model for promotion management page.
/// </summary>
public class PromotionListViewModel
{
  public PagedResult<PromotionDto> PagedData { get; set; } = new();
  public string? Keyword { get; set; }
  public bool? IsActive { get; set; }
}
