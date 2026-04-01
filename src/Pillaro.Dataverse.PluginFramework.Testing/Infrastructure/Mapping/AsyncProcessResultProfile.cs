using AutoMapper;
using Microsoft.Xrm.Sdk;
using Pillaro.Dataverse.PluginFramework.Testing.Domain.Models;

namespace Pillaro.Dataverse.PluginFramework.Testing.Infrastructure.Mapping;

public class AsyncProcessResultProfile : Profile
{
    public AsyncProcessResultProfile()
    {

        CreateMap<Entity, AsyncProcessResult>()
            .ForMember(des => des.AsyncOperationId, opt => opt.MapFrom(x => x.Id))
            .ForMember(des => des.StatusCode, opt => opt.MapFrom(x => ((OptionSetValue)x["statecode"]).Value))
            .ForMember(des => des.StatusCode, opt => opt.MapFrom(x => ((OptionSetValue)x["statuscode"]).Value))
            .ForMember(des => des.Message, opt => opt.Ignore())
            .ForMember(des => des.CreatedOn, opt => opt.MapFrom(x => (DateTime)x["createdon"]))
            .ForMember(des => des.CompletedOn, opt => opt.MapFrom(x => (DateTime)x["completedon"]))
            .ForMember(des => des.RetryCount, opt => opt.MapFrom(x => (int)x["retrycount"]))
            ;
    }
}