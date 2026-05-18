using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace TasteAtDoor.Models.ViewModels;

public class AgreementViewModel
{
    public string AgreementNumber { get; set; } = "";
    public DateTime AgreementDate { get; set; } = DateTime.Now;

    public string CustomerName { get; set; } = "";
    public string CustomerEmail { get; set; } = "";
    public string CustomerAddress { get; set; } = "";

    public List<AgreementPackageLineViewModel> Packages { get; set; } = new();

    public int TotalGuestCount => Packages.Sum(p => p.GuestCount);

    public decimal TotalEstimatedPrice => Packages.Sum(p => p.TotalPrice);

    [Required(ErrorMessage = "You must accept the catering agreement before payment.")]
    public bool Accepted { get; set; }
}

public class AgreementPackageLineViewModel
{
    public int MenuItemId { get; set; }

    public string PackageName { get; set; } = "";
    public string CatererName { get; set; } = "";
    public string CatererEmail { get; set; } = "";
    public string CatererAddress { get; set; } = "";

    public byte[]? CatererLogoData { get; set; }
    public string? CatererLogoContentType { get; set; }

    public string EventType { get; set; } = "";
    public string PackageCategory { get; set; } = "";

    public int GuestCount { get; set; }

    public int? MinimumGuestCount { get; set; }
    public int? MaximumGuestCount { get; set; }

    public decimal PricePerPerson { get; set; }

    public decimal TotalPrice => PricePerPerson * GuestCount;

    public string Description { get; set; } = "";
    public string IncludedItems { get; set; } = "";
    public string ServiceDetails { get; set; } = "";

    public bool IncludesMainCourse { get; set; }
    public bool IncludesDessert { get; set; }
    public bool IncludesSnacks { get; set; }
    public bool IncludesDrinks { get; set; }
}
