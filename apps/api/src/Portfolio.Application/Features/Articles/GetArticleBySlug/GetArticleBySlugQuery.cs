using MediatR;

namespace Portfolio.Application.Features.Articles.GetArticleBySlug;

public sealed record GetArticleBySlugQuery(string Slug) : IRequest<GetArticleBySlugResponse?>;
