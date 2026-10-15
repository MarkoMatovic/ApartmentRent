using Lander.Helpers;
using Lander.src.Common;
using Lander.src.Modules.Billing.Dtos;
using Lander.src.Modules.Billing.Implementation;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lander.src.Modules.Billing.Controllers;

/// <summary>Apartments rented out on the site: who lives there, the rent, and where it is paid.</summary>
[Route(ApiActionsV1.Tenancies)]
[ApiController]
[Authorize]
public class TenanciesController : ApiControllerBase
{
    private readonly ITenancyService _tenancies;
    private readonly IBillService _bills;

    public TenanciesController(ITenancyService tenancies, IBillService bills, IUserInterface userService) : base(userService)
    {
        _tenancies = tenancies;
        _bills = bills;
    }

    private int CurrentUserId => TryGetCurrentUserId() ?? throw new UnauthorizedAccessException();

    [HttpGet]
    public async Task<ActionResult<List<TenancyDto>>> GetMine() => Ok(await _tenancies.GetMineAsync(CurrentUserId));

    [HttpGet(ApiActionsV1.TenancyCandidates)]
    public async Task<ActionResult<List<TenancyCandidateDto>>> GetCandidates([FromQuery] int apartmentId) =>
        Ok(await _tenancies.GetCandidatesAsync(CurrentUserId, apartmentId));

    [HttpGet(ApiActionsV1.Tenancy)]
    public async Task<ActionResult<TenancyDto>> Get([FromRoute] int id) => Ok(await _tenancies.GetAsync(CurrentUserId, id));

    [HttpPost]
    public async Task<ActionResult<TenancyDto>> Create([FromBody] CreateTenancyInputDto input) =>
        Ok(await _tenancies.CreateAsync(CurrentUserId, input));

    [HttpPut(ApiActionsV1.Tenancy)]
    public async Task<ActionResult<TenancyDto>> Update([FromRoute] int id, [FromBody] UpdateTenancyInputDto input) =>
        Ok(await _tenancies.UpdateAsync(CurrentUserId, id, input));

    [HttpPost(ApiActionsV1.TenancyTenants)]
    public async Task<ActionResult<TenancyDto>> AddTenant([FromRoute] int id, [FromBody] AddTenantInputDto input) =>
        Ok(await _tenancies.AddTenantAsync(CurrentUserId, id, input.UserId));

    /// <summary>The landlord removes a tenant; a tenant names themselves to leave.</summary>
    [HttpDelete(ApiActionsV1.TenancyTenant)]
    public async Task<ActionResult<TenancyDto>> RemoveTenant([FromRoute] int id, [FromRoute] int userId) =>
        Ok(await _tenancies.RemoveTenantAsync(CurrentUserId, id, userId));

    [HttpPost(ApiActionsV1.EndTenancy)]
    public async Task<ActionResult<TenancyDto>> End([FromRoute] int id) => Ok(await _tenancies.EndAsync(CurrentUserId, id));

    [HttpGet(ApiActionsV1.TenancyBills)]
    public async Task<ActionResult<BillPageDto>> GetBills([FromRoute] int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20) =>
        Ok(await _bills.GetTenancyBillsAsync(CurrentUserId, id, page, pageSize));
}

/// <summary>Bills of a tenancy and the payments on them. The money goes bank to bank (IPS QR); the site keeps the record.</summary>
[Route(ApiActionsV1.Bills)]
[ApiController]
[Authorize]
public class BillsController : ApiControllerBase
{
    private readonly IBillService _bills;

    public BillsController(IBillService bills, IUserInterface userService) : base(userService)
    {
        _bills = bills;
    }

    private int CurrentUserId => TryGetCurrentUserId() ?? throw new UnauthorizedAccessException();

    [HttpGet(ApiActionsV1.MyBills)]
    public async Task<ActionResult<MyBillsDto>> GetMine() => Ok(await _bills.GetMineAsync(CurrentUserId));

    [HttpGet(ApiActionsV1.BillsSummary)]
    public async Task<ActionResult<BillingSummaryDto>> GetSummary() => Ok(await _bills.GetSummaryAsync(CurrentUserId));

    [HttpGet(ApiActionsV1.BillingProfile)]
    public async Task<ActionResult<BillingProfileDto>> GetProfile() => Ok(await _bills.GetProfileAsync(CurrentUserId));

    [HttpPut(ApiActionsV1.BillingProfile)]
    public async Task<ActionResult<BillingProfileDto>> SaveProfile([FromBody] BillingProfileInputDto input) =>
        Ok(await _bills.SaveProfileAsync(CurrentUserId, input));

    [HttpGet(ApiActionsV1.Bill)]
    public async Task<ActionResult<BillDto>> Get([FromRoute] int id) => Ok(await _bills.GetAsync(CurrentUserId, id));

    [HttpPost]
    public async Task<ActionResult<BillDto>> Create([FromBody] CreateBillInputDto input) =>
        Ok(await _bills.CreateAsync(CurrentUserId, input));

    [HttpPut(ApiActionsV1.Bill)]
    public async Task<ActionResult<BillDto>> Update([FromRoute] int id, [FromBody] UpdateBillInputDto input) =>
        Ok(await _bills.UpdateAsync(CurrentUserId, id, input));

    [HttpPost(ApiActionsV1.CancelBill)]
    public async Task<IActionResult> Cancel([FromRoute] int id)
    {
        await _bills.CancelAsync(CurrentUserId, id);
        return NoContent();
    }

    [HttpGet(ApiActionsV1.CompanyPayment)]
    public async Task<ActionResult<PaymentInstructionsDto>> GetCompanyPayment([FromRoute] int id) =>
        Ok(await _bills.GetCompanyPaymentAsync(CurrentUserId, id));

    [HttpPost(ApiActionsV1.PayCompanyBill)]
    public async Task<ActionResult<BillDto>> PayCompanyBill([FromRoute] int id, [FromBody] PayCompanyBillInputDto? input) =>
        Ok(await _bills.PayCompanyBillAsync(CurrentUserId, id, input ?? new PayCompanyBillInputDto()));

    [HttpPost(ApiActionsV1.UndoCompanyPayment)]
    public async Task<ActionResult<BillDto>> UndoCompanyPayment([FromRoute] int id) =>
        Ok(await _bills.UndoCompanyPaymentAsync(CurrentUserId, id));

    [HttpGet(ApiActionsV1.SharePayment)]
    public async Task<ActionResult<PaymentInstructionsDto>> GetSharePayment([FromRoute] int shareId) =>
        Ok(await _bills.GetSharePaymentAsync(CurrentUserId, shareId));

    [HttpPost(ApiActionsV1.MarkSharePaid)]
    public async Task<ActionResult<BillDto>> MarkSharePaid([FromRoute] int shareId) =>
        Ok(await _bills.MarkSharePaidAsync(CurrentUserId, shareId));

    [HttpPost(ApiActionsV1.UnmarkShare)]
    public async Task<ActionResult<BillDto>> UnmarkShare([FromRoute] int shareId) =>
        Ok(await _bills.UnmarkShareAsync(CurrentUserId, shareId));

    [HttpPost(ApiActionsV1.ConfirmShare)]
    public async Task<ActionResult<BillDto>> ConfirmShare([FromRoute] int shareId) =>
        Ok(await _bills.ConfirmShareAsync(CurrentUserId, shareId));

    [HttpPost(ApiActionsV1.RejectShare)]
    public async Task<ActionResult<BillDto>> RejectShare([FromRoute] int shareId) =>
        Ok(await _bills.RejectShareAsync(CurrentUserId, shareId));
}
