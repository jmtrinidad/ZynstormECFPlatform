using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Web.Api.Controllers;

//[JsonConverter(typeof(JsonInheritanceConverter), "discriminator")]
//[KnownType(typeof(UserController))]
//[Authorize]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/[controller]")]
[ApiController]
public abstract class BaseController<TController, TModel, TCreateDto, TUpdateDto, TViewDto>(
    IRepository<TModel> repository,
    IMapper mapper,
    ILoggerFactory loggerFactory) : ControllerBase
        where TController : class
        where TModel : BaseEntity
        where TCreateDto : class
        where TUpdateDto : class
        where TViewDto : class

{
    protected readonly IRepository<TModel> Repository = repository;
    protected readonly ILogger<TController> Logger = loggerFactory.CreateLogger<TController>();
    protected readonly IMapper Mapper = mapper;

    //TODO: CUANDO TENGAS TIEMPO MEJORA ESTE ENPOINT PARA QUE UTILICES POR COMPLETO EL DataTableFilter.
    //NO SE ESTAN USANDO EL FILTERED AND SORTERED, SOLO ESTA LA OPCION DE NEXT PAGE Y PAGESIZE
    //[Route("paginate", Order = 1)]
    //[HttpPost]
    //[ProducesResponseType(200)]
    //[ProducesResponseType(401)]
    //[ProducesResponseType(422)]
    //[ProducesResponseType(503)]
    //public virtual async Task<ActionResult<TDataTableDto>> Paginate([FromBody] DataTableFilter dto)
    //{
    //    try
    //    {
    //        var results = await Repository.GetAllPagedAsync(dto);
    //        var dtos = Mapper.Map<IEnumerable<TModel>, IEnumerable<TViewDto>>(results);
    //        var instance = Activator.CreateInstance(typeof(TDataTableDto)) as TDataTableDto;

    //        var propData = typeof(TDataTableDto).GetProperty("Data");
    //        var propRecordsTotal = typeof(TDataTableDto).GetProperty("RecordsTotal");
    //        var propRecordsFiltered = typeof(TDataTableDto).GetProperty("RecordsFiltered");
    //        var propItemsPerPage = typeof(TDataTableDto).GetProperty("ItemsPerPage");
    //        var propTotalPage = typeof(TDataTableDto).GetProperty("TotalPage");

    //        propData?.SetValue(instance, dtos, null);
    //        propRecordsTotal?.SetValue(instance, results.TotalItemCount, null);
    //        propRecordsFiltered?.SetValue(instance, results.TotalItemCount, null);
    //        propItemsPerPage?.SetValue(instance, results.PageSize, null);
    //        propTotalPage?.SetValue(instance, results.PageCount, null);

    //        return instance;
    //    }
    //    catch (AutoMapperMappingException exception)
    //    {
    //        Logger.LogError(exception, exception.Message);
    //        return StatusCode(422,
    //            exception.InnerException != null ?
    //                exception.InnerException.Message
    //                : "Error validando campos"
    //        );
    //    }
    //    catch (Exception exception)
    //    {
    //        Logger.LogError(exception, exception.Message);
    //        return StatusCode(StatusCodes.Status503ServiceUnavailable);
    //    }
    //}

    [Route("all", Order = 1)]
    [HttpGet]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(422)]
    [ProducesResponseType(499)]
    [ProducesResponseType(503)]
    public virtual async Task<ActionResult<IEnumerable<TViewDto>>> Get(CancellationToken cancellationToken = default)
    {
        try
        {
            var results = await Repository.GetAllAsync(cancellationToken);

            return Ok(Mapper.Map<IEnumerable<TModel>, IEnumerable<TViewDto>>(results));
        }
        catch (AutoMapperMappingException exception)
        {
            Logger.LogError(exception, exception.Message);

            return StatusCode(422,
                exception.InnerException != null ?
                    exception.InnerException.Message
                    : "Error validando campos"
            );
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("La solicitud fue cancelada por el cliente.");

            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, exception.Message);

            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    [HttpGet]
    [Route("", Order = 1)]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    [ProducesResponseType(499)]
    [ProducesResponseType(503)]
    public virtual async Task<ActionResult<TViewDto>> GetById([FromQuery] int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await Repository.GetAsync(id, cancellationToken);

            if (result == null)
                return NotFound();

            return Ok(Mapper.Map<TModel, TViewDto>(result));
        }
        catch (AutoMapperMappingException exception)
        {
            Logger.LogError(exception, exception.Message);
            return StatusCode(422,
                exception.InnerException != null ?
                    exception.InnerException.Message
                    : "Error validando campos"
            );
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("La solicitud fue cancelada por el cliente.");

            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, exception.Message);
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    [HttpDelete]
    [Route("", Order = 1)]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    [ProducesResponseType(503)]
    public virtual async Task<IActionResult> Delete([FromQuery] int id)
    {
        try
        {
            var result = await Repository.GetAsync(id);

            if (result == null)
                return NotFound();

            await Repository.SoftDeleteAsync(id);

            return NoContent();
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, exception.Message);
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    [HttpPost]
    [Route("", Order = 1)]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(422)]
    [ProducesResponseType(503)]
    public virtual async Task<ActionResult<TViewDto>> Post([FromBody] TCreateDto dto)
    {
        try
        {
            var model = Mapper.Map<TCreateDto, TModel>(dto);

            model = await Repository.InsertAsync(model);

            return Ok(Mapper.Map<TModel, TViewDto>(model!));
        }
        catch (AutoMapperMappingException exception)
        {
            Logger.LogError(exception, exception.Message);
            return StatusCode(422,
                exception.InnerException != null ?
                    exception.InnerException.Message
                    : "Error validando campos"
            );
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx &&
                                   (sqlEx.Number == 2601 || sqlEx.Number == 2627))
        {
            //IX_Client_DocumentTypeId_Document
            Logger.LogError(ex, ex.Message);

            var message = ex.InnerException.Message.Contains("DocumentTypeId_Document") ?
                                               "Ya existe un registro con ese tipo y número de documento." : "Existe un registro con esta descripción.";
            return Conflict(new
            {
                error = "Duplicate",
                message,
                code = 409
            });
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, exception.Message);
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    [HttpPut]
    [Route("", Order = 1)]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    [ProducesResponseType(503)]
    public virtual async Task<ActionResult<TViewDto>> Put([FromBody] TUpdateDto dto)
    {
        try
        {
            var prop = typeof(TUpdateDto).GetProperty(typeof(TModel).Name + "Id");

            var id = prop?.GetValue(dto) as int?;

            var model = await Repository.GetAsync(id ?? 0);

            if (model == null)
                return NotFound();

            Mapper.Map(dto, model);

            await Repository.UpdateAsync(model);

            return Mapper.Map<TModel, TViewDto>(model);
        }
        catch (AutoMapperMappingException exception)
        {
            Logger.LogError(exception, exception.Message);
            return StatusCode(422,
                exception.InnerException != null ?
                    exception.InnerException.Message
                    : "Error validando campos"
            );
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, exception.Message);
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }
}