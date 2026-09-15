using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Services.Billing;

namespace ZynstormECFPlatform.Web.Api.Controllers
{
    [Authorize(Roles = "SA")]
    public class PlanController(
        IPlanService planService,
        IPlanOverageTierService planOverageTierService,
        IMapper mapper,
        ILoggerFactory loggerFactory) : BaseController<PlanController, Plan, PlanCreateDto, PlanUpdateDto, PlanViewDto>(planService, mapper, loggerFactory)
    {
        private IQueryable<Plan> PlansWithDetails =>
            Repository.Table.AsNoTracking().Include(p => p.OverageTiers).Include(p => p.Clients);

        [HttpGet]
        [Route("", Order = 1)]
        public override async Task<ActionResult> Get([FromQuery] string? guidId, [FromQuery] string? id, CancellationToken cancellationToken = default)
        {
            try
            {
                var plans = await PlansWithDetails.OrderBy(p => p.Name).ToListAsync(cancellationToken);
                return Ok(Mapper.Map<IEnumerable<Plan>, IEnumerable<PlanViewDto>>(plans));
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, exception.Message);
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        [HttpGet]
        [Route("guid/{guid}", Order = 1)]
        public override async Task<ActionResult<PlanViewDto>> GetByGuid(string guid, CancellationToken cancellationToken = default)
        {
            try
            {
                var plan = await PlansWithDetails.FirstOrDefaultAsync(p => p.GuidId == guid, cancellationToken);
                if (plan == null) return NotFound();
                return Ok(Mapper.Map<Plan, PlanViewDto>(plan));
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, exception.Message);
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        [HttpPost]
        [Route("", Order = 1)]
        public override async Task<ActionResult<PlanViewDto>> Post([FromBody] PlanCreateDto dto)
        {
            var errors = Validate(dto);
            if (errors.Count > 0)
                return BadRequest(new { success = false, message = string.Join(" ", errors), errors });

            try
            {
                var model = Mapper.Map<PlanCreateDto, Plan>(dto);
                model = await Repository.InsertAsync(model);
                return Ok(Mapper.Map<Plan, PlanViewDto>(model!));
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, exception.Message);
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        [HttpPut]
        [Route("", Order = 1)]
        public override async Task<ActionResult<PlanViewDto>> Put([FromBody] PlanUpdateDto dto)
        {
            var errors = Validate(dto);
            if (errors.Count > 0)
                return BadRequest(new { success = false, message = string.Join(" ", errors), errors });

            try
            {
                var model = await Repository.Table
                    .Include(p => p.OverageTiers)
                    .Include(p => p.Clients)
                    .FirstOrDefaultAsync(p => p.GuidId == dto.GuidId);

                if (model == null)
                    return NotFound("No se encontró el plan.");

                Mapper.Map(dto, model);

                var previousTiers = model.OverageTiers.ToList();
                if (previousTiers.Count > 0)
                    await planOverageTierService.HardDeleteAsync(previousTiers);

                model.OverageTiers = dto.OverageTiers
                    .Select(t => new PlanOverageTier { FromUnit = t.FromUnit, ToUnit = t.ToUnit, UnitPrice = t.UnitPrice })
                    .ToList();

                await Repository.UpdateAsync(model);

                return Ok(Mapper.Map<Plan, PlanViewDto>(model));
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, exception.Message);
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        private static List<string> Validate(PlanCreateDto dto) =>
            BillingCalculator.ValidatePlan(
                dto.MonthlyDocumentLimit,
                dto.MonthlyFee,
                dto.OverageTiers.Select(t => new OverageTier(t.FromUnit, t.ToUnit, t.UnitPrice)));
    }
}
